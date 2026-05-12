import type { Edge } from '@vue-flow/core'
import {
  BUTTON_BRANCHING_NODE_TYPE,
  DATA_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GOTO_NODE_TYPE,
  GROUP_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
  isSmartButtonBranchingNodeType,
  normalizeButtonBranchingButtonRows,
  readSmartButtonBranchingButtons,
  type EditorFlowNode,
  type EditorNodeData,
} from '@/components/editor/editorTypes'
import {
  backendNodeTypeFromDataAction,
  createButtonBranchingCompiledNodeIds,
  createButtonPayloadVariable,
  createDerivedGraphId,
  normalizeButtonBranchingCompiledNodeIds,
  BUTTON_BRANCHING_NODE_IDS_PARAM,
} from './editorNodeDefinitions'
import type {
  ButtonBranchingCompiledNodeIds,
  EditorBlock,
  EditorToRuntimeOptions,
  GotoIncomingConnection,
  GroupEditorBlock,
  RuntimeConnection,
  RuntimeNode,
  RuntimeScenario,
  ScenarioPosition,
} from './editorScenario.types'

const DEFAULT_VK_REMOVE_KEYBOARD_TEXT = 'Действие выполнено'

export function editorToRuntimeScenario(options: EditorToRuntimeOptions): RuntimeScenario {
  const expandedGroups = expandGroupNodes(options)
  if (expandedGroups) {
    const runtime = editorToRuntimeScenario({
      ...options,
      nodes: expandedGroups.nodes,
      edges: expandedGroups.edges,
      visualStartNodeId: expandedGroups.visualStartNodeId,
    })

    return {
      ...runtime,
      editorBlocks: [
        ...(runtime.editorBlocks ?? []),
        ...expandedGroups.blocks,
      ],
    }
  }

  const buttonBranchingNodes = new Map<string, ButtonBranchingCompiledNodeIds>()
  const deleteAfterReceiveNodes = new Map<string, string>()
  const gotoTargets = new Map<string, string>()
  const gotoIncomingConnections = new Map<string, GotoIncomingConnection[]>()
  const editorBlocks: EditorBlock[] = []
  const nodes: RuntimeNode[] = []
  const editorNodeIds = new Set(options.nodes.map(node => node.id))

  for (const node of options.nodes) {
    const nodeData = (node.data ?? { type: '', params: {} }) as EditorNodeData
    if (shouldCompileButtonBranchingNode(nodeData)) {
      const compiledNodeIds = getButtonBranchingCompiledNodeIds(node)
      buttonBranchingNodes.set(node.id, compiledNodeIds)
      editorBlocks.push({
        id: node.id,
        type: BUTTON_BRANCHING_NODE_TYPE,
        sourceType: editorBlockSourceType(nodeData.type),
        position: options.snapPosition(node.position),
        ...(nodeData.type === EDIT_MESSAGE_NODE_TYPE && typeof nodeData.params.targetMessageNodeId === 'string'
          ? { targetMessageNodeId: nodeData.params.targetMessageNodeId }
          : {}),
        ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
        nodes: compiledNodeIds,
      })
      nodes.push(...compileButtonBranchingNodes(node, compiledNodeIds, options))
      continue
    }

    if (nodeData.type === GOTO_NODE_TYPE) {
      const targetNodeId = readString(nodeData.params.targetNodeId)
      const validTargetNodeId = targetNodeId && editorNodeIds.has(targetNodeId) ? targetNodeId : null
      if (validTargetNodeId) gotoTargets.set(node.id, validTargetNodeId)
      editorBlocks.push({
        id: node.id,
        type: GOTO_NODE_TYPE,
        position: options.snapPosition(node.position),
        ...(validTargetNodeId ? { targetNodeId: validTargetNodeId } : {}),
      })
      continue
    }

    const backendNodeType = nodeData.type === BUTTON_BRANCHING_NODE_TYPE
      ? MESSAGE_NODE_TYPE
      : nodeData.type === DATA_NODE_TYPE
        ? backendNodeTypeFromDataAction(nodeData.params.action)
        : nodeData.type

    if (nodeData.type === 'receive_message') {
      const compiled = compileReceiveMessageNode(node, backendNodeType, options)
      nodes.push(...compiled.nodes)
      if (compiled.deleteMessageNodeId) {
        deleteAfterReceiveNodes.set(node.id, compiled.deleteMessageNodeId)
      }
      continue
    }

    nodes.push({
      id: node.id,
      type: backendNodeType,
      params: nodeData.type === DATA_NODE_TYPE
        ? options.normalizeDataNodeParams(backendNodeType, nodeData.params)
        : options.normalizeParamsForNode(backendNodeType, nodeData.params),
      position: options.snapPosition(node.position),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
    })
  }

  const connections: RuntimeConnection[] = []
  for (const edge of options.edges) {
    const sourceMacro = buttonBranchingNodes.get(edge.source)
    const targetMacro = buttonBranchingNodes.get(edge.target)
    const gotoTarget = gotoTargets.get(edge.target)
    const deleteAfterReceiveNodeId = deleteAfterReceiveNodes.get(edge.source)
    const branch = sourceMacro
      ? resolveSmartButtonConnectionBranch(edge, options.nodes)
      : edge.sourceHandle

    if (sourceMacro && edge.sourceHandle && branch === null) continue

    if (gotoTarget) {
      const from = sourceMacro?.switch ?? deleteAfterReceiveNodeId ?? edge.source
      const incoming = gotoIncomingConnections.get(edge.target) ?? []
      incoming.push({
        from,
        ...(branch ? { branch } : {}),
      })
      gotoIncomingConnections.set(edge.target, incoming)
      connections.push({
        from,
        to: buttonBranchingNodes.get(gotoTarget)?.sendButtons ?? gotoTarget,
        ...(branch ? { branch } : {}),
      })
      continue
    }

    const savedRoute = options.getSavedEdgeRoute(edge)
    connections.push({
      from: sourceMacro?.switch ?? deleteAfterReceiveNodeId ?? edge.source,
      to: targetMacro?.sendButtons ?? edge.target,
      ...(branch ? { branch } : {}),
      ...(savedRoute.length ? { route: savedRoute, routeVersion: 2 } : {}),
    })
  }

  for (const compiledNodeIds of buttonBranchingNodes.values()) {
    connections.push({ from: compiledNodeIds.sendButtons, to: compiledNodeIds.receiveButtonPress })
    if (compiledNodeIds.deleteMessage) {
      connections.push(
        { from: compiledNodeIds.receiveButtonPress, to: compiledNodeIds.deleteMessage },
        { from: compiledNodeIds.deleteMessage, to: compiledNodeIds.switch },
      )
    } else if (compiledNodeIds.removeKeyboard && compiledNodeIds.removeKeyboardDeleteMessage) {
      connections.push(
        { from: compiledNodeIds.receiveButtonPress, to: compiledNodeIds.removeKeyboard },
        { from: compiledNodeIds.removeKeyboard, to: compiledNodeIds.removeKeyboardDeleteMessage },
        { from: compiledNodeIds.removeKeyboardDeleteMessage, to: compiledNodeIds.switch },
      )
    } else if (compiledNodeIds.removeKeyboard) {
      connections.push(
        { from: compiledNodeIds.receiveButtonPress, to: compiledNodeIds.removeKeyboard },
        { from: compiledNodeIds.removeKeyboard, to: compiledNodeIds.switch },
      )
    } else {
      connections.push({ from: compiledNodeIds.receiveButtonPress, to: compiledNodeIds.switch })
    }
  }

  for (const [receiveNodeId, deleteMessageNodeId] of deleteAfterReceiveNodes) {
    connections.push({ from: receiveNodeId, to: deleteMessageNodeId })
  }

  for (const block of editorBlocks) {
    if (block.type !== GOTO_NODE_TYPE) continue
    const incomingConnections = gotoIncomingConnections.get(block.id)
    if (incomingConnections?.length) block.incomingConnections = incomingConnections
  }

  const startNodeId = buttonBranchingNodes.get(options.visualStartNodeId)?.sendButtons
    ?? gotoTargets.get(options.visualStartNodeId)
    ?? options.visualStartNodeId

  return {
    startNodeId,
    nodes,
    connections,
    ...(editorBlocks.length ? { editorBlocks } : {}),
  }
}

