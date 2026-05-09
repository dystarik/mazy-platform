<template>
  <div class="editor">

    <EditorTopbar
      :project-id="projectId"
      :is-read-only="isReadOnly"
      :scenario-mode-label="scenarioModeLabel"
      :save-status="saveStatus"
      :status-message="statusMessage"
      :saving="saving"
      :checking-draft="checkingDraft"
      :nodes-count="nodes.length"
      :can-undo="canUndo"
      :can-redo="canRedo"
      :can-edit-copy="canEditCopy"
      :active-group-title="activeGroupSession?.title ?? ''"
      @import-json="importScenarioJson"
      @export-json="exportScenarioJson"
      @export-schema="exportAuthoringSchemaJson"
      @auto-layout="autoLayoutGraph"
      @undo="undoGraphChange"
      @redo="redoGraphChange"
      @exit-group="exitActiveGroup"
      @edit-copy="enableReleaseCopyEditing"
      @validate-draft="validateDraft"
      @save="saveDraft"
    />

    <div v-if="scenarioNodeErrors.length" class="editor__scenario-errors">
      <p class="editor__scenario-errors-title">Ошибки сценария</p>
      <div
        v-for="(error, i) in scenarioNodeErrors"
        :key="`${error.nodeId ?? 'scenario'}:${error.path ?? error.code}:${i}`"
        class="editor__scenario-error"
      >
        <p class="editor__scenario-error-message">{{ error.message }}</p>
        <p v-if="formatScenarioNodeErrorMeta(error)" class="editor__scenario-error-meta">
          {{ formatScenarioNodeErrorMeta(error) }}
        </p>
        <button
          v-if="canSelectScenarioNodeError(error)"
          class="editor__scenario-error-action"
          type="button"
          @click="selectScenarioNodeError(error)"
        >
          Перейти к узлу
        </button>
      </div>
    </div>

    <div class="editor__body">

      <!-- Панель каталога -->
      <NodeCatalogSidebar
        v-if="!isReadOnly"
        :loading="catalogLoading"
        :categories="filteredCategories"
        @drag-start="onDragStart"
      />

      <GraphCanvas
        v-model:nodes="nodes"
        v-model:edges="edges"
        :is-read-only="isReadOnly"
        :start-node-id="graphData?.startNodeId ?? null"
        :message-picker-target-node-id="messagePickerState?.targetNodeId ?? null"
        :right-selection-visible="Boolean(rightSelectionRect)"
        :right-selection-style="rightSelectionStyle"
        :catalog-params-for="getCatalogParams"
        :project-schema-details="projectSchemaDetails"
        :known-variables="knownVariables"
        :variable-scope-for-node="variableScopeForNode"
        :is-message-pickable-node="isMessagePickableNode"
        :is-related-message-node="isRelatedMessageNode"
        :message-relation-tone-for-node="messageRelationToneForNode"
        :message-source-label-for="messageSourceLabelFor"
        @drop="onDrop"
        @canvas-wheel="onCanvasWheel"
        @canvas-pointer-down-capture="onCanvasPointerDownCapture"
        @canvas-mouse-down-capture="onCanvasMouseDownCapture"
        @connect="onConnect"
        @node-click="onNodeClick"
        @node-context-menu="onNodeContextMenu"
        @node-drag-start="onNodeDragStart"
        @node-drag="onNodeDrag"
        @node-drag-stop="onNodeDragStop"
        @edge-click="onEdgeClick"
        @edge-context-menu="onEdgeContextMenu"
        @pane-click="onPaneClick"
        @pane-context-menu="onPaneContextMenu"
        @update-param="updateParamById"
        @update-node-ui="updateNodeUiById"
        @pick-message="startMessagePick"
        @set-start="setStartNode"
        @delete-node="deleteNode"
      />

    </div>

    <EditorContextMenu
      :menu="contextMenu"
      :has-clipboard="Boolean(clipboard)"
      :can-open-group="canOpenContextGroup"
      :categories="filteredCategories"
      @open-group="openContextGroup"
      @set-node-start="setContextNodeStart"
      @copy-node="copyContextNode"
      @delete-node="deleteContextNode"
      @group-selection="groupSelectedNodes"
      @copy-selection="copySelectedNodes"
      @delete-selection="deleteSelectedNodes"
      @add-node="addNodeFromMenu"
      @paste="pasteFromMenu"
      @delete-edge="deleteContextEdge"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  useVueFlow,
  type Edge,
  type Connection,
  type EdgeMouseEvent,
  type NodeDragEvent,
  type NodeMouseEvent,
} from '@vue-flow/core'
import { scenarioApi, nodesApi, entitySchemasApi, projectsApi } from '@/api'
import { parseScenarioError, type ScenarioNodeHeaderError } from '@/composables/useScenarioApiError'
import type { NodeCatalogItem, NodeParamItem, PlatformType, ScenarioValidationErrorItem } from '@/types/api'
import type { GetEntitySchemaResponse } from '@/types/api/entity-schemas.types'
import type { ELK, ElkExtendedEdge, ElkNode, ElkPoint, ElkPort } from 'elkjs/lib/elk.bundled'
import EditorContextMenu from '@/components/editor/EditorContextMenu.vue'
import EditorTopbar from '@/components/editor/EditorTopbar.vue'
import GraphCanvas from '@/components/editor/GraphCanvas.vue'
import NodeInspector from '@/components/editor/NodeInspector.vue'
import NodeCatalogSidebar from '@/components/editor/NodeCatalogSidebar.vue'
import {
  BUTTON_BRANCHING_NODE_TYPE,
  DATA_NODE_TYPE,
  EDIT_MESSAGE_NODE_TYPE,
  GOTO_NODE_TYPE,
  GROUP_ENTRY_NODE_TYPE,
  GROUP_EXIT_NODE_TYPE,
  GROUP_NODE_TYPE,
  MESSAGE_NODE_TYPE,
  canProvideMessageId,
  getNodeOutputPorts,
  getUiParams,
  isSmartButtonBranchingNodeType,
  messageConsumersFor,
  normalizeButtonBranchingButtonRows,
  normalizeNodeParamType,
  readButtonBranchingButtons,
  type EditorFlowNode,
  type EditorNodeData,
  type EditorNodeUiState,
  type SelectedInspector,
} from '@/components/editor/editorTypes'
import {
  NODE_META,
  getNodeLabel,
} from '@/components/editor/nodes/nodeMeta'
import {
  BUTTON_BRANCHING_NODE_IDS_PARAM,
  BUTTON_BRANCHING_PAYLOAD_VARIABLE_DEFAULT,
  EDITOR_NODE_DEFINITIONS,
  GROUP_CATALOG_ITEM,
  buildEditorCatalogCategories,
  withEditorCatalogItems,
  createButtonBranchingCompiledNodeIds,
  createButtonPayloadVariable,
  backendNodeTypeFromDataAction as resolveBackendNodeTypeFromDataAction,
} from '@/components/editor/scenario-adapters/editorNodeDefinitions'
import { runtimeToEditorScenario } from '@/components/editor/scenario-adapters/runtimeToEditorScenario'
import { editorToRuntimeScenario } from '@/components/editor/scenario-adapters/editorToRuntimeScenario'
import { editorScenarioToFlow } from '@/components/editor/scenario-adapters/editorScenarioToFlow'
import {
  isAuthoringScenario,
  normalizeAuthoringScenario,
  normalizeRuntimeScenario,
} from '@/components/editor/scenario-adapters/normalizeRuntimeScenario'
import type {
  RuntimeConnection,
  RuntimeNode,
  RuntimeScenario,
  ScenarioRoutePoint,
} from '@/components/editor/scenario-adapters/editorScenario.types'
import { getObjectMatrixLimitViolation } from '@/components/editor/buttonMatrixLimits'
import type { VariableScope } from '@/components/editor/variableHighlight'

const route = useRoute()
const projectId = computed(() => route.params.id as string)
const scenarioId = computed(() => route.params.sid as string)

const isReleaseCopyMode = computed(() => scenarioId.value === 'release-copy')
const isEditingScenarioCopy = ref(false)
const isReadOnly = computed(() => scenarioId.value !== 'draft' && !isEditingScenarioCopy.value)
const scenarioVersion = computed(() => parseScenarioVersion(scenarioId.value))
const scenarioModeLabel = computed(() => {
  if (isEditingScenarioCopy.value && scenarioId.value === 'release') {
    return 'Копия релиза: сохранение в черновик'
  }
  if (isEditingScenarioCopy.value && scenarioVersion.value != null) {
    return `Копия v${scenarioVersion.value}: сохранение в черновик`
  }
  if (scenarioId.value === 'release') return 'Текущий релиз'
  if (scenarioVersion.value != null) return `Версия v${scenarioVersion.value}`
  return ''
})
const canEditCopy = computed(() =>
  isReadOnly.value
  && scenarioId.value !== 'draft'
  && nodes.value.length > 0
  && !activeGroupSession.value,
)
const EDITOR_GRID_SIZE = 12
const GROUP_ENTRY_MARKER_ID = '__group_entry_marker__'
const GROUP_EXIT_MARKER_ID = '__group_exit_marker__'
const DATA_NODE_BACKEND_TYPES = new Set([
  'create_record',
  'get_record',
  'query_records',
  'update_record',
  'delete_record',
])

// ── Граф ─────────────────────────────────────────────────────────────────────
type GraphNode = RuntimeNode
type GraphConnection = RuntimeConnection
type GraphConnectionRoutePoint = ScenarioRoutePoint
type GraphData = RuntimeScenario
interface NodePositionSnapshot {
  x: number
  y: number
}
interface EditorHistoryEntry {
  graph: GraphData
  signature: string
}
interface GroupEditSession {
  groupId: string
  title: string
  rootNodes: EditorFlowNode[]
  rootEdges: Edge[]
  rootGraphData: GraphData | null
}
interface NodeDragRouteSnapshot {
  nodeIds: Set<string>
  nodePositions: Map<string, NodePositionSnapshot>
  edgeRoutes: Map<string, GraphConnectionRoutePoint[]>
}

const graphData = ref<GraphData | null>(null)
const nodes = ref<EditorFlowNode[]>([])
const edges = ref<Edge[]>([])
const activeGroupSession = ref<GroupEditSession | null>(null)
const undoStack = ref<EditorHistoryEntry[]>([])
const redoStack = ref<EditorHistoryEntry[]>([])
const pendingHistorySignature = ref<string | null>(null)
const canUndo = computed(() =>
  !isReadOnly.value && !activeGroupSession.value && (undoStack.value.length > 1 || Boolean(pendingHistorySignature.value)),
)
const canRedo = computed(() => !isReadOnly.value && !activeGroupSession.value && redoStack.value.length > 0)
let historyCommitTimer: ReturnType<typeof setTimeout> | null = null
let isApplyingHistory = false
let currentHistorySignature = ''
let nodeDragRouteSnapshot: NodeDragRouteSnapshot | null = null

interface SelectionBox {
  x: number
  y: number
  width: number
  height: number
}

interface RightSelectionDrag {
  pointerId: number | null
  startX: number
  startY: number
}

interface ClipboardNode {
  oldId: string
  type: string
  params: Record<string, unknown>
  ui?: EditorNodeUiState
  position: { x: number; y: number }
}

interface ClipboardEdge {
  source: string
  target: string
  sourceHandle?: string | null
  targetHandle?: string | null
  label?: string
}

interface ClipboardPayload {
  nodes: ClipboardNode[]
  edges: ClipboardEdge[]
}

interface MessagePickerState {
  targetNodeId: string
  kind: 'message' | 'node'
}

type EditorMenu =
  | { type: 'node'; x: number; y: number; nodeId: string }
  | { type: 'selection'; x: number; y: number }
  | { type: 'pane'; x: number; y: number; position: { x: number; y: number } }
  | { type: 'edge'; x: number; y: number; edgeId: string }

const rightSelectionDrag = ref<RightSelectionDrag | null>(null)
const rightSelectionRect = ref<SelectionBox | null>(null)
const clipboard = ref<ClipboardPayload | null>(null)
const contextMenu = ref<EditorMenu | null>(null)
const messagePickerState = ref<MessagePickerState | null>(null)
const canOpenContextGroup = computed(() => {
  const menu = contextMenu.value
  if (!menu || menu.type !== 'node') return false

  const node = nodes.value.find(item => item.id === menu.nodeId)
  return node?.data.type === GROUP_NODE_TYPE
})
const knownVariables = computed(() => {
  const variables = new Set<string>()
  collectVariablesFromFlowNodes(nodes.value, variables)

  if (activeGroupSession.value) {
    collectVariablesFromFlowNodes(activeGroupSession.value.rootNodes, variables)
  }

  return [...variables].sort((a, b) => a.localeCompare(b))
})
const allVariableScope = computed<VariableScope>(() => ({
  available: knownVariables.value,
  future: [],
}))
const rightSelectionStyle = computed(() => {
  const rect = rightSelectionRect.value
  if (!rect) return {}

  return {
    left: `${rect.x}px`,
    top: `${rect.y}px`,
    width: `${rect.width}px`,
    height: `${rect.height}px`,
  }
})
const selectedNodes = computed(() => nodes.value.filter((node) => node.selected))

// ── Каталог ───────────────────────────────────────────────────────────────────
const catalog = ref<NodeCatalogItem[]>([])
const catalogLoading = ref(false)
const projectPlatformType = ref<PlatformType>('PLATFORM_TYPE_VK')

// ── Схемы данных проекта ─────────────────────────────────────────────────────
const schemasLoading = ref(false)
const projectSchemaDetails = ref<GetEntitySchemaResponse[]>([])

const selectedNodeId = ref<string | null>(null)
const isInspectorOpen = ref(false)
const handledShiftClickNodeId = ref<string | null>(null)

const selectedInspector = computed<SelectedInspector | null>(() => {
  const node = nodes.value.find((item) => item.id === selectedNodeId.value)
  if (!node) return null

  const data = node.data
  return {
    node,
    data,
    params: getCatalogParams(data.type),
  }
})

function onNodeClick(event: NodeMouseEvent): void {
  if (messagePickerState.value) {
    pickMessageSource(event.node.id)
    return
  }

  if (event.event.shiftKey) {
    event.event.preventDefault()
    event.event.stopPropagation()
    if (handledShiftClickNodeId.value === event.node.id) {
      handledShiftClickNodeId.value = null
      contextMenu.value = null
      return
    }

    toggleNodeSelection(event.node.id)
    contextMenu.value = null
    return
  }

  selectNode(event.node.id)
  contextMenu.value = null
}

function onNodeContextMenu(event: NodeMouseEvent): void {
  if (isReadOnly.value) return

  const selected = selectedNodes.value
  const point = getMenuPoint(event.event)

  if (selected.length > 1 && selected.some((node) => node.id === event.node.id)) {
    event.event.preventDefault()
    event.event.stopPropagation()
    selectedNodeId.value = null
    contextMenu.value = { type: 'selection', ...point }
    return
  }

  if (event.event.shiftKey) return

  nodes.value = nodes.value.map((node) => ({ ...node, selected: node.id === event.node.id }))
  selectedNodeId.value = event.node.id
  contextMenu.value = { type: 'node', nodeId: event.node.id, ...point }
}

function clearSelectedNode(): void {
  selectedNodeId.value = null
  isInspectorOpen.value = false
  contextMenu.value = null
}

function onPaneClick(event: MouseEvent): void {
  messagePickerState.value = null
  selectedNodeId.value = null
  isInspectorOpen.value = false
  contextMenu.value = null
}

function openPaneContextMenu(event: MouseEvent): void {
  if (isReadOnly.value) return

  contextMenu.value = {
    type: 'pane',
    ...getMenuPoint(event),
    position: screenToFlowCoordinate({
      x: event.clientX,
      y: event.clientY,
    }),
  }
}

function onPaneContextMenu(event: MouseEvent): void {
  if (isReadOnly.value || event.shiftKey || rightSelectionDrag.value) return

  const target = event.target
  if (!(target instanceof HTMLElement)) return
  if (target.closest('.vue-flow__node, .vue-flow__edge, .vue-flow__controls, .editor-menu')) return
  if (!target.closest('.vue-flow__pane')) return

  event.preventDefault()
  event.stopPropagation()
  openPaneContextMenu(event)
}

function onEdgeClick(event: EdgeMouseEvent): void {
  selectedNodeId.value = null
  contextMenu.value = null
}

function onEdgeContextMenu(event: EdgeMouseEvent): void {
  if (isReadOnly.value) return
  if (event.event.shiftKey) return

  selectedNodeId.value = null
  contextMenu.value = {
    type: 'edge',
    edgeId: event.edge.id,
    ...getMenuPoint(event.event),
  }
}

function getMenuPoint(event: MouseEvent | TouchEvent): { x: number; y: number } {
  if ('touches' in event && event.touches[0]) {
    return { x: event.touches[0].clientX, y: event.touches[0].clientY }
  }

  if ('changedTouches' in event && event.changedTouches[0]) {
    return { x: event.changedTouches[0].clientX, y: event.changedTouches[0].clientY }
  }

  return {
    x: (event as MouseEvent).clientX,
    y: (event as MouseEvent).clientY,
  }
}

function updateSelectedParam(key: string, value: unknown): void {
  if (isReadOnly.value) return
  if (!selectedInspector.value || !key) return
  updateParamById(selectedInspector.value.node.id, key, value)
}

function messageConsumers(inspector: SelectedInspector): EditorFlowNode[] {
  return messageConsumersFor(nodes.value, inspector)
}

