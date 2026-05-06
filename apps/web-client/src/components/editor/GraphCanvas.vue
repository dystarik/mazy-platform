<template>
  <div
    class="editor__canvas"
    @dragover.prevent
    @drop="$emit('drop', $event)"
    @wheel.capture="$emit('canvasWheel', $event)"
    @pointerdown.capture="$emit('canvasPointerDownCapture', $event)"
    @mousedown.capture="$emit('canvasMouseDownCapture', $event)"
    @contextmenu.prevent
  >
    <VueFlow
      v-model:nodes="nodesModel"
      v-model:edges="edgesModel"
      :default-viewport="{ zoom: 1 }"
      :min-zoom="0.3"
      :max-zoom="2"
      :snap-to-grid="true"
      :snap-grid="[12, 12]"
      fit-view-on-init
      :connection-line-type="ConnectionLineType.SmoothStep"
      :default-edge-options="defaultEdgeOptions"
      :delete-key-code="isReadOnly ? null : 'Delete'"
      :pan-on-drag="[0]"
      :selection-key-code="null"
      :multi-selection-key-code="null"
      :nodes-draggable="!isReadOnly"
      :nodes-connectable="!isReadOnly"
      :edges-updatable="!isReadOnly"
      :zoom-on-scroll="false"
      :pan-on-scroll="false"
      @connect="$emit('connect', $event)"
      @node-click="$emit('nodeClick', $event)"
      @node-context-menu="$emit('nodeContextMenu', $event)"
      @node-drag-start="$emit('nodeDragStart', $event)"
      @node-drag="$emit('nodeDrag', $event)"
      @node-drag-stop="$emit('nodeDragStop', $event)"
      @selection-drag-start="$emit('nodeDragStart', $event)"
      @selection-drag="$emit('nodeDrag', $event)"
      @selection-drag-stop="$emit('nodeDragStop', $event)"
      @edge-click="$emit('edgeClick', $event)"
      @edge-context-menu="$emit('edgeContextMenu', $event)"
      @pane-click="$emit('paneClick', $event)"
    >
      <Background :gap="12" />
      <Controls />

      <template #node-default="nodeProps">
        <component
          :is="getNodeComponent(nodeProps.data.type)"
          :node-type="nodeProps.data.type"
          :params="nodeProps.data.params"
          :catalog-params="catalogParamsFor(nodeProps.data.type)"
          :project-schema-details="projectSchemaDetails"
          :is-start="startNodeId === nodeProps.id"
          :is-selected="nodeProps.selected"
          :is-read-only="isReadOnly"
          :is-pickable="isMessagePickableNode(nodeProps.id)"
          :is-pick-target="messagePickerTargetNodeId === nodeProps.id"
          :is-related="isRelatedMessageNode(nodeProps.id)"
          :relation-tone="messageRelationToneForNode(nodeProps.id)"
          :label="getNodeLabel(nodeProps.data.type)"
          :ui-state="nodeProps.data.ui"
          :known-variables="knownVariables"
          :variable-scope="variableScopeForNode(nodeProps.id)"
          :message-source-label="getMessageSourceLabel(nodeProps.id)"
          @update-param="(key: string, val: unknown) => $emit('updateParam', nodeProps.id, key, val)"
          @update-ui="(value: EditorNodeUiState) => emitNodeUi(nodeProps.id, value)"
          @pick-message="$emit('pickMessage', nodeProps.id)"
          @set-start="$emit('setStart', nodeProps.id)"
          @delete="$emit('deleteNode', nodeProps.id)"
        />
      </template>

      <template #edge-routed="edgeProps">
        <RoutedEdge v-bind="edgeProps" />
      </template>
    </VueFlow>

    <div
      v-if="rightSelectionVisible"
      class="editor__selection-rect"
      :style="rightSelectionStyle"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import {
  ConnectionLineType,
  VueFlow,
  type Connection,
  type DefaultEdgeOptions,
  type Edge,
  type EdgeMouseEvent,
  type NodeDragEvent,
  type NodeMouseEvent,
} from '@vue-flow/core'
import { Background } from '@vue-flow/background'
import { Controls } from '@vue-flow/controls'
import type { NodeParamItem } from '@/types/api'
import type { GetEntitySchemaResponse } from '@/types/api/entity-schemas.types'
import type { EditorFlowNode, EditorNodeUiState } from '@/components/editor/editorTypes'
import type { VariableScope } from '@/components/editor/variableHighlight'
import RoutedEdge from '@/components/editor/edges/RoutedEdge.vue'
import { getNodeComponent } from '@/components/editor/nodes/nodeRegistry'
import { getNodeLabel } from '@/components/editor/nodes/nodeMeta'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import '@vue-flow/controls/dist/style.css'

