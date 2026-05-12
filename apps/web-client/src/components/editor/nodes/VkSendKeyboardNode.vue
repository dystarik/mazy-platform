<template>
  <BaseNode
    class="vk-send-keyboard-node"
    :class="{ 'vk-send-keyboard-node--branched': keyboardOutputPorts.length > 0 }"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="keyboardOutputPorts.length === 0"
    :has-output-ports="keyboardOutputPorts.length > 0"
    :output-ports="keyboardOutputPorts"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="vk-send-keyboard-node__body">
      <label class="vk-send-keyboard-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="vk-send-keyboard-node__label">Текст</span>
        <EditorGridTextarea
          :model-value="text"
          :readonly="isReadOnly"
          placeholder="Сообщение над клавиатурой"
          :min-rows="2"
          :height="textHeight"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('text', value)"
          @update:height="updateTextHeight"
        />
      </label>

      <div class="vk-send-keyboard-node__section">
        <VkButtonMatrix
          :model-value="params.buttons"
          :schema="buttonsSchema"
          :readonly="isReadOnly"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          title="Кнопки"
          @update:model-value="value => updateParam('buttons', value)"
        />
      </div>

      <label class="vk-send-keyboard-node__option nodrag" @mousedown.stop @pointerdown.stop>
        <Checkbox
          binary
          :model-value="deleteAfterButtonPress"
          :disabled="isReadOnly"
          @update:model-value="value => updateParam('deleteAfterButtonPress', Boolean(value))"
        />
        <span>Скрыть после нажатия</span>
      </label>

      <label
        v-if="deleteAfterButtonPress"
        class="vk-send-keyboard-node__field vk-send-keyboard-node__field--compact nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="vk-send-keyboard-node__label">Текст скрытия</span>
        <EditorGridTextarea
          :model-value="removeKeyboardText"
          :readonly="isReadOnly"
          placeholder="Действие выполнено"
          :min-rows="2"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('removeKeyboardText', value)"
        />
      </label>

    </div>

  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Checkbox from 'primevue/checkbox'
import type { NodeParamItem } from '@/types/api'
import {
  type EditorNodeUiState,
} from '@/components/editor/editorTypes'
import { describeEditorNodeLayout } from '@/components/editor/editorNodeLayoutContract'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'
import VkButtonMatrix from './VkButtonMatrix.vue'

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

const fallbackButtonsSchema = {
  key: 'buttons',
  type: 'NODE_PARAM_TYPE_OBJECT_MATRIX',
  isRequired: false,
} as NodeParamItem

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const text = computed(() => stringValue(props.params.text))
const deleteAfterButtonPress = computed(() => props.params.deleteAfterButtonPress === true)
const removeKeyboardText = computed(() => stringValue(props.params.removeKeyboardText) || 'Действие выполнено')
const textHeight = computed(() => props.uiState?.textHeight)
const keyboardOutputPorts = computed(() =>
  describeEditorNodeLayout({ type: props.nodeType, params: props.params }).outputPorts,
)
const buttonsSchema = computed(() =>
  props.catalogParams.find(param => param.key === 'buttons') ?? fallbackButtonsSchema,
)
const mainPortStyle: CSSProperties = { top: '72px' }

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
.vk-send-keyboard-node {
  width: 252px;
  min-width: 252px;
  max-width: 252px;
}

.vk-send-keyboard-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.vk-send-keyboard-node__field,
.vk-send-keyboard-node__section {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.vk-send-keyboard-node__section,
.vk-send-keyboard-node__option,
.vk-send-keyboard-node__field--compact {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.vk-send-keyboard-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.vk-send-keyboard-node__option {
  display: flex;
  align-items: center;
  gap: 6px;
  min-height: 24px;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.vk-send-keyboard-node__option :deep(.p-checkbox) {
  width: 14px;
  height: 14px;
  flex: 0 0 14px;
}

.vk-send-keyboard-node__option :deep(.p-checkbox-box) {
  width: 14px;
  height: 14px;
  border-radius: 4px;
}

</style>
