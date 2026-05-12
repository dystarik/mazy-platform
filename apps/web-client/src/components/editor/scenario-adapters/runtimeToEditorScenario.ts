import {
  BUTTON_BRANCHING_NODE_TYPE,
  DATA_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GOTO_NODE_TYPE,
  GROUP_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
} from '@/components/editor/editorTypes'
import {
  BUTTON_BRANCHING_NODE_IDS_PARAM,
  dataActionFromBackendNodeType,
  isRuntimeDataNodeType,
} from './editorNodeDefinitions'
import type {
  ButtonBranchingEditorBlock,
  ButtonBranchingCompiledNodeIds,
  EditorScenario,
  GotoEditorBlock,
  GroupEditorBlock,
  RuntimeConnection,
  RuntimeNode,
  RuntimeScenario,
} from './editorScenario.types'
import type { EditorPlatformCapabilities } from './editorPlatformCapabilities'

const DEFAULT_VK_REMOVE_KEYBOARD_TEXT = 'Действие выполнено'

export interface RuntimeToEditorScenarioOptions {
  capabilities: EditorPlatformCapabilities
}

export function runtimeToEditorScenario(data: RuntimeScenario, options?: RuntimeToEditorScenarioOptions): EditorScenario {
  const nodesById = new Map(data.nodes.map(node => [node.id, node]))
  const deleteAfterReceiveNodes = options?.capabilities.canDeleteIncomingUserMessage === true
    ? inferDeleteAfterReceiveNodes(data.connections, nodesById)
    : new Map<string, string>()
  const hiddenDeleteAfterReceiveNodeIds = new Set(deleteAfterReceiveNodes.values())
  const editorConnections = collapseDeleteAfterReceiveConnections(data.connections, deleteAfterReceiveNodes)
  const buttonBlocks = (data.editorBlocks ?? [])
    .filter((block): block is ButtonBranchingEditorBlock => block.type === BUTTON_BRANCHING_NODE_TYPE)
  const gotoBlocks = (data.editorBlocks ?? [])
    .filter((block): block is GotoEditorBlock => block.type === GOTO_NODE_TYPE)
  const groupBlocks = (data.editorBlocks ?? [])
    .filter((block): block is GroupEditorBlock => block.type === GROUP_NODE_TYPE)

  if (!buttonBlocks.length && !gotoBlocks.length) {
    return restoreGroupBlocks({
      startNodeId: data.startNodeId,
      nodes: data.nodes
        .filter(node => !hiddenDeleteAfterReceiveNodeIds.has(node.id))
        .map(node => toEditorNode(node, deleteAfterReceiveNodes)),
      connections: editorConnections,
    }, groupBlocks)
  }

  const visualNodesBySendId = new Map<string, RuntimeNode>()
  const childToBlock = new Map<string, { blockId: string; nodes: ButtonBranchingCompiledNodeIds }>()
  const hiddenNodeIds = new Set<string>()

  for (const block of buttonBlocks) {
    const primaryNode = nodesById.get(block.nodes.sendButtons)
    const receiveNode = nodesById.get(block.nodes.receiveButtonPress)
    const switchNode = nodesById.get(block.nodes.switch)
    if (!primaryNode || !receiveNode || !switchNode) continue
    const compiledNodes = {
      ...block.nodes,
      ...(!block.nodes.deleteMessage
        ? inferDeleteAfterPressNodeId(data.connections, nodesById, block.nodes)
        : {}),
      ...(!block.nodes.removeKeyboard
        ? inferRemoveKeyboardAfterPressNodeIds(data.connections, nodesById, block.nodes)
        : {}),
    }
    if (compiledNodes.removeKeyboard && !compiledNodes.removeKeyboardDeleteMessage) {
      Object.assign(
        compiledNodes,
        inferRemoveKeyboardDeleteMessageAfterPressNodeId(data.connections, nodesById, compiledNodes),
      )
    }

    hiddenNodeIds.add(primaryNode.id)
    hiddenNodeIds.add(receiveNode.id)
    hiddenNodeIds.add(switchNode.id)
    if (compiledNodes.deleteMessage) hiddenNodeIds.add(compiledNodes.deleteMessage)
    if (compiledNodes.removeKeyboard) hiddenNodeIds.add(compiledNodes.removeKeyboard)
    if (compiledNodes.removeKeyboardDeleteMessage) hiddenNodeIds.add(compiledNodes.removeKeyboardDeleteMessage)
    childToBlock.set(primaryNode.id, { blockId: block.id, nodes: compiledNodes })
    childToBlock.set(receiveNode.id, { blockId: block.id, nodes: compiledNodes })
    childToBlock.set(switchNode.id, { blockId: block.id, nodes: compiledNodes })
    if (compiledNodes.deleteMessage) {
      childToBlock.set(compiledNodes.deleteMessage, { blockId: block.id, nodes: compiledNodes })
    }
    if (compiledNodes.removeKeyboard) {
      childToBlock.set(compiledNodes.removeKeyboard, { blockId: block.id, nodes: compiledNodes })
    }
    if (compiledNodes.removeKeyboardDeleteMessage) {
      childToBlock.set(compiledNodes.removeKeyboardDeleteMessage, { blockId: block.id, nodes: compiledNodes })
    }

    visualNodesBySendId.set(primaryNode.id, {
      id: block.id,
      type: resolveEditorBlockSourceType(block, primaryNode),
      params: createButtonBranchingParamsFromCompiledNodes(primaryNode, receiveNode, nodesById.get(compiledNodes.removeKeyboard ?? ''), {
        ...block,
        nodes: compiledNodes,
      }),
      position: block.position ?? primaryNode.position,
      ui: block.ui ?? primaryNode.ui,
    })
  }

  const nodes: RuntimeNode[] = []
  for (const node of data.nodes) {
    const visualNode = visualNodesBySendId.get(node.id)
    if (visualNode) {
      nodes.push(visualNode)
      continue
    }

    if (!hiddenNodeIds.has(node.id) && !hiddenDeleteAfterReceiveNodeIds.has(node.id)) {
      nodes.push(toEditorNode(node, deleteAfterReceiveNodes))
    }
  }

  nodes.push(...gotoBlocks.map(block => ({
    id: block.id,
    type: GOTO_NODE_TYPE,
      params: { targetNodeId: block.targetNodeId ?? '' },
      position: block.position,
    })))

  const connections = restoreGotoBlockConnections(
    collapseEditorBlockConnections(editorConnections, childToBlock),
    gotoBlocks,
    groupBlocks,
    childToBlock,
  )
  const startBlock = childToBlock.get(data.startNodeId)

  return restoreGroupBlocks({
    startNodeId: startBlock?.blockId ?? data.startNodeId,
    nodes,
    connections,
  }, groupBlocks)
}

