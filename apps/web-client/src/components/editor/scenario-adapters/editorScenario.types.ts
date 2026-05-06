import type { Edge } from '@vue-flow/core'
import type { EditorFlowNode, EditorNodeUiState } from '@/components/editor/editorTypes'

export interface ScenarioPosition {
  x: number
  y: number
}

export interface ScenarioRoutePoint {
  x: number
  y: number
}

export interface RuntimeNode {
  id: string
  type: string
  params: Record<string, unknown>
  position?: ScenarioPosition
  ui?: EditorNodeUiState
}

export interface RuntimeConnection {
  from: string
  to: string
  branch?: string
  route?: ScenarioRoutePoint[]
  routeVersion?: number
}

export interface ButtonBranchingCompiledNodeIds {
  sendButtons: string
  receiveButtonPress: string
  switch: string
  deleteMessage?: string
}

export interface ButtonBranchingEditorBlock {
  id: string
  type: 'button_branching'
  sourceType?: 'send_message' | 'edit_message' | 'button_branching'
  position?: ScenarioPosition
  ui?: EditorNodeUiState
  targetMessageNodeId?: string
  nodes: ButtonBranchingCompiledNodeIds
}

export interface GotoIncomingConnection {
  from: string
  branch?: string
}

export interface GotoEditorBlock {
  id: string
  type: 'goto_node'
  position?: ScenarioPosition
  targetNodeId?: string
  incomingConnections?: GotoIncomingConnection[]
}

export interface GroupEditorBlock {
  id: string
  type: 'group_node'
  title: string
  position?: ScenarioPosition
  boundaryPositions?: {
    entry?: ScenarioPosition
    exit?: ScenarioPosition
  }
  entryNodeId: string
  exitNodeId: string
  subgraph: EditorScenario
}

export type EditorBlock = ButtonBranchingEditorBlock | GotoEditorBlock | GroupEditorBlock

export interface RuntimeScenario {
  startNodeId: string
  nodes: RuntimeNode[]
  connections: RuntimeConnection[]
  editorBlocks?: EditorBlock[]
}

export interface EditorScenario {
  startNodeId: string
  nodes: RuntimeNode[]
  connections: RuntimeConnection[]
}

export interface EditorToRuntimeOptions {
  nodes: EditorFlowNode[]
  edges: Edge[]
  visualStartNodeId: string
  snapPosition: (position: ScenarioPosition) => ScenarioPosition
  normalizeParamsForNode: (nodeType: string, params: Record<string, unknown>) => Record<string, unknown>
  normalizeDataNodeParams: (backendNodeType: string, params: Record<string, unknown>) => Record<string, unknown>
  resolveEditMessageIdVariable: (node: EditorFlowNode) => string
  getSavedEdgeRoute: (edge: Edge) => ScenarioRoutePoint[]
}
