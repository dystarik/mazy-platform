<template>
  <BaseNode
    class="vk-remove-keyboard-node"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :style="nodeLayoutStyle"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="vk-remove-keyboard-node__body">
      <label class="vk-remove-keyboard-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="vk-remove-keyboard-node__label">Текст</span>
        <EditorGridTextarea
          :model-value="text"
          :readonly="isReadOnly"
          placeholder="Сообщение вместе с удалением клавиатуры"
          :min-rows="2"
          :height="textHeight"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('text', value)"
          @update:height="updateTextHeight"
        />
      </label>

      <label class="vk-remove-keyboard-node__field vk-remove-keyboard-node__field--compact nodrag" @mousedown.stop @pointerdown.stop>
        <span class="vk-remove-keyboard-node__label">ID сообщения</span>
        <EditorVariableInput
          :model-value="messageIdVariable"
          :readonly="isReadOnly"
          placeholder="message_id"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('messageIdVariable', stringValue(value))"
        />
      </label>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import type { NodeParamItem } from '@/types/api'
import type { EditorNodeUiState } from '@/components/editor/editorTypes'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'
import { getEditorNodeLayoutMetrics } from '@/components/editor/editorLayoutMetrics'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'

const props = defineProps<{
  nodeType: string
  params: Record<string, unknown>
  catalogParams: NodeParamItem[]
  isStart: boolean
  isSelected: boolean
  isReadOnly?: boolean
  isPickable?: boolean
  isPickTarget?: boolean
  isRelated?: boolean
  label: string
  uiState?: EditorNodeUiState
  knownVariables?: string[]
  variableScope?: VariableScope
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'update-ui': [value: EditorNodeUiState]
  'set-start': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const text = computed(() => stringValue(props.params.text))
const messageIdVariable = computed(() => stringValue(props.params.messageIdVariable))
const textHeight = computed(() => props.uiState?.textHeight)
const layoutMetrics = computed(() => getEditorNodeLayoutMetrics({
  type: props.nodeType,
  params: props.params,
  ui: props.uiState,
}))
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutMetrics.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutMetrics.value.width}px`,
  '--node-min-height': `${layoutMetrics.value.height}px`,
}) as CSSProperties)

function updateParam(key: string, value: unknown): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function updateTextHeight(value: number): void {
  if (props.isReadOnly) return
  emit('update-ui', { ...(props.uiState ?? {}), textHeight: value })
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}
</script>

<style scoped>
.vk-remove-keyboard-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.vk-remove-keyboard-node__field {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 8px;
}

.vk-remove-keyboard-node__field--compact {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 8px;
}

.vk-remove-keyboard-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}
</style>
