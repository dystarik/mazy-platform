<template>
  <div class="button-matrix">
    <div class="button-matrix__head">
      <span>{{ title }}</span>
    </div>

    <Button
      v-if="!readonly && rows.length === 0"
      class="button-matrix__first-add nodrag"
      type="button"
      label="Добавить кнопку"
      :disabled="!canAddNewRow"
      @mousedown.stop
      @pointerdown.stop
      @click.stop="addRow"
    />

    <div
      v-for="(row, rowIndex) in rows"
      :key="`row-${rowIndex}`"
      class="button-matrix__row"
    >
      <div class="button-matrix__row-header">
        <span class="button-matrix__row-title">Ряд {{ rowIndex + 1 }}</span>
        <Button
          v-if="!readonly"
          class="button-matrix__row-add nodrag"
          type="button"
          label="+"
          title="Добавить кнопку в этот ряд"
          :disabled="!canAddButtonToRow(rowIndex)"
          @mousedown.stop
          @pointerdown.stop
          @click.stop="addButtonToRow(rowIndex)"
        />
      </div>

      <div class="button-matrix__grid">
        <div
          v-for="(button, buttonIndex) in row"
          :key="`${button.payload}-${buttonIndex}`"
          class="button-matrix__item"
        >
          <div
            class="button-matrix__control nodrag"
            :class="{ 'button-matrix__control--readonly': readonly }"
            @mousedown.stop
            @pointerdown.stop
          >
            <EditorVariableInput
              class="button-matrix__label"
              :model-value="button.label"
              :readonly="readonly"
              :placeholder="labelPlaceholder"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateLabel(rowIndex, buttonIndex, stringValue(value))"
            />
            <Select
              v-if="!readonly"
              class="button-matrix__style"
              :model-value="button.style ?? DEFAULT_BUTTON_STYLE"
              :options="styleOptions"
              option-label="label"
              option-value="value"
              overlay-class="editor-node-select-overlay"
              title="Цвет кнопки"
              @update:model-value="value => updateStyle(rowIndex, buttonIndex, stringValue(value))"
            />
            <Button
              v-if="!readonly"
              class="button-matrix__delete"
              type="button"
              title="Удалить кнопку"
              label="×"
              @click.stop="removeButton(rowIndex, buttonIndex)"
            />
          </div>
          <Handle
            :id="button.payload"
            type="source"
            :position="Position.Right"
            class="button-matrix__handle"
          />
        </div>
      </div>
    </div>

    <Button
      v-if="!readonly && rows.length > 0"
      class="button-matrix__add-row nodrag"
      type="button"
      label="+ Новый ряд"
      :disabled="!canAddNewRow"
      @mousedown.stop
      @pointerdown.stop
      @click.stop="addRow"
    />

    <p v-if="limitMessage" class="button-matrix__limit">{{ limitMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Handle, Position } from '@vue-flow/core'
import Button from 'primevue/button'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import {
  canAddObjectMatrixItem,
  canAddObjectMatrixRow,
  getObjectMatrixLimitMessage,
} from '@/components/editor/buttonMatrixLimits'
import {
  flattenButtonBranchingRows,
  readButtonBranchingButtonRows,
  type ButtonBranchingButtonRow,
} from '@/components/editor/editorTypes'
import type { VariableScope } from '@/components/editor/variableHighlight'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'

const DEFAULT_BUTTON_STYLE = 'primary'

const props = defineProps<{
  modelValue: unknown
  schema: NodeParamItem
  readonly?: boolean
  knownVariables?: string[]
  variableScope?: VariableScope
  title?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: ButtonBranchingButtonRow[]]
}>()

const rows = computed(() => readEditableRows(props.modelValue))
const buttons = computed(() => flattenButtonBranchingRows(rows.value))
const title = computed(() => props.title ?? props.schema.description ?? 'Кнопки')
const labelField = computed(() => props.schema.fields?.find(field => field.key === 'label') ?? null)
const labelPlaceholder = computed(() => labelField.value?.description ?? 'Текст кнопки')
const canAddNewRow = computed(() => canAddObjectMatrixRow(rows.value, props.schema, 1))
const limitMessage = computed(() => getObjectMatrixLimitMessage(rows.value, props.schema, 'кнопок'))
const styleOptions = [
  { value: 'primary', label: 'Осн.' },
  { value: 'success', label: 'Усп.' },
  { value: 'danger', label: 'Опас.' },
]

function addRow(): void {
  if (props.readonly || !canAddNewRow.value) return
  const index = buttons.value.length
  emit('update:modelValue', [
    ...rows.value,
    [{ label: `Кнопка ${index + 1}`, payload: createButtonPayload(index) }],
  ])
}

