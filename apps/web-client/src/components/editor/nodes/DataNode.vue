<template>
  <BaseNode
    class="data-node"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="data-node__body">
      <label class="data-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="data-node__label">Действие</span>
        <Select
          class="data-node__select"
          :model-value="action"
          :options="actionOptions"
          option-label="label"
          option-value="value"
          :disabled="isReadOnly"
          overlay-class="editor-node-select-overlay"
          @update:model-value="value => updateAction(stringValue(value))"
        />
      </label>

      <label
        v-if="needsEntityName && hasProjectSchemas"
        class="data-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="data-node__label">Сущность</span>
        <Select
          class="data-node__select"
          :model-value="selectedEntitySelectValue"
          :options="entityOptions"
          option-label="label"
          option-value="value"
          :disabled="isReadOnly"
          overlay-class="editor-node-select-overlay"
          @update:model-value="updateEntitySelection"
        />
      </label>

      <div
        v-else-if="needsEntityName"
        class="data-node__field data-node__empty-state"
      >
        Схем данных нет
      </div>

      <label
        v-if="needsRecordId"
        class="data-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="data-node__label">ID записи</span>
        <EditorVariableInput
          class="data-node__variable-input"
          :model-value="stringParam('recordIdVariable')"
          :readonly="isReadOnly"
          placeholder="Переменная с ID"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('recordIdVariable', stringValue(value))"
        />
      </label>

      <div
        v-if="shouldShowFieldsSection"
        class="data-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <div class="data-node__section-head">
          <span class="data-node__label">{{ action === 'query' ? 'Фильтр' : 'Поля' }}</span>
          <Button
            v-if="!isReadOnly && !selectedDataSchema"
            class="data-node__head-add"
            type="button"
            title="Добавить поле"
            label="+"
            @click.stop="addDictionaryRow(dictionaryKey)"
          />
        </div>
        <template v-if="selectedDataSchema">
          <div
            v-for="field in selectedSchemaFields"
            :key="field.fieldId ?? field.name"
            class="data-node__schema-row"
          >
            <div class="data-node__field-meta">
              <span class="data-node__field-name">
                {{ field.name }}
                <span v-if="field.isRequired" class="data-node__required">*</span>
              </span>
              <span class="data-node__field-type">{{ formatFieldType(field.fieldType) }}</span>
            </div>
            <EditorVariableInput
              class="data-node__variable-input data-node__variable-input--value"
              :model-value="readDictionaryValue(dictionaryKey, field.name)"
              :readonly="isReadOnly"
              :placeholder="field.defaultValue || 'Значение или {переменная}'"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="value => updateDictionaryValue(dictionaryKey, field.name ?? '', stringValue(value))"
            />
          </div>
        </template>
        <template v-else>
          <div
            v-for="([key, value]) in dictionaryEntries(dictionaryKey)"
            :key="key"
            class="data-node__dict-row"
          >
            <EditorOverflowTooltip :value="key" :known-variables="knownVariables" :variable-scope="variableScope">
              <InputText
                class="data-node__dict-input"
                :model-value="key"
                :readonly="isReadOnly"
                placeholder="поле"
                @change="event => renameDictionaryKey(dictionaryKey, key, (event.target as HTMLInputElement).value)"
              />
            </EditorOverflowTooltip>
            <EditorVariableInput
              class="data-node__variable-input data-node__variable-input--value"
              :model-value="value"
              :readonly="isReadOnly"
              placeholder="значение"
              :known-variables="knownVariables"
              :variable-scope="variableScope"
              @update:model-value="nextValue => updateDictionaryValue(dictionaryKey, key, stringValue(nextValue))"
            />
            <Button
              v-if="!isReadOnly"
              class="data-node__icon-button"
              type="button"
              title="Удалить поле"
              label="×"
              @click.stop="removeDictionaryKey(dictionaryKey, key)"
            />
          </div>
        </template>
      </div>

      <label
        v-if="needsResultVariable && hasProjectSchemas"
        class="data-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="data-node__label">{{ resultVariableLabel }}</span>
        <EditorVariableInput
          class="data-node__variable-input"
          :model-value="stringParam(resultVariableKey)"
          :readonly="isReadOnly"
          :placeholder="resultVariablePlaceholder"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam(resultVariableKey, stringValue(value))"
        />
      </label>

      <label
        v-if="action === 'query'"
        class="data-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="data-node__label">Количество</span>
        <EditorVariableInput
          class="data-node__variable-input"
          :model-value="stringParam('countVariable')"
          :readonly="isReadOnly"
          placeholder="Переменная количества"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('countVariable', stringValue(value))"
        />
      </label>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import type { FieldType, GetEntitySchemaResponse } from '@/types/api/entity-schemas.types'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import { DATA_NODE_TYPE } from '@/components/editor/editorTypes'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'