function compileReceiveMessageNode(
  node: EditorFlowNode,
  backendNodeType: string,
  options: EditorToRuntimeOptions,
): { nodes: RuntimeNode[]; deleteMessageNodeId?: string } {
  const deleteAfterReceive = options.capabilities.canDeleteIncomingUserMessage
    && readBoolean(node.data.params.deleteAfterReceive)
  const explicitMessageIdVariable = readString(node.data.params.messageIdVariable)
  const shouldCaptureMessageId = deleteAfterReceive || Boolean(explicitMessageIdVariable)
  const messageIdVariable = explicitMessageIdVariable ?? createReceiveMessageIdVariable(node.id)
  const params: Record<string, unknown> = {
    ...node.data.params,
    ...(shouldCaptureMessageId ? { messageIdVariable } : {}),
  }
  delete params.deleteAfterReceive
  if (!shouldCaptureMessageId) delete params.messageIdVariable

  const nodes: RuntimeNode[] = [
    {
      id: node.id,
      type: backendNodeType,
      params: options.normalizeParamsForNode(backendNodeType, params),
      position: options.snapPosition(node.position),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
    },
  ]

  if (!deleteAfterReceive) return { nodes }

  const deleteMessageNodeId = createReceiveDeleteNodeId(node.id)
  nodes.push({
    id: deleteMessageNodeId,
    type: 'delete_message',
    params: options.normalizeParamsForNode('delete_message', { messageIdVariable }),
    position: options.snapPosition({ x: node.position.x + 216, y: node.position.y }),
  })

  return { nodes, deleteMessageNodeId }
}

