<template>
  <BaseNode
    class="message-node"
    :class="{ 'message-node--branched': buttons.length > 0 }"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="buttons.length === 0"
    :has-output-ports="false"
    :input-style="mainPortStyle"
    :output-style="mainOutputStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="message-node__body">
      <label
        class="message-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="message-node__label">Текст</span>
        <EditorGridTextarea
          class="message-node__textarea"
          :model-value="messageText"
          :readonly="isReadOnly"
          placeholder="Напишите сообщение"
          :min-rows="2"
          :height="textHeight"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="updateText"
          @update:height="updateTextHeight"
        />
      </label>

      <div class="message-node__buttons">
        <EditorButtonMatrix
          :model-value="buttonRows"
          :schema="buttonsSchema"
          :readonly="isReadOnly"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          title="Кнопки"
          @update:model-value="value => emit('update-param', 'buttons', value)"
        />

        <label
          v-if="buttons.length"
          class="message-node__option nodrag"
          @mousedown.stop
          @pointerdown.stop
        >
          <Checkbox
            binary
            :model-value="deleteAfterButtonPress"
            :disabled="isReadOnly"
            @update:model-value="value => updateDeleteAfterButtonPress(Boolean(value))"
          />
          <span>Удалять после нажатия</span>
        </label>
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Checkbox from 'primevue/checkbox'
import type { NodeParamItem } from '@/types/api'
import {
  flattenButtonBranchingRows,
  readButtonBranchingButtonRows,
  type EditorNodeUiState,
} from '@/components/editor/editorTypes'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { BUTTON_BRANCHING_CATALOG_ITEM } from '@/components/editor/scenario-adapters/editorNodeDefinitions'
import EditorButtonMatrix from '@/components/editor/EditorButtonMatrix.vue'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
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
const messageText = computed(() => stringValue(props.params.text))
const buttonRows = computed(() => readButtonBranchingButtonRows(props.params.buttons))
const buttons = computed(() => flattenButtonBranchingRows(buttonRows.value))
const deleteAfterButtonPress = computed(() => props.params.deleteAfterButtonPress === true)
const textHeight = computed(() => props.uiState?.textHeight)
const buttonsSchema = computed(() =>
  props.catalogParams.find(param => param.key === 'buttons')
  ?? fallbackButtonsSchema,
)
const fallbackButtonsSchema = (BUTTON_BRANCHING_CATALOG_ITEM.schema?.find(param => param.key === 'buttons') ?? {
  key: 'buttons',
  type: 'NODE_PARAM_TYPE_OBJECT_MATRIX',
  isRequired: false,
}) as NodeParamItem
const MAIN_MESSAGE_PORT_Y = 72
const mainPortStyle: CSSProperties = { top: `${MAIN_MESSAGE_PORT_Y}px` }
const mainOutputStyle: CSSProperties = { top: `${MAIN_MESSAGE_PORT_Y}px` }

function updateText(value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'text', value)
}

function updateTextHeight(value: number): void {
  if (props.isReadOnly) return
  emit('update-ui', { ...(props.uiState ?? {}), textHeight: value })
}

function updateDeleteAfterButtonPress(value: boolean): void {
  if (props.isReadOnly) return
  emit('update-param', 'deleteAfterButtonPress', value)
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

</script>

<style scoped>
.message-node {
  width: 228px;
  min-width: 228px;
  max-width: 228px;
}

.message-node--branched {
  width: 228px;
  min-width: 228px;
  max-width: 228px;
}

.message-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding-right: 0;
}

.message-node__field,
.message-node__buttons {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 6px;
}

.message-node__field {
  position: relative;
  padding-top: 0;
}

.message-node__buttons {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 6px;
  position: relative;
}

.message-node__label,
.message-node__buttons-head,
.message-node__row-actions,
.message-node__inline-add {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: left;
}

.message-node__label {
  align-self: flex-start;
}

.message-node__buttons-head,
.message-node__row-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 24px;
}

.message-node__button-line {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 2px;
  margin-bottom: 8px;
}

.message-node__button-grid {
  position: relative;
  display: grid;
  align-items: start;
  gap: 6px;
  min-height: 24px;
}

.message-node__button-row {
  position: static;
  display: flex;
  align-items: center;
  height: 24px;
  min-height: 24px;
  max-height: 24px;
  flex: 0 0 24px;
}

.message-node__variable-input {
  flex: 1;
  min-width: 0;
}

.message-node__button-control {
  box-sizing: border-box;
  width: 100%;
  height: 24px;
  min-height: 24px;
  max-height: 24px;
  min-width: 0;
  display: grid;
  grid-template-columns: minmax(0, 1fr) 60px 24px;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  padding: 0 5px 0 0;
}

.message-node__button-control:focus-within {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.message-node__style-select {
  width: 60px;
  height: 24px;
  border: 0;
  background: color-mix(in srgb, var(--color-bg-secondary) 78%, transparent);
}

.message-node__style-select :deep(.p-select-label) {
  padding: 4px 6px;
  font-size: 9px;
  line-height: 12px;
}

.message-node__style-select :deep(.p-select-dropdown) {
  width: 16px;
}

.message-node__icon-button,
.message-node__row-button,
.message-node__head-add {
  box-sizing: border-box;
  border-radius: 4px;
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.message-node__icon-button {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  min-width: 24px;
  min-height: 24px;
  border: 0;
  background: transparent;
  color: var(--color-text-secondary);
}

.message-node__icon-button:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}

.message-node__head-add,
.message-node__row-button {
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  background: var(--color-bg);
  color: var(--color-text-secondary);
}

.message-node__inline-add {
  align-self: flex-start;
  justify-self: flex-start;
  border: 0;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  padding: 2px 0 0;
}

.message-node__head-add:hover,
.message-node__row-button:hover,
.message-node__inline-add:hover {
  color: var(--color-primary);
}

.message-node__option {
  display: flex;
  align-items: center;
  gap: 6px;
  min-height: 24px;
  padding-top: 6px;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.message-node__option :deep(.p-checkbox) {
  width: 14px;
  height: 14px;
  flex: 0 0 14px;
}

.message-node__option :deep(.p-checkbox-box) {
  width: 14px;
  height: 14px;
  border-radius: 4px;
}

.message-node__handle {
  box-sizing: border-box;
  position: absolute !important;
  right: -16px;
  top: 50%;
  width: 8px !important;
  height: 8px !important;
  transform: translateY(-50%) !important;
  background: var(--color-primary) !important;
  border: 1px solid var(--color-bg-card) !important;
  border-radius: 50%;
  pointer-events: auto;
}
</style>