function restoreGroupBlocks(data: EditorScenario, groupBlocks: GroupEditorBlock[]): EditorScenario {
  if (!groupBlocks.length) return data

  let nodes = [...data.nodes]
  let connections = [...data.connections]
  let startNodeId = data.startNodeId

  for (const block of groupBlocks) {
    const childIds = new Set(block.subgraph.nodes.map(node => node.id))
    const hasAnyChild = nodes.some(node => childIds.has(node.id))
    if (!hasAnyChild) continue

    nodes = [
      ...nodes
        .filter(node => !childIds.has(node.id))
        .map(node => redirectGotoTargetFromGroupChild(node, childIds, block.id)),
      {
        id: block.id,
        type: GROUP_NODE_TYPE,
        params: {
          title: block.title,
          nodeCount: block.subgraph.nodes.length,
          ...(block.boundaryPositions ? { boundaryPositions: block.boundaryPositions } : {}),
          entryNodeId: block.entryNodeId,
          exitNodeId: block.exitNodeId,
          subgraph: block.subgraph,
        },
        position: block.position,
      },
    ]

    const nextConnections: RuntimeConnection[] = []
    const seen = new Set<string>()

    for (const connection of connections) {
      const fromInside = childIds.has(connection.from)
      const toInside = childIds.has(connection.to)
      if (fromInside && toInside) continue

      const nextConnection: RuntimeConnection = {
        ...connection,
        from: fromInside ? block.id : connection.from,
        to: toInside ? block.id : connection.to,
      }
      const key = `${nextConnection.from}:${nextConnection.to}:${nextConnection.branch ?? ''}`
      if (seen.has(key)) continue
      seen.add(key)
      nextConnections.push(nextConnection)
    }

    connections = nextConnections
    if (childIds.has(startNodeId)) startNodeId = block.id
  }

  return { startNodeId, nodes, connections }
}