function relatedMessageNodeIds(): Set<string> {
  const result = new Set<string>()
  for (const node of nodes.value) {
    if (messageRelationToneForNode(node.id)) {
      result.add(node.id)
    }
  }
  return result
}

function isRelatedMessageNode(nodeId: string): boolean {
  return Boolean(messageRelationToneForNode(nodeId))
}

function messageRelationToneForNode(nodeId: string): 'edit' | 'delete' | 'source' | 'jump' | null {
  const inspector = selectedInspector.value
  if (!inspector) return null

  if (inspector.data.type === GOTO_NODE_TYPE) {
    const targetNodeId = stringValue(inspector.data.params.targetNodeId)
    return targetNodeId === nodeId ? 'source' : null
  }

  const candidateNode = nodes.value.find(node => node.id === nodeId)
  if (
    candidateNode?.data.type === GOTO_NODE_TYPE
    && stringValue(candidateNode.data.params.targetNodeId) === inspector.node.id
  ) {
    return 'jump'
  }

  if (canProvideMessageId(inspector.data.type)) {
    const consumer = messageConsumers(inspector).find(item => item.id === nodeId)
    if (!consumer) return null
    if (consumer.data.type === 'delete_message') return 'delete'
    if (consumer.data.type === EDIT_MESSAGE_NODE_TYPE) return 'edit'
    return null
  }

  if (![EDIT_MESSAGE_NODE_TYPE, 'delete_message'].includes(inspector.data.type)) return null

  const targetNodeId = stringValue(inspector.data.params.targetMessageNodeId)
  if (targetNodeId) {
    return targetNodeId === nodeId ? 'source' : null
  }

  const variable = stringValue(inspector.data.params.messageIdVariable)
  if (!variable) return null

  const source = nodes.value.find(node =>
    canProvideMessageId(node.data.type)
    && node.data.params.messageIdVariable === variable,
  )

  return source?.id === nodeId ? 'source' : null
}

function selectRelatedNode(nodeId: string): void {
  selectNode(nodeId)
  contextMenu.value = null
}

function selectNode(nodeId: string): void {
  selectedNodeId.value = nodeId
  isInspectorOpen.value = true
  nodes.value = nodes.value.map(node => ({ ...node, selected: node.id === nodeId }))
}

function toggleNodeSelection(nodeId: string): void {
  const selectedIds = new Set(
    nodes.value
      .filter(node => node.selected)
      .map(node => node.id),
  )

  if (selectedIds.has(nodeId)) {
    selectedIds.delete(nodeId)
  } else {
    selectedIds.add(nodeId)
  }

  nodes.value = nodes.value.map(node => ({
    ...node,
    selected: selectedIds.has(node.id),
  }))

  selectedNodeId.value = selectedIds.size === 1 ? [...selectedIds][0] ?? null : null
  isInspectorOpen.value = selectedIds.size === 1
}

function startMessagePick(targetNodeId: string): void {
  if (isReadOnly.value) return
  if (messagePickerState.value?.targetNodeId === targetNodeId) {
    messagePickerState.value = null
    return
  }

  const targetNode = nodes.value.find(node => node.id === targetNodeId)
  messagePickerState.value = {
    targetNodeId,
    kind: targetNode?.data.type === GOTO_NODE_TYPE ? 'node' : 'message',
  }
  contextMenu.value = null
}

function cancelMessagePick(): void {
  messagePickerState.value = null
}

function pickMessageSource(sourceNodeId: string): void {
  const state = messagePickerState.value
  if (!state) return

  if (!isMessagePickableNode(sourceNodeId)) {
    messagePickerState.value = null
    selectNode(sourceNodeId)
    contextMenu.value = null
    return
  }

  const sourceNode = nodes.value.find(node => node.id === sourceNodeId)
  const targetNode = nodes.value.find(node => node.id === state.targetNodeId)
  if (!sourceNode || !targetNode) return

  if (state.kind === 'node') {
    targetNode.data.params = {
      ...targetNode.data.params,
      targetNodeId: sourceNode.id,
    }
    selectedNodeId.value = targetNode.id
    messagePickerState.value = null
    contextMenu.value = null
    return
  }

  const variable = ensureMessageIdVariable(sourceNode)
  targetNode.data.params = {
    ...targetNode.data.params,
    targetMessageNodeId: sourceNode.id,
    messageIdVariable: variable,
  }

  selectedNodeId.value = targetNode.id
  messagePickerState.value = null
  contextMenu.value = null
}

function isMessagePickableNode(nodeId: string): boolean {
  if (!messagePickerState.value) return false
  if (messagePickerState.value.targetNodeId === nodeId) return false

  const node = nodes.value.find(item => item.id === nodeId)
  if (!node) return false

  if (messagePickerState.value.kind === 'node') {
    return node.id !== messagePickerState.value.targetNodeId
  }

  return canProvideMessageId(node.data.type)
}

function messageSourceLabelFor(nodeId: string): string | null {
  const node = nodes.value.find(item => item.id === nodeId)
  if (!node) return null

  if (node.data.type === GOTO_NODE_TYPE) {
    const targetNodeId = stringValue(node.data.params.targetNodeId)
    if (!targetNodeId) return null

    const targetNode = nodes.value.find(item => item.id === targetNodeId)
    return targetNode ? getNodeLabel(targetNode.data.type) : null
  }

  if (![EDIT_MESSAGE_NODE_TYPE, 'delete_message'].includes(node.data.type)) return null

  const targetNodeId = stringValue(node.data.params.targetMessageNodeId)
  if (!targetNodeId) return null

  const sourceNode = nodes.value.find(item => item.id === targetNodeId)
  if (!sourceNode) return null

  return sourceNode.data.type === MESSAGE_NODE_TYPE ? 'Сообщение' : getNodeLabel(sourceNode.data.type)
}

function ensureMessageIdVariable(node: EditorFlowNode): string {
  const existing = node.data.params.messageIdVariable
  if (typeof existing === 'string' && existing.trim()) {
    return existing.trim()
  }

  const variable = node.data.type === 'receive_message'
    ? `user_message_${shortNodeId(node.id)}_id`
    : `message_${shortNodeId(node.id)}_id`
  node.data.params = {
    ...node.data.params,
    messageIdVariable: variable,
  }
  return variable
}

function shortNodeId(id: string): string {
  return id.replace(/-/g, '').slice(0, 8)
}

function parseScenarioVersion(value: string): number | null {
  const normalized = value.startsWith('version-') ? value.slice('version-'.length) : value
  const version = Number(normalized)
  return Number.isInteger(version) && version > 0 ? version : null
}

// ── Сохранение ────────────────────────────────────────────────────────────────
const saving = ref(false)
const checkingDraft = ref(false)
const saveStatus = ref<'idle' | 'saved' | 'imported' | 'exported' | 'validated' | 'error'>('idle')
const statusMessage = ref('')
const scenarioNodeErrors = ref<ScenarioNodeHeaderError[]>([])
let statusResetTimer: ReturnType<typeof setTimeout> | null = null

// ── Vue Flow utils ────────────────────────────────────────────────────────────
const { screenToFlowCoordinate, addEdges, fitView, viewport, setViewport } = useVueFlow()

// ── Scroll → zoom toward cursor ───────────────────────────────────────────────
function onCanvasWheel(e: WheelEvent): void {
  if (e.shiftKey || e.ctrlKey) {
    e.preventDefault()
    e.stopPropagation()
    return
  }

  e.preventDefault()
  e.stopPropagation()
  const rect   = (e.currentTarget as HTMLElement).getBoundingClientRect()
  const mouseX = e.clientX - rect.left
  const mouseY = e.clientY - rect.top
  const { x, y, zoom } = viewport.value
  const factor  = e.deltaY < 0 ? 1.12 : 1 / 1.12
  const newZoom = Math.min(2, Math.max(0.3, zoom * factor))
  const scale   = newZoom / zoom
  setViewport({ x: mouseX - (mouseX - x) * scale, y: mouseY - (mouseY - y) * scale, zoom: newZoom })
}

function onCanvasPointerDownCapture(event: PointerEvent): void {
  if (event.button === 0 && event.shiftKey) {
    const target = event.target
    if (!(target instanceof HTMLElement)) return

    const nodeId = getNodeIdFromTarget(target)
    if (nodeId) {
      toggleNodeSelection(nodeId)
      handledShiftClickNodeId.value = nodeId
      contextMenu.value = null
    }
  }

  if (event.button !== 2 || !event.shiftKey) return

  startRightSelection(event, event.pointerId)
}

function onCanvasMouseDownCapture(event: MouseEvent): void {
  const target = event.target
  if (event.button === 2 && event.shiftKey && target instanceof HTMLElement) {
    const nodeId = getNodeIdFromTarget(target)
    if (nodeId && isSelectedGroupNode(nodeId)) {
      event.stopPropagation()
      return
    }
  }

  if (event.button !== 2 || !event.shiftKey) return

  if (!rightSelectionDrag.value) {
    startRightSelection(event, null)
    return
  }

  event.preventDefault()
  event.stopPropagation()
}

function startRightSelection(event: MouseEvent | PointerEvent, pointerId: number | null): void {
  const target = event.target
  if (!(target instanceof HTMLElement)) return

  const nodeId = getNodeIdFromTarget(target)
  if (nodeId && isSelectedGroupNode(nodeId)) {
    event.stopPropagation()
    return
  }

  if (!target.closest('.vue-flow__pane')) return
  if (target.closest('.vue-flow__node, .vue-flow__edge, .vue-flow__controls')) return

  const canvas = event.currentTarget
  if (!(canvas instanceof HTMLElement)) return

  const bounds = canvas.getBoundingClientRect()
  const startX = clamp(event.clientX - bounds.left, 0, bounds.width)
  const startY = clamp(event.clientY - bounds.top, 0, bounds.height)

  stopRightSelectionListeners()
  rightSelectionDrag.value = {
    pointerId,
    startX,
    startY,
  }
  rightSelectionRect.value = { x: startX, y: startY, width: 0, height: 0 }

  window.addEventListener('pointermove', onRightSelectionMove)
  window.addEventListener('pointerup', onRightSelectionUp)
  window.addEventListener('mousemove', onRightSelectionMouseMove)
  window.addEventListener('mouseup', onRightSelectionMouseUp)
  window.addEventListener('contextmenu', onRightSelectionContextMenu, true)
  event.preventDefault()
  event.stopPropagation()
}

function onRightSelectionMove(event: PointerEvent): void {
  const drag = rightSelectionDrag.value
  if (!drag || (drag.pointerId !== null && event.pointerId !== drag.pointerId)) return

  updateRightSelectionRect(event.clientX, event.clientY)
  event.preventDefault()
  event.stopPropagation()
}

function onRightSelectionUp(event: PointerEvent): void {
  const drag = rightSelectionDrag.value
  if (!drag || (drag.pointerId !== null && event.pointerId !== drag.pointerId)) return

  finishRightSelection(event)
}

function onRightSelectionMouseMove(event: MouseEvent): void {
  if (!rightSelectionDrag.value) return

  updateRightSelectionRect(event.clientX, event.clientY)
  event.preventDefault()
  event.stopPropagation()
}

function onRightSelectionMouseUp(event: MouseEvent): void {
  if (!rightSelectionDrag.value || event.button !== 2) return

  finishRightSelection(event)
}

function onRightSelectionContextMenu(event: MouseEvent): void {
  if (!rightSelectionDrag.value) return

  event.preventDefault()
  event.stopPropagation()
}

function finishRightSelection(event: MouseEvent | PointerEvent): void {
  updateRightSelectionRect(event.clientX, event.clientY)
  const rect = rightSelectionRect.value
  if (rect && rect.width < 4 && rect.height < 4) {
    nodes.value = nodes.value.map((node) => ({ ...node, selected: false }))
    selectedNodeId.value = null
  } else {
    selectNodesByRightSelection()
  }
  rightSelectionDrag.value = null
  rightSelectionRect.value = null
  stopRightSelectionListeners()
  event.preventDefault()
  event.stopPropagation()
}

function updateRightSelectionRect(clientX: number, clientY: number): void {
  const drag = rightSelectionDrag.value
  const canvas = getCanvasElement()
  if (!drag || !canvas) return

  const bounds = canvas.getBoundingClientRect()
  const currentX = clamp(clientX - bounds.left, 0, bounds.width)
  const currentY = clamp(clientY - bounds.top, 0, bounds.height)

  rightSelectionRect.value = {
    x: Math.min(drag.startX, currentX),
    y: Math.min(drag.startY, currentY),
    width: Math.abs(currentX - drag.startX),
    height: Math.abs(currentY - drag.startY),
  }
}

function selectNodesByRightSelection(): void {
  const rect = rightSelectionRect.value
  const canvas = getCanvasElement()
  if (!rect || !canvas) return

  if (rect.width < 4 && rect.height < 4) {
    nodes.value = nodes.value.map((node) => ({ ...node, selected: false }))
    selectedNodeId.value = null
    return
  }

  const bounds = canvas.getBoundingClientRect()
  const selection = {
    left: bounds.left + rect.x,
    top: bounds.top + rect.y,
    right: bounds.left + rect.x + rect.width,
    bottom: bounds.top + rect.y + rect.height,
  }
  const selectedIds = new Set<string>()

  for (const element of canvas.querySelectorAll<HTMLElement>('.vue-flow__node[data-id]')) {
    const id = element.dataset.id
    if (!id) continue

    const nodeRect = element.getBoundingClientRect()
    const intersects =
      nodeRect.left <= selection.right &&
      nodeRect.right >= selection.left &&
      nodeRect.top <= selection.bottom &&
      nodeRect.bottom >= selection.top

    if (intersects) selectedIds.add(id)
  }

  nodes.value = nodes.value.map((node) => ({
    ...node,
    selected: selectedIds.has(node.id),
  }))

  selectedNodeId.value = selectedIds.size === 1
    ? [...selectedIds][0] ?? null
    : null

  if (selectedIds.size !== 1) {
    isInspectorOpen.value = false
  }
}

function getCanvasElement(): HTMLElement | null {
  return document.querySelector('.editor__canvas')
}

function getNodeIdFromTarget(target: HTMLElement): string | null {
  return target.closest<HTMLElement>('.vue-flow__node[data-id]')?.dataset.id ?? null
}

function isSelectedGroupNode(nodeId: string): boolean {
  const selected = selectedNodes.value
  return selected.length > 1 && selected.some((node) => node.id === nodeId)
}

function onNodeDragStart(event: NodeDragEvent): void {
  if (isReadOnly.value) return

  nodeDragRouteSnapshot = createNodeDragRouteSnapshot(getNodeDragIds(event))
}

function onNodeDrag(event: NodeDragEvent): void {
  if (isReadOnly.value) return

  nodeDragRouteSnapshot ??= createNodeDragRouteSnapshot(getNodeDragIds(event))
  applyNodeDragRouteSnapshot()
}

function onNodeDragStop(event: NodeDragEvent): void {
  if (isReadOnly.value) return

  nodeDragRouteSnapshot ??= createNodeDragRouteSnapshot(getNodeDragIds(event))
  applyNodeDragRouteSnapshot()
  snapDraggedNodesToGrid(getNodeDragIds(event))
  nodeDragRouteSnapshot = null
}

function snapDraggedNodesToGrid(nodeIds: Set<string>): void {
  if (nodeIds.size === 0) return

  let hasChanged = false
  const nextNodes = nodes.value.map((node) => {
    if (!nodeIds.has(node.id)) return node

    const nextPosition = snapEditorPosition(node.position)
    if (nextPosition.x === node.position.x && nextPosition.y === node.position.y) return node

    hasChanged = true
    return {
      ...node,
      position: nextPosition,
    }
  })

  if (hasChanged) {
    nodes.value = nextNodes
  }
}

function getNodeDragIds(event: NodeDragEvent): Set<string> {
  const ids = new Set<string>()

  for (const node of event.nodes ?? []) {
    if (node.id) ids.add(node.id)
  }

  if (event.node?.id) ids.add(event.node.id)

  const selectedIds = selectedNodes.value.map(node => node.id)
  if (selectedIds.length > 1 && event.node?.id && selectedIds.includes(event.node.id)) {
    selectedIds.forEach(nodeId => ids.add(nodeId))
  }

  return ids
}

function createNodeDragRouteSnapshot(nodeIds: Set<string>): NodeDragRouteSnapshot {
  const nodePositions = new Map<string, NodePositionSnapshot>()
  for (const node of nodes.value) {
    if (nodeIds.has(node.id)) {
      nodePositions.set(node.id, { ...node.position })
    }
  }

  const edgeRoutes = new Map<string, GraphConnectionRoutePoint[]>()
  for (const edge of edges.value) {
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) continue

    const route = getSavedEdgeRoute(edge)
    if (route.length) {
      edgeRoutes.set(edge.id, route)
    }
  }

  return { nodeIds, nodePositions, edgeRoutes }
}

function applyNodeDragRouteSnapshot(): void {
  const snapshot = nodeDragRouteSnapshot
  if (!snapshot || snapshot.edgeRoutes.size === 0) return

  const delta = getNodeDragSnapshotDelta(snapshot)
  if (!delta) return

  let hasChanged = false
  const nextEdges = edges.value.map((edge) => {
    const route = snapshot.edgeRoutes.get(edge.id)
    if (!route) return edge

    hasChanged = true
    return withEdgeRoute(edge, translateRoute(route, delta))
  })

  if (hasChanged) {
    edges.value = nextEdges
  }
}