function expandGroupNodes(options: EditorToRuntimeOptions): {
  nodes: EditorFlowNode[]
  edges: Edge[]
  visualStartNodeId: string
  blocks: GroupEditorBlock[]
} | null {
  const groupNodes = options.nodes.filter(node => node.data.type === GROUP_NODE_TYPE)
  if (!groupNodes.length) return null

  const groupIds = new Set(groupNodes.map(node => node.id))
  const nodes: EditorFlowNode[] = options.nodes.filter(node => !groupIds.has(node.id))
  const edges: Edge[] = []
  const blocks: GroupEditorBlock[] = []
  const groupEntryById = new Map<string, string>()
  const groupExitById = new Map<string, string>()

  for (const groupNode of groupNodes) {
    const subgraph = readGroupSubgraph(groupNode)
    const boundaryNodeIds = resolveGroupBoundaryNodeIds(
      subgraph,
      readString(groupNode.data.params.entryNodeId),
      readString(groupNode.data.params.exitNodeId),
    )
    const entryNodeId = boundaryNodeIds.entryNodeId
    const exitNodeId = boundaryNodeIds.exitNodeId
    groupEntryById.set(groupNode.id, entryNodeId)
    groupExitById.set(groupNode.id, exitNodeId)

    blocks.push({
      id: groupNode.id,
      type: GROUP_NODE_TYPE,
      title: readString(groupNode.data.params.title) ?? 'Группа',
      position: options.snapPosition(groupNode.position),
      ...(isBoundaryPositions(groupNode.data.params.boundaryPositions)
        ? { boundaryPositions: groupNode.data.params.boundaryPositions }
        : {}),
      entryNodeId,
      exitNodeId,
      subgraph,
    })

    nodes.push(...subgraph.nodes.map(node => ({
      id: node.id,
      type: 'default',
      position: node.position ?? { x: 0, y: 0 },
      data: {
        type: node.type,
        params: node.params ?? {},
        ...(node.ui ? { ui: cloneNodeUi(node.ui) } : {}),
      },
    })))

    edges.push(...subgraph.connections.map((connection, index) => ({
      id: `group-${groupNode.id}-${connection.from}-${connection.branch ?? 'default'}-${connection.to}-${index}`,
      type: 'routed',
      source: connection.from,
      target: connection.to,
      ...(connection.branch ? { sourceHandle: connection.branch } : {}),
      data: connection.route?.length ? { route: connection.route, routeVersion: connection.routeVersion ?? 2 } : {},
    })))
  }

  for (const edge of options.edges) {
    const sourceExit = groupExitById.get(edge.source)
    const targetEntry = groupEntryById.get(edge.target)
    if (sourceExit && targetEntry && edge.source === edge.target) continue

    edges.push({
      ...edge,
      source: sourceExit ?? edge.source,
      target: targetEntry ?? edge.target,
    })
  }

  return {
    nodes: nodes.map(node => redirectGotoTargetFromGroupToEntry(node, groupEntryById)),
    edges,
    visualStartNodeId: groupEntryById.get(options.visualStartNodeId) ?? options.visualStartNodeId,
    blocks,
  }
}