function redirectGotoTargetFromGroupChild(
  node: RuntimeNode,
  childIds: Set<string>,
  groupId: string,
): RuntimeNode {
  if (node.type !== GOTO_NODE_TYPE) return node

  const targetNodeId = stringValue(node.params?.targetNodeId)
  if (!targetNodeId || !childIds.has(targetNodeId)) return node

  return {
    ...node,
    params: {
      ...(node.params ?? {}),
      targetNodeId: groupId,
    },
  }
}

function toEditorNode(node: RuntimeNode, deleteAfterReceiveNodes?: Map<string, string>): RuntimeNode {
  if (node.type === 'receive_message' && deleteAfterReceiveNodes?.has(node.id)) {
    return {
      ...node,
      params: {
        ...(node.params ?? {}),
        deleteAfterReceive: true,
      },
    }
  }

  if (isRuntimeDataNodeType(node.type)) {
    return {
      ...node,
      type: DATA_NODE_TYPE,
      params: {
        ...(node.params ?? {}),
        action: dataActionFromBackendNodeType(node.type),
      },
    }
  }

  return node
}

function createButtonBranchingParamsFromCompiledNodes(
  primaryNode: RuntimeNode,
  receiveNode: RuntimeNode,
  removeKeyboardNode: RuntimeNode | undefined,
  block: ButtonBranchingEditorBlock,
): Record<string, unknown> {
  const sourceType = resolveEditorBlockSourceType(block, primaryNode)
  const params: Record<string, unknown> = {
    messageIdVariable: primaryNode.params.messageIdVariable,
    buttonPayloadVariable: receiveNode.params.buttonPayloadVariable ?? 'button_payload',
    [BUTTON_BRANCHING_NODE_IDS_PARAM]: block.nodes,
  }
  if (sourceType !== VK_SEND_CAROUSEL_NODE_TYPE && block.nodes.deleteMessage) {
    params.deleteAfterButtonPress = true
  }
  if (sourceType === VK_SEND_KEYBOARD_NODE_TYPE && block.nodes.removeKeyboard) {
    params.deleteAfterButtonPress = true
    params.removeKeyboardText = stringValue(removeKeyboardNode?.params.text) || DEFAULT_VK_REMOVE_KEYBOARD_TEXT
  }

  if (sourceType === EDIT_MESSAGE_NODE_TYPE) {
    params.buttons = normalizeButtonParams(primaryNode.params.buttons)
    params.newText = primaryNode.params.newText
    if (block.targetMessageNodeId) params.targetMessageNodeId = block.targetMessageNodeId
  } else if (sourceType === VK_SEND_KEYBOARD_NODE_TYPE) {
    params.text = primaryNode.params.text
    params.buttons = normalizeVkKeyboardButtonRows(primaryNode.params.buttons)
    params.oneTime = primaryNode.params.oneTime === true
  } else if (sourceType === VK_SEND_CAROUSEL_NODE_TYPE) {
    params.text = primaryNode.params.text
    params.cards = normalizeVkCarouselCards(primaryNode.params.cards)
  } else {
    params.buttons = normalizeButtonParams(primaryNode.params.buttons)
    params.text = primaryNode.params.text
  }

  return params
}

