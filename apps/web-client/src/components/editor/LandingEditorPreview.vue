<template>
  <div class="editor-demo">
    <div class="editor-demo__topbar">
      <span class="editor-demo__title">Мини-кафе / релиз сценария</span>
      <span class="editor-demo__status">Релиз</span>
    </div>

    <div class="editor-demo__body">
      <GraphCanvas
        v-model:nodes="nodes"
        v-model:edges="edges"
        :is-read-only="true"
        :start-node-id="previewScenario.startNodeId"
        :message-picker-target-node-id="null"
        :right-selection-visible="false"
        :right-selection-style="{}"
        :catalog-params-for="catalogParamsFor"
        :project-schema-details="[]"
        :pan-on-drag="[0]"
        :zoom-on-scroll="true"
        :known-variables="knownVariables"
        :variable-scope-for-node="variableScopeForNode"
        :is-message-pickable-node="() => false"
        :is-related-message-node="() => false"
        :message-relation-tone-for-node="() => null"
        :message-source-label-for="messageSourceLabelFor"
        @drop="noopDrag"
        @connect="noopConnect"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Connection } from '@vue-flow/core'
import type { NodeParamItem } from '@/types/api'
import type {
  EditorScenario,
  RuntimeNode,
} from '@/components/editor/scenario-adapters/editorScenario.types'
import type { VariableScope } from '@/components/editor/variableHighlight'
import GraphCanvas from '@/components/editor/GraphCanvas.vue'
import { getNodeLabel } from '@/components/editor/nodes/nodeMeta'
import { GROUP_NODE_TYPE } from '@/components/editor/editorTypes'
import { editorScenarioToFlow } from '@/components/editor/scenario-adapters/editorScenarioToFlow'
import demoScenario from '@/assets/demo/mini-cafe-scenario.json'

const previewScenario = demoScenario as EditorScenario
const previewFlow = editorScenarioToFlow(previewScenario, { edgeIdPrefix: 'demo' })
const nodes = ref(previewFlow.nodes)
const edges = ref(previewFlow.edges)
const knownVariables = computed(() => {
  const variables = new Set<string>()
  for (const node of nodes.value) {
    collectVariablesFromNodeParams(node.data.type, node.data.params, variables)

    if (node.data.type === GROUP_NODE_TYPE) {
      const subgraph = readEditorSubgraph(node.data.params.subgraph)
      if (subgraph) {
        collectVariablesFromRuntimeNodes(subgraph.nodes, variables)
      }
    }
  }

  return [...variables].sort((a, b) => a.localeCompare(b))
})
const demoVariableScope = computed<VariableScope>(() => ({
  available: knownVariables.value,
  future: [],
}))

function catalogParamsFor(type: string): NodeParamItem[] {
  return []
}

function variableScopeForNode(): VariableScope {
  return demoVariableScope.value
}

const nodeLabelById = computed(() => {
  return new Map(nodes.value.map((node) => [node.id, getNodeLabel(node.data.type)]))
})

function messageSourceLabelFor(nodeId: string): string | null {
  const node = nodes.value.find((candidate) => candidate.id === nodeId)
  const targetNodeId = typeof node?.data.params.targetNodeId === 'string'
    ? node.data.params.targetNodeId
    : ''

  return targetNodeId ? (nodeLabelById.value.get(targetNodeId) ?? null) : null
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

function readEditorSubgraph(value: unknown): { nodes: RuntimeNode[] } | null {
  if (!isRecord(value) || !Array.isArray(value.nodes)) return null

  return {
    nodes: value.nodes
      .filter((item): item is RuntimeNode => isRecord(item) && typeof item.id === 'string' && typeof item.type === 'string')
      .map(item => ({
        id: item.id,
        type: item.type,
        params: isRecord(item.params) ? item.params : {},
        ...(isRecord(item.position) && typeof item.position.x === 'number' && typeof item.position.y === 'number'
          ? { position: { x: item.position.x, y: item.position.y } }
          : {}),
      })),
  }
}

function readString(value: unknown): string | null {
  return typeof value === 'string' && value.trim() ? value.trim() : null
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function noopDrag(event: DragEvent): void {
  event.preventDefault()
}

function noopConnect(_params: Connection): void {
  // Static release preview.
}
</script>

<style scoped>
.editor-demo {
  width: 100%;
  height: 100%;
  min-height: 480px;
  border: 0.5px solid var(--color-border);
  border-radius: 18px;
  background: var(--color-bg-card);
  box-shadow: 0 24px 58px rgba(0, 0, 0, 0.12);
  overflow: hidden;
}

.editor-demo__topbar {
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 0 18px;
  border-bottom: 0.5px solid var(--color-border);
  background: var(--color-bg-card);
}

.editor-demo__title {
  overflow: hidden;
  color: var(--color-text);
  font-size: 13px;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.editor-demo__status {
  padding: 5px 10px;
  border-radius: 999px;
  background: color-mix(in srgb, #0f766e 13%, transparent);
  color: #0f766e;
  font-size: 12px;
  font-weight: 700;
}

.editor-demo__body {
  height: calc(100% - 50px);
  min-height: 430px;
  display: flex;
}

.editor-demo :deep(.vue-flow__pane) {
  cursor: grab;
}

.editor-demo :deep(.vue-flow__pane:active) {
  cursor: grabbing;
}

.editor-demo :deep(.vue-flow__controls) {
  display: flex;
  overflow: hidden;
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  background: var(--color-bg-card);
  box-shadow: 0 10px 24px rgba(0, 0, 0, 0.18);
}

.editor-demo :deep(.vue-flow__controls-button) {
  border-bottom-color: var(--color-border);
  background: var(--color-bg-card);
  color: var(--color-text);
}

.editor-demo :deep(.vue-flow__controls-button:hover) {
  background: var(--color-bg-soft);
}

.editor-demo :deep(.bn) {
  cursor: default;
}

@media (max-width: 760px) {
  .editor-demo {
    min-height: 410px;
  }

  .editor-demo__body {
    min-height: 360px;
  }
}
</style>