const props = defineProps<{
  nodes: EditorFlowNode[]
  edges: Edge[]
  isReadOnly: boolean
  startNodeId: string | null
  messagePickerTargetNodeId: string | null
  rightSelectionVisible: boolean
  rightSelectionStyle: CSSProperties
  catalogParamsFor: (type: string) => NodeParamItem[]
  projectSchemaDetails: GetEntitySchemaResponse[]
  knownVariables: string[]
  variableScopeForNode?: (nodeId: string) => VariableScope
  isMessagePickableNode: (nodeId: string) => boolean
  isRelatedMessageNode: (nodeId: string) => boolean
  messageRelationToneForNode: (nodeId: string) => 'edit' | 'delete' | 'source' | 'jump' | null
  messageSourceLabelFor: (nodeId: string) => string | null
}>()

const emit = defineEmits<{
  'update:nodes': [nodes: EditorFlowNode[]]
  'update:edges': [edges: Edge[]]
  drop: [event: DragEvent]
  canvasWheel: [event: WheelEvent]
  canvasPointerDownCapture: [event: PointerEvent]
  canvasMouseDownCapture: [event: MouseEvent]
  connect: [params: Connection]
  nodeClick: [event: NodeMouseEvent]
  nodeContextMenu: [event: NodeMouseEvent]
  nodeDragStart: [event: NodeDragEvent]
  nodeDrag: [event: NodeDragEvent]
  nodeDragStop: [event: NodeDragEvent]
  edgeClick: [event: EdgeMouseEvent]
  edgeContextMenu: [event: EdgeMouseEvent]
  paneClick: [event: MouseEvent]
  updateParam: [nodeId: string, key: string, value: unknown]
  updateNodeUi: [nodeId: string, value: EditorNodeUiState]
  pickMessage: [nodeId: string]
  setStart: [nodeId: string]
  deleteNode: [nodeId: string]
}>()

function emitNodeUi(nodeId: string, value: EditorNodeUiState): void {
  emit('updateNodeUi', nodeId, value)
}

const defaultEdgeOptions: DefaultEdgeOptions = {
  type: 'routed',
}

const nodesModel = computed({
  get: () => props.nodes,
  set: (value: EditorFlowNode[]) => emit('update:nodes', value),
})

const edgesModel = computed({
  get: () => props.edges,
  set: (value: Edge[]) => emit('update:edges', value),
})

function getMessageSourceLabel(nodeId: string): string | null {
  return typeof props.messageSourceLabelFor === 'function'
    ? props.messageSourceLabelFor(nodeId)
    : null
}

function variableScopeForNode(nodeId: string): VariableScope {
  return typeof props.variableScopeForNode === 'function'
    ? props.variableScopeForNode(nodeId)
    : { available: props.knownVariables, future: [] }
}
</script>

<style scoped>
.editor__canvas {
  flex: 1;
  position: relative;
  overflow: hidden;
}

.editor__selection-rect {
  position: absolute;
  z-index: 20;
  border: 1.5px solid var(--color-primary);
  border-radius: 4px;
  background: color-mix(in srgb, var(--color-primary) 8%, transparent);
  pointer-events: none;
}

:deep(.vue-flow__node-default) {
  padding: 0 !important;
  border: none !important;
  border-radius: 10px !important;
  background: transparent !important;
  box-shadow: none !important;
  width: auto !important;
}

:deep(.vue-flow__node-default.selected) {
  box-shadow: none !important;
}

:deep(.vue-flow__nodesselection-rect) {
  border: none !important;
  background: transparent !important;
}

:deep(.vue-flow__selection) {
  background: color-mix(in srgb, var(--color-primary) 8%, transparent);
  border: 1.5px solid var(--color-primary);
  border-radius: 4px;
}

:deep(.vue-flow) {
  background: var(--color-bg);
}

:deep(.vue-flow__background) {
  background: var(--color-bg);
}

:deep(.vue-flow__edge-path) {
  stroke: var(--color-primary);
  stroke-width: 2;
  stroke-linecap: square;
  stroke-linejoin: miter;
}

:deep(.vue-flow__edge-text) {
  font-size: 10px;
  fill: var(--color-text-secondary);
}

:deep(.vue-flow__edge-textbg) {
  fill: var(--color-bg-card);
}

:deep(.vue-flow__edge.animated path) {
  animation: none;
}

:deep(.vue-flow__handle) {
  box-sizing: border-box;
  width: 8px;
  height: 8px;
  background: var(--color-primary);
  border: 1px solid var(--color-bg-card);
}

:deep(.vue-flow__controls) {
  bottom: 16px;
  left: 16px;
  background: var(--color-bg-card);
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  overflow: hidden;
}

:deep(.vue-flow__controls-button) {
  background: var(--color-bg-card);
  border: none;
  border-bottom: 0.5px solid var(--color-border);
  color: var(--color-text);
  fill: var(--color-text);
  transition: background 0.15s;
}

:deep(.vue-flow__controls-button:hover) {
  background: var(--color-bg-secondary);
}

:deep(.vue-flow__controls-button:last-child) {
  border-bottom: none;
}
</style>