function resolveEditorBlockSourceType(
  block: ButtonBranchingEditorBlock,
  primaryNode: RuntimeNode,
): typeof MESSAGE_NODE_TYPE | typeof EDIT_MESSAGE_NODE_TYPE | typeof VK_SEND_KEYBOARD_NODE_TYPE | typeof VK_SEND_CAROUSEL_NODE_TYPE {
  if (block.sourceType === EDIT_MESSAGE_NODE_TYPE || primaryNode.type === EDIT_MESSAGE_NODE_TYPE) {
    return EDIT_MESSAGE_NODE_TYPE
  }

  if (block.sourceType === VK_SEND_KEYBOARD_NODE_TYPE || primaryNode.type === VK_SEND_KEYBOARD_NODE_TYPE) {
    return VK_SEND_KEYBOARD_NODE_TYPE
  }

  if (block.sourceType === VK_SEND_CAROUSEL_NODE_TYPE || primaryNode.type === VK_SEND_CAROUSEL_NODE_TYPE) {
    return VK_SEND_CAROUSEL_NODE_TYPE
  }

  return MESSAGE_NODE_TYPE
}

function collapseEditorBlockConnections(
  connections: RuntimeConnection[],
  childToBlock: Map<string, { blockId: string; nodes: ButtonBranchingCompiledNodeIds }>,
): RuntimeConnection[] {
  const result: RuntimeConnection[] = []

  for (const connection of connections) {
    const sourceBlock = childToBlock.get(connection.from)
    const targetBlock = childToBlock.get(connection.to)
    if (sourceBlock && targetBlock && sourceBlock.blockId === targetBlock.blockId) continue

    result.push({
      ...connection,
      from: sourceBlock?.blockId ?? connection.from,
      to: targetBlock?.blockId ?? connection.to,
    })
  }

  return result
}

function restoreGotoBlockConnections(
  connections: RuntimeConnection[],
  gotoBlocks: GotoEditorBlock[],
  groupBlocks: GroupEditorBlock[],
  childToBlock: Map<string, { blockId: string; nodes: ButtonBranchingCompiledNodeIds }>,
): RuntimeConnection[] {
  if (!gotoBlocks.length) return connections

  const result = [...connections]
  for (const block of gotoBlocks) {
    if (!block.targetNodeId) continue
    const targetIds = getGotoRuntimeTargetCandidates(block.targetNodeId, groupBlocks)

    for (const incoming of block.incomingConnections ?? []) {
      const incomingFrom = childToBlock.get(incoming.from)?.blockId ?? incoming.from
      const index = result.findIndex(connection =>
        connection.from === incomingFrom
        && targetIds.has(connection.to)
        && (connection.branch ?? '') === (incoming.branch ?? ''),
      )
      if (index < 0) continue

      const [connection] = result.splice(index, 1)
      if (!connection) continue

      result.push({
        ...connection,
        to: block.id,
        route: undefined,
        routeVersion: undefined,
      })
    }
  }

  return result
}

function getGotoRuntimeTargetCandidates(targetNodeId: string, groupBlocks: GroupEditorBlock[]): Set<string> {
  const result = new Set([targetNodeId])
  const groupBlock = groupBlocks.find(block => block.id === targetNodeId)
  if (groupBlock?.entryNodeId) result.add(groupBlock.entryNodeId)
  return result
}

function normalizeButtonParams(value: unknown): Array<Array<{ label: string; payload: string; style?: string }>> {
  if (!Array.isArray(value)) return []

  const rawRows = value.every(item => Array.isArray(item)) ? value : value.map(item => [item])
  let index = 0

  return rawRows
    .map(row => Array.isArray(row) ? row : [])
    .map(row => row
      .filter((item): item is Record<string, unknown> => isRecord(item))
      .map((item) => {
      const label = stringValue(item.label) || stringValue(item.payload) || `Кнопка ${index + 1}`
      const payload = stringValue(item.payload) || createButtonPayload(label, index)
      index += 1
      return {
        label,
        payload,
        ...(isButtonStyle(item.style) ? { style: item.style } : {}),
      }
    }))
    .filter(row => row.length > 0)
}