function isBoundaryPositions(value: unknown): value is GroupEditorBlock['boundaryPositions'] {
  if (!isRecord(value)) return false
  return (!('entry' in value) || isPosition(value.entry))
    && (!('exit' in value) || isPosition(value.exit))
}

function isPosition(value: unknown): value is { x: number; y: number } {
  return isRecord(value)
    && typeof value.x === 'number'
    && Number.isFinite(value.x)
    && typeof value.y === 'number'
    && Number.isFinite(value.y)
}

function redirectGotoTargetFromGroupToEntry(
  node: EditorFlowNode,
  groupEntryById: Map<string, string>,
): EditorFlowNode {
  if (node.data.type !== GOTO_NODE_TYPE) return node

  const targetNodeId = readString(node.data.params.targetNodeId)
  const entryNodeId = targetNodeId ? groupEntryById.get(targetNodeId) : undefined
  if (!entryNodeId) return node

  return {
    ...node,
    data: {
      ...node.data,
      params: {
        ...node.data.params,
        targetNodeId: entryNodeId,
      },
    },
  }
}

function readGroupSubgraph(node: EditorFlowNode): {
  startNodeId: string
  nodes: RuntimeNode[]
  connections: RuntimeConnection[]
} {
  const raw = node.data.params.subgraph
  if (!isRecord(raw) || !Array.isArray(raw.nodes) || !Array.isArray(raw.connections)) {
    return { startNodeId: '', nodes: [], connections: [] }
  }

  const nodes = raw.nodes
    .filter((item): item is RuntimeNode => isRecord(item) && typeof item.id === 'string' && typeof item.type === 'string')
    .map(item => ({
      id: item.id,
      type: item.type,
      params: isRecord(item.params) ? item.params : {},
        ...(isRecord(item.position) && typeof item.position.x === 'number' && typeof item.position.y === 'number'
          ? { position: { x: item.position.x, y: item.position.y } }
          : {}),
        ...(cloneNodeUi(item.ui) ? { ui: cloneNodeUi(item.ui) } : {}),
      }))

  const nodeIds = new Set(nodes.map(item => item.id))
  const connections = raw.connections
    .filter((item): item is RuntimeConnection =>
      isRecord(item)
      && typeof item.from === 'string'
      && typeof item.to === 'string'
      && nodeIds.has(item.from)
      && nodeIds.has(item.to),
    )
    .map(item => ({
      from: item.from,
      to: item.to,
      ...(typeof item.branch === 'string' && item.branch ? { branch: item.branch } : {}),
      ...(Array.isArray(item.route) ? { route: item.route } : {}),
      ...(typeof item.routeVersion === 'number' ? { routeVersion: item.routeVersion } : {}),
    }))

  const startNodeId = readString(raw.startNodeId) ?? nodes[0]?.id ?? ''
  return { startNodeId, nodes, connections }
}

function resolveGroupBoundaryNodeIds(
  subgraph: {
    startNodeId: string
    nodes: RuntimeNode[]
    connections: RuntimeConnection[]
  },
  entryCandidateId: string | null,
  exitCandidateId: string | null,
): { entryNodeId: string; exitNodeId: string } {
  const nodeIds = new Set(subgraph.nodes.map(node => node.id))
  const fallbackEntryNodeId = nodeIds.has(subgraph.startNodeId)
    ? subgraph.startNodeId
    : inferGroupEntryNodeId(subgraph)
  const entryNodeId = entryCandidateId && nodeIds.has(entryCandidateId)
    ? entryCandidateId
    : fallbackEntryNodeId
  const exitNodeId = exitCandidateId && nodeIds.has(exitCandidateId)
    ? exitCandidateId
    : inferGroupExitNodeId(subgraph)

  return { entryNodeId, exitNodeId }
}

