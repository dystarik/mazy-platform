import {
  BUTTON_BRANCHING_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GOTO_NODE_TYPE,
  GROUP_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  VK_SEND_CAROUSEL_NODE_TYPE,
  VK_SEND_KEYBOARD_NODE_TYPE,
} from '@/components/editor/editorTypes'
import { normalizeButtonBranchingCompiledNodeIds } from './editorNodeDefinitions'
import type {
  EditorBlock,
  GotoIncomingConnection,
  GroupEditorBlock,
  RuntimeConnection,
  RuntimeNode,
  RuntimeScenario,
  ScenarioPosition,
  ScenarioRoutePoint,
} from './editorScenario.types'

const EDITOR_GRID_SIZE = 12

export function normalizeRuntimeScenario(value: unknown): RuntimeScenario {
  if (isRecord(value) && typeof value.graphJson === 'string') {
    return normalizeRuntimeScenario(JSON.parse(value.graphJson) as unknown)
  }

  if (!isRecord(value) || !Array.isArray(value.nodes) || !Array.isArray(value.connections)) {
    throw new Error('Invalid scenario JSON')
  }

  const nodes = value.nodes.map((node, index) => normalizeRuntimeNode(node, index))
  const nodeIds = new Set<string>()
  for (const node of nodes) {
    if (nodeIds.has(node.id)) throw new Error('Duplicate node id')
    nodeIds.add(node.id)
  }

  const connections = value.connections.map(connection => normalizeRuntimeConnection(connection, nodeIds))
  const startNodeId = readString(value.startNodeId) ?? (nodes[0]?.id ?? '')
  if (startNodeId && !nodeIds.has(startNodeId)) throw new Error('Start node not found')

  return {
    startNodeId,
    nodes,
    connections,
    editorBlocks: normalizeRuntimeEditorBlocks(value.editorBlocks, nodeIds),
  }
}

export function normalizeAuthoringScenario(value: unknown): RuntimeScenario {
  if (!isRecord(value) || value.format !== 'mazy-editor-scenario') {
    throw new Error('Invalid authoring scenario JSON')
  }

  if (!Array.isArray(value.nodes) || !Array.isArray(value.connections)) {
    throw new Error('Invalid authoring scenario graph')
  }

  const nodes = value.nodes.map((node, index) => normalizeRuntimeNode(node, index))
  const nodeIds = new Set<string>()
  for (const node of nodes) {
    if (nodeIds.has(node.id)) throw new Error('Duplicate node id')
    nodeIds.add(node.id)
  }

  const connections = value.connections.map(connection => normalizeRuntimeConnection(connection, nodeIds))
  const startNodeId = readString(value.startNodeId) ?? (nodes[0]?.id ?? '')
  if (startNodeId && !nodeIds.has(startNodeId)) throw new Error('Start node not found')

  return {
    startNodeId,
    nodes,
    connections,
  }
}

export function isAuthoringScenario(value: unknown): boolean {
  return isRecord(value) && value.format === 'mazy-editor-scenario'
}

function normalizeRuntimeNode(value: unknown, index: number): RuntimeNode {
  if (!isRecord(value)) throw new Error('Invalid node')

  const id = readString(value.id) ?? createGraphId()
  const type = readString(value.type)
  if (!type) throw new Error('Invalid node type')
  const params = isRecord(value.params) ? cloneParams(value.params) : {}

  if (type === EDIT_MESSAGE_NODE_TYPE && params.newText === undefined && params.text !== undefined) {
    params.newText = params.text
    delete params.text
  }

  return {
    id,
    type,
    params,
    position: normalizePosition(value.position, index),
    ...(normalizeNodeUi(value.ui) ? { ui: normalizeNodeUi(value.ui) } : {}),
  }
}