function getNodeDragSnapshotDelta(
  snapshot: NodeDragRouteSnapshot,
): NodePositionSnapshot | null {
  let dx = 0
  let dy = 0
  let count = 0

  for (const nodeId of snapshot.nodeIds) {
    const start = snapshot.nodePositions.get(nodeId)
    const current = nodes.value.find(node => node.id === nodeId)?.position
    if (!start || !current) continue

    dx += current.x - start.x
    dy += current.y - start.y
    count += 1
  }

  if (count === 0) return null

  const delta = {
    x: snapEditorGrid(dx / count),
    y: snapEditorGrid(dy / count),
  }

  return Math.abs(delta.x) < 1 && Math.abs(delta.y) < 1 ? null : delta
}

function translateRoute(
  route: GraphConnectionRoutePoint[],
  delta: NodePositionSnapshot,
): GraphConnectionRoutePoint[] {
  return route.map(point => ({
    x: snapEditorGrid(point.x + delta.x),
    y: snapEditorGrid(point.y + delta.y),
  }))
}

function withEdgeRoute(edge: Edge, route: GraphConnectionRoutePoint[]): Edge {
  return {
    ...edge,
    data: {
      ...(isRecord(edge.data) ? edge.data : {}),
      route,
      routeVersion: 2,
    },
  }
}

function stopRightSelectionListeners(): void {
  window.removeEventListener('pointermove', onRightSelectionMove)
  window.removeEventListener('pointerup', onRightSelectionUp)
  window.removeEventListener('mousemove', onRightSelectionMouseMove)
  window.removeEventListener('mouseup', onRightSelectionMouseUp)
  window.removeEventListener('contextmenu', onRightSelectionContextMenu, true)
}

function clamp(value: number, min: number, max: number): number {
  if (min > max) return value
  return Math.min(max, Math.max(min, value))
}

function getCatalogParams(type: string) {
  if (type === GROUP_NODE_TYPE) {
    return GROUP_CATALOG_ITEM.schema ?? []
  }

  return getUiParams(catalog.value, type)
    .filter(param => param.key !== BUTTON_BRANCHING_NODE_IDS_PARAM)
}

/**
 * Подготовка catalog для sidebar.
 * Категории больше не рисуем, но их метаданные нужны для цвета и иконки узла.
 * Узлы которых нет в NODE_META — игнорируются (пользователь обновит мету
 * когда захочет их добавить в UI).
 */
const filteredCategories = computed(() => {
  return buildEditorCatalogCategories(catalog.value)
})

// ── Lifecycle ─────────────────────────────────────────────────────────────────
onMounted(async () => {
  window.addEventListener('keydown', onEditorKeyDown, true)
  await loadProjectPlatformType()
  await Promise.all([loadCatalog(), loadProjectSchemas(), loadScenarioGraph()])
})

watch(
  () => [route.params.id, route.params.sid],
  () => {
    isEditingScenarioCopy.value = isReleaseCopyMode.value
    void loadProjectPlatformType().then(loadCatalog)
    void loadProjectSchemas()
    void loadScenarioGraph()
  },
)

watch(
  () => createHistorySignature(),
  (signature) => {
    if (isApplyingHistory || !signature || signature === currentHistorySignature) return

    scheduleHistoryCommit(signature)
  },
  { flush: 'post' },
)

onUnmounted(() => {
  stopRightSelectionListeners()
  window.removeEventListener('keydown', onEditorKeyDown, true)
  clearHistoryCommitTimer()
  if (statusResetTimer) {
    clearTimeout(statusResetTimer)
  }
})

// ── API ───────────────────────────────────────────────────────────────────────
async function loadCatalog(): Promise<void> {
  catalogLoading.value = true
  try {
    const res = await nodesApi.getCatalog(projectPlatformType.value)
    catalog.value = withEditorCatalogItems(res.nodes ?? [])
  } catch {
    catalog.value = withEditorCatalogItems([])
  } finally {
    catalogLoading.value = false
  }
}

async function loadProjectPlatformType(): Promise<void> {
  try {
    const project = await projectsApi.get(projectId.value)
    projectPlatformType.value = normalizeProjectPlatform(project.platformType)
  } catch {
    projectPlatformType.value = 'PLATFORM_TYPE_VK'
  }
}

function normalizeProjectPlatform(platformType?: PlatformType): PlatformType {
  if (!platformType || platformType === 'PLATFORM_TYPE_UNSPECIFIED') {
    return 'PLATFORM_TYPE_UNIVERSAL'
  }

  return platformType
}

async function loadProjectSchemas(): Promise<void> {
  schemasLoading.value = true
  try {
    const response = await entitySchemasApi.list(projectId.value)
    const items = response.items ?? []
    const details = await Promise.all(
      items
        .filter(item => Boolean(item.schemaId))
        .map(item => entitySchemasApi.get(item.schemaId!)),
    )
    projectSchemaDetails.value = details
  } catch {
    projectSchemaDetails.value = []
  } finally {
    schemasLoading.value = false
  }
}

async function loadScenarioGraph(): Promise<void> {
  clearGraph()
  isEditingScenarioCopy.value = isReleaseCopyMode.value

  if (scenarioId.value === 'draft') {
    await loadDraft()
    resetEditorHistory()
    return
  }

  if (scenarioId.value === 'release' || isReleaseCopyMode.value) {
    await loadRelease()
    resetEditorHistory()
    return
  }

  const version = scenarioVersion.value
  if (version != null) {
    await loadVersion(version)
  }
  resetEditorHistory()
}

function enableReleaseCopyEditing(): void {
  if (!isReadOnly.value || scenarioId.value === 'draft') return

  isEditingScenarioCopy.value = true
  resetEditorHistory()
  setEditorStatus('idle')
}

async function loadDraft(): Promise<void> {
  try {
    const draft = await scenarioApi.getDraft(projectId.value)
    if (draft.graphJson) {
      graphData.value = JSON.parse(draft.graphJson) as GraphData
      applyGraphData(graphData.value)
    }
  } catch { /* нет черновика */ }
}

async function loadRelease(): Promise<void> {
  try {
    const release = await scenarioApi.getRelease(projectId.value)
    if (release.graphJson) {
      graphData.value = JSON.parse(release.graphJson) as GraphData
      applyGraphData(graphData.value)
    }
  } catch { /* нет релиза */ }
}

async function loadVersion(version: number): Promise<void> {
  try {
    const scenario = await scenarioApi.getByVersion(projectId.value, version)
    if (scenario.graphJson) {
      graphData.value = JSON.parse(scenario.graphJson) as GraphData
      applyGraphData(graphData.value)
    }
  } catch { /* версия недоступна */ }
}

function clearGraph(): void {
  activeGroupSession.value = null
  nodeDragRouteSnapshot = null
  graphData.value = null
  nodes.value = []
  edges.value = []
  selectedNodeId.value = null
  contextMenu.value = null
  messagePickerState.value = null
  rightSelectionRect.value = null
}

function applyGraphData(data: GraphData): void {
  nodeDragRouteSnapshot = null
  const editorData = normalizeEditorScenarioGroups(runtimeToEditorScenario(data))
  graphData.value = editorData
  const shouldAutoLayout = editorData.nodes.some(node => !node.position)

  const flow = editorScenarioToFlow(editorData, {
    snapPosition: snapEditorPosition,
    fallbackPositionForNode: (_node, index) => ({
      x: AUTO_LAYOUT_LEFT + (index % 6) * AUTO_LAYOUT_COLUMN_GAP,
      y: AUTO_LAYOUT_TOP + Math.floor(index / 6) * AUTO_LAYOUT_ROW_GAP,
    }),
  })
  nodes.value = flow.nodes
  edges.value = flow.edges

  if (shouldAutoLayout && nodes.value.length > 1) {
    void applyAutoLayoutToCurrentGraph(editorData.startNodeId)
  }
}

function buildGraphJson(): string {
  const source = getSerializationSource()
  const data = editorToRuntimeScenario({
    nodes: source.nodes,
    edges: source.edges,
    visualStartNodeId: source.startNodeId,
    snapPosition: snapEditorPosition,
    normalizeParamsForNode,
    normalizeDataNodeParams,
    resolveEditMessageIdVariable,
    getSavedEdgeRoute,
  })
  return JSON.stringify(data)
}

function normalizeEditorScenarioGroups(data: GraphData): GraphData {
  return {
    ...data,
    nodes: data.nodes.map(node =>
      node.type === GROUP_NODE_TYPE
        ? {
            ...node,
            params: normalizeAuthoringNodeParams(node.type, node.params),
          }
        : node,
    ),
  }
}

function buildAuthoringJson(): string {
  const source = getSerializationSource()
  const data = {
    format: 'mazy-editor-scenario',
    formatVersion: 1,
    startNodeId: source.startNodeId,
    nodes: source.nodes.map(node => ({
      id: node.id,
      type: node.data.type,
      params: stripAuthoringParams(normalizeAuthoringNodeParams(node.data.type, node.data.params ?? {})),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
      position: snapEditorPosition(node.position),
    })),
    connections: source.edges.map(edge => ({
      from: edge.source,
      to: edge.target,
      ...(edge.sourceHandle ? { branch: edge.sourceHandle } : {}),
      ...(() => {
        const route = getSavedEdgeRoute(edge)
        return route.length ? { route, routeVersion: 2 } : {}
      })(),
    })),
  }

  return JSON.stringify(data)
}

function getSerializationSource(): { nodes: EditorFlowNode[]; edges: Edge[]; startNodeId: string } {
  if (!activeGroupSession.value) {
    return {
      nodes: nodes.value,
      edges: edges.value,
      startNodeId: graphData.value?.startNodeId ?? (nodes.value[0]?.id ?? ''),
    }
  }

  const root = buildRootStateWithCurrentGroup(activeGroupSession.value)
  return {
    nodes: root.nodes,
    edges: root.edges,
    startNodeId: activeGroupSession.value.rootGraphData?.startNodeId ?? (root.nodes[0]?.id ?? ''),
  }
}

function buildAuthoringSchemaJson(): string {
  const categories = filteredCategories.value
  const categoryByType = new Map<string, string>()
  const visibleNodeTypes = new Set<string>()

  for (const category of categories) {
    for (const item of category.nodes) {
      if (!item.type) continue
      visibleNodeTypes.add(item.type)
      categoryByType.set(item.type, category.label)
    }
  }

  const data = {
    format: 'mazy-editor-node-schema',
    formatVersion: 1,
    scenarioFormat: 'mazy-editor-scenario',
    scenarioShape: {
      startNodeId: 'string',
      nodes: [
        {
          id: 'string',
          type: 'one of nodes[].type',
          params: 'object matching the selected node params',
          position: { x: 'number', y: 'number' },
        },
      ],
      connections: [
        {
          from: 'source node id',
          to: 'target node id',
          branch: 'optional output port id; omit for default output',
        },
      ],
    },
    nodes: [...visibleNodeTypes].map(type => {
      const definition = EDITOR_NODE_DEFINITIONS[type]
      const defaultParams = stripAuthoringParams(createDefaultParamsForNode(type, `schema-${type}`))

      return {
        type,
        label: getNodeLabel(type),
        category: categoryByType.get(type) ?? null,
        runtimeTypes: definition?.runtimeTypes ?? [type],
        params: getCatalogParams(type).map(param => cloneParams(param as unknown as Record<string, unknown>)),
        defaultParams,
        outputs: describeAuthoringNodeOutputs(type, defaultParams),
      }
    }),
    dataSchemas: projectSchemaDetails.value,
  }

  return JSON.stringify(data)
}

function stripAuthoringParams(params: Record<string, unknown>): Record<string, unknown> {
  const next = cloneParams(params)
  delete next[BUTTON_BRANCHING_NODE_IDS_PARAM]
  return next
}

function normalizeAuthoringNodeParams(nodeType: string, params: Record<string, unknown>): Record<string, unknown> {
  if (nodeType !== GROUP_NODE_TYPE) return params

  const subgraph = readEditorSubgraph(params.subgraph)
  if (!subgraph || subgraph.nodes.length === 0) return params

  const boundaryNodeIds = resolveGroupBoundaryNodeIds(
    subgraph,
    readString(params.entryNodeId),
    readString(params.exitNodeId),
  )

  return {
    ...params,
    nodeCount: subgraph.nodes.length,
    entryNodeId: boundaryNodeIds.entryNodeId,
    exitNodeId: boundaryNodeIds.exitNodeId,
    subgraph: {
      ...subgraph,
      startNodeId: boundaryNodeIds.entryNodeId,
    },
  }
}

function describeAuthoringNodeOutputs(type: string, params: Record<string, unknown>) {
  if (isSmartButtonBranchingNodeType(type)) {
    return {
      kind: 'dynamic',
      source: 'params.buttons[][].payload',
      labelSource: 'params.buttons[][].label',
      note: 'Create one connection per button branch. Buttons are stored as rows, top-to-bottom and left-to-right.',
    }
  }

  const ports = getNodeOutputPorts(type, params)
  if (ports.length > 0) {
    return {
      kind: 'fixed',
      ports,
    }
  }

  return {
    kind: 'default',
    note: 'Use a connection without branch.',
  }
}

function resolveEditMessageIdVariable(node: EditorFlowNode): string {
  const targetNodeId = readString(node.data.params.targetMessageNodeId)
  const targetNode = targetNodeId
    ? nodes.value.find(item => item.id === targetNodeId)
    : null

  if (targetNode && canProvideMessageId(targetNode.data.type)) {
    return ensureMessageIdVariable(targetNode)
  }

  return readString(node.data.params.messageIdVariable) ?? ''
}

function createHistorySignature(): string {
  try {
    return JSON.stringify(captureGraphSnapshot())
  } catch {
    return ''
  }
}

function captureGraphSnapshot(): GraphData {
  return cloneGraphData(JSON.parse(buildGraphJson()) as GraphData)
}

function resetEditorHistory(): void {
  clearHistoryCommitTimer()

  const graph = captureGraphSnapshot()
  const signature = JSON.stringify(graph)
  undoStack.value = [{ graph, signature }]
  redoStack.value = []
  pendingHistorySignature.value = null
  currentHistorySignature = signature
}

function scheduleHistoryCommit(signature: string): void {
  if (isReadOnly.value) return

  pendingHistorySignature.value = signature
  clearHistoryCommitTimer()
  historyCommitTimer = setTimeout(() => {
    commitPendingHistory()
  }, 250)
}

function commitPendingHistory(): void {
  if (!pendingHistorySignature.value || isApplyingHistory) return

  clearHistoryCommitTimer()
  const graph = captureGraphSnapshot()
  const signature = JSON.stringify(graph)
  pendingHistorySignature.value = null

  if (!signature || signature === currentHistorySignature) return

  undoStack.value = [...undoStack.value, { graph, signature }].slice(-80)
  redoStack.value = []
  currentHistorySignature = signature
}

function clearHistoryCommitTimer(): void {
  if (!historyCommitTimer) return

  clearTimeout(historyCommitTimer)
  historyCommitTimer = null
}

function undoGraphChange(): void {
  if (isReadOnly.value) return

  commitPendingHistory()
  if (undoStack.value.length <= 1) return

  const nextUndoStack = [...undoStack.value]
  const current = nextUndoStack.pop()
  const target = nextUndoStack.at(-1)
  if (!current || !target) return

  undoStack.value = nextUndoStack
  redoStack.value = [...redoStack.value, current]
  applyHistoryEntry(target)
}

function redoGraphChange(): void {
  if (isReadOnly.value) return

  commitPendingHistory()
  const target = redoStack.value.at(-1)
  if (!target) return

  redoStack.value = redoStack.value.slice(0, -1)
  undoStack.value = [...undoStack.value, target]
  applyHistoryEntry(target)
}

function applyHistoryEntry(entry: EditorHistoryEntry): void {
  isApplyingHistory = true
  clearHistoryCommitTimer()
  pendingHistorySignature.value = null
  applyGraphData(cloneGraphData(entry.graph))
  selectedNodeId.value = null
  isInspectorOpen.value = false
  contextMenu.value = null
  currentHistorySignature = entry.signature
  isApplyingHistory = false
}

function cloneGraphData(graph: GraphData): GraphData {
  return JSON.parse(JSON.stringify(graph)) as GraphData
}

function setEditorStatus(
  status: typeof saveStatus.value,
  message = '',
  resetMs = 3000,
): void {
  if (statusResetTimer) {
    clearTimeout(statusResetTimer)
    statusResetTimer = null
  }

  saveStatus.value = status
  statusMessage.value = message

  if (status !== 'idle' && resetMs > 0) {
    statusResetTimer = setTimeout(() => {
      saveStatus.value = 'idle'
      statusMessage.value = ''
      statusResetTimer = null
    }, resetMs)
  }
}

function formatScenarioNodeErrorMeta(error: ScenarioNodeHeaderError): string {
  return [
    error.nodeId ? `Узел: ${error.nodeId}` : '',
    error.path ? `Поле: ${error.path}` : '',
    error.code ? `Код: ${error.code}` : '',
  ].filter(Boolean).join(' · ')
}

function canSelectScenarioNodeError(error: ScenarioNodeHeaderError): boolean {
  return Boolean(error.nodeId && nodes.value.some(node => node.id === error.nodeId))
}

function selectScenarioNodeError(error: ScenarioNodeHeaderError): void {
  if (!error.nodeId || !canSelectScenarioNodeError(error)) return

  selectNode(error.nodeId)
  contextMenu.value = null
}