function inferGroupEntryNodeId(subgraph: {
  nodes: RuntimeNode[]
  connections: RuntimeConnection[]
}): string {
  const targetIds = new Set(subgraph.connections.map(connection => connection.to))
  return subgraph.nodes.find(node => !targetIds.has(node.id))?.id
    ?? subgraph.nodes[0]?.id
    ?? ''
}

function inferGroupExitNodeId(subgraph: {
  nodes: RuntimeNode[]
  connections: RuntimeConnection[]
}): string {
  const sourceIds = new Set(subgraph.connections.map(connection => connection.from))
  return [...subgraph.nodes].reverse().find(node => !sourceIds.has(node.id))?.id
    ?? subgraph.nodes.at(-1)?.id
    ?? ''
}

function compileButtonBranchingNodes(
  node: EditorFlowNode,
  compiledNodeIds: ButtonBranchingCompiledNodeIds,
  options: EditorToRuntimeOptions,
): RuntimeNode[] {
  const params = node.data.params
  const buttonRows = normalizeButtonBranchingButtonRows(params.buttons)
  const buttons = readSmartButtonBranchingButtons(node.data.type, params)
  const buttonPayloadVariable = readString(params.buttonPayloadVariable) ?? createButtonPayloadVariable(node.id)
  const basePosition = options.snapPosition(node.position)
  const isEditNode = node.data.type === EDIT_MESSAGE_NODE_TYPE
  const isVkKeyboardNode = node.data.type === VK_SEND_KEYBOARD_NODE_TYPE
  const shouldHideAfterButtonPress = readBoolean(params.deleteAfterButtonPress)
  const removeKeyboardNodeId = shouldHideAfterButtonPress && isVkKeyboardNode
    ? compiledNodeIds.removeKeyboard
    : undefined
  const removeKeyboardDeleteMessageNodeId = removeKeyboardNodeId
    ? compiledNodeIds.removeKeyboardDeleteMessage
    : undefined
  const deleteMessageNodeId = shouldHideAfterButtonPress && !removeKeyboardNodeId
    ? compiledNodeIds.deleteMessage
    : undefined
  const primaryNodeType = primaryRuntimeNodeType(node.data.type)
  const primaryParams = primaryRuntimeParamsForButtonBranchingNode(node, buttonRows, options)

  const nodes: RuntimeNode[] = [
    {
      id: compiledNodeIds.sendButtons,
      type: primaryNodeType,
      params: options.normalizeParamsForNode(primaryNodeType, primaryParams),
      position: basePosition,
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
    },
    {
      id: compiledNodeIds.receiveButtonPress,
      type: 'receive_button_press',
      params: options.normalizeParamsForNode('receive_button_press', {
        buttonPayloadVariable,
        expectedPayloads: buttons.map(button => button.payload),
      }),
      position: options.snapPosition({ x: basePosition.x + 216, y: basePosition.y }),
    },
  ]

  if (deleteMessageNodeId) {
    nodes.push({
      id: deleteMessageNodeId,
      type: 'delete_message',
      params: options.normalizeParamsForNode('delete_message', {
        messageIdVariable: isEditNode
          ? options.resolveEditMessageIdVariable(node)
          : isVkKeyboardNode || node.data.type === MESSAGE_NODE_TYPE || node.data.type === BUTTON_BRANCHING_NODE_TYPE
            ? params.messageIdVariable
            : undefined,
      }),
      position: options.snapPosition({ x: basePosition.x + 432, y: basePosition.y }),
    })
  }

  if (removeKeyboardNodeId) {
    const removeKeyboardMessageIdVariable = createRemoveKeyboardMessageIdVariable(node.id)
    nodes.push({
      id: removeKeyboardNodeId,
      type: 'vk_remove_keyboard',
      params: options.normalizeParamsForNode('vk_remove_keyboard', {
        text: readString(params.removeKeyboardText) ?? DEFAULT_VK_REMOVE_KEYBOARD_TEXT,
        messageIdVariable: removeKeyboardMessageIdVariable,
      }),
      position: options.snapPosition({ x: basePosition.x + 432, y: basePosition.y }),
    })

    if (removeKeyboardDeleteMessageNodeId) {
      nodes.push({
        id: removeKeyboardDeleteMessageNodeId,
        type: 'delete_message',
        params: options.normalizeParamsForNode('delete_message', {
          messageIdVariable: removeKeyboardMessageIdVariable,
        }),
        position: options.snapPosition({ x: basePosition.x + 648, y: basePosition.y }),
      })
    }
  }

  nodes.push({
      id: compiledNodeIds.switch,
      type: 'switch',
      params: options.normalizeParamsForNode('switch', {
        variable: buttonPayloadVariable,
        cases: buttons.map(button => ({
          value: button.payload,
          branchKey: button.payload,
        })),
      }),
      position: options.snapPosition({ x: basePosition.x + (removeKeyboardDeleteMessageNodeId ? 864 : deleteMessageNodeId || removeKeyboardNodeId ? 648 : 432), y: basePosition.y }),
    })

  if (!deleteMessageNodeId) {
    delete compiledNodeIds.deleteMessage
  }
  if (!removeKeyboardNodeId) {
    delete compiledNodeIds.removeKeyboard
    delete compiledNodeIds.removeKeyboardDeleteMessage
  }
  if (removeKeyboardNodeId && !removeKeyboardDeleteMessageNodeId) {
    delete compiledNodeIds.removeKeyboardDeleteMessage
  }

  return nodes
}