import type { VariableScope } from '@/components/editor/variableHighlight'
import BaseNode from './BaseNode.vue'

type DataAction = 'create' | 'get' | 'query' | 'update' | 'delete'

const props = defineProps<{
  nodeType: string
  params: Record<string, unknown>
  catalogParams: NodeParamItem[]
  projectSchemaDetails: GetEntitySchemaResponse[]
  isStart: boolean
  isSelected: boolean
  isReadOnly?: boolean
  isPickable?: boolean
  isPickTarget?: boolean
  isRelated?: boolean
  label: string
  knownVariables?: string[]
  variableScope?: VariableScope
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  delete: []
}>()

const actionOptions: Array<{ value: DataAction; label: string }> = [
  { value: 'create', label: 'Создать запись' },
  { value: 'get', label: 'Получить запись' },
  { value: 'query', label: 'Найти записи' },
  { value: 'update', label: 'Обновить запись' },
  { value: 'delete', label: 'Удалить запись' },
]
const emptyEntityValue = '__none__'
const accentColor = computed(() => getNodeAccentColor(DATA_NODE_TYPE))
const action = computed<DataAction>(() => readAction(props.params.action))
const hasProjectSchemas = computed(() => props.projectSchemaDetails.length > 0)
const selectedEntityName = computed(() => stringParam('entityName'))
const selectedEntitySelectValue = computed(() => selectedEntityName.value || emptyEntityValue)
const entityOptions = computed(() => [
  { label: 'Выберите сущность', value: emptyEntityValue },
  ...props.projectSchemaDetails.map(schema => ({
    label: schema.name ?? 'Без названия',
    value: schema.name ?? '',
  })),
])
const selectedDataSchema = computed(() =>
  props.projectSchemaDetails.find(schema => schema.name === selectedEntityName.value) ?? null,
)
const selectedSchemaFields = computed(() =>
  (selectedDataSchema.value?.fields ?? []).filter(field => Boolean(field.name)),
)
const needsEntityName = computed(() => ['create', 'query', 'update'].includes(action.value))
const needsRecordId = computed(() => ['get', 'update', 'delete'].includes(action.value))
const needsFields = computed(() => ['create', 'query', 'update'].includes(action.value))
const shouldShowFieldsSection = computed(() =>
  needsFields.value && hasProjectSchemas.value && Boolean(selectedDataSchema.value),
)
const dictionaryKey = computed(() => action.value === 'query' ? 'filter' : 'fields')
const needsResultVariable = computed(() => ['create', 'get', 'query'].includes(action.value))
const resultVariableKey = computed(() => {
  if (action.value === 'create') return 'recordIdVariable'
  if (action.value === 'get') return 'recordVariable'
  return 'recordsVariable'
})
const resultVariableLabel = computed(() => {
  if (action.value === 'create') return 'Сохранить ID'
  if (action.value === 'get') return 'Сохранить запись'
  return 'Сохранить список'
})
const resultVariablePlaceholder = computed(() => {
  if (action.value === 'create') return 'created_record_id'
  if (action.value === 'get') return 'record'
  return 'records'
})

function updateAction(value: string): void {
  if (props.isReadOnly) return
  const nextAction = readAction(value)
  emit('update-param', 'action', nextAction)
  if (['create', 'query', 'update'].includes(nextAction) && selectedDataSchema.value) {
    emit('update-param', nextAction === 'query' ? 'filter' : 'fields', normalizeFieldsForSchema(selectedDataSchema.value, nextAction === 'query' ? 'filter' : 'fields'))
  }
}

function updateParam(key: string, value: unknown): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function updateEntityName(value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'entityName', value)
  const schema = props.projectSchemaDetails.find(item => item.name === value)
  if (schema && needsFields.value) {
    emit('update-param', dictionaryKey.value, normalizeFieldsForSchema(schema, dictionaryKey.value))
  }
}

function updateEntitySelection(value: unknown): void {
  const nextValue = stringValue(value)
  updateEntityName(nextValue === emptyEntityValue ? '' : nextValue)
}