function mapScenarioValidationErrors(errors: ScenarioValidationErrorItem[] | undefined): ScenarioNodeHeaderError[] {
  return (errors ?? [])
    .map(error => ({
      code: error.code ?? '',
      message: error.message || error.code || 'Ошибка сценария',
      nodeId: error.nodeId || undefined,
      path: error.path || undefined,
    }))
    .filter(error => error.message)
}

async function saveDraft(): Promise<boolean> {
  if (isReadOnly.value) return false
  scenarioNodeErrors.value = []
  if (!validateScenarioBeforeSerialization()) return false

  saving.value = true
  setEditorStatus('idle')
  try {
    await scenarioApi.saveDraft(projectId.value, { projectId: projectId.value, graphJson: buildGraphJson() })
    scenarioNodeErrors.value = []
    setEditorStatus('saved')
    return true
  } catch (error) {
    const parsedError = parseScenarioError(error)
    scenarioNodeErrors.value = parsedError.nodeErrors
    setEditorStatus('error', parsedError.nodeErrors.length ? 'Есть ошибки в сценарии' : (parsedError.messages[0] ?? 'Ошибка сохранения'), 0)
    return false
  } finally {
    saving.value = false
  }
}

async function validateDraft(): Promise<void> {
  if (isReadOnly.value || checkingDraft.value || saving.value) return

  const saved = await saveDraft()
  if (!saved) return

  checkingDraft.value = true
  setEditorStatus('idle')

  try {
    const result = await scenarioApi.validateDraft(projectId.value)
    const errors = mapScenarioValidationErrors(result.errors)
    const validationErrors = errors.length > 0 || result.isValid !== false
      ? errors
      : [{ code: '', message: 'Сценарий содержит ошибки валидации' }]

    scenarioNodeErrors.value = validationErrors

    if ((result.isValid ?? validationErrors.length === 0) && validationErrors.length === 0) {
      setEditorStatus('validated', 'Ошибок не найдено')
      return
    }

    setEditorStatus('error', 'Есть ошибки в сценарии', 0)
  } catch (error) {
    const parsedError = parseScenarioError(error)
    scenarioNodeErrors.value = parsedError.nodeErrors
    setEditorStatus('error', parsedError.nodeErrors.length ? 'Есть ошибки в сценарии' : (parsedError.messages[0] ?? 'Ошибка проверки'), 0)
  } finally {
    checkingDraft.value = false
  }
}

async function importScenarioJson(event: Event): Promise<void> {
  if (isReadOnly.value) return

  const input = event.target as HTMLInputElement | null
  const file = input?.files?.[0]
  if (!file) return

  try {
    const graph = parseImportedScenario(await file.text())
    clearGraph()
    applyGraphData(graph)
    resetEditorHistory()
    setEditorStatus('imported')
  } catch {
    setEditorStatus('error', 'Ошибка импорта JSON')
  } finally {
    if (input) input.value = ''
  }
}

function exportScenarioJson(): void {
  if (!validateScenarioBeforeSerialization()) return

  try {
    const graph = JSON.parse(buildAuthoringJson()) as unknown
    downloadJsonFile(graph, createExportFileName('authoring'))
    setEditorStatus('exported', 'JSON скачан')
  } catch {
    setEditorStatus('error', 'Ошибка экспорта JSON')
  }
}

function exportAuthoringSchemaJson(): void {
  try {
    const schema = JSON.parse(buildAuthoringSchemaJson()) as unknown
    downloadJsonFile(schema, createSchemaExportFileName())
    setEditorStatus('exported', 'Схема скачана')
  } catch {
    setEditorStatus('error', 'Ошибка экспорта схемы')
  }
}

function downloadJsonFile(data: unknown, fileName: string): void {
  const blob = new Blob([`${JSON.stringify(data, null, 2)}\n`], {
    type: 'application/json;charset=utf-8',
  })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

function parseImportedScenario(text: string): GraphData {
  const parsed = JSON.parse(text) as unknown
  return isAuthoringScenario(parsed)
    ? normalizeAuthoringScenario(parsed)
    : normalizeRuntimeScenario(parsed)
}

function snapEditorPosition(position: { x: number; y: number }): { x: number; y: number } {
  return {
    x: snapEditorGrid(position.x),
    y: snapEditorGrid(position.y),
  }
}

function snapEditorGrid(value: number): number {
  return Math.round(value / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE
}

function normalizeConnectionRoute(value: unknown): GraphConnectionRoutePoint[] {
  if (!Array.isArray(value)) return []

  return value
    .filter((point): point is GraphConnectionRoutePoint =>
      isRecord(point)
      && typeof point.x === 'number'
      && Number.isFinite(point.x)
      && typeof point.y === 'number'
      && Number.isFinite(point.y),
    )
    .map(point => ({
      x: snapEditorGrid(point.x),
      y: snapEditorGrid(point.y),
    }))
}

function getSavedEdgeRoute(edge: Edge): GraphConnectionRoutePoint[] {
  if (!isRecord(edge.data) || !Array.isArray(edge.data.route)) return []

  return normalizeConnectionRoute(edge.data.route)
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function backendNodeTypeFromDataAction(actionValue: unknown): string {
  switch (actionValue) {
    case 'get':
      return 'get_record'
    case 'query':
      return 'query_records'
    case 'update':
      return 'update_record'
    case 'delete':
      return 'delete_record'
    case 'create':
    default:
      return 'create_record'
  }
}

function dataActionFromBackendNodeType(nodeType: string): string {
  switch (nodeType) {
    case 'get_record':
      return 'get'
    case 'query_records':
      return 'query'
    case 'update_record':
      return 'update'
    case 'delete_record':
      return 'delete'
    case 'create_record':
    default:
      return 'create'
  }
}

function createExportFileName(kind: 'authoring' | 'runtime' = 'runtime'): string {
  const mode = scenarioId.value === 'draft'
    ? 'draft'
    : scenarioId.value === 'release'
      ? 'release'
      : `v${scenarioVersion.value ?? scenarioId.value}`
  const date = new Date().toISOString().slice(0, 10)
  return `scenario-${projectId.value}-${mode}-${kind}-${date}.json`
}

function createSchemaExportFileName(): string {
  const date = new Date().toISOString().slice(0, 10)
  return `scenario-schema-${projectId.value}-${date}.json`
}

// ── Drag & Drop ───────────────────────────────────────────────────────────────
const dragType = ref<string | null>(null)

function onDragStart(e: DragEvent, type: string): void {
  if (isReadOnly.value) return
  dragType.value = type
  e.dataTransfer!.effectAllowed = 'move'
}

function onDrop(e: DragEvent): void {
  if (isReadOnly.value) return
  if (!dragType.value) return
  const canvasEl = document.querySelector('.editor__canvas') as HTMLElement
  const bounds = canvasEl.getBoundingClientRect()
  const pos = screenToFlowCoordinate({ x: e.clientX - bounds.left, y: e.clientY - bounds.top })
  addNode(dragType.value, pos)
  dragType.value = null
}

function addNode(type: string, position: { x: number; y: number }): void {
  if (isReadOnly.value) return
  const id = createGraphId()
  nodes.value = [...nodes.value, {
    id,
    type: 'default',
    position: snapEditorPosition(position),
    data: { type, params: createDefaultParamsForNode(type, id) },
  }]
  selectedNodeId.value = id
  isInspectorOpen.value = true
  if (!graphData.value) {
    graphData.value = { startNodeId: id, nodes: [], connections: [] }
  }
}

function addNodeFromMenu(type: string): void {
  if (!type || !contextMenu.value || contextMenu.value.type !== 'pane') return

  addNode(type, contextMenu.value.position)
  contextMenu.value = null
}

function createDefaultParamsForNode(type: string, nodeId: string): Record<string, unknown> {
  if (type === DATA_NODE_TYPE) {
    return {
      action: 'create',
      entityName: '',
      fields: {},
      recordIdVariable: `record_${shortNodeId(nodeId)}_id`,
    }
  }

  if (type === MESSAGE_NODE_TYPE || type === BUTTON_BRANCHING_NODE_TYPE) {
    return {
      text: '',
      messageIdVariable: `message_${shortNodeId(nodeId)}_id`,
      buttonPayloadVariable: createButtonPayloadVariable(nodeId),
      buttons: [],
      [BUTTON_BRANCHING_NODE_IDS_PARAM]: createButtonBranchingCompiledNodeIds(nodeId),
    }
  }

  if (type === EDIT_MESSAGE_NODE_TYPE) {
    return {
      newText: '',
      buttonPayloadVariable: createButtonPayloadVariable(nodeId),
      buttons: [],
      [BUTTON_BRANCHING_NODE_IDS_PARAM]: createButtonBranchingCompiledNodeIds(nodeId),
    }
  }

  if (type === 'delete_message') {
    return {
      messageIdVariable: '',
    }
  }

  if (type === GOTO_NODE_TYPE) {
    return {
      targetNodeId: '',
    }
  }

  if (type === 'receive_message') {
    return {
      messageTextVariable: `message_${shortNodeId(nodeId)}_text`,
      validatorType: '',
    }
  }

  if (type === 'receive_image') {
    return {
      imageUrlVariable: `image_${shortNodeId(nodeId)}_url`,
      captionVariable: `image_${shortNodeId(nodeId)}_caption`,
    }
  }

  if (type === 'send_image') {
    return {
      imageUrl: '',
      caption: '',
      messageIdVariable: `message_${shortNodeId(nodeId)}_id`,
    }
  }

  if (type === 'condition') {
    return {
      leftOperand: '',
      operator: '==',
      rightOperand: '',
    }
  }

  if (type === 'set_variable') {
    return {
      variable: '',
      value: '',
    }
  }

  if (type === 'delay') {
    return {
      seconds: 0,
    }
  }

  if (type === 'get_user_info') {
    return {
      prefix: 'user',
    }
  }

  if (type === 'http_request') {
    return {
      url: '',
      method: 'GET',
      responseBodyVariable: `http_${shortNodeId(nodeId)}_body`,
      body: '',
      bodyType: 'json',
      headers: {},
      responseStatusVariable: '',
    }
  }

  return {}
}

function deleteNode(id: string): void {
  if (isReadOnly.value) return
  const removedIds = new Set([id])
  nodes.value = nodes.value.filter(n => n.id !== id)
  clearGotoTargetsForRemovedNodes(removedIds)
  const nextEdges = [] as Edge[]
  for (const edge of edges.value as Edge[]) {
    if (edge.source !== id && edge.target !== id) {
      nextEdges.push(edge as Edge)
    }
  }
  edges.value = nextEdges
  if (graphData.value?.startNodeId === id) {
    graphData.value = { ...graphData.value, startNodeId: nodes.value[0]?.id ?? '' }
  }
  if (selectedNodeId.value === id) {
    selectedNodeId.value = null
  }
  if (messagePickerState.value?.targetNodeId === id) {
    messagePickerState.value = null
  }
}

function setStartNode(id: string): void {
  if (isReadOnly.value) return
  if (graphData.value) {
    graphData.value = { ...graphData.value, startNodeId: id }
  }
}

function setContextNodeStart(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'node') return
  setStartNode(contextMenu.value.nodeId)
  contextMenu.value = null
}

function openContextGroup(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'node') return
  openGroupNode(contextMenu.value.nodeId)
}

function copyContextNode(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'node') return
  copyNodesByIds([contextMenu.value.nodeId])
  contextMenu.value = null
}

function deleteContextNode(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'node') return
  deleteNodesByIds([contextMenu.value.nodeId])
}

function copySelectedNodes(): void {
  const ids = selectedNodes.value.map((node) => node.id)
  copyNodesByIds(ids)
  contextMenu.value = null
}

function deleteSelectedNodes(): void {
  const ids = selectedNodes.value.map((node) => node.id)
  deleteNodesByIds(ids)
}

function groupSelectedNodes(): void {
  if (isReadOnly.value) return

  const selected = selectedNodes.value
  if (selected.length < 2) {
    setEditorStatus('error', 'Выберите минимум два узла')
    return
  }

  if (selected.some(node => node.data.type === GROUP_NODE_TYPE)) {
    setEditorStatus('error', 'Группы нельзя вкладывать друг в друга')
    return
  }

  const selectedIds = new Set(selected.map(node => node.id))
  const internalEdges = edges.value.filter(edge => selectedIds.has(edge.source) && selectedIds.has(edge.target))
  const incomingEdges = edges.value.filter(edge => !selectedIds.has(edge.source) && selectedIds.has(edge.target))
  const outgoingEdges = edges.value.filter(edge => selectedIds.has(edge.source) && !selectedIds.has(edge.target))

  if (!isGroupSelectionConnected(selectedIds, internalEdges)) {
    setEditorStatus('error', 'Группа должна быть цельным фрагментом сценария')
    return
  }

  if (hasExternalTransitBetweenSelectedNodes(selectedIds, edges.value)) {
    setEditorStatus('error', 'Нельзя группировать узлы, если между ними есть невыбранный узел')
    return
  }

  if (hasGotoOutsideNodeSet(selected, selectedIds)) {
    setEditorStatus('error', 'Переход внутри группы должен указывать на узел этой группы')
    return
  }

  const entryNodeId = resolveGroupEntryNodeId(selected, internalEdges, incomingEdges)
  const exitNodeId = resolveGroupExitNodeId(selected, internalEdges, outgoingEdges)
  if (!entryNodeId || !exitNodeId) {
    setEditorStatus('error', 'У группы должен быть один вход и один выход')
    return
  }

  if (incomingEdges.some(edge => edge.target !== entryNodeId) || outgoingEdges.some(edge => edge.source !== exitNodeId)) {
    setEditorStatus('error', 'Все внешние связи группы должны идти через общий вход и выход')
    return
  }

  const groupId = createGraphId()
  const groupPosition = calculateGroupPosition(selected)
  const subgraph: GraphData = {
    startNodeId: entryNodeId,
    nodes: selected.map(node => ({
      id: node.id,
      type: node.data.type,
      params: cloneParams(node.data.params),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
      position: snapEditorPosition(node.position),
    })),
    connections: internalEdges.map(edgeToRuntimeConnection),
  }

  const nextEdges: Edge[] = []
  for (const edge of edges.value) {
    const sourceInside = selectedIds.has(edge.source)
    const targetInside = selectedIds.has(edge.target)
    if (sourceInside && targetInside) continue

    if (!sourceInside && targetInside) {
      nextEdges.push(createFlowEdge(edge.source, groupId, edge.sourceHandle, edge.targetHandle))
      continue
    }

    if (sourceInside && !targetInside) {
      nextEdges.push(createFlowEdge(groupId, edge.target, null, edge.targetHandle))
      continue
    }

    nextEdges.push(edge)
  }

  nodes.value = [
    ...nodes.value.filter(node => !selectedIds.has(node.id)).map(node => ({ ...node, selected: false })),
    {
      id: groupId,
      type: 'default',
      position: groupPosition,
      data: {
        type: GROUP_NODE_TYPE,
        params: {
          title: 'Группа',
          nodeCount: selected.length,
          entryNodeId,
          exitNodeId,
          subgraph,
        },
      },
      selected: true,
    },
  ]
  edges.value = nextEdges

  if (graphData.value && selectedIds.has(graphData.value.startNodeId)) {
    graphData.value = { ...graphData.value, startNodeId: groupId }
  }

  selectedNodeId.value = groupId
  isInspectorOpen.value = true
  contextMenu.value = null
}

function pasteFromMenu(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'pane') return
  pasteClipboard(contextMenu.value.position)
  contextMenu.value = null
}

function deleteContextEdge(): void {
  if (!contextMenu.value || contextMenu.value.type !== 'edge') return
  deleteEdge(contextMenu.value.edgeId)
}

function openGroupNode(groupId: string): void {
  if (activeGroupSession.value) return

  const groupNode = nodes.value.find(node => node.id === groupId)
  if (!groupNode || groupNode.data.type !== GROUP_NODE_TYPE) return

  const subgraph = readEditorSubgraph(groupNode.data.params.subgraph)
  if (!subgraph || subgraph.nodes.length === 0) {
    setEditorStatus('error', 'Группа пустая')
    return
  }

  activeGroupSession.value = {
    groupId,
    title: readString(groupNode.data.params.title) ?? 'Группа',
    rootNodes: cloneFlowNodes(nodes.value),
    rootEdges: cloneFlowEdges(edges.value),
    rootGraphData: graphData.value ? cloneGraphData(graphData.value) : null,
  }

  const boundaryNodeIds = resolveGroupBoundaryNodeIds(
    subgraph,
    readString(groupNode.data.params.entryNodeId),
    readString(groupNode.data.params.exitNodeId),
  )

  applyEditorScenarioToCanvas(subgraph)
  addGroupBoundaryMarkers(
    boundaryNodeIds.entryNodeId,
    boundaryNodeIds.exitNodeId,
    readGroupBoundaryPositions(groupNode.data.params.boundaryPositions),
  )
  selectedNodeId.value = null
  isInspectorOpen.value = false
  contextMenu.value = null
}

function exitActiveGroup(): void {
  const session = activeGroupSession.value
  if (!session) return

  const root = buildRootStateWithCurrentGroup(session)
  activeGroupSession.value = null
  nodes.value = root.nodes
  edges.value = root.edges
  graphData.value = session.rootGraphData
  selectedNodeId.value = session.groupId
  isInspectorOpen.value = true
  contextMenu.value = null
  resetEditorHistory()
}