function normalizeNodeUi(value: unknown): { textHeight?: number } | undefined {
  if (!isRecord(value)) return undefined

  const ui: { textHeight?: number } = {}
  if (typeof value.textHeight === 'number' && Number.isFinite(value.textHeight)) {
    ui.textHeight = Math.max(24, Math.round(value.textHeight / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE)
  }

  return Object.keys(ui).length ? ui : undefined
}

function normalizeRuntimeConnection(value: unknown, nodeIds: Set<string>): RuntimeConnection {
  if (!isRecord(value)) throw new Error('Invalid connection')

  const from = readString(value.from) ?? readString(value.source)
  const to = readString(value.to) ?? readString(value.target)
  if (!from || !to || !nodeIds.has(from) || !nodeIds.has(to)) {
    throw new Error('Invalid connection endpoint')
  }

  const branch = readString(value.branch) ?? readString(value.branchKey) ?? readString(value.sourceHandle)
  const route = normalizeRoute(value.route)
  const routeVersion = typeof value.routeVersion === 'number' && Number.isFinite(value.routeVersion)
    ? value.routeVersion
    : undefined

  return {
    from,
    to,
    ...(branch ? { branch } : {}),
    ...(route.length ? { route, routeVersion: routeVersion ?? 2 } : {}),
  }
}

function normalizeRuntimeEditorBlocks(value: unknown, nodeIds: Set<string>): EditorBlock[] {
  if (!Array.isArray(value)) return []

  return value
    .map(item => normalizeRuntimeEditorBlock(item, nodeIds))
    .filter((item): item is EditorBlock => item !== null)
}

function normalizeRuntimeEditorBlock(value: unknown, nodeIds: Set<string>): EditorBlock | null {
  if (!isRecord(value)) return null

  const id = readString(value.id) ?? createGraphId()
  if (value.type === GOTO_NODE_TYPE) {
    return {
      id,
      type: GOTO_NODE_TYPE,
      targetNodeId: readString(value.targetNodeId) ?? undefined,
      incomingConnections: normalizeGotoIncomingConnections(value.incomingConnections),
      position: readPosition(value.position),
    }
  }

  if (value.type === GROUP_NODE_TYPE) {
    return normalizeRuntimeGroupBlock(value, nodeIds)
  }

  if (value.type !== BUTTON_BRANCHING_NODE_TYPE || !isRecord(value.nodes)) return null

  const nodes = normalizeButtonBranchingCompiledNodeIds(value.nodes)
  if (!nodes || !nodeIds.has(nodes.sendButtons) || !nodeIds.has(nodes.receiveButtonPress) || !nodeIds.has(nodes.switch)) {
    return null
  }

  return {
    id,
    type: BUTTON_BRANCHING_NODE_TYPE,
    sourceType: normalizeEditorBlockSourceType(value.sourceType),
    targetMessageNodeId: readString(value.targetMessageNodeId) ?? undefined,
    position: readPosition(value.position),
    nodes,
  }
}

function normalizeRuntimeGroupBlock(value: Record<string, unknown>, _nodeIds: Set<string>): GroupEditorBlock | null {
  const id = readString(value.id) ?? createGraphId()
  const entryNodeId = readString(value.entryNodeId)
  const exitNodeId = readString(value.exitNodeId)
  if (!entryNodeId || !exitNodeId) return null

  if (!isRecord(value.subgraph)) return null

  const subgraph = normalizeRuntimeScenario(value.subgraph)
  const subgraphNodeIds = new Set(subgraph.nodes.map(node => node.id))
  if (!subgraphNodeIds.has(entryNodeId) || !subgraphNodeIds.has(exitNodeId)) return null

  return {
    id,
    type: GROUP_NODE_TYPE,
    title: readString(value.title) ?? 'Группа',
    position: readPosition(value.position),
    entryNodeId,
    exitNodeId,
    subgraph,
  }
}

function normalizeGotoIncomingConnections(value: unknown): GotoIncomingConnection[] | undefined {
  if (!Array.isArray(value)) return undefined

  const connections = value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map(item => {
      const from = readString(item.from)
      if (!from) return null

      const branch = readString(item.branch)
      return { from, ...(branch ? { branch } : {}) }
    })
    .filter((item): item is GotoIncomingConnection => item !== null)

  return connections.length ? connections : undefined
}

function normalizeEditorBlockSourceType(value: unknown): 'send_message' | 'edit_message' | 'vk_send_keyboard' | 'vk_send_carousel' | undefined {
  if (value === EDIT_MESSAGE_NODE_TYPE) return EDIT_MESSAGE_NODE_TYPE
  if (value === VK_SEND_KEYBOARD_NODE_TYPE) return VK_SEND_KEYBOARD_NODE_TYPE
  if (value === VK_SEND_CAROUSEL_NODE_TYPE) return VK_SEND_CAROUSEL_NODE_TYPE
  if (value === MESSAGE_NODE_TYPE || value === BUTTON_BRANCHING_NODE_TYPE) return MESSAGE_NODE_TYPE
  return undefined
}

function normalizePosition(value: unknown, index: number): ScenarioPosition {
  return readPosition(value) ?? snapPosition({ x: 120 + index * 80, y: 120 + index * 40 })
}

function readPosition(value: unknown): ScenarioPosition | undefined {
  if (isRecord(value) && typeof value.x === 'number' && typeof value.y === 'number') {
    return snapPosition({ x: value.x, y: value.y })
  }

  return undefined
}

function normalizeRoute(value: unknown): ScenarioRoutePoint[] {
  if (!Array.isArray(value)) return []

  return value
    .filter((point): point is ScenarioRoutePoint =>
      isRecord(point)
      && typeof point.x === 'number'
      && Number.isFinite(point.x)
      && typeof point.y === 'number'
      && Number.isFinite(point.y),
    )
    .map(point => snapPosition(point))
}

function snapPosition(position: ScenarioPosition): ScenarioPosition {
  return {
    x: Math.round(position.x / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE,
    y: Math.round(position.y / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE,
  }
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function cloneParams(params: Record<string, unknown>): Record<string, unknown> {
  return JSON.parse(JSON.stringify(params)) as Record<string, unknown>
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function createGraphId(): string {
  return crypto.randomUUID?.() ?? `node-${Date.now()}-${Math.random().toString(16).slice(2)}`
}