function addButtonToRow(rowIndex: number): void {
  if (props.readonly || !canAddButtonToRow(rowIndex)) return
  const next = cloneRows()
  const row = next[rowIndex]
  if (!row) return

  const index = buttons.value.length
  row.push({ label: `Кнопка ${index + 1}`, payload: createButtonPayload(index) })
  emit('update:modelValue', next)
}

function canAddButtonToRow(rowIndex: number): boolean {
  return canAddObjectMatrixItem(rows.value, rowIndex, props.schema)
}

function removeButton(rowIndex: number, buttonIndex: number): void {
  if (props.readonly) return
  const next = cloneRows()
  next[rowIndex]?.splice(buttonIndex, 1)
  emit('update:modelValue', next.filter(row => row.length > 0))
}

function updateLabel(rowIndex: number, buttonIndex: number, label: string): void {
  if (props.readonly) return
  const next = cloneRows()
  const button = next[rowIndex]?.[buttonIndex]
  if (!button) return

  button.label = label
  emit('update:modelValue', next)
}

function updateStyle(rowIndex: number, buttonIndex: number, style: string): void {
  if (props.readonly) return
  const next = cloneRows()
  const button = next[rowIndex]?.[buttonIndex]
  if (!button) return

  button.style = style === DEFAULT_BUTTON_STYLE ? undefined : style
  emit('update:modelValue', next)
}

function cloneRows(): ButtonBranchingButtonRow[] {
  return rows.value.map(row => row.map(button => ({ ...button })))
}

function readEditableRows(value: unknown): ButtonBranchingButtonRow[] {
  return readButtonBranchingButtonRows(value).map(row =>
    row.map(button => ({
      label: button.label,
      payload: button.payload,
      ...(button.style && button.style !== 'secondary' ? { style: button.style } : {}),
    })),
  )
}

function createButtonPayload(index: number): string {
  const usedPayloads = new Set(buttons.value.map(button => button.payload))
  let candidate = `button_${index + 1}`
  let suffix = 2
  while (usedPayloads.has(candidate)) {
    candidate = `button_${index + 1}_${suffix}`
    suffix += 1
  }
  return candidate
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}
</script>

<style scoped>
.button-matrix {
  --button-row-height: 24px;
  --button-inner-height: 24px;
  --button-style-width: 54px;
  --button-delete-width: 24px;
  --button-add-width: 24px;
  display: flex;
  flex-direction: column;
  gap: 0;
  position: relative;
}

.button-matrix__head {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  height: 12px;
  min-height: 12px;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  margin-bottom: 12px;
  text-align: left;
}

.button-matrix__first-add,
.button-matrix__add-row {
  margin-top: 6px;
}

.button-matrix__first-add,
.button-matrix__add-row {
  box-sizing: border-box;
  width: 100%;
  height: var(--button-row-height);
  min-height: var(--button-row-height);
  max-height: var(--button-row-height);
  border: 1px dashed color-mix(in srgb, var(--color-primary) 48%, var(--color-border));
  border-radius: 4px;
  background: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg));
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  font-size: 10px;
  line-height: 12px;
  padding: 0 6px;
}

.button-matrix__row {
  box-sizing: border-box;
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  grid-auto-rows: auto;
  gap: 0;
  border: 0;
  border-radius: 0;
  outline: 1px solid color-mix(in srgb, var(--color-primary) 26%, var(--color-border));
  outline-offset: -1px;
  background: color-mix(in srgb, var(--color-primary) 4%, var(--color-bg));
  padding: 0;
  overflow: visible;
}

.button-matrix__row-header {
  box-sizing: border-box;
  display: grid;
  grid-template-columns: minmax(0, 1fr) var(--button-add-width);
  align-items: center;
  height: var(--button-row-height);
  min-height: var(--button-row-height);
  max-height: var(--button-row-height);
  padding: 0;
  color: var(--color-text-secondary);
  font-size: 9px;
  line-height: 12px;
}

.button-matrix__row-title {
  box-sizing: border-box;
  justify-self: start;
  font-weight: 600;
  letter-spacing: 0;
  min-width: 0;
  padding: 0 6px;
  text-align: left;
}

.button-matrix__row-add {
  box-sizing: border-box;
  width: var(--button-add-width);
  min-width: var(--button-add-width);
  max-width: var(--button-add-width);
  height: var(--button-row-height);
  min-height: var(--button-row-height);
  max-height: var(--button-row-height);
  border: 0;
  border-left: 1px solid color-mix(in srgb, var(--color-primary) 26%, var(--color-border));
  border-radius: 0 !important;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font: inherit;
  font-size: 13px;
  line-height: 12px;
  padding: 0;
}

.button-matrix__row-add :deep(.p-button-label) {
  line-height: 1;
}