function buildRootStateWithCurrentGroup(session: GroupEditSession): { nodes: EditorFlowNode[]; edges: Edge[] } {
  const subgraph = captureCurrentEditorScenario()
  const groupNode = session.rootNodes.find(node => node.id === session.groupId)
  if (!groupNode) {
    return {
      nodes: cloneFlowNodes(session.rootNodes),
      edges: cloneFlowEdges(session.rootEdges),
    }
  }

  const title = readString(groupNode.data.params.title) ?? session.title
  const boundaryNodeIds = captureGroupBoundaryNodeIds()
  const resolvedBoundaryNodeIds = resolveGroupBoundaryNodeIds(
    subgraph,
    boundaryNodeIds.entryNodeId ?? readString(groupNode.data.params.entryNodeId),
    boundaryNodeIds.exitNodeId ?? readString(groupNode.data.params.exitNodeId),
  )
  const entryNodeId = resolvedBoundaryNodeIds.entryNodeId
  const exitNodeId = resolvedBoundaryNodeIds.exitNodeId
  const nextSubgraph = {
    ...subgraph,
    startNodeId: entryNodeId,
  }
  const boundaryPositions = captureGroupBoundaryPositions()

  return {
    nodes: session.rootNodes.map(node => {
      if (node.id !== session.groupId) return cloneFlowNode(node)

      return {
        ...cloneFlowNode(node),
        data: {
          type: GROUP_NODE_TYPE,
          params: {
            ...cloneParams(node.data.params),
            title,
            nodeCount: nextSubgraph.nodes.length,
            boundaryPositions,
            entryNodeId,
            exitNodeId,
            subgraph: nextSubgraph,
          },
        },
      }
    }),
    edges: cloneFlowEdges(session.rootEdges),
  }
}

function captureCurrentEditorScenario(): GraphData {
  const serializableNodes = nodes.value.filter(node => !isGroupBoundaryNode(node))
  const serializableNodeIds = new Set(serializableNodes.map(node => node.id))
  const serializableEdges = edges.value.filter(edge =>
    serializableNodeIds.has(edge.source) && serializableNodeIds.has(edge.target),
  )

  return {
    startNodeId: graphData.value?.startNodeId ?? (serializableNodes[0]?.id ?? ''),
    nodes: serializableNodes.map(node => ({
      id: node.id,
      type: node.data.type,
      params: cloneParams(node.data.params),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
      position: snapEditorPosition(node.position),
    })),
    connections: serializableEdges.map(edgeToRuntimeConnection),
  }
}

function applyEditorScenarioToCanvas(data: GraphData): void {
  graphData.value = data
  nodes.value = data.nodes.map((node, index) => ({
    id: node.id,
    type: 'default',
    position: snapEditorPosition(node.position ?? {
      x: AUTO_LAYOUT_LEFT + (index % 6) * AUTO_LAYOUT_COLUMN_GAP,
      y: AUTO_LAYOUT_TOP + Math.floor(index / 6) * AUTO_LAYOUT_ROW_GAP,
    }),
    data: {
      type: node.type,
      params: node.params ?? {},
      ...(cloneNodeUi(node.ui) ? { ui: cloneNodeUi(node.ui) } : {}),
    },
  }))
  edges.value = data.connections.map((connection, index) => ({
    id: `group-e-${index}-${connection.from}-${connection.to}`,
    source: connection.from,
    target: connection.to,
    sourceHandle: connection.branch ?? null,
    label: edgeLabelForSource(connection.from, connection.branch),
    type: 'routed',
    animated: false,
    style: { stroke: 'var(--color-primary)', strokeWidth: 2 },
    labelStyle: { fill: 'var(--color-text-secondary)', fontSize: 10 },
    labelBgStyle: { fill: 'var(--color-bg-card)' },
    data: connection.route?.length
      ? { route: connection.route, routeVersion: connection.routeVersion ?? 2 }
      : undefined,
  }))
}

function addGroupBoundaryMarkers(
  entryNodeId: string,
  exitNodeId: string,
  savedPositions: GroupBoundaryPositions | null = null,
): void {
  const entryNode = nodes.value.find(node => node.id === entryNodeId)
  const exitNode = nodes.value.find(node => node.id === exitNodeId)
  if (!entryNode || !exitNode) return

  const entryPosition = savedPositions?.entry ?? snapEditorPosition({
    x: entryNode.position.x - 168,
    y: entryNode.position.y,
  })
  const exitPosition = savedPositions?.exit ?? snapEditorPosition({
    x: exitNode.position.x + 276,
    y: exitNode.position.y,
  })

  nodes.value = [
    createGroupBoundaryNode(GROUP_ENTRY_MARKER_ID, GROUP_ENTRY_NODE_TYPE, entryPosition),
    ...nodes.value,
    createGroupBoundaryNode(GROUP_EXIT_MARKER_ID, GROUP_EXIT_NODE_TYPE, exitPosition),
  ]
  edges.value = [
    createGroupBoundaryEdge(GROUP_ENTRY_MARKER_ID, entryNodeId, 'group-entry'),
    ...edges.value,
    createGroupBoundaryEdge(exitNodeId, GROUP_EXIT_MARKER_ID, 'group-exit'),
  ]
}

function resolveGroupBoundaryNodeIds(
  subgraph: GraphData,
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

function inferGroupEntryNodeId(subgraph: GraphData): string {
  const targetIds = new Set(subgraph.connections.map(connection => connection.to))
  return subgraph.nodes.find(node => !targetIds.has(node.id))?.id
    ?? subgraph.nodes[0]?.id
    ?? ''
}

function inferGroupExitNodeId(subgraph: GraphData): string {
  const sourceIds = new Set(subgraph.connections.map(connection => connection.from))
  return [...subgraph.nodes].reverse().find(node => !sourceIds.has(node.id))?.id
    ?? subgraph.nodes.at(-1)?.id
    ?? ''
}

function createGroupBoundaryNode(
  id: string,
  type: typeof GROUP_ENTRY_NODE_TYPE | typeof GROUP_EXIT_NODE_TYPE,
  position: { x: number; y: number },
): EditorFlowNode {
  return {
    id,
    type: 'default',
    position,
    data: {
      type,
      params: { __service: true },
    },
    selectable: false,
    draggable: true,
  } as EditorFlowNode
}

function createGroupBoundaryEdge(source: string, target: string, id: string): Edge {
  return {
    id,
    source,
    target,
    type: 'routed',
    animated: false,
    style: {
      stroke: 'var(--color-text-secondary)',
      strokeDasharray: '4 4',
      strokeWidth: 1.5,
    },
    data: { __service: true },
  }
}

function isGroupBoundaryNode(node: EditorFlowNode): boolean {
  return node.data.type === GROUP_ENTRY_NODE_TYPE || node.data.type === GROUP_EXIT_NODE_TYPE
}

interface GroupBoundaryPositions {
  entry?: { x: number; y: number }
  exit?: { x: number; y: number }
}

function captureGroupBoundaryPositions(): GroupBoundaryPositions {
  return {
    ...(readBoundaryNodePosition(GROUP_ENTRY_MARKER_ID) ? { entry: readBoundaryNodePosition(GROUP_ENTRY_MARKER_ID)! } : {}),
    ...(readBoundaryNodePosition(GROUP_EXIT_MARKER_ID) ? { exit: readBoundaryNodePosition(GROUP_EXIT_MARKER_ID)! } : {}),
  }
}

function captureGroupBoundaryNodeIds(): { entryNodeId?: string; exitNodeId?: string } {
  const entryEdge = [...edges.value].reverse().find(edge =>
    edge.source === GROUP_ENTRY_MARKER_ID
    && edge.target !== GROUP_EXIT_MARKER_ID
  )
  const exitEdge = [...edges.value].reverse().find(edge =>
    edge.target === GROUP_EXIT_MARKER_ID
    && edge.source !== GROUP_ENTRY_MARKER_ID
  )

  return {
    ...(entryEdge?.target ? { entryNodeId: entryEdge.target } : {}),
    ...(exitEdge?.source ? { exitNodeId: exitEdge.source } : {}),
  }
}

function readBoundaryNodePosition(nodeId: string): { x: number; y: number } | null {
  const node = nodes.value.find(item => item.id === nodeId)
  return node ? snapEditorPosition(node.position) : null
}

function readGroupBoundaryPositions(value: unknown): GroupBoundaryPositions | null {
  if (!isRecord(value)) return null

  const entry = readPositionValue(value.entry)
  const exit = readPositionValue(value.exit)
  if (!entry && !exit) return null

  return {
    ...(entry ? { entry } : {}),
    ...(exit ? { exit } : {}),
  }
}

function readPositionValue(value: unknown): { x: number; y: number } | null {
  if (!isRecord(value) || typeof value.x !== 'number' || typeof value.y !== 'number') return null
  if (!Number.isFinite(value.x) || !Number.isFinite(value.y)) return null
  return snapEditorPosition({ x: value.x, y: value.y })
}

function readEditorSubgraph(value: unknown): GraphData | null {
  if (!isRecord(value) || !Array.isArray(value.nodes) || !Array.isArray(value.connections)) return null

  const subNodes = value.nodes
    .filter((item): item is RuntimeNode => isRecord(item) && typeof item.id === 'string' && typeof item.type === 'string')
    .map(item => ({
      id: item.id,
      type: item.type,
      params: isRecord(item.params) ? cloneParams(item.params) : {},
      ...(cloneNodeUi(item.ui) ? { ui: cloneNodeUi(item.ui) } : {}),
      ...(isRecord(item.position) && typeof item.position.x === 'number' && typeof item.position.y === 'number'
        ? { position: { x: item.position.x, y: item.position.y } }
        : {}),
    }))

  const nodeIds = new Set(subNodes.map(node => node.id))
  const subConnections = value.connections
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
      ...(Array.isArray(item.route) ? { route: item.route.filter(isRoutePoint) } : {}),
      ...(typeof item.routeVersion === 'number' ? { routeVersion: item.routeVersion } : {}),
    }))

  return {
    startNodeId: readString(value.startNodeId) ?? subNodes[0]?.id ?? '',
    nodes: subNodes,
    connections: subConnections,
  }
}

function cloneFlowNodes(items: EditorFlowNode[]): EditorFlowNode[] {
  return items.map(cloneFlowNode)
}

function cloneFlowNode(node: EditorFlowNode): EditorFlowNode {
  return {
    ...node,
    position: { ...node.position },
    data: {
      type: node.data.type,
      params: cloneParams(node.data.params),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
    },
  }
}

function cloneFlowEdges(items: Edge[]): Edge[] {
  return items.map(edge => JSON.parse(JSON.stringify(edge)) as Edge)
}

function onEditorKeyDown(event: KeyboardEvent): void {
  const modifierPressed = event.ctrlKey || event.metaKey
  if (!modifierPressed || event.altKey || isEditableTarget(event.target)) return

  if (event.code === 'KeyZ') {
    if (event.shiftKey) {
      redoGraphChange()
    } else {
      undoGraphChange()
    }
    event.preventDefault()
    return
  }

  if (event.code === 'KeyY') {
    redoGraphChange()
    event.preventDefault()
    return
  }

  if (event.code === 'KeyC') {
    const ids = getSelectedOrActiveNodeIds()
    if (!ids.length) return
    copyNodesByIds(ids)
    event.preventDefault()
    return
  }

  if (event.code === 'KeyV') {
    pasteClipboard(getCanvasCenterPosition())
    event.preventDefault()
    return
  }

  if (event.code === 'KeyA') {
    selectAllNodes()
    event.preventDefault()
  }
}

function getSelectedOrActiveNodeIds(): string[] {
  const selected = selectedNodes.value.map((node) => node.id)
  if (selected.length) return selected
  return selectedNodeId.value ? [selectedNodeId.value] : []
}

function copyNodesByIds(ids: string[]): void {
  const idSet = new Set(ids)
  const copiedNodes: ClipboardNode[] = nodes.value
    .filter((node) => idSet.has(node.id))
    .map((node) => ({
      oldId: node.id,
      type: node.data.type,
      params: cloneParams(node.data.params),
      ...(cloneNodeUi(node.data.ui) ? { ui: cloneNodeUi(node.data.ui) } : {}),
      position: { ...node.position },
    }))

  if (!copiedNodes.length) return

  const copiedEdges: ClipboardEdge[] = []
  for (const edge of edges.value) {
    if (!idSet.has(edge.source) || !idSet.has(edge.target)) continue

    copiedEdges.push({
      source: edge.source,
      target: edge.target,
      sourceHandle: edge.sourceHandle ?? null,
      targetHandle: edge.targetHandle ?? null,
      label: typeof edge.label === 'string' ? edge.label : undefined,
    })
  }

  clipboard.value = {
    nodes: copiedNodes,
    edges: copiedEdges,
  }
}

function pasteClipboard(position: { x: number; y: number } | null): void {
  if (isReadOnly.value) return
  if (!clipboard.value || !position) return

  const sourceNodes = clipboard.value.nodes
  const minX = Math.min(...sourceNodes.map((node) => node.position.x))
  const minY = Math.min(...sourceNodes.map((node) => node.position.y))
  const idMap = new Map<string, string>()

  const pastedNodes: EditorFlowNode[] = sourceNodes.map((node) => {
    const id = createGraphId()
    idMap.set(node.oldId, id)

    return {
      id,
      type: 'default',
      position: snapEditorPosition({
        x: position.x + node.position.x - minX,
        y: position.y + node.position.y - minY,
      }),
      data: {
        type: node.type,
        params: clonePastedNodeParams(node.type, node.params, id),
        ...(cloneNodeUi(node.ui) ? { ui: cloneNodeUi(node.ui) } : {}),
      },
      selected: true,
    }
  })

  for (const node of pastedNodes) {
    const targetMessageNodeId = stringValue(node.data.params.targetMessageNodeId)
    const remappedTargetId = targetMessageNodeId ? idMap.get(targetMessageNodeId) : null
    if (remappedTargetId) {
      node.data.params = {
        ...node.data.params,
        targetMessageNodeId: remappedTargetId,
      }
    }
  }

  const pastedEdges: Edge[] = []
  for (const edge of clipboard.value.edges) {
    const source = idMap.get(edge.source)
    const target = idMap.get(edge.target)
    if (!source || !target) continue

    pastedEdges.push(createFlowEdge(source, target, edge.sourceHandle, edge.targetHandle))
  }

  nodes.value = [
    ...nodes.value.map((node) => ({ ...node, selected: false })),
    ...pastedNodes,
  ]
  const nextEdges: Edge[] = []
  for (const edge of edges.value as unknown[]) nextEdges.push(edge as Edge)
  for (const edge of pastedEdges) nextEdges.push(edge)
  edges.value = nextEdges
  selectedNodeId.value = pastedNodes.length === 1 ? pastedNodes[0]?.id ?? null : null

  if (!graphData.value && pastedNodes[0]) {
    graphData.value = { startNodeId: pastedNodes[0].id, nodes: [], connections: [] }
  }
}

function deleteNodesByIds(ids: string[]): void {
  if (isReadOnly.value) return
  if (!ids.length) return

  const idSet = new Set(ids)
  nodes.value = nodes.value.filter((node) => !idSet.has(node.id))
  clearGotoTargetsForRemovedNodes(idSet)
  const nextEdges: Edge[] = []
  for (const edge of edges.value as unknown[]) {
    const candidate = edge as Edge
    if (!idSet.has(candidate.source) && !idSet.has(candidate.target)) {
      nextEdges.push(candidate)
    }
  }
  edges.value = nextEdges

  if (graphData.value && idSet.has(graphData.value.startNodeId)) {
    graphData.value = { ...graphData.value, startNodeId: nodes.value[0]?.id ?? '' }
  }

  if (selectedNodeId.value && idSet.has(selectedNodeId.value)) {
    selectedNodeId.value = null
  }
  contextMenu.value = null
}

function clearGotoTargetsForRemovedNodes(removedIds: Set<string>): void {
  nodes.value = nodes.value.map((node) => {
    if (
      node.data.type !== GOTO_NODE_TYPE
      || !removedIds.has(stringValue(node.data.params.targetNodeId))
    ) {
      return node
    }

    return {
      ...node,
      data: {
        ...node.data,
        params: {
          ...node.data.params,
          targetNodeId: '',
        },
      },
    }
  })
}

function deleteEdge(edgeId: string): void {
  if (isReadOnly.value) return
  const nextEdges: Edge[] = []
  for (const edge of edges.value as unknown[]) {
    const candidate = edge as Edge
    if (candidate.id !== edgeId) {
      nextEdges.push(candidate)
    }
  }
  edges.value = nextEdges
  contextMenu.value = null
}

function validateScenarioBeforeSerialization(): boolean {
  const root = activeGroupSession.value
    ? buildRootStateWithCurrentGroup(activeGroupSession.value)
    : { nodes: nodes.value, edges: edges.value }
  const rootNodeIds = new Set(root.nodes.map(node => node.id))

  if (hasGotoOutsideNodeSet(root.nodes, rootNodeIds)) {
    setEditorStatus('error', 'Переход указывает на несуществующий узел')
    return false
  }

  if (activeGroupSession.value && hasGotoOutsideNodeSet(nodes.value, new Set(nodes.value.map(node => node.id)))) {
    setEditorStatus('error', 'Переход внутри группы должен указывать на узел этой группы')
    return false
  }

  const invalidGroupTitle = findGroupWithInvalidGotoTarget(root.nodes)
  if (invalidGroupTitle) {
    setEditorStatus('error', `В группе «${invalidGroupTitle}» есть переход наружу`)
    return false
  }

  const limitError = findObjectMatrixLimitErrorInFlowNodes(root.nodes)
  if (limitError) {
    setEditorStatus('error', limitError, 6000)
    return false
  }

  return true
}

