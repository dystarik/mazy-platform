<template>
  <div class="vk-button-matrix">
    <div class="vk-button-matrix__head">
      <span>{{ title }}</span>
    </div>

    <Button
      v-if="!readonly && rows.length === 0"
      class="vk-button-matrix__first-add nodrag"
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
      class="vk-button-matrix__row"
    >
      <div class="vk-button-matrix__row-head">
        <span class="vk-button-matrix__row-title">Ряд {{ rowIndex + 1 }}</span>
        <Button
          v-if="!readonly"
          class="vk-button-matrix__row-add nodrag"
          type="button"
          label="+"
          title="Добавить кнопку в ряд"
          :disabled="!canAddButtonToRow(rowIndex)"
          @mousedown.stop
          @pointerdown.stop
          @click.stop="addButtonToRow(rowIndex)"
        />
      </div>

      <div class="vk-button-matrix__grid">
        <EditorButtonPortRow
          v-for="(button, buttonIndex) in row"
          :key="`${button.payload}-${buttonIndex}`"
        >
          <div
            class="vk-button-matrix__control nodrag"
            :class="{ 'vk-button-matrix__control--readonly': readonly }"
            @mousedown.stop
            @pointerdown.stop
          >
            <EditorVariableInput
              class="vk-button-matrix__input vk-button-matrix__input--label"
              :model-value="button.label"
              :readonly="readonly"
              placeholder="Текст"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateButton(rowIndex, buttonIndex, 'label', stringValue(value))"
            />
            <Select
              v-if="!readonly"
              class="vk-button-matrix__color"
              :model-value="button.color"
              :options="colorOptions"
              option-label="label"
              option-value="value"
              overlay-class="editor-node-select-overlay"
              title="Цвет VK-кнопки"
              @update:model-value="value => updateButton(rowIndex, buttonIndex, 'color', normalizeColor(value))"
            />
            <Button
              v-if="!readonly"
              class="vk-button-matrix__delete"
              type="button"
              title="Удалить кнопку"
              label="×"
              @click.stop="removeButton(rowIndex, buttonIndex)"
            />
          </div>
        </EditorButtonPortRow>
      </div>
    </div>

    <Button
      v-if="!readonly && rows.length > 0"
      class="vk-button-matrix__add-row nodrag"
      type="button"
      label="+ Новый ряд"
      :disabled="!canAddNewRow"
      @mousedown.stop
      @pointerdown.stop
      @click.stop="addRow"
    />

    <p v-if="limitMessage" class="vk-button-matrix__limit">{{ limitMessage }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import {
  canAddObjectMatrixItem,
  canAddObjectMatrixRow,
  getObjectMatrixLimitMessage,
} from '@/components/editor/buttonMatrixLimits'
import EditorButtonPortRow from '@/components/editor/EditorButtonPortRow.vue'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'
import type { VariableScope } from '@/components/editor/variableHighlight'

const VK_BUTTON_COLORS = ['primary', 'secondary', 'negative', 'positive'] as const
type VkButtonColor = typeof VK_BUTTON_COLORS[number]
type VkButtonKey = 'label' | 'color'

interface VkButton {
  label: string
  payload: string
  color?: VkButtonColor
}

type VkButtonRow = VkButton[]

const DEFAULT_BUTTON_COLOR: VkButtonColor = 'primary'
const fallbackSchema = {
  key: 'buttons',
  type: 'NODE_PARAM_TYPE_OBJECT_MATRIX',
  isRequired: false,
} as NodeParamItem

const props = withDefaults(defineProps<{
  modelValue: unknown
  schema?: NodeParamItem
  readonly?: boolean
  knownVariables?: string[]
  variableScope?: VariableScope
  title?: string
}>(), {
  schema: undefined,
  readonly: false,
  knownVariables: () => [],
  variableScope: undefined,
  title: 'Кнопки',
})

const emit = defineEmits<{
  'update:modelValue': [value: VkButtonRow[]]
}>()

const schema = computed(() => props.schema ?? fallbackSchema)
const rows = computed(() => readEditableRows(props.modelValue))
const buttons = computed(() => rows.value.flat())
const canAddNewRow = computed(() => canAddObjectMatrixRow(rows.value, schema.value, 1))
const limitMessage = computed(() => getObjectMatrixLimitMessage(rows.value, schema.value, 'кнопок'))
const colorOptions = [
  { value: 'primary', label: 'Син.' },
  { value: 'secondary', label: 'Бел.' },
  { value: 'negative', label: 'Крас.' },
  { value: 'positive', label: 'Зел.' },
]

function addRow(): void {
  if (props.readonly || !canAddNewRow.value) return
  const index = buttons.value.length
  emit('update:modelValue', [
    ...cloneRows(),
    [createButton(index)],
  ])
}

function addButtonToRow(rowIndex: number): void {
  if (props.readonly || !canAddButtonToRow(rowIndex)) return
  const next = cloneRows()
  const row = next[rowIndex]
  if (!row) return

  row.push(createButton(buttons.value.length))
  emit('update:modelValue', next)
}

function canAddButtonToRow(rowIndex: number): boolean {
  return canAddObjectMatrixItem(rows.value, rowIndex, schema.value)
}

function removeButton(rowIndex: number, buttonIndex: number): void {
  if (props.readonly) return
  const next = cloneRows()
  next[rowIndex]?.splice(buttonIndex, 1)
  emit('update:modelValue', next.filter(row => row.length > 0))
}

function updateButton(rowIndex: number, buttonIndex: number, key: VkButtonKey, value: string): void {
  if (props.readonly) return
  const next = cloneRows()
  const button = next[rowIndex]?.[buttonIndex]
  if (!button) return

  if (key === 'color') {
    button.color = normalizeColor(value)
  } else {
    button[key] = value
  }
  emit('update:modelValue', next)
}

function createButton(index: number): VkButton {
  return {
    label: `Кнопка ${index + 1}`,
    payload: createButtonPayload(index),
    color: DEFAULT_BUTTON_COLOR,
  }
}

function cloneRows(): VkButtonRow[] {
  return rows.value.map(row => row.map(button => ({ ...button })))
}

function readEditableRows(value: unknown): VkButtonRow[] {
  if (!Array.isArray(value)) return []

  const usedPayloads = new Set<string>()
  const sourceRows = value.every(item => Array.isArray(item))
    ? value
    : [value]

  const result: VkButtonRow[] = []
  let index = 0

  for (const row of sourceRows) {
    if (!Array.isArray(row)) continue
    const nextRow: VkButtonRow = []

    for (const item of row) {
      if (!isRecord(item)) continue

      const label = stringValue(item.label) || stringValue(item.payload)
      if (!label) continue

      const payload = uniquePayload(stringValue(item.payload), index, usedPayloads)
      nextRow.push({
        label,
        payload,
        color: normalizeColor(item.color),
      })
      index += 1
    }

    if (nextRow.length) result.push(nextRow)
  }

  return result
}

function uniquePayload(value: string, index: number, usedPayloads: Set<string>): string {
  const base = value || `button_${index + 1}`
  let candidate = base
  let suffix = 2

  while (usedPayloads.has(candidate)) {
    candidate = `${base}_${suffix}`
    suffix += 1
  }

  usedPayloads.add(candidate)
  return candidate
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

function normalizeColor(value: unknown): VkButtonColor {
  return typeof value === 'string' && (VK_BUTTON_COLORS as readonly string[]).includes(value)
    ? value as VkButtonColor
    : DEFAULT_BUTTON_COLOR
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
</script>

<style scoped>
.vk-button-matrix {
  --vk-row-height: 24px;
  --vk-color-width: 54px;
  --vk-delete-width: 24px;
  display: flex;
  flex-direction: column;
  gap: 0;
  min-width: 0;
}

.vk-button-matrix__head,
.vk-button-matrix__row-head {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: left;
}

.vk-button-matrix__head {
  margin-bottom: 12px;
}

.vk-button-matrix__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 0;
  outline: 1px solid color-mix(in srgb, var(--color-primary) 24%, var(--color-border));
  outline-offset: -1px;
  background: color-mix(in srgb, var(--color-primary) 4%, var(--color-bg));
}

.vk-button-matrix__row + .vk-button-matrix__row {
  margin-top: 12px;
}

.vk-button-matrix__row-head {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 24px;
  align-items: center;
  height: var(--vk-row-height);
}

.vk-button-matrix__row-title {
  font-weight: 600;
  min-width: 0;
  padding: 0 6px;
}

.vk-button-matrix__row-add {
  width: 24px;
  height: var(--vk-row-height);
  min-width: 24px;
  border: 0;
  border-left: 1px solid color-mix(in srgb, var(--color-primary) 24%, var(--color-border));
  border-radius: 0 !important;
  background: transparent;
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  padding: 0;
}

.vk-button-matrix__grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 0;
}

.vk-button-matrix__control {
  box-sizing: border-box;
  display: grid;
  grid-template-columns: minmax(0, 1fr) var(--vk-color-width) var(--vk-delete-width);
  align-items: center;
  min-width: 0;
  height: var(--vk-row-height);
  border-top: 1px solid var(--color-border-input);
  background: var(--color-bg);
}

.vk-button-matrix__control--readonly {
  grid-template-columns: minmax(0, 1fr);
}

.vk-button-matrix__input {
  min-width: 0;
}

.vk-button-matrix__input :deep(.editor-variable-input__highlight),
.vk-button-matrix__input :deep(.editor-variable-input__control) {
  height: var(--vk-row-height);
  border: 0;
  border-radius: 0;
  background: transparent !important;
  line-height: 18px;
  padding: 3px 6px;
}

.vk-button-matrix__color {
  width: var(--vk-color-width);
  min-width: var(--vk-color-width);
  height: var(--vk-row-height);
  border: 0 !important;
  border-left: 1px solid var(--color-border-input) !important;
  border-radius: 0 !important;
  background: transparent !important;
  font-size: 8px;
}

.vk-button-matrix__color :deep(.p-select-label) {
  padding: 4px 2px 4px 6px;
  font-size: 8px;
  line-height: 14px;
}

.vk-button-matrix__color :deep(.p-select-dropdown) {
  width: 18px;
}

.vk-button-matrix__delete {
  width: var(--vk-delete-width);
  height: var(--vk-row-height);
  min-width: var(--vk-delete-width);
  border: 0;
  border-left: 1px solid var(--color-border-input);
  border-radius: 0 !important;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.vk-button-matrix__delete:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}

.vk-button-matrix__first-add,
.vk-button-matrix__add-row {
  box-sizing: border-box;
  width: 100%;
  height: var(--vk-row-height);
  min-height: var(--vk-row-height);
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

.vk-button-matrix__add-row {
  margin-top: 12px;
}

.vk-button-matrix__first-add:hover,
.vk-button-matrix__add-row:hover,
.vk-button-matrix__row-add:hover {
  color: var(--color-primary);
}

.vk-button-matrix__first-add:disabled,
.vk-button-matrix__add-row:disabled,
.vk-button-matrix__row-add:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.vk-button-matrix__limit {
  margin: 12px 0 0;
  color: var(--color-danger);
  font-size: 10px;
  line-height: 14px;
  text-align: left;
}

</style>