.button-matrix__grid {
  position: static;
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  align-items: start;
  gap: 0;
  min-height: var(--button-row-height);
  overflow: visible;
}

.button-matrix__item {
  position: relative;
  display: flex;
  align-items: center;
  height: var(--button-row-height);
  min-height: var(--button-row-height);
  max-height: var(--button-row-height);
  min-width: 0;
}

.button-matrix__control {
  box-sizing: border-box;
  position: relative;
  width: 100%;
  height: var(--button-row-height);
  min-height: var(--button-row-height);
  max-height: var(--button-row-height);
  min-width: 0;
  display: grid;
  grid-template-columns: minmax(0, 1fr) var(--button-style-width) var(--button-delete-width);
  align-items: center;
  gap: 0;
  border: 1px solid var(--color-border-input);
  border-radius: 0;
  background: var(--color-bg);
  padding: 0;
  overflow: visible;
}

.button-matrix__control::after {
  content: '';
  position: absolute;
  z-index: 2;
  right: -1px;
  bottom: -1px;
  left: -1px;
  height: 1px;
  background: var(--color-border-input);
  pointer-events: none;
}

.button-matrix__control--readonly {
  grid-template-columns: minmax(0, 1fr);
}

.button-matrix__control:focus-within {
  border-color: var(--color-border-input);
  box-shadow: none;
}

.button-matrix__label {
  height: var(--button-row-height);
  min-width: 0;
  font-size: 11px;
  line-height: 16px;
}

.button-matrix__label :deep(.editor-variable-input__highlight),
.button-matrix__label :deep(.editor-variable-input__control) {
  height: var(--button-row-height);
  border: 0;
  border-radius: 0;
  background: transparent !important;
  line-height: 16px;
  padding-bottom: 4px;
  padding-top: 4px;
  box-shadow: none !important;
}

.button-matrix__style {
  box-sizing: border-box;
  width: var(--button-style-width);
  height: var(--button-inner-height);
  min-width: var(--button-style-width);
  max-width: var(--button-style-width);
  min-height: var(--button-inner-height);
  max-height: var(--button-inner-height);
  border: 0 !important;
  border-left: 1px solid var(--color-border-input) !important;
  border-radius: 0 !important;
  background: transparent !important;
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  font-size: 8px;
  line-height: 12px;
  outline: none !important;
  padding: 0;
  box-shadow: none !important;
}

.button-matrix__style :deep(.p-select) {
  border-radius: 0 !important;
  outline: none !important;
  box-shadow: none !important;
}

.button-matrix__style :deep(.p-select-label),
.button-matrix__style :deep(.p-select-dropdown) {
  background: transparent !important;
  border-radius: 0 !important;
  outline: none !important;
  box-shadow: none !important;
}

.button-matrix__style:hover,
.button-matrix__style.p-focus,
.button-matrix__style.p-select-open,
.button-matrix__style:focus,
.button-matrix__style:focus-visible,
.button-matrix__style:focus-within {
  border-color: var(--color-border-input) !important;
  outline: none !important;
  box-shadow: none !important;
}

.button-matrix__style :deep(.p-select-label:focus),
.button-matrix__style :deep(.p-select-label:focus-visible),
.button-matrix__style :deep(.p-select-dropdown:focus),
.button-matrix__style :deep(.p-select-dropdown:focus-visible) {
  outline: none !important;
  box-shadow: none !important;
}

.button-matrix__style :deep(.p-select-label) {
  padding: 4px 2px 4px 6px;
  font-size: 8px;
  line-height: 14px;
}

.button-matrix__style :deep(.p-select-dropdown) {
  width: 18px;
}

.button-matrix__style :deep(.p-select-dropdown-icon) {
  width: 12px;
  height: 12px;
}

.button-matrix__delete {
  box-sizing: border-box;
  border-radius: 0;
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.button-matrix__delete {
  width: var(--button-delete-width);
  height: var(--button-inner-height);
  min-width: var(--button-delete-width);
  min-height: var(--button-inner-height);
  max-width: var(--button-delete-width);
  max-height: var(--button-inner-height);
  border: 0;
  border-left: 1px solid var(--color-border-input);
  border-radius: 0 !important;
  background: transparent;
  color: var(--color-text-secondary);
}

.button-matrix__delete:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}

.button-matrix__first-add:hover,
.button-matrix__add-row:hover,
.button-matrix__row-add:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.button-matrix__first-add:disabled,
.button-matrix__add-row:disabled,
.button-matrix__row-add:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.button-matrix__limit {
  margin: 6px 0 0;
  color: var(--color-danger);
  font-size: 10px;
  line-height: 14px;
  text-align: left;
}

.button-matrix__handle {
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