function stringParam(key: string): string {
  const value = props.params[key]
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function dictionaryEntries(key: string): Array<[string, string]> {
  const value = props.params[key]
  if (!isRecord(value)) return []
  return Object.entries(value)
    .filter(([itemKey]) => itemKey.trim().length > 0)
    .map(([itemKey, itemValue]) => [itemKey, stringValue(itemValue)])
}

function readDictionaryValue(dictionaryKeyValue: string, fieldName?: string): string {
  if (!fieldName) return ''
  const value = props.params[dictionaryKeyValue]
  if (!isRecord(value)) return ''
  return stringValue(value[fieldName])
}

function normalizeFieldsForSchema(schema: GetEntitySchemaResponse, dictionaryKeyValue: string): Record<string, string> {
  const current = readDictionary(dictionaryKeyValue)
  const next: Record<string, string> = {}
  for (const field of schema.fields ?? []) {
    if (!field.name) continue
    next[field.name] = current[field.name] ?? field.defaultValue ?? ''
  }
  return next
}

function addDictionaryRow(key: string): void {
  if (props.isReadOnly) return
  const dictionary = readDictionary(key)
  let index = Object.keys(dictionary).length + 1
  let nextKey = `field_${index}`
  while (nextKey in dictionary) {
    index += 1
    nextKey = `field_${index}`
  }
  emit('update-param', key, { ...dictionary, [nextKey]: '' })
}

function renameDictionaryKey(dictionaryKeyValue: string, oldKey: string, newKey: string): void {
  if (props.isReadOnly) return
  const trimmed = newKey.trim()
  if (!trimmed || trimmed === oldKey) return
  const dictionary = readDictionary(dictionaryKeyValue)
  const previousValue = dictionary[oldKey] ?? ''
  delete dictionary[oldKey]
  dictionary[trimmed] = previousValue
  emit('update-param', dictionaryKeyValue, dictionary)
}

function updateDictionaryValue(dictionaryKeyValue: string, key: string, value: string): void {
  if (props.isReadOnly) return
  emit('update-param', dictionaryKeyValue, { ...readDictionary(dictionaryKeyValue), [key]: value })
}

function removeDictionaryKey(dictionaryKeyValue: string, key: string): void {
  if (props.isReadOnly) return
  const dictionary = readDictionary(dictionaryKeyValue)
  delete dictionary[key]
  emit('update-param', dictionaryKeyValue, dictionary)
}

function readDictionary(key: string): Record<string, string> {
  const value = props.params[key]
  if (!isRecord(value)) return {}
  return Object.fromEntries(
    Object.entries(value).map(([itemKey, itemValue]) => [itemKey, stringValue(itemValue)]),
  )
}

function readAction(value: unknown): DataAction {
  return ['create', 'get', 'query', 'update', 'delete'].includes(String(value))
    ? String(value) as DataAction
    : 'create'
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}

function formatFieldType(type?: FieldType): string {
  switch (type) {
    case 'FIELD_TYPE_STRING':
      return 'Текст'
    case 'FIELD_TYPE_NUMBER':
      return 'Число'
    case 'FIELD_TYPE_BOOLEAN':
      return 'Да/нет'
    case 'FIELD_TYPE_DATE_TIME':
      return 'Дата'
    case 'FIELD_TYPE_REFERENCE':
      return 'Ссылка'
    case 'FIELD_TYPE_ENUM':
      return 'Список'
    default:
      return 'Тип не указан'
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
</script>

<style scoped>
.data-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.data-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.data-node__field {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.data-node__field + .data-node__field {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.data-node__label,
.data-node__empty {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.data-node__input,
.data-node__select,
.data-node__dict-input {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 12px;
  height: 24px;
  outline: none;
  padding: 3px 8px;
}

.data-node__variable-input {
  width: 100%;
  min-width: 0;
}

.data-node__variable-input--value {
  color: var(--color-text-secondary);
}

.data-node__variable-input :deep(.editor-variable-input__control),
.data-node__variable-input :deep(.editor-variable-input__highlight) {
  font-size: 12px;
}

.data-node__input:focus,
.data-node__select:focus,
.data-node__dict-input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.data-node__select :deep(.p-select-label) {
  padding: 3px 8px;
  font-size: 11px;
  line-height: 16px;
}

.data-node__select :deep(.p-select-dropdown) {
  width: 24px;
}

.data-node__section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.data-node__head-add {
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 4px;
  background: var(--color-bg);
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.data-node__head-add:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.data-node__dict-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) 24px;
  align-items: center;
  gap: 6px;
}

.data-node__schema-row {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.data-node__field-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.data-node__field-name {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--color-text);
  font-size: 11px;
  line-height: 12px;
  white-space: nowrap;
}

.data-node__required {
  color: var(--color-danger);
}

.data-node__field-type {
  flex-shrink: 0;
  color: var(--color-text-secondary);
  font-size: 9px;
  line-height: 12px;
}

.data-node__dict-input--value {
  color: var(--color-text-secondary);
}

.data-node__icon-button {
  width: 24px;
  height: 24px;
  border: 0;
  border-radius: 3px;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  line-height: 1;
  padding: 0;
}

.data-node__icon-button:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}

.data-node__empty {
  padding: 6px 0;
}

.data-node__empty-state {
  color: var(--color-text-secondary);
  font-size: 12px;
  line-height: 1.25;
  text-align: center;
}
</style>