function findObjectMatrixLimitErrorInFlowNodes(sourceNodes: EditorFlowNode[]): string | null {
  for (const node of sourceNodes) {
    const error = findObjectMatrixLimitErrorInParams(node.data.type, node.data.params)
    if (error) return error

    if (node.data.type === GROUP_NODE_TYPE) {
      const subgraph = readEditorSubgraph(node.data.params.subgraph)
      if (!subgraph) continue

      const subgraphError = findObjectMatrixLimitErrorInRuntimeNodes(subgraph.nodes)
      if (subgraphError) return subgraphError
    }
  }

  return null
}

function findObjectMatrixLimitErrorInRuntimeNodes(sourceNodes: RuntimeNode[]): string | null {
  for (const node of sourceNodes) {
    const params = isRecord(node.params) ? node.params : {}
    const error = findObjectMatrixLimitErrorInParams(node.type, params)
    if (error) return error

    if (node.type === GROUP_NODE_TYPE) {
      const subgraph = readEditorSubgraph(params.subgraph)
      if (!subgraph) continue

      const subgraphError = findObjectMatrixLimitErrorInRuntimeNodes(subgraph.nodes)
      if (subgraphError) return subgraphError
    }
  }

  return null
}

function findObjectMatrixLimitErrorInParams(
  nodeType: string,
  params: Record<string, unknown>,
): string | null {
  const schema = getUiParams(catalog.value, nodeType)
  if (!schema.length) return null

  return findObjectMatrixLimitErrorInSchema(schema, params, getNodeLabel(nodeType))
}

function findObjectMatrixLimitErrorInSchema(
  schema: NodeParamItem[],
  params: Record<string, unknown>,
  nodeLabel: string,
  parentPath = '',
): string | null {
  for (const param of schema) {
    if (!param.key) continue

    const value = params[param.key]
    const normalizedType = normalizeNodeParamType(param.type)
    const path = parentPath ? `${parentPath}.${param.key}` : param.key

    if (normalizedType === 'objectmatrix' && Array.isArray(value)) {
      const rows = value.filter((row): row is unknown[] => Array.isArray(row))
      const message = getObjectMatrixLimitViolation(rows, param, param.key === 'buttons' ? 'кнопок' : 'элементов')
      if (message) return `Узел «${nodeLabel}», параметр «${path}»: ${message}`
      continue
    }

    if (normalizedType === 'object' && isRecord(value) && param.fields?.length) {
      const error = findObjectMatrixLimitErrorInSchema(param.fields, value, nodeLabel, path)
      if (error) return error
      continue
    }

    if (normalizedType === 'objectlist' && Array.isArray(value) && param.fields?.length) {
      for (let index = 0; index < value.length; index += 1) {
        const item = value[index]
        if (!isRecord(item)) continue

        const error = findObjectMatrixLimitErrorInSchema(param.fields, item, nodeLabel, `${path}[${index}]`)
        if (error) return error
      }
    }
  }

  return null
}

function findGroupWithInvalidGotoTarget(sourceNodes: EditorFlowNode[]): string | null {
  for (const node of sourceNodes) {
    if (node.data.type !== GROUP_NODE_TYPE) continue

    const subgraph = readEditorSubgraph(node.data.params.subgraph)
    if (!subgraph) continue

    const subgraphNodeIds = new Set(subgraph.nodes.map(item => item.id))
    const hasInvalidGoto = subgraph.nodes.some(item =>
      item.type === GOTO_NODE_TYPE
      && hasGotoTargetOutside(readString(item.params?.targetNodeId), subgraphNodeIds),
    )
    if (hasInvalidGoto) {
      return readString(node.data.params.title) ?? 'Группа'
    }
  }

  return null
}

function isGroupSelectionConnected(selectedIds: Set<string>, internalEdges: Edge[]): boolean {
  const [firstId] = selectedIds
  if (!firstId) return false
  if (selectedIds.size === 1) return true

  const neighbors = new Map<string, Set<string>>()
  for (const id of selectedIds) {
    neighbors.set(id, new Set<string>())
  }

  for (const edge of internalEdges) {
    neighbors.get(edge.source)?.add(edge.target)
    neighbors.get(edge.target)?.add(edge.source)
  }

  const visited = new Set<string>()
  const queue = [firstId]
  while (queue.length) {
    const current = queue.shift()
    if (!current || visited.has(current)) continue
    visited.add(current)
    for (const next of neighbors.get(current) ?? []) {
      if (!visited.has(next)) queue.push(next)
    }
  }

  return visited.size === selectedIds.size
}

function hasExternalTransitBetweenSelectedNodes(selectedIds: Set<string>, allEdges: Edge[]): boolean {
  const outgoingBySource = new Map<string, Edge[]>()
  for (const edge of allEdges) {
    const sourceEdges = outgoingBySource.get(edge.source) ?? []
    sourceEdges.push(edge)
    outgoingBySource.set(edge.source, sourceEdges)
  }

  for (const edge of allEdges) {
    if (!selectedIds.has(edge.source) || selectedIds.has(edge.target)) continue

    const visited = new Set<string>()
    const queue = [edge.target]
    while (queue.length) {
      const current = queue.shift()
      if (!current || visited.has(current)) continue
      visited.add(current)

      for (const nextEdge of outgoingBySource.get(current) ?? []) {
        if (selectedIds.has(nextEdge.target)) {
          return true
        }
        queue.push(nextEdge.target)
      }
    }
  }

  return false
}

function hasGotoOutsideNodeSet(sourceNodes: EditorFlowNode[], allowedNodeIds: Set<string>): boolean {
  return sourceNodes.some(node =>
    node.data.type === GOTO_NODE_TYPE
    && hasGotoTargetOutside(readString(node.data.params.targetNodeId), allowedNodeIds),
  )
}

function hasGotoTargetOutside(targetNodeId: string | null, allowedNodeIds: Set<string>): boolean {
  return Boolean(targetNodeId && !allowedNodeIds.has(targetNodeId))
}

function resolveGroupEntryNodeId(
  groupNodes: EditorFlowNode[],
  internalEdges: Edge[],
  incomingEdges: Edge[],
): string | null {
  const incomingTargets = uniqueStrings(incomingEdges.map(edge => edge.target))
  if (incomingTargets.length === 1) return incomingTargets[0] ?? null
  if (incomingTargets.length > 1) return null

  const internalTargets = new Set(internalEdges.map(edge => edge.target))
  const entryCandidates = groupNodes
    .map(node => node.id)
    .filter(nodeId => !internalTargets.has(nodeId))
  return entryCandidates.length === 1 ? entryCandidates[0] ?? null : null
}

function resolveGroupExitNodeId(
  groupNodes: EditorFlowNode[],
  internalEdges: Edge[],
  outgoingEdges: Edge[],
): string | null {
  const outgoingSources = uniqueStrings(outgoingEdges.map(edge => edge.source))
  if (outgoingSources.length === 1) return outgoingSources[0] ?? null
  if (outgoingSources.length > 1) return null

  const internalSources = new Set(internalEdges.map(edge => edge.source))
  const exitCandidates = groupNodes
    .map(node => node.id)
    .filter(nodeId => !internalSources.has(nodeId))
  return exitCandidates.length === 1 ? exitCandidates[0] ?? null : null
}

function calculateGroupPosition(groupNodes: EditorFlowNode[]): { x: number; y: number } {
  const minX = Math.min(...groupNodes.map(node => node.position.x))
  const minY = Math.min(...groupNodes.map(node => node.position.y))
  const maxX = Math.max(...groupNodes.map(node => node.position.x))
  const maxY = Math.max(...groupNodes.map(node => node.position.y))

  return snapEditorPosition({
    x: (minX + maxX) / 2,
    y: (minY + maxY) / 2,
  })
}

function edgeToRuntimeConnection(edge: Edge): RuntimeConnection {
  const route = getSavedEdgeRoute(edge)
  return {
    from: edge.source,
    to: edge.target,
    ...(edge.sourceHandle ? { branch: edge.sourceHandle } : {}),
    ...(route.length ? { route, routeVersion: 2 } : {}),
  }
}

function uniqueStrings(values: string[]): string[] {
  return [...new Set(values.filter(Boolean))]
}

function selectAllNodes(): void {
  nodes.value = nodes.value.map((node) => ({ ...node, selected: true }))
  selectedNodeId.value = null
  contextMenu.value = null
}

function getCanvasCenterPosition(): { x: number; y: number } | null {
  const canvas = getCanvasElement()
  if (!canvas) return null

  const bounds = canvas.getBoundingClientRect()
  return screenToFlowCoordinate({
    x: bounds.width / 2,
    y: bounds.height / 2,
  })
}

function createFlowEdge(
  source: string,
  target: string,
  sourceHandle?: string | null,
  targetHandle?: string | null,
): Edge {
  return {
    id: `e-${source}-${target}-${sourceHandle ?? 'def'}-${createGraphId()}`,
    source,
    target,
    sourceHandle,
    targetHandle,
    label: edgeLabelForSource(source, sourceHandle),
    type: 'routed',
    animated: false,
    style: { stroke: 'var(--color-primary)', strokeWidth: 2 },
    labelStyle: { fill: 'var(--color-text-secondary)', fontSize: 10 },
    labelBgStyle: { fill: 'var(--color-bg-card)' },
  }
}

function edgeLabelForSource(sourceNodeId: string, sourceHandle?: string | null): string | undefined {
  if (!sourceHandle) return undefined

  const sourceNode = nodes.value.find(node => node.id === sourceNodeId)
  if (sourceNode && isSmartButtonBranchingNodeType(sourceNode.data.type)) {
    return undefined
  }

  return sourceHandle
}

const AUTO_LAYOUT_COLUMN_GAP = 330
const AUTO_LAYOUT_ROW_GAP = 170
const AUTO_LAYOUT_BRANCH_GAP = 170
const AUTO_LAYOUT_COLUMNS_PER_BAND = 999
const AUTO_LAYOUT_BAND_GAP = 280
const AUTO_LAYOUT_LEFT = 80
const AUTO_LAYOUT_TOP = 80
const AUTO_LAYOUT_FIT_OPTIONS = { padding: 0.16, minZoom: 0.65, maxZoom: 1.15, duration: 260 }
const AUTO_LAYOUT_DEFAULT_NODE_WIDTH = 204
const AUTO_LAYOUT_DEFAULT_NODE_HEIGHT = 92
const AUTO_LAYOUT_PORT_NODE_WIDTH = 432
const AUTO_LAYOUT_PORT_NODE_MIN_HEIGHT = 132
const AUTO_LAYOUT_PORT_ROW_HEIGHT = 24
const AUTO_LAYOUT_PORT_BASE_Y = 48
const AUTO_LAYOUT_PORT_SIZE = 8
const AUTO_LAYOUT_SWITCH_NODE_WIDTH = 276
const AUTO_LAYOUT_SWITCH_PORT_BASE_Y = 84
const AUTO_LAYOUT_SWITCH_PORT_ROW_HEIGHT = 24
const ELK_DEFAULT_OUTPUT_PORT = '__default'
let elkInstance: ELK | null = null

async function autoLayoutGraph(): Promise<void> {
  if (nodes.value.length < 2) return

  await applyAutoLayoutToCurrentGraph(graphData.value?.startNodeId ?? null)
  contextMenu.value = null
  rightSelectionRect.value = null
}

async function applyAutoLayoutToCurrentGraph(startNodeId: string | null): Promise<void> {
  if (nodes.value.length < 2) return

  const layout = await calculateAutoLayoutPositions(nodes.value, edges.value, startNodeId)

  nodes.value = nodes.value.map(node => ({
    ...node,
    position: snapEditorPosition(layout.positions.get(node.id) ?? node.position),
  }))

  edges.value = edges.value.map(edge => ({
    ...edge,
    type: 'routed',
    data: createAutoLayoutEdgeData(edge, layout.routes.get(edge.id)),
    pathOptions: undefined,
    animated: false,
  }))

  await nextTick()
  await fitView(AUTO_LAYOUT_FIT_OPTIONS)
}

async function calculateAutoLayoutPositions(
  layoutNodes: EditorFlowNode[],
  layoutEdges: Edge[],
  startNodeId: string | null,
): Promise<AutoLayoutResult> {
  try {
    const layout = await calculateElkLayoutPositions(layoutNodes, layoutEdges, startNodeId)
    if (layout.positions.size === layoutNodes.length) return layout
  } catch {
    // ELK is the primary path, but imported legacy JSON should still be placeable.
  }

  return {
    positions: calculateFallbackAutoLayoutPositions(layoutNodes, layoutEdges, startNodeId),
    routes: new Map(),
  }
}

interface AutoLayoutResult {
  positions: Map<string, { x: number; y: number }>
  routes: Map<string, ElkPoint[]>
}

async function calculateElkLayoutPositions(
  layoutNodes: EditorFlowNode[],
  layoutEdges: Edge[],
  startNodeId: string | null,
): Promise<AutoLayoutResult> {
  const nodeIds = new Set(layoutNodes.map(node => node.id))
  const sourceHandlesByNode = collectSourceHandles(layoutEdges, nodeIds)
  const incomingTargets = collectIncomingTargets(layoutEdges, nodeIds)
  const forceStartFirst = startNodeId !== null && nodeIds.has(startNodeId) && !incomingTargets.has(startNodeId)
  const graph: ElkNode = {
    id: 'scenario',
    width: 2200,
    layoutOptions: {
      'org.eclipse.elk.algorithm': 'layered',
      'org.eclipse.elk.direction': 'RIGHT',
      'org.eclipse.elk.edgeRouting': 'ORTHOGONAL',
      'org.eclipse.elk.aspectRatio': '3.0',
      'org.eclipse.elk.padding': '[top=48,left=48,bottom=48,right=48]',
      'org.eclipse.elk.spacing.nodeNode': '46',
      'org.eclipse.elk.spacing.edgeNode': '18',
      'org.eclipse.elk.spacing.edgeEdge': '12',
      'org.eclipse.elk.layered.thoroughness': '10',
      'org.eclipse.elk.layered.layering.strategy': 'NETWORK_SIMPLEX',
      'org.eclipse.elk.layered.cycleBreaking.strategy': 'GREEDY',
      'org.eclipse.elk.layered.spacing.nodeNodeBetweenLayers': '76',
      'org.eclipse.elk.layered.spacing.edgeEdgeBetweenLayers': '14',
      'org.eclipse.elk.layered.spacing.edgeNodeBetweenLayers': '24',
      'org.eclipse.elk.layered.mergeEdges': 'false',
      'org.eclipse.elk.layered.crossingMinimization.strategy': 'LAYER_SWEEP',
      'org.eclipse.elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF',
      'org.eclipse.elk.layered.nodePlacement.bk.fixedAlignment': 'BALANCED',
      'org.eclipse.elk.layered.nodePlacement.favorStraightEdges': 'true',
      'org.eclipse.elk.layered.unnecessaryBendpoints': 'true',
      'org.eclipse.elk.separateConnectedComponents': 'false',
    },
    children: layoutNodes.map(node =>
      createElkNode(node, sourceHandlesByNode.get(node.id), forceStartFirst && node.id === startNodeId),
    ),
    edges: createElkEdges(layoutEdges, nodeIds),
  }

  const elk = await getElk()
  const result = await elk.layout(graph)
  const normalized = normalizeElkLayout(result.children ?? [], result.edges ?? [])
  return {
    positions: normalized.positions,
    routes: createElkControlRoutes(normalized.routes),
  }
}

async function getElk(): Promise<ELK> {
  if (elkInstance) return elkInstance

  const module = await import('elkjs/lib/elk.bundled')
  elkInstance = new module.default()
  return elkInstance
}

function collectSourceHandles(
  layoutEdges: Edge[],
  nodeIds: Set<string>,
): Map<string, Set<string>> {
  const sourceHandlesByNode = new Map<string, Set<string>>()

  for (const edge of layoutEdges) {
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) continue
    const handles = sourceHandlesByNode.get(edge.source) ?? new Set<string>()
    handles.add(edge.sourceHandle ?? ELK_DEFAULT_OUTPUT_PORT)
    sourceHandlesByNode.set(edge.source, handles)
  }

  return sourceHandlesByNode
}

function collectIncomingTargets(layoutEdges: Edge[], nodeIds: Set<string>): Set<string> {
  const incomingTargets = new Set<string>()

  for (const edge of layoutEdges) {
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) continue
    incomingTargets.add(edge.target)
  }

  return incomingTargets
}