function primaryRuntimeNodeType(nodeType: string): string {
  if (nodeType === EDIT_MESSAGE_NODE_TYPE) return EDIT_MESSAGE_NODE_TYPE
  if (nodeType === VK_SEND_KEYBOARD_NODE_TYPE) return VK_SEND_KEYBOARD_NODE_TYPE
  if (nodeType === VK_SEND_CAROUSEL_NODE_TYPE) return VK_SEND_CAROUSEL_NODE_TYPE
  return 'send_buttons'
}

function editorBlockSourceType(
  nodeType: string,
): typeof MESSAGE_NODE_TYPE | typeof EDIT_MESSAGE_NODE_TYPE | typeof BUTTON_BRANCHING_NODE_TYPE | typeof VK_SEND_KEYBOARD_NODE_TYPE | typeof VK_SEND_CAROUSEL_NODE_TYPE {
  if (nodeType === EDIT_MESSAGE_NODE_TYPE) return EDIT_MESSAGE_NODE_TYPE
  if (nodeType === VK_SEND_KEYBOARD_NODE_TYPE) return VK_SEND_KEYBOARD_NODE_TYPE
  if (nodeType === VK_SEND_CAROUSEL_NODE_TYPE) return VK_SEND_CAROUSEL_NODE_TYPE
  if (nodeType === BUTTON_BRANCHING_NODE_TYPE) return BUTTON_BRANCHING_NODE_TYPE
  return MESSAGE_NODE_TYPE
}

function primaryRuntimeParamsForButtonBranchingNode(
  node: EditorFlowNode,
  buttonRows: ReturnType<typeof normalizeButtonBranchingButtonRows>,
  options: EditorToRuntimeOptions,
): Record<string, unknown> {
  const params = node.data.params

  if (node.data.type === EDIT_MESSAGE_NODE_TYPE) {
    return {
      newText: params.newText,
      messageIdVariable: options.resolveEditMessageIdVariable(node),
      buttons: buttonRows,
    }
  }

  if (node.data.type === VK_SEND_KEYBOARD_NODE_TYPE) {
    return pickParams(params, ['text', 'buttons', 'oneTime', 'messageIdVariable'])
  }

  if (node.data.type === VK_SEND_CAROUSEL_NODE_TYPE) {
    return pickParams(params, ['text', 'cards', 'messageIdVariable'])
  }

  return {
    text: params.text,
    messageIdVariable: params.messageIdVariable,
    buttons: buttonRows,
  }
}

function cloneNodeUi(value: unknown): { textHeight?: number } | undefined {
  if (!isRecord(value)) return undefined

  const ui: { textHeight?: number } = {}
  if (typeof value.textHeight === 'number' && Number.isFinite(value.textHeight)) {
    ui.textHeight = Math.max(24, Math.round(value.textHeight / 12) * 12)
  }

  return Object.keys(ui).length ? ui : undefined
}

