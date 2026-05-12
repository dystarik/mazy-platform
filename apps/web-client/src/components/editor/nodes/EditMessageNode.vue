<template>
  <BaseNode
    class="edit-message-node"
    :class="{ 'edit-message-node--branched': buttonOutputPorts.length > 0 }"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="buttonOutputPorts.length === 0"
    :has-output-ports="buttonOutputPorts.length > 0"
    :output-ports="buttonOutputPorts"
    :input-style="mainPortStyle"
    :output-style="mainOutputStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="edit-message-node__body">
      <div
        class="edit-message-node__target nodrag"
        :class="{ 'edit-message-node__target--missing': hasMissingTarget }"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="edit-message-node__label">{{ messageSourceLabel ? 'Выбран узел' : 'Редактировать' }}</span>
        <div class="edit-message-node__target-control">
          <Button
            class="edit-message-node__target-button"
            type="button"
            :disabled="isReadOnly"
            :label="targetLabel"
            @click.stop="emit('pick-message')"
          />
          <Button
            v-if="hasTargetNode && !isReadOnly"
            class="edit-message-node__clear-target"
            type="button"
            title="Отменить выбор"
            label="×"
            @click.stop="clearTarget"
          />
        </div>
      </div>

      <label
        class="edit-message-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="edit-message-node__label">Новый текст</span>
        <EditorGridTextarea
          class="edit-message-node__textarea"
          :model-value="messageText"
          :readonly="isReadOnly"
          placeholder="Новый текст сообщения"
          :min-rows="2"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="updateText"
        />
      </label>

      <div class="edit-message-node__buttons">
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
          class="edit-message-node__option nodrag"
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
import Button from 'primevue/button'
import Checkbox from 'primevue/checkbox'
import type { NodeParamItem } from '@/types/api'
import {
  flattenButtonBranchingRows,
  readButtonBranchingButtonRows,
} from '@/components/editor/editorTypes'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { describeEditorNodeLayout } from '@/components/editor/editorNodeLayoutContract'
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
  messageSourceLabel?: string | null
  knownVariables?: string[]
  variableScope?: VariableScope
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  'pick-message': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const messageText = computed(() => stringValue(props.params.newText))
const buttonRows = computed(() => readButtonBranchingButtonRows(props.params.buttons))
const buttons = computed(() => flattenButtonBranchingRows(buttonRows.value))
const buttonOutputPorts = computed(() =>
  describeEditorNodeLayout({ type: props.nodeType, params: props.params }).outputPorts,
)
const deleteAfterButtonPress = computed(() => props.params.deleteAfterButtonPress === true)
const hasMissingTarget = computed(() =>
  Boolean(props.params.targetMessageNodeId) && !props.messageSourceLabel,
)
const hasTargetNode = computed(() => Boolean(props.params.targetMessageNodeId))
const targetLabel = computed(() => {
  if (props.messageSourceLabel) return props.messageSourceLabel
  if (hasMissingTarget.value) return 'Сообщение удалено'
  return 'Выбрать сообщение'
})
const buttonsSchema = computed(() =>
  props.catalogParams.find(param => param.key === 'buttons')
  ?? fallbackButtonsSchema,
)
const fallbackButtonsSchema = (BUTTON_BRANCHING_CATALOG_ITEM.schema?.find(param => param.key === 'buttons') ?? {
  key: 'buttons',
  type: 'NODE_PARAM_TYPE_OBJECT_MATRIX',
  isRequired: false,
}) as NodeParamItem
const MAIN_EDIT_MESSAGE_PORT_Y = 72
const mainPortStyle: CSSProperties = { top: `${MAIN_EDIT_MESSAGE_PORT_Y}px` }
const mainOutputStyle: CSSProperties = { top: `${MAIN_EDIT_MESSAGE_PORT_Y}px` }

function updateText(value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'newText', value)
}

function clearTarget(): void {
  if (props.isReadOnly) return
  emit('update-param', 'targetMessageNodeId', '')
  emit('update-param', 'messageIdVariable', '')
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
.edit-message-node {
  width: 228px;
  min-width: 228px;
  max-width: 228px;
}

.edit-message-node--branched {
  width: 228px;
  min-width: 228px;
  max-width: 228px;
}

.edit-message-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.edit-message-node__target,
.edit-message-node__field,
.edit-message-node__buttons {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 6px;
}

.edit-message-node__field,
.edit-message-node__buttons {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 6px;
  position: relative;
}

.edit-message-node__label,
.edit-message-node__buttons-head,
.edit-message-node__row-actions,
.edit-message-node__inline-add {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: left;
}

.edit-message-node__label {
  align-self: flex-start;
}

.edit-message-node__buttons-head,
.edit-message-node__row-actions {
  display: flex;
  align-items: center;
  justify-content: space-between;
  min-height: 24px;
}

.edit-message-node__target-control {
  display: flex;
  align-items: center;
  gap: 6px;
  min-width: 0;
}

.edit-message-node__target-button {
  box-sizing: border-box;
  display: flex;
  align-items: center;
  flex: 1;
  min-width: 0;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  font-size: 11px;
  padding: 3px 8px;
  text-align: left;
}

.edit-message-node__clear-target {
  box-sizing: border-box;
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.edit-message-node__target-button:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.edit-message-node__target--missing .edit-message-node__target-button,
.edit-message-node__clear-target:hover {
  border-color: var(--color-danger);
  color: var(--color-danger);
}

.edit-message-node__button-line {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 2px;
  margin-bottom: 8px;
}

.edit-message-node__button-grid {
  position: relative;
  display: grid;
  align-items: start;
  gap: 6px;
  min-height: 24px;
}

.edit-message-node__button-row {
  position: static;
  display: flex;
  align-items: center;
  height: 24px;
  min-height: 24px;
  max-height: 24px;
  flex: 0 0 24px;
}

.edit-message-node__variable-input {
  flex: 1;
  min-width: 0;
}

.edit-message-node__button-control {
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

.edit-message-node__button-control:focus-within {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.edit-message-node__style-select {
  width: 60px;
  height: 24px;
  border: 0;
  background: color-mix(in srgb, var(--color-bg-secondary) 78%, transparent);
}

.edit-message-node__style-select :deep(.p-select-label) {
  padding: 4px 6px;
  font-size: 9px;
  line-height: 12px;
}

.edit-message-node__style-select :deep(.p-select-dropdown) {
  width: 16px;
}

.edit-message-node__icon-button,
.edit-message-node__row-button,
.edit-message-node__head-add {
  box-sizing: border-box;
  border-radius: 4px;
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.edit-message-node__icon-button {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  min-width: 24px;
  min-height: 24px;
  border: 0;
  background: transparent;
  color: var(--color-text-secondary);
}

.edit-message-node__icon-button:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}

.edit-message-node__head-add,
.edit-message-node__row-button {
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  background: var(--color-bg);
  color: var(--color-text-secondary);
}

.edit-message-node__inline-add {
  align-self: flex-start;
  justify-self: flex-start;
  border: 0;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  padding: 2px 0 0;
}

.edit-message-node__head-add:hover,
.edit-message-node__row-button:hover,
.edit-message-node__inline-add:hover {
  color: var(--color-primary);
}

.edit-message-node__option {
  display: flex;
  align-items: center;
  gap: 6px;
  min-height: 24px;
  padding-top: 6px;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.edit-message-node__option :deep(.p-checkbox) {
  width: 14px;
  height: 14px;
  flex: 0 0 14px;
}

.edit-message-node__option :deep(.p-checkbox-box) {
  width: 14px;
  height: 14px;
  border-radius: 4px;
}

.edit-message-node__handle {
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