function createElkNode(
  node: EditorFlowNode,
  edgeSourceHandles?: Set<string>,
  forceFirstLayer = false,
): ElkNode {
  const nodeData = node.data
  const outputPorts = getNodeOutputPorts(nodeData.type, nodeData.params)
  const outputPortIds = outputPorts.map(port => port.id)

  if (outputPortIds.length === 0) {
    outputPortIds.push(ELK_DEFAULT_OUTPUT_PORT)
  }

  for (const handle of edgeSourceHandles ?? []) {
    if (!outputPortIds.includes(handle)) outputPortIds.push(handle)
  }

  const size = estimateElkNodeSize(nodeData.type, outputPortIds.length)
  const outputPortBaseY = nodeData.type === 'switch'
    ? AUTO_LAYOUT_SWITCH_PORT_BASE_Y
    : AUTO_LAYOUT_PORT_BASE_Y
  const outputPortRowHeight = nodeData.type === 'switch'
    ? AUTO_LAYOUT_SWITCH_PORT_ROW_HEIGHT
    : AUTO_LAYOUT_PORT_ROW_HEIGHT

  return {
    id: node.id,
    width: size.width,
    height: size.height,
    layoutOptions: {
      'org.eclipse.elk.portConstraints': 'FIXED_POS',
      'org.eclipse.elk.nodeLabels.placement': 'INSIDE V_CENTER H_CENTER',
      ...(forceFirstLayer
        ? { 'org.eclipse.elk.layered.layering.layerConstraint': 'FIRST' }
        : {}),
    },
    ports: [
      {
        id: elkInputPortId(node.id),
        width: 8,
        height: 8,
        x: -AUTO_LAYOUT_PORT_SIZE / 2,
        y: AUTO_LAYOUT_PORT_BASE_Y - AUTO_LAYOUT_PORT_SIZE / 2,
        layoutOptions: {
          'org.eclipse.elk.port.side': 'WEST',
          'org.eclipse.elk.port.index': '0',
        },
      },
      ...outputPortIds.map((portId, index): ElkPort => ({
        id: elkOutputPortId(node.id, portId),
        width: 8,
        height: 8,
        x: size.width - AUTO_LAYOUT_PORT_SIZE / 2,
        y: outputPortBaseY + index * outputPortRowHeight - AUTO_LAYOUT_PORT_SIZE / 2,
        layoutOptions: {
          'org.eclipse.elk.port.side': 'EAST',
          'org.eclipse.elk.port.index': String(index),
        },
      })),
    ],
  }
}

function createElkEdges(layoutEdges: Edge[], nodeIds: Set<string>): ElkExtendedEdge[] {
  const result: ElkExtendedEdge[] = []

  layoutEdges.forEach((edge, index) => {
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) return

    result.push({
      id: edge.id || `edge-${index}-${edge.source}-${edge.target}`,
      sources: [elkOutputPortId(edge.source, edge.sourceHandle ?? ELK_DEFAULT_OUTPUT_PORT)],
      targets: [elkInputPortId(edge.target)],
    })
  })

  return result
}

function estimateElkNodeSize(nodeType: string, outputPortCount: number): { width: number; height: number } {
  if (outputPortCount <= 1) {
    return {
      width: AUTO_LAYOUT_DEFAULT_NODE_WIDTH,
      height: AUTO_LAYOUT_DEFAULT_NODE_HEIGHT,
    }
  }

  if (nodeType === 'switch') {
    return {
      width: AUTO_LAYOUT_SWITCH_NODE_WIDTH,
      height: Math.max(
        156,
        AUTO_LAYOUT_SWITCH_PORT_BASE_Y
          + Math.max(0, outputPortCount - 1) * AUTO_LAYOUT_SWITCH_PORT_ROW_HEIGHT
          + 28,
      ),
    }
  }

  return {
    width: AUTO_LAYOUT_PORT_NODE_WIDTH,
    height: Math.max(
      AUTO_LAYOUT_PORT_NODE_MIN_HEIGHT,
      72 + outputPortCount * AUTO_LAYOUT_PORT_ROW_HEIGHT,
    ),
  }
}

function elkInputPortId(nodeId: string): string {
  return `${nodeId}::in`
}

function elkOutputPortId(nodeId: string, portId: string): string {
  return `${nodeId}::out::${portId}`
}

function normalizeElkLayout(children: ElkNode[], elkEdges: ElkExtendedEdge[]): AutoLayoutResult {
  const rawPositions = children
    .filter((node): node is ElkNode & { x: number; y: number } =>
      typeof node.x === 'number' && typeof node.y === 'number',
    )

  const minX = Math.min(...rawPositions.map(node => node.x))
  const minY = Math.min(...rawPositions.map(node => node.y))
  const xShift = Number.isFinite(minX) ? AUTO_LAYOUT_LEFT - minX : 0
  const yShift = Number.isFinite(minY) ? AUTO_LAYOUT_TOP - minY : 0
  const positions = new Map<string, { x: number; y: number }>()
  const routes = new Map<string, ElkPoint[]>()

  for (const node of rawPositions) {
    positions.set(node.id, {
      x: snapEditorGrid(node.x + xShift),
      y: snapEditorGrid(node.y + yShift),
    })
  }

  for (const edge of elkEdges) {
    const section = edge.sections?.[0]
    if (!edge.id || !section) continue

    routes.set(edge.id, normalizeElkRoute(section, xShift, yShift))
  }

  return { positions, routes }
}

function normalizeElkRoute(
  section: { startPoint: ElkPoint; bendPoints?: ElkPoint[]; endPoint: ElkPoint },
  xShift: number,
  yShift: number,
): ElkPoint[] {
  return [
    section.startPoint,
    ...(section.bendPoints ?? []),
    section.endPoint,
  ].map(point => ({
    x: snapEditorGrid(point.x + xShift),
    y: snapEditorGrid(point.y + yShift),
  }))
}

function createElkControlRoutes(routes: Map<string, ElkPoint[]>): Map<string, ElkPoint[]> {
  const controlRoutes = new Map<string, ElkPoint[]>()

  for (const [edgeId, route] of routes) {
    const controlPoints = route
      .slice(1, -1)
      .map(point => ({
        x: Math.round(point.x),
        y: Math.round(point.y),
      }))

    controlRoutes.set(edgeId, controlPoints)
  }

  return controlRoutes
}

function clearAutoLayoutEdgeRoute(data: unknown): Record<string, unknown> | undefined {
  if (!isRecord(data)) return undefined

  const { route, routeVersion, ...rest } = data
  void route
  void routeVersion
  return Object.keys(rest).length ? rest : undefined
}

function createAutoLayoutEdgeData(
  edge: Edge,
  routeValue: unknown,
): Record<string, unknown> | undefined {
  const data = clearAutoLayoutEdgeRoute(edge.data)
  const route = normalizeConnectionRoute(routeValue)

  if (!route.length) return data

  return {
    ...data,
    route,
    routeVersion: 2,
  }
}

function calculateFallbackAutoLayoutPositions(
  layoutNodes: EditorFlowNode[],
  layoutEdges: Edge[],
  startNodeId: string | null,
): Map<string, { x: number; y: number }> {
  const nodeById = new Map(layoutNodes.map(node => [node.id, node]))
  const nodeIds = new Set(nodeById.keys())
  const currentOrder = new Map(
    [...layoutNodes]
      .sort(compareNodesByCurrentPosition)
      .map((node, index) => [node.id, index]),
  )
  const outgoing = createLayoutAdjacency(nodeIds)
  const incoming = createLayoutAdjacency(nodeIds)

  for (const edge of layoutEdges) {
    if (!nodeIds.has(edge.source) || !nodeIds.has(edge.target)) continue
    addUniqueLayoutNeighbor(outgoing.get(edge.source), edge.target)
    addUniqueLayoutNeighbor(incoming.get(edge.target), edge.source)
  }

  for (const targets of outgoing.values()) {
    targets.sort((a, b) => compareIdsByCurrentOrder(a, b, currentOrder))
  }
  for (const sources of incoming.values()) {
    sources.sort((a, b) => compareIdsByCurrentOrder(a, b, currentOrder))
  }

  const traversal = buildLayoutTraversal(nodeIds, currentOrder, outgoing, incoming, startNodeId)
  const traversalIndex = new Map(traversal.map((id, index) => [id, index]))
  const layerById = new Map<string, number>()

  for (const id of traversal) {
    if (!layerById.has(id)) layerById.set(id, 0)
    const sourceLayer = layerById.get(id) ?? 0
    const sourceIndex = traversalIndex.get(id) ?? 0

    for (const target of outgoing.get(id) ?? []) {
      const targetIndex = traversalIndex.get(target) ?? 0
      if (targetIndex <= sourceIndex) continue

      const nextLayer = sourceLayer + 1
      layerById.set(target, Math.max(layerById.get(target) ?? 0, nextLayer))
    }
  }

  return buildLayerPositions(layoutNodes, layerById, incoming, outgoing, currentOrder)
}

function createLayoutAdjacency(nodeIds: Set<string>): Map<string, string[]> {
  const map = new Map<string, string[]>()
  for (const id of nodeIds) map.set(id, [])
  return map
}

function addUniqueLayoutNeighbor(list: string[] | undefined, id: string): void {
  if (!list || list.includes(id)) return
  list.push(id)
}

function buildLayoutTraversal(
  nodeIds: Set<string>,
  currentOrder: Map<string, number>,
  outgoing: Map<string, string[]>,
  incoming: Map<string, string[]>,
  startNodeId: string | null,
): string[] {
  const orderedIds = [...nodeIds].sort((a, b) => compareIdsByCurrentOrder(a, b, currentOrder))
  const seeds: string[] = []
  const seedSet = new Set<string>()

  if (startNodeId && nodeIds.has(startNodeId)) {
    seeds.push(startNodeId)
    seedSet.add(startNodeId)
  }

  for (const id of orderedIds) {
    if (seedSet.has(id)) continue
    if ((incoming.get(id)?.length ?? 0) > 0) continue
    seeds.push(id)
    seedSet.add(id)
  }

  for (const id of orderedIds) {
    if (seedSet.has(id)) continue
    seeds.push(id)
    seedSet.add(id)
  }

  const visited = new Set<string>()
  const traversal: string[] = []

  for (const seed of seeds) {
    const queue = [seed]

    while (queue.length) {
      const id = queue.shift()
      if (!id || visited.has(id)) continue

      visited.add(id)
      traversal.push(id)

      for (const target of outgoing.get(id) ?? []) {
        if (!visited.has(target)) queue.push(target)
      }
    }
  }

  return traversal
}

function buildLayerPositions(
  layoutNodes: EditorFlowNode[],
  layerById: Map<string, number>,
  incoming: Map<string, string[]>,
  outgoing: Map<string, string[]>,
  currentOrder: Map<string, number>,
): Map<string, { x: number; y: number }> {
  const maxLayer = Math.max(0, ...layerById.values())
  const layers = Array.from({ length: maxLayer + 1 }, () => [] as string[])

  for (const node of [...layoutNodes].sort(compareNodesByCurrentPosition)) {
    const layer = layerById.get(node.id) ?? 0
    layers[layer]?.push(node.id)
  }

  optimizeLayerOrder(layers, incoming, outgoing, currentOrder)

  const yById = calculateLayerYPositions(layers, layerById, outgoing, currentOrder)
  const bands = calculateLayoutBands(layers, yById)
  const positions = new Map<string, { x: number; y: number }>()

  layers.forEach((layer, layerIndex) => {
    const bandIndex = Math.floor(layerIndex / AUTO_LAYOUT_COLUMNS_PER_BAND)
    const columnIndex = layerIndex % AUTO_LAYOUT_COLUMNS_PER_BAND
    const visualColumnIndex = bandIndex % 2 === 0
      ? columnIndex
      : AUTO_LAYOUT_COLUMNS_PER_BAND - 1 - columnIndex
    const band = bands[bandIndex] ?? { minY: 0, offsetY: 0 }

    layer.forEach((id) => {
      positions.set(id, {
        x: AUTO_LAYOUT_LEFT + visualColumnIndex * AUTO_LAYOUT_COLUMN_GAP,
        y: AUTO_LAYOUT_TOP + band.offsetY + (yById.get(id) ?? 0) - band.minY,
      })
    })
  })

  return positions
}

interface LayoutBand {
  minY: number
  maxY: number
  offsetY: number
}

function calculateLayoutBands(
  layers: string[][],
  yById: Map<string, number>,
): LayoutBand[] {
  const bandCount = Math.max(1, Math.ceil(layers.length / AUTO_LAYOUT_COLUMNS_PER_BAND))
  const bands = Array.from({ length: bandCount }, () => ({
    minY: Number.POSITIVE_INFINITY,
    maxY: Number.NEGATIVE_INFINITY,
    offsetY: 0,
  }))

  layers.forEach((layer, layerIndex) => {
    const bandIndex = Math.floor(layerIndex / AUTO_LAYOUT_COLUMNS_PER_BAND)
    const band = bands[bandIndex]
    if (!band) return

    for (const id of layer) {
      const y = yById.get(id) ?? 0
      band.minY = Math.min(band.minY, y)
      band.maxY = Math.max(band.maxY, y)
    }
  })

  let offsetY = 0
  for (const band of bands) {
    if (!Number.isFinite(band.minY) || !Number.isFinite(band.maxY)) {
      band.minY = 0
      band.maxY = 0
    }

    band.offsetY = offsetY
    offsetY += Math.max(AUTO_LAYOUT_ROW_GAP, band.maxY - band.minY) + AUTO_LAYOUT_BAND_GAP
  }

  return bands
}

function calculateLayerYPositions(
  layers: string[][],
  layerById: Map<string, number>,
  outgoing: Map<string, string[]>,
  currentOrder: Map<string, number>,
): Map<string, number> {
  const yById = new Map<string, number>()
  const proposalsById = new Map<string, number[]>()

  layers.forEach((layer, layerIndex) => {
    for (const id of layer) {
      if (yById.has(id)) continue

      const proposals = proposalsById.get(id) ?? []
      yById.set(id, proposals.length ? average(proposals) : 0)
    }

    spreadLayerVertically(layer, yById, currentOrder)

    for (const id of layer) {
      const sourceY = yById.get(id) ?? 0
      const forwardTargets = (outgoing.get(id) ?? [])
        .filter(target => (layerById.get(target) ?? layerIndex) > layerIndex)
        .sort((a, b) => compareIdsByCurrentOrder(a, b, currentOrder))

      forwardTargets.forEach((target, index) => {
        const proposals = proposalsById.get(target) ?? []
        proposals.push(sourceY + branchOffset(index, forwardTargets.length))
        proposalsById.set(target, proposals)
      })
    }
  })

  return yById
}

function spreadLayerVertically(
  layer: string[],
  yById: Map<string, number>,
  currentOrder: Map<string, number>,
): void {
  if (layer.length <= 1) return

  const ordered = [...layer].sort((a, b) =>
    (yById.get(a) ?? 0) - (yById.get(b) ?? 0)
    || compareIdsByCurrentOrder(a, b, currentOrder),
  )
  const desired = ordered.map(id => yById.get(id) ?? 0)
  const assigned: number[] = []

  ordered.forEach((id, index) => {
    const previous = assigned[index - 1]
    const minY = typeof previous === 'number'
      ? previous + AUTO_LAYOUT_ROW_GAP
      : Number.NEGATIVE_INFINITY

    assigned[index] = Math.max(desired[index] ?? 0, minY)
    yById.set(id, assigned[index] ?? 0)
  })

  const shift = average(desired) - average(assigned)
  ordered.forEach((id, index) => {
    yById.set(id, (assigned[index] ?? 0) + shift)
  })
}

function branchOffset(index: number, count: number): number {
  if (count <= 1) return 0
  return (index - (count - 1) / 2) * AUTO_LAYOUT_BRANCH_GAP
}

function average(values: number[]): number {
  if (!values.length) return 0
  return values.reduce((sum, value) => sum + value, 0) / values.length
}

function optimizeLayerOrder(
  layers: string[][],
  incoming: Map<string, string[]>,
  outgoing: Map<string, string[]>,
  currentOrder: Map<string, number>,
): void {
  for (let pass = 0; pass < 4; pass += 1) {
    let positionIndex = indexLayerPositions(layers)
    for (let index = 1; index < layers.length; index += 1) {
      layers[index]?.sort((a, b) => compareByNeighborOrder(a, b, incoming, positionIndex, currentOrder))
    }

    positionIndex = indexLayerPositions(layers)
    for (let index = layers.length - 2; index >= 0; index -= 1) {
      layers[index]?.sort((a, b) => compareByNeighborOrder(a, b, outgoing, positionIndex, currentOrder))
    }
  }
}

function indexLayerPositions(layers: string[][]): Map<string, number> {
  const result = new Map<string, number>()
  for (const layer of layers) {
    layer.forEach((id, index) => result.set(id, index))
  }
  return result
}

function compareByNeighborOrder(
  a: string,
  b: string,
  neighbors: Map<string, string[]>,
  positionIndex: Map<string, number>,
  currentOrder: Map<string, number>,
): number {
  const aScore = averageNeighborOrder(neighbors.get(a) ?? [], positionIndex)
  const bScore = averageNeighborOrder(neighbors.get(b) ?? [], positionIndex)

  if (aScore !== bScore) return aScore - bScore
  return compareIdsByCurrentOrder(a, b, currentOrder)
}

function averageNeighborOrder(neighbors: string[], positionIndex: Map<string, number>): number {
  const indexes = neighbors
    .map(id => positionIndex.get(id))
    .filter((index): index is number => typeof index === 'number')

  if (!indexes.length) return Number.POSITIVE_INFINITY
  return indexes.reduce((sum, index) => sum + index, 0) / indexes.length
}

function compareNodesByCurrentPosition(a: EditorFlowNode, b: EditorFlowNode): number {
  const xDelta = a.position.x - b.position.x
  if (Math.abs(xDelta) > 24) return xDelta

  const yDelta = a.position.y - b.position.y
  if (yDelta !== 0) return yDelta

  return a.id.localeCompare(b.id)
}

function compareIdsByCurrentOrder(a: string, b: string, currentOrder: Map<string, number>): number {
  return (currentOrder.get(a) ?? 0) - (currentOrder.get(b) ?? 0) || a.localeCompare(b)
}