function normalizeVkKeyboardButtonRows(value: unknown): Array<Array<{ label: string; payload: string; color?: string }>> {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  let index = 0
  const rawRows = value.every(item => Array.isArray(item)) ? value : value.map(item => [item])
  return rawRows
    .map(row => Array.isArray(row) ? row : [])
    .map(row => row
      .filter((item): item is Record<string, unknown> => isRecord(item))
      .map((item) => {
        const payload = stringValue(item.payload)
        const label = stringValue(item.label) || payload
        const normalized = {
          label,
          payload: payload
            ? reserveUniquePayload(payload, usedPayloads)
            : createUniqueStableCarouselPayload(label, index, usedPayloads),
          ...(isVkKeyboardColor(item.color) ? { color: item.color } : {}),
        }
        index += 1
        return normalized
      })
      .filter(button => button.label && button.payload))
    .filter(row => row.length > 0)
}

function normalizeVkCarouselCards(value: unknown): Array<{
  title: string
  description: string
  photoId?: string
  buttons?: Array<{ label: string; payload?: string; style?: string; link?: string }>
}> {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  return value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map((item) => {
      const buttons = normalizeVkCarouselButtons(item.buttons, usedPayloads)
      return {
        title: stringValue(item.title),
        description: stringValue(item.description),
        ...(stringValue(item.photoId) ? { photoId: stringValue(item.photoId) } : {}),
        ...(buttons.length ? { buttons } : {}),
      }
    })
}

function normalizeVkCarouselButtons(
  value: unknown,
  usedPayloads: Set<string>,
): Array<{ label: string; payload?: string; style?: string; link?: string }> {
  if (!Array.isArray(value)) return []

  return value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map((item, index) => {
      const payload = stringValue(item.payload)
      const link = stringValue(item.link)
      const label = stringValue(item.label) || payload || link
      return {
        label,
        ...(!link
          ? {
              payload: payload
                ? reserveUniquePayload(payload, usedPayloads)
                : createUniqueStableCarouselPayload(label || link, index, usedPayloads),
            }
          : payload ? { payload } : {}),
        ...(isButtonStyle(item.style) ? { style: item.style } : {}),
        ...(link ? { link } : {}),
      }
    })
    .filter(button => Boolean(button.label) && (Boolean(button.link) || Boolean(button.payload)))
}

function reserveUniquePayload(payload: string, usedPayloads: Set<string>): string {
  let candidate = payload
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${payload}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function createUniqueStableCarouselPayload(label: string, index: number, usedPayloads: Set<string>): string {
  const base = createButtonPayload(label, index)
  let candidate = base
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${base}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
}

function isButtonStyle(value: unknown): value is string {
  return ['primary', 'secondary', 'success', 'danger'].includes(stringValue(value))
}

function isVkKeyboardColor(value: unknown): value is string {
  return ['primary', 'secondary', 'negative', 'positive'].includes(stringValue(value))
}

function createButtonPayload(label: string, index: number): string {
  return label.trim()
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^\p{L}\p{N}_-]+/gu, '_')
    .replace(/^_+|_+$/g, '')
    || `button_${index + 1}`
}

function inferDeleteAfterPressNodeId(
  connections: RuntimeConnection[],
  nodesById: Map<string, RuntimeNode>,
  blockNodes: ButtonBranchingCompiledNodeIds,
): Pick<ButtonBranchingCompiledNodeIds, 'deleteMessage'> | Record<string, never> {
  const afterReceive = connections
    .filter(connection => connection.from === blockNodes.receiveButtonPress)
    .map(connection => connection.to)

  for (const candidateId of afterReceive) {
    const candidate = nodesById.get(candidateId)
    if (candidate?.type !== 'delete_message') continue

    const reachesSwitch = connections.some(connection =>
      connection.from === candidateId
      && connection.to === blockNodes.switch,
    )
    if (reachesSwitch) return { deleteMessage: candidateId }
  }

  return {}
}