function shouldCompileButtonBranchingNode(nodeData: EditorNodeData): boolean {
  return isSmartButtonBranchingNodeType(nodeData.type)
    && readSmartButtonBranchingButtons(nodeData.type, nodeData.params).length > 0
}

function getButtonBranchingCompiledNodeIds(node: EditorFlowNode): ButtonBranchingCompiledNodeIds {
  const existing = normalizeButtonBranchingCompiledNodeIds(node.data.params[BUTTON_BRANCHING_NODE_IDS_PARAM])
  const compiled = existing ?? createButtonBranchingCompiledNodeIds(node.id)
  const shouldHideAfterButtonPress = readBoolean(node.data.params.deleteAfterButtonPress)
  const isVkKeyboardNode = node.data.type === VK_SEND_KEYBOARD_NODE_TYPE

  if (shouldHideAfterButtonPress && isVkKeyboardNode && !compiled.removeKeyboard) {
    compiled.removeKeyboard = createButtonBranchingCompiledNodeIds(node.id).removeKeyboard
  }

  if (shouldHideAfterButtonPress && isVkKeyboardNode && !compiled.removeKeyboardDeleteMessage) {
    compiled.removeKeyboardDeleteMessage = createButtonBranchingCompiledNodeIds(node.id).removeKeyboardDeleteMessage
  }

  if (shouldHideAfterButtonPress && !isVkKeyboardNode && node.data.type !== VK_SEND_CAROUSEL_NODE_TYPE && !compiled.deleteMessage) {
    compiled.deleteMessage = createButtonBranchingCompiledNodeIds(node.id).deleteMessage
  }

  if (isVkKeyboardNode || node.data.type === VK_SEND_CAROUSEL_NODE_TYPE) delete compiled.deleteMessage
  if (!isVkKeyboardNode) {
    delete compiled.removeKeyboard
    delete compiled.removeKeyboardDeleteMessage
  }

  return compiled
}

function resolveSmartButtonConnectionBranch(edge: Edge, nodes: EditorFlowNode[]): string | null | undefined {
  const sourceHandle = typeof edge.sourceHandle === 'string' ? edge.sourceHandle : ''
  if (!sourceHandle) return undefined

  const sourceNode = nodes.find(node => node.id === edge.source)
  if (!sourceNode || !isSmartButtonBranchingNodeType(sourceNode.data.type)) return sourceHandle

  const buttons = readSmartButtonBranchingButtons(sourceNode.data.type, sourceNode.data.params)
  if (buttons.some(button => button.payload === sourceHandle)) return sourceHandle

  const legacyIndex = readLegacyButtonPayloadIndex(sourceHandle)
  const remappedByIndex = legacyIndex != null ? buttons[legacyIndex]?.payload : undefined
  if (remappedByIndex) return remappedByIndex

  return null
}

function readLegacyButtonPayloadIndex(value: string): number | null {
  const match = value.match(/^(?:button|кнопка)_(\d+)$/iu)
  if (!match) return null

  const index = Number(match[1])
  return Number.isInteger(index) && index > 0 ? index - 1 : null
}

function createReceiveMessageIdVariable(nodeId: string): string {
  return `user_message_${nodeId.slice(0, 8)}_id`
}

function createReceiveDeleteNodeId(nodeId: string): string {
  return createDerivedGraphId(nodeId, 'delete_received_message')
}

function createRemoveKeyboardMessageIdVariable(nodeId: string): string {
  return `vk_remove_keyboard_${nodeId.replace(/-/g, '').slice(0, 32)}_message_id`
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function readBoolean(value: unknown): boolean {
  return value === true
}

function pickParams(params: Record<string, unknown>, keys: string[]): Record<string, unknown> {
  const result: Record<string, unknown> = {}
  for (const key of keys) {
    if (params[key] !== undefined) result[key] = params[key]
  }
  return result
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

export function stripInternalEditorParams(params: Record<string, unknown>): Record<string, unknown> {
  const next = { ...params }
  delete next[BUTTON_BRANCHING_NODE_IDS_PARAM]
  return next
}