function variableScopeForNode(nodeId: string): VariableScope {
  const targetNode = nodes.value.find(node => node.id === nodeId)
  if (!targetNode) return allVariableScope.value

  const available = new Set<string>()
  const upstreamNodeIds = collectUpstreamNodeIds(nodeId)

  for (const node of nodes.value) {
    if (!upstreamNodeIds.has(node.id)) continue
    collectVariablesFromFlowNode(node, available)
  }

  const future = knownVariables.value.filter(variable => !available.has(variable))

  return {
    available: [...available].sort((a, b) => a.localeCompare(b)),
    future,
  }
}

function collectUpstreamNodeIds(nodeId: string): Set<string> {
  const upstream = new Set<string>()
  const reverse = new Map<string, string[]>()

  for (const edge of edges.value) {
    if (!edge.source || !edge.target) continue
    const sources = reverse.get(edge.target) ?? []
    sources.push(edge.source)
    reverse.set(edge.target, sources)
  }

  const stack = [...(reverse.get(nodeId) ?? [])]
  while (stack.length) {
    const currentId = stack.pop()
    if (!currentId || upstream.has(currentId) || currentId === nodeId) continue
    upstream.add(currentId)
    stack.push(...(reverse.get(currentId) ?? []))
  }

  return upstream
}

function collectVariablesFromFlowNode(node: EditorFlowNode, target: Set<string>): void {
  collectVariablesFromNodeParams(node.data.type, node.data.params, target)

  if (node.data.type === GROUP_NODE_TYPE) {
    const subgraph = readEditorSubgraph(node.data.params.subgraph)
    if (subgraph) {
      collectVariablesFromRuntimeNodes(subgraph.nodes, target)
    }
  }
}

function collectVariablesFromFlowNodes(sourceNodes: EditorFlowNode[], target: Set<string>): void {
  for (const node of sourceNodes) {
    collectVariablesFromFlowNode(node, target)
  }
}

function collectVariablesFromRuntimeNodes(sourceNodes: RuntimeNode[], target: Set<string>): void {
  for (const node of sourceNodes) {
    collectVariablesFromNodeParams(node.type, node.params ?? {}, target)

    if (node.type === GROUP_NODE_TYPE) {
      const subgraph = readEditorSubgraph(node.params?.subgraph)
      if (subgraph) {
        collectVariablesFromRuntimeNodes(subgraph.nodes, target)
      }
    }
  }
}

function collectVariablesFromNodeParams(
  nodeType: string,
  params: Record<string, unknown>,
  target: Set<string>,
): void {
  addVariableName(target, params.messageIdVariable)
  addVariableName(target, params.buttonPayloadVariable)
  addVariableName(target, params.messageTextVariable)
  addVariableName(target, params.imageUrlVariable)
  addVariableName(target, params.captionVariable)
  addVariableName(target, params.variable)
  addVariableName(target, params.recordIdVariable)
  addVariableName(target, params.recordVariable)
  addVariableName(target, params.recordsVariable)
  addVariableName(target, params.countVariable)
  addVariableName(target, params.responseBodyVariable)
  addVariableName(target, params.responseStatusVariable)

  if (nodeType === 'get_user_info') {
    const prefix = readString(params.prefix) || 'user'
    addVariableName(target, `${prefix}_first_name`)
    addVariableName(target, `${prefix}_last_name`)
    addVariableName(target, `${prefix}_username`)
    addVariableName(target, `${prefix}_avatar_url`)
  }
}

function addVariableName(target: Set<string>, value: unknown): void {
  const variableName = readString(value)?.trim()
  if (variableName) {
    target.add(variableName)
  }
}

function cloneParams(params: Record<string, unknown>): Record<string, unknown> {
  return JSON.parse(JSON.stringify(params)) as Record<string, unknown>
}

function cloneNodeUi(value: unknown): EditorNodeUiState | undefined {
  if (!isRecord(value)) return undefined

  const ui: EditorNodeUiState = {}
  if (typeof value.textHeight === 'number' && Number.isFinite(value.textHeight)) {
    ui.textHeight = Math.max(24, Math.round(value.textHeight / EDITOR_GRID_SIZE) * EDITOR_GRID_SIZE)
  }

  return Object.keys(ui).length > 0 ? ui : undefined
}

function clonePastedNodeParams(
  nodeType: string,
  params: Record<string, unknown>,
  newNodeId: string,
): Record<string, unknown> {
  const cloned = cloneParams(params)
  if (isSmartButtonBranchingNodeType(nodeType)) {
    cloned[BUTTON_BRANCHING_NODE_IDS_PARAM] = createButtonBranchingCompiledNodeIds(newNodeId)
    cloned.buttonPayloadVariable = createButtonPayloadVariable(newNodeId)
  }

  return cloned
}

function normalizeParamsForNode(nodeType: string, params: Record<string, unknown>): Record<string, unknown> {
  const schema = getCatalogParams(nodeType)
  if (schema.length === 0) return normalizePlatformSpecificParams(nodeType, params)

  return normalizePlatformSpecificParams(nodeType, normalizeParamsBySchema(params, schema))
}

function normalizeDataNodeParams(
  backendNodeType: string,
  params: Record<string, unknown>,
): Record<string, unknown> {
  const next: Record<string, unknown> = {}

  if (['create_record', 'query_records'].includes(backendNodeType)) {
    next.entityName = params.entityName
  }

  if (['get_record', 'update_record', 'delete_record'].includes(backendNodeType)) {
    next.recordIdVariable = params.recordIdVariable
  }

  if (['create_record', 'update_record'].includes(backendNodeType)) {
    next.fields = params.fields
  }

  if (backendNodeType === 'query_records') {
    next.recordsVariable = params.recordsVariable
    next.filter = params.filter
    next.countVariable = params.countVariable
  }

  if (backendNodeType === 'create_record') {
    next.recordIdVariable = params.recordIdVariable
  }

  if (backendNodeType === 'get_record') {
    next.recordVariable = params.recordVariable
  }

  return normalizeParamsForNode(backendNodeType, next)
}

function normalizePlatformSpecificParams(
  nodeType: string,
  params: Record<string, unknown>,
): Record<string, unknown> {
  const next = { ...params }

  if (nodeType === 'receive_message') {
    normalizeReceiveMessageValidationParams(next, next.validatorType)
  }

  if (nodeType === 'send_buttons') {
    next.buttons = normalizeButtonBranchingButtonRows(next.buttons)
  }

  if (nodeType === MESSAGE_NODE_TYPE) {
    delete next.buttons
    delete next.isEntry
    delete next.buttonPayloadVariable
    delete next[BUTTON_BRANCHING_NODE_IDS_PARAM]
  }

  if (nodeType === EDIT_MESSAGE_NODE_TYPE) {
    next.buttons = normalizeButtonBranchingButtonRows(next.buttons)
    delete next.targetMessageNodeId
    delete next.isEntry
    delete next.buttonPayloadVariable
    delete next[BUTTON_BRANCHING_NODE_IDS_PARAM]

    if (Array.isArray(next.buttons) && next.buttons.length === 0) {
      delete next.buttons
    }
  }

  if (nodeType === 'delete_message') {
    delete next.targetMessageNodeId
  }

  if (nodeType === 'http_request') {
    normalizeHttpRequestParams(next)
  }

  if (nodeType === 'vk_send_keyboard') {
    delete next.inline
  }

  return next
}

function normalizeHttpRequestParams(params: Record<string, unknown>): void {
  if (typeof params.method !== 'string' || !params.method.trim()) {
    params.method = 'GET'
  }

  if (typeof params.bodyType !== 'string' || !params.bodyType.trim()) {
    params.bodyType = 'json'
  }

  if (params.method === 'GET' || !readString(params.body)) {
    delete params.body
    delete params.bodyType
  }

  if (!readString(params.responseStatusVariable)) {
    delete params.responseStatusVariable
  }

  if (isRecord(params.headers)) {
    const headers = Object.fromEntries(
      Object.entries(params.headers)
        .map(([key, value]) => [key.trim(), typeof value === 'string' ? value : String(value)] as const)
        .filter(([key, value]) => key.length > 0 && value.trim().length > 0),
    )

    if (Object.keys(headers).length > 0) {
      params.headers = headers
    } else {
      delete params.headers
    }
  } else {
    delete params.headers
  }
}

function normalizeParamsBySchema(
  params: Record<string, unknown>,
  schema: NodeParamItem[],
): Record<string, unknown> {
  const schemaByKey = new Map(schema.map(item => [item.key, item]))
  const result: Record<string, unknown> = { ...params }

  for (const [key, value] of Object.entries(params)) {
    const paramSchema = schemaByKey.get(key)
    if (!paramSchema) continue

    const normalized = normalizeParamValue(value, paramSchema)
    if (normalized === undefined) {
      delete result[key]
    } else {
      result[key] = normalized
    }
  }

  return result
}

function normalizeParamValue(value: unknown, schema: NodeParamItem): unknown {
  const type = normalizeNodeParamType(schema.type)

  if (type === 'int') {
    return normalizeIntParamValue(value)
  }

  if (type === 'object' && isRecord(value)) {
    return normalizeParamsBySchema(value, schema.fields ?? [])
  }

  if (type === 'objectlist' && Array.isArray(value)) {
    return value.map(item => isRecord(item) ? normalizeParamsBySchema(item, schema.fields ?? []) : item)
  }

  if (type === 'objectmatrix' && Array.isArray(value)) {
    return value.map(row =>
      Array.isArray(row)
        ? row.map(item => isRecord(item) ? normalizeParamsBySchema(item, schema.fields ?? []) : item)
        : row,
    )
  }

  return value
}

function normalizeIntParamValue(value: unknown): unknown {
  if (value === null || value === undefined || value === '') return undefined
  if (typeof value === 'number') return Number.isInteger(value) ? value : value
  if (typeof value !== 'string') return value

  const trimmed = value.trim()
  if (!trimmed) return undefined

  const numberValue = Number(trimmed)
  return Number.isInteger(numberValue) ? numberValue : value
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function isRoutePoint(value: unknown): value is ScenarioRoutePoint {
  return isRecord(value)
    && typeof value.x === 'number'
    && Number.isFinite(value.x)
    && typeof value.y === 'number'
    && Number.isFinite(value.y)
}

function createGraphId(): string {
  return crypto.randomUUID?.() ?? `node-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function isEditableTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false
  return Boolean(target.closest('input, textarea, select, [contenteditable="true"]'))
}

// ── Соединения ────────────────────────────────────────────────────────────────
function onConnect(params: Connection): void {
  if (isReadOnly.value) return
  addEdges([createFlowEdge(params.source, params.target, params.sourceHandle, params.targetHandle)])
}

// ── Мутация параметров ────────────────────────────────────────────────────────
function updateParamById(nodeId: string, key: string, value: unknown): void {
  if (isReadOnly.value) return
  const node = nodes.value.find(n => n.id === nodeId)
  if (node) {
    const nodeData = (node.data ?? { type: '', params: {} }) as EditorNodeData
    const previousParams = nodeData.params
    const nextParams = { ...nodeData.params, [key]: value }

    if (nodeData.type === 'receive_message' && key === 'validatorType') {
      normalizeReceiveMessageValidationParams(nextParams, value)
    }

    if (nodeData.type === 'receive_message' && key === 'validationParams') {
      normalizeReceiveMessageValidationParams(nextParams, nextParams.validatorType)
    }

    if (nodeData.type === 'switch' && key === 'cases') {
      const remap = normalizeSwitchCaseBranchKeys(previousParams.cases, nextParams)
      if (remap.size) {
        remapOutputConnections(nodeId, remap)
      }
    }

    if (isSmartButtonBranchingNodeType(nodeData.type) && key === 'buttons') {
      nextParams.buttons = normalizeButtonBranchingButtonParams(value)
      const remap = createButtonBranchingPayloadRemap(previousParams.buttons, nextParams.buttons)
      if (remap.size) {
        remapOutputConnections(nodeId, remap)
      }
    }

    node.data = { ...nodeData, params: nextParams }
  }
}

function updateNodeUiById(nodeId: string, value: EditorNodeUiState): void {
  if (isReadOnly.value) return

  const nextUi = cloneNodeUi(value)
  nodes.value = nodes.value.map((node) => {
    if (node.id !== nodeId) return node

    return {
      ...node,
      data: {
        ...node.data,
        ui: nextUi,
      },
    }
  })
}

function normalizeSwitchCaseBranchKeys(
  previousCasesValue: unknown,
  params: Record<string, unknown>,
): Map<string, string> {
  const previousCases = readSwitchCases(previousCasesValue)
  const nextCases = readSwitchCases(params.cases)
  const usedKeys = new Set<string>()
  const remap = new Map<string, string>()

  params.cases = nextCases.map((item, index) => {
    const value = item.value
    const branchKey = createSwitchBranchKey(value, index, usedKeys)
    const previousBranchKey = previousCases[index]?.branchKey

    if (previousBranchKey && previousBranchKey !== branchKey) {
      remap.set(previousBranchKey, branchKey)
    }

    return {
      ...item.raw,
      value,
      branchKey,
    }
  })

  return remap
}

function remapOutputConnections(nodeId: string, remap: Map<string, string>): void {
  edges.value = edges.value.map(edge => {
    if (edge.source !== nodeId || !edge.sourceHandle) return edge

    const nextSourceHandle = remap.get(edge.sourceHandle)
    if (!nextSourceHandle) return edge

    return {
      ...edge,
      id: `${edge.source}-${nextSourceHandle}-${edge.target}-${edge.targetHandle ?? 'default'}`,
      sourceHandle: nextSourceHandle,
      label: edgeLabelForSource(edge.source, nextSourceHandle),
    }
  })
}

function createButtonBranchingPayloadRemap(
  previousButtonsValue: unknown,
  nextButtonsValue: unknown,
): Map<string, string> {
  const previousButtons = readButtonBranchingButtons(previousButtonsValue)
  const nextButtons = readButtonBranchingButtons(nextButtonsValue)
  const remap = new Map<string, string>()

  nextButtons.forEach((button, index) => {
    const previousPayload = previousButtons[index]?.payload
    if (previousPayload && previousPayload !== button.payload) {
      remap.set(previousPayload, button.payload)
    }
  })

  return remap
}

function normalizeButtonBranchingButtonParams(
  value: unknown,
): Array<Array<{ label: string; payload: string; style?: string }>> {
  return normalizeButtonBranchingButtonRows(value)
}

function readSwitchCases(value: unknown): Array<{
  raw: Record<string, unknown>
  value: string
  branchKey: string
}> {
  if (!Array.isArray(value)) return []

  return value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map(item => ({
      raw: item,
      value: stringValue(item.value),
      branchKey: stringValue(item.branchKey),
    }))
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value.trim() : ''
}

function rawStringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function createSwitchBranchKey(value: string, index: number, usedKeys: Set<string>): string {
  const base = value.trim()
    .replace(/\s+/g, '_')
    .replace(/[^\p{L}\p{N}_-]+/gu, '_')
    .replace(/^_+|_+$/g, '')
    || `case_${index + 1}`

  let candidate = base
  let suffix = 2
  while (usedKeys.has(candidate)) {
    candidate = `${base}_${suffix}`
    suffix += 1
  }

  usedKeys.add(candidate)
  return candidate
}

function normalizeReceiveMessageValidationParams(
  params: Record<string, unknown>,
  validatorTypeValue: unknown,
): void {
  const validatorType = typeof validatorTypeValue === 'string' ? validatorTypeValue.trim() : ''
  const validationParams = isRecord(params.validationParams) ? params.validationParams : {}

  if (!validatorType) {
    delete params.validatorType
    delete params.validationParams
    delete params.errorMessage
    return
  }

  params.validatorType = validatorType

  if (validatorType === 'regex') {
    params.validationParams = {
      pattern: stringRecordValue(validationParams, 'pattern'),
    }
    return
  }

  if (validatorType === 'number') {
    params.validationParams = {
      min: stringRecordValue(validationParams, 'min'),
      max: stringRecordValue(validationParams, 'max'),
    }
    return
  }

  delete params.validationParams
}

function stringRecordValue(record: Record<string, unknown>, key: string): string {
  const value = record[key]
  if (value === null || value === undefined) return ''
  return typeof value === 'string' ? value : String(value)
}

</script>

<style scoped>
.editor {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: var(--color-bg);
}

.editor__scenario-errors {
  flex: 0 0 auto;
  margin: 10px 12px 0;
  padding: 12px 14px;
  border: 0.5px solid color-mix(in srgb, var(--color-danger) 35%, var(--color-border));
  border-radius: 8px;
  background: var(--color-bg-card);
  color: var(--color-danger);
  font-size: 13px;
}

.editor__scenario-errors-title,
.editor__scenario-error-message,
.editor__scenario-error-meta {
  margin: 0;
}

.editor__scenario-errors-title {
  margin-bottom: 8px;
  font-weight: 700;
}

.editor__scenario-error + .editor__scenario-error {
  margin-top: 8px;
}

.editor__scenario-error-message {
  font-weight: 600;
}

.editor__scenario-error-meta {
  margin-top: 4px;
  color: var(--color-text-secondary);
  font-size: 12px;
}

.editor__scenario-error-action {
  margin-top: 8px;
  padding: 0;
  border: 0;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  font-size: 12px;
  font-weight: 700;
}

.editor__scenario-error-action:hover {
  text-decoration: underline;
}

/* ── Тело ── */
.editor__body {
  display: flex;
  flex: 1;
  overflow: hidden;
}

</style>