function inferRemoveKeyboardAfterPressNodeIds(
  connections: RuntimeConnection[],
  nodesById: Map<string, RuntimeNode>,
  blockNodes: ButtonBranchingCompiledNodeIds,
): Pick<ButtonBranchingCompiledNodeIds, 'removeKeyboard' | 'removeKeyboardDeleteMessage'> | Record<string, never> {
  const afterReceive = connections
    .filter(connection => connection.from === blockNodes.receiveButtonPress)
    .map(connection => connection.to)

  for (const candidateId of afterReceive) {
    const candidate = nodesById.get(candidateId)
    if (candidate?.type !== 'vk_remove_keyboard') continue

    const reachesSwitch = connections.some(connection =>
      connection.from === candidateId
      && connection.to === blockNodes.switch,
    )
    if (reachesSwitch) return { removeKeyboard: candidateId }

    const removeKeyboardDeleteMessage = inferRemoveKeyboardDeleteMessageAfterPressNodeId(
      connections,
      nodesById,
      { ...blockNodes, removeKeyboard: candidateId },
    )
    if (removeKeyboardDeleteMessage.removeKeyboardDeleteMessage) {
      return { removeKeyboard: candidateId, ...removeKeyboardDeleteMessage }
    }
  }

  return {}
}

function inferRemoveKeyboardDeleteMessageAfterPressNodeId(
  connections: RuntimeConnection[],
  nodesById: Map<string, RuntimeNode>,
  blockNodes: ButtonBranchingCompiledNodeIds,
): Pick<ButtonBranchingCompiledNodeIds, 'removeKeyboardDeleteMessage'> | Record<string, never> {
  if (!blockNodes.removeKeyboard) return {}

  const afterRemoveKeyboard = connections
    .filter(connection => connection.from === blockNodes.removeKeyboard)
    .map(connection => connection.to)

  for (const candidateId of afterRemoveKeyboard) {
    const candidate = nodesById.get(candidateId)
    if (candidate?.type !== 'delete_message') continue

    const reachesSwitch = connections.some(connection =>
      connection.from === candidateId
      && connection.to === blockNodes.switch,
    )
    if (reachesSwitch) return { removeKeyboardDeleteMessage: candidateId }
  }

  return {}
}

function inferDeleteAfterReceiveNodes(
  connections: RuntimeConnection[],
  nodesById: Map<string, RuntimeNode>,
): Map<string, string> {
  const result = new Map<string, string>()

  for (const connection of connections) {
    const receiveNode = nodesById.get(connection.from)
    const deleteNode = nodesById.get(connection.to)
    if (receiveNode?.type !== 'receive_message' || deleteNode?.type !== 'delete_message') continue

    const receiveMessageIdVariable = stringValue(receiveNode.params?.messageIdVariable)
    const deleteMessageIdVariable = stringValue(deleteNode.params?.messageIdVariable)
    if (!receiveMessageIdVariable || receiveMessageIdVariable !== deleteMessageIdVariable) continue

    result.set(receiveNode.id, deleteNode.id)
  }

  return result
}

function collapseDeleteAfterReceiveConnections(
  connections: RuntimeConnection[],
  deleteAfterReceiveNodes: Map<string, string>,
): RuntimeConnection[] {
  if (!deleteAfterReceiveNodes.size) return connections

  const receiveByDeleteId = new Map(
    [...deleteAfterReceiveNodes.entries()].map(([receiveId, deleteId]) => [deleteId, receiveId]),
  )
  const result: RuntimeConnection[] = []

  for (const connection of connections) {
    if (deleteAfterReceiveNodes.get(connection.from) === connection.to) continue

    const receiveId = receiveByDeleteId.get(connection.from)
    if (receiveId) {
      result.push({
        ...connection,
        from: receiveId,
      })
      continue
    }

    result.push(connection)
  }

  return result
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value.trim() : ''
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
