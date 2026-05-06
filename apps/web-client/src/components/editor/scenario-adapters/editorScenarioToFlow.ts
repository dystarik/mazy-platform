import type { Edge } from '@vue-flow/core'
import type { EditorFlowNode } from '@/components/editor/editorTypes'
import { isSmartButtonBranchingNodeType } from '@/components/editor/editorTypes'
import type {
  EditorScenario,
  RuntimeConnection,
  RuntimeNode,
  ScenarioPosition,
} from '@/components/editor/scenario-adapters/editorScenario.types'

export interface EditorScenarioFlow {
  nodes: EditorFlowNode[]
  edges: Edge[]
}

export interface EditorScenarioToFlowOptions {
  snapPosition?: (position: ScenarioPosition) => ScenarioPosition
  fallbackPositionForNode?: (node: RuntimeNode, index: number) => ScenarioPosition
  edgeIdPrefix?: string
}

export function editorScenarioToFlow(
  scenario: EditorScenario,
  options: EditorScenarioToFlowOptions = {},
): EditorScenarioFlow {
  const nodes = scenario.nodes.map((node, index) => createFlowNode(node, index, options))
  const edges = scenario.connections.map((connection, index) =>
    createFlowEdge(connection, index, nodes, options),
  )

  return { nodes, edges }
}

function createFlowNode(
  node: RuntimeNode,
  index: number,
  options: EditorScenarioToFlowOptions,
): EditorFlowNode {
  const fallback = options.fallbackPositionForNode?.(node, index) ?? { x: 0, y: 0 }
  const position = options.snapPosition?.(node.position ?? fallback) ?? node.position ?? fallback

  return {
    id: node.id,
    type: 'default',
    position,
    data: {
      type: node.type,
      params: node.params ?? {},
      ...(node.ui ? { ui: node.ui } : {}),
    },
  }
}

function createFlowEdge(
  connection: RuntimeConnection,
  index: number,
  nodes: EditorFlowNode[],
  options: EditorScenarioToFlowOptions,
): Edge {
  const prefix = options.edgeIdPrefix ?? 'e'
  const route = Array.isArray(connection.route) ? connection.route : []

  return {
    id: `${prefix}-${index}-${connection.from}-${connection.to}`,
    source: connection.from,
    target: connection.to,
    sourceHandle: connection.branch ?? null,
    label: edgeLabelForConnection(nodes, connection),
    type: 'routed',
    animated: false,
    style: { stroke: 'var(--color-primary)', strokeWidth: 2 },
    labelStyle: { fill: 'var(--color-text-secondary)', fontSize: 10 },
    labelBgStyle: { fill: 'var(--color-bg-card)' },
    data: route.length
      ? { route, routeVersion: connection.routeVersion ?? 2 }
      : undefined,
  }
}

function edgeLabelForConnection(
  nodes: EditorFlowNode[],
  connection: RuntimeConnection,
): string | undefined {
  if (!connection.branch) return undefined

  const sourceNode = nodes.find(node => node.id === connection.from)
  if (sourceNode && isSmartButtonBranchingNodeType(sourceNode.data.type)) {
    return undefined
  }

  return connection.branch
}
