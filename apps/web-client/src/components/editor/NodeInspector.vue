<template>
  <aside class="editor__inspector">
    <div class="inspector">
      <div class="inspector__header">
        <div>
          <span class="inspector__eyebrow">Узел</span>
          <h2 class="inspector__title">{{ getNodeLabel(inspector.data.type) }}</h2>
          <p class="inspector__type">{{ inspector.data.type }}</p>
        </div>
        <Button class="inspector__close" label="×" text title="Закрыть" @click="$emit('close')" />
      </div>

      <div class="inspector__section">
        <div class="inspector__section-head">
          <h3>Параметры</h3>
          <span>{{ visibleParams.length }}</span>
        </div>

        <div v-if="!visibleParams.length" class="inspector__empty">
          У этого узла нет параметров.
        </div>

        <div v-else class="inspector__fields">
          <div
            v-if="messagePickerTargetNodeId === inspector.node.id"
            class="inspector__picker-hint"
          >
            <span>Кликните по сообщению на схеме, которое нужно изменить.</span>
            <Button type="button" label="Отмена" text @click="$emit('cancelMessagePick')" />
          </div>

          <div
            v-for="param in visibleParams"
            :key="param.key"
            class="inspector__field"
          >
            <template v-if="isMessageReferenceParam(param)">
              <label class="inspector__label">
                Сообщение
                <span v-if="param.isRequired" class="inspector__required">*</span>
              </label>
              <p class="inspector__description">
                Выберите на схеме узел, который отправляет сообщение. Переменная ID создастся автоматически.
              </p>
              <div v-if="isReadOnly" class="inspector__readonly-value">
                {{ formatReadonlyValue(inspector.data.params[param.key ?? '']) }}
              </div>
              <Button
                v-else
                class="inspector__pick-button"
                type="button"
                @click="$emit('startMessagePick', inspector.node.id)"
              >
                <span>{{ selectedMessageReferenceLabel }}</span>
                <small>{{ inspector.data.params.messageIdVariable ? 'Можно выбрать другое' : 'Кликните и выберите узел' }}</small>
              </Button>
            </template>

            <template v-else>
              <label class="inspector__label">
                {{ formatParamLabel(param.key ?? '') }}
                <span v-if="param.isRequired" class="inspector__required">*</span>
              </label>
              <p v-if="param.description" class="inspector__description">
                {{ param.description }}
              </p>
              <div v-if="isReadOnly" class="inspector__readonly-value">
                {{ formatReadonlyValue(inspector.data.params[param.key ?? '']) }}
              </div>
              <template v-else-if="isDataEntityParam(param)">
                <Select
                  class="inspector__select"
                  :model-value="selectedDataEntityName"
                  :options="dataEntityOptions"
                  option-label="label"
                  option-value="value"
                  placeholder="Выберите сущность"
                  @update:model-value="updateDataEntityName"
                />
                <p v-if="schemasLoading" class="inspector__hint">Загружаю схемы проекта...</p>
                <p v-else-if="!projectSchemaDetails.length" class="inspector__hint">
                  В проекте пока нет схем данных.
                </p>
              </template>
              <template v-else-if="isDataFieldsParam(param)">
                <div v-if="selectedDataSchema" class="data-fields">
                  <div
                    v-for="field in selectedDataSchema.fields ?? []"
                    :key="field.fieldId ?? field.name"
                    class="data-fields__row"
                  >
                    <div class="data-fields__meta">
                      <span class="data-fields__name">
                        {{ field.name }}
                        <span v-if="field.isRequired" class="inspector__required">*</span>
                      </span>
                      <span class="data-fields__type">{{ formatFieldType(field.fieldType) }}</span>
                    </div>
                    <InputText
                      class="data-fields__input"
                      :model-value="readDataFieldValue(field.name)"
                      :placeholder="field.defaultValue || 'Значение или {переменная}'"
                      @update:model-value="updateDataFieldValue(field.name, $event ?? '')"
                    />
                  </div>
                  <p v-if="!(selectedDataSchema.fields?.length)" class="inspector__hint">
                    У выбранной сущности пока нет полей.
                  </p>
                </div>
                <div v-else class="inspector__empty-card">
                  Сначала выберите сущность выше, затем здесь появятся её поля.
                </div>
              </template>
              <template v-else-if="isSwitchCasesParam(param)">
                <div class="switch-cases">
                  <div
                    v-for="(switchCase, index) in switchCases"
                    :key="index"
                    class="switch-cases__row"
                  >
                    <InputText
                      class="switch-cases__input"
                      :model-value="switchCase.value"
                      placeholder="Например: new_ticket"
                      @update:model-value="updateSwitchCaseValue(index, $event ?? '')"
                    />
                    <Button
                      class="switch-cases__remove"
                      type="button"
                      label="×"
                      text
                      severity="danger"
                      title="Удалить ветку"
                      @click="removeSwitchCase(index)"
                    />
                  </div>
                  <Button
                    class="switch-cases__add"
                    type="button"
                    label="+ Добавить ветку"
                    text
                    severity="secondary"
                    @click="addSwitchCase"
                  />
                </div>
              </template>
              <ParamRenderer
                v-else
                :schema="visibleParamSchema(param)"
                :value="inspector.data.params[param.key ?? '']"
                @update="(value: unknown) => updateParam(param, value)"
              />
            </template>
          </div>

          <div
            v-if="inspector.data.type === 'vk_send_keyboard' && inspector.data.params.inline === true"
            class="inspector__empty-card"
          >
            Inline-клавиатура в VK отправляется без параметра «Скрывать после нажатия».
          </div>

          <div
            v-if="messageConsumers.length"
            class="inspector__relations"
          >
            <div class="inspector__relations-head">
              <span>Используется в</span>
              <small>{{ messageConsumers.length }}</small>
            </div>
            <Button
              v-for="consumer in messageConsumers"
              :key="consumer.id"
              class="inspector__relation"
              type="button"
              @click="$emit('selectRelatedNode', consumer.id)"
            >
              <span>{{ getNodeLabel(consumer.data.type) }}</span>
              <small>{{ consumer.data.type }}</small>
            </Button>
          </div>
        </div>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import type { FieldType, GetEntitySchemaResponse } from '@/types/api/entity-schemas.types'
import {
  canProvideMessageId,
  messageConsumersFor,
  type EditorFlowNode,
  type SelectedInspector,
} from '@/components/editor/editorTypes'
import { getNodeLabel } from '@/components/editor/nodes/nodeMeta'
import ParamRenderer from '@/components/editor/params/ParamRenderer.vue'

const props = defineProps<{
  inspector: SelectedInspector
  nodes: EditorFlowNode[]
  isReadOnly: boolean
  projectSchemaDetails: GetEntitySchemaResponse[]
  schemasLoading: boolean
  messagePickerTargetNodeId: string | null
}>()

const emit = defineEmits<{
  close: []
  updateParam: [key: string, value: unknown]
  startMessagePick: [targetNodeId: string]
  cancelMessagePick: []
  selectRelatedNode: [nodeId: string]
}>()

const visibleParams = computed(() =>
  props.inspector.params
    .map(param => normalizeVisibleParam(param))
    .filter((param): param is NodeParamItem => param !== null),
)

const selectedDataEntityName = computed(() => {
  const value = props.inspector.data.params.entityName
  return typeof value === 'string' ? value : ''
})

const selectedDataSchema = computed(() => {
  const entityName = selectedDataEntityName.value.trim()
  if (!entityName) return null
  return props.projectSchemaDetails.find(schema => schema.name === entityName) ?? null
})

const dataEntityOptions = computed(() => {
  const options = props.projectSchemaDetails.map(schema => ({
    label: schema.name ?? '',
    value: schema.name ?? '',
  }))

  if (selectedDataEntityName.value && !selectedDataSchema.value) {
    options.push({
      label: `${selectedDataEntityName.value} (нет в списке)`,
      value: selectedDataEntityName.value,
    })
  }

  return options
})

const selectedValidatorType = computed(() => {
  if (props.inspector.data.type !== 'receive_message') return ''
  const value = props.inspector.data.params.validatorType
  return typeof value === 'string' ? value.trim() : ''
})

const switchCases = computed(() => readSwitchCases(props.inspector.data.params.cases))

const messageConsumers = computed(() =>
  messageConsumersFor(props.nodes, props.inspector),
)

const selectedMessageReferenceLabel = computed(() => {
  const variable = props.inspector.data.params.messageIdVariable
  if (typeof variable !== 'string' || !variable.trim()) {
    return 'Выбрать сообщение на схеме'
  }

  const sourceNode = props.nodes.find(node =>
    canProvideMessageId(node.data.type)
    && node.data.params.messageIdVariable === variable,
  )
  if (!sourceNode) {
    return `Переменная: ${variable}`
  }

  return getNodeLabel(sourceNode.data.type)
})

function formatParamLabel(key: string): string {
  const labels: Record<string, string> = {
    entityName: 'Сущность',
    fields: 'Поля записи',
    recordIdVariable: 'Переменная ID записи',
    cases: 'Ветки',
    buttons: 'Кнопки',
    buttonPayloadVariable: 'Переменная выбранной кнопки',
    inline: 'Inline-клавиатура',
    oneTime: 'Скрывать после нажатия',
    text: 'Сообщение',
  }
  if (labels[key]) return labels[key]

  return key.replace(/([A-Z])/g, ' $1').replace(/^./, (s) => s.toUpperCase())
}

function isMessageReferenceParam(param: NodeParamItem): boolean {
  return param.key === 'messageIdVariable'
    && ['edit_message', 'delete_message'].includes(props.inspector.data.type)
}

function isDataEntityParam(param: NodeParamItem): boolean {
  return isDataNode(props.inspector.data.type) && param.key === 'entityName'
}

function isDataFieldsParam(param: NodeParamItem): boolean {
  return isDataNode(props.inspector.data.type) && param.key === 'fields'
}

function isSwitchCasesParam(param: NodeParamItem): boolean {
  return props.inspector.data.type === 'switch' && param.key === 'cases'
}

function isDataNode(type: string): boolean {
  return ['create_record', 'get_record', 'query_records', 'update_record', 'delete_record'].includes(type)
}

function updateParam(param: NodeParamItem, value: unknown): void {
  const key = param.key ?? ''
  if (!key) return

  emit('updateParam', key, value)
}

function updateDataEntityName(entityName: string): void {
  if (props.isReadOnly) return

  emit('updateParam', 'entityName', entityName)
  emit('updateParam', 'fields', normalizeFieldsForEntity(entityName))
}

function normalizeFieldsForEntity(entityName: string): Record<string, string> {
  const currentFields = readDataFields()
  const schema = props.projectSchemaDetails.find(item => item.name === entityName)
  if (!schema) return currentFields

  const next: Record<string, string> = {}
  for (const field of schema.fields ?? []) {
    if (!field.name) continue
    next[field.name] = currentFields[field.name] ?? ''
  }
  return next
}

function readDataFields(): Record<string, string> {
  const fields = props.inspector.data.params.fields
  if (!isRecord(fields)) return {}

  const result: Record<string, string> = {}
  for (const [key, value] of Object.entries(fields)) {
    result[key] = typeof value === 'string' ? value : String(value ?? '')
  }
  return result
}

function readDataFieldValue(fieldName?: string): string {
  if (!fieldName) return ''
  return readDataFields()[fieldName] ?? ''
}

function updateDataFieldValue(fieldName: string | undefined, value: string): void {
  if (props.isReadOnly || !fieldName) return

  emit('updateParam', 'fields', {
    ...readDataFields(),
    [fieldName]: value,
  })
}

function addSwitchCase(): void {
  if (props.isReadOnly) return

  emit('updateParam', 'cases', [
    ...switchCases.value,
    { value: '' },
  ])
}

function removeSwitchCase(index: number): void {
  if (props.isReadOnly) return

  const nextCases = [...switchCases.value]
  nextCases.splice(index, 1)
  emit('updateParam', 'cases', nextCases)
}

function updateSwitchCaseValue(index: number, value: string): void {
  if (props.isReadOnly) return

  emit('updateParam', 'cases', switchCases.value.map((item, itemIndex) =>
    itemIndex === index
      ? { ...item, value }
      : item,
  ))
}

function readSwitchCases(value: unknown): Array<Record<string, unknown> & { value: string }> {
  if (!Array.isArray(value)) return []

  return value
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map(item => ({
      ...item,
      value: typeof item.value === 'string' ? item.value : String(item.value ?? ''),
    }))
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

function isHiddenServiceParam(param: NodeParamItem): boolean {
  if (param.key === 'messageIdVariable' && canProvideMessageId(props.inspector.data.type)) {
    return true
  }

  if (props.inspector.data.type === 'receive_message') {
    if (param.key === 'validationParams') {
      return !['regex', 'number'].includes(selectedValidatorType.value)
    }

    if (param.key === 'errorMessage') {
      return !selectedValidatorType.value
    }
  }

  return param.key === 'oneTime'
    && props.inspector.data.type === 'vk_send_keyboard'
    && props.inspector.data.params.inline === true
}

function normalizeVisibleParam(param: NodeParamItem): NodeParamItem | null {
  if (isHiddenServiceParam(param)) return null

  if (props.inspector.data.type === 'receive_message' && param.key === 'validationParams') {
    return {
      ...param,
      fields: validationParamFields(param.fields ?? []),
    }
  }

  return param
}

function visibleParamSchema(param: NodeParamItem): NodeParamItem {
  if (props.inspector.data.type === 'switch' && param.key === 'cases') {
    return {
      ...param,
      description: 'Список значений переменной. Ключи веток создаются автоматически.',
      fields: (param.fields ?? []).filter(field => field.key !== 'branchKey'),
    }
  }

  return param
}

function validationParamFields(fields: NodeParamItem[]): NodeParamItem[] {
  if (selectedValidatorType.value === 'regex') {
    return fields.filter(field => field.key === 'pattern')
  }

  if (selectedValidatorType.value === 'number') {
    return fields.filter(field => field.key === 'min' || field.key === 'max')
  }

  return []
}

function formatReadonlyValue(value: unknown): string {
  if (value == null || value === '') return '—'
  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') {
    return String(value)
  }

  return JSON.stringify(value, null, 2)
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
</script>

<style scoped>
.editor__inspector {
  width: 390px;
  flex-shrink: 0;
  border-left: 0.5px solid var(--color-border);
  background: var(--color-bg-card);
  overflow: hidden;
}

.inspector {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}

.inspector__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 20px 20px 18px;
  border-bottom: 0.5px solid var(--color-border);
}

.inspector__eyebrow {
  display: block;
  margin-bottom: 5px;
  color: var(--color-text-secondary);
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.inspector__title {
  margin: 0;
  color: var(--color-text);
  font-size: 18px;
  font-weight: 600;
}

.inspector__type {
  margin: 5px 0 0;
  color: var(--color-text-secondary);
  font-family: var(--font-mono, monospace);
  font-size: 12px;
}

.inspector__close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  font-size: 18px;
  line-height: 1;
  transition: color 0.15s, background 0.15s;
}

.inspector__close:hover {
  background: var(--color-bg-secondary);
  color: var(--color-text);
}

.inspector__section {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
  padding: 20px 20px 50vh;
  scroll-padding-bottom: 50vh;
  overflow-y: auto;
}

.inspector__section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}

.inspector__section-head h3 {
  margin: 0;
  color: var(--color-text);
  font-size: 15px;
  font-weight: 600;
}

.inspector__section-head span {
  color: var(--color-text-secondary);
  font-size: 13px;
}

.inspector__fields {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.inspector__picker-hint {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px;
  border: 0.5px solid color-mix(in srgb, var(--color-primary) 35%, var(--color-border));
  border-radius: 10px;
  background: color-mix(in srgb, var(--color-primary) 9%, transparent);
  color: var(--color-text);
  font-size: 13px;
  line-height: 1.4;
}

.inspector__picker-hint button {
  border: none;
  background: none;
  color: var(--color-primary);
  cursor: pointer;
  font: inherit;
  font-weight: 600;
  padding: 0;
}

.inspector__field {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.inspector__label {
  color: var(--color-text);
  font-size: 14px;
  font-weight: 600;
}

.inspector__required {
  color: var(--color-danger);
}

.inspector__description {
  margin: -2px 0 2px;
  color: var(--color-text-secondary);
  font-size: 12.5px;
  line-height: 1.5;
}

.inspector__hint {
  margin: 2px 0 0;
  color: var(--color-text-secondary);
  font-size: 12px;
  line-height: 1.45;
}

.inspector__readonly-value {
  min-height: 32px;
  padding: 8px 10px;
  border: 0.5px solid var(--color-border-input);
  border-radius: 7px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  font-family: var(--font-mono, monospace);
  font-size: 12px;
  line-height: 1.45;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}

.inspector__empty {
  color: var(--color-text-secondary);
  font-size: 14px;
}

.inspector__empty-card {
  padding: 12px;
  border: 0.5px dashed var(--color-border-input);
  border-radius: 9px;
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font-size: 12.5px;
  line-height: 1.45;
}

.inspector__select {
  min-height: 38px;
  width: 100%;
  border: 0.5px solid var(--color-border-input);
  border-radius: 8px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  font-family: inherit;
  font-size: 13px;
  outline: none;
  padding: 9px 34px 9px 11px;
}

.inspector__select:focus {
  border-color: var(--color-primary);
}

.data-fields {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.data-fields__row {
  display: grid;
  grid-template-columns: minmax(96px, 0.8fr) minmax(0, 1.2fr);
  gap: 10px;
  align-items: center;
  padding: 10px;
  border: 0.5px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-secondary);
}

.data-fields__meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}

.data-fields__name {
  color: var(--color-text);
  font-size: 13px;
  font-weight: 600;
  line-height: 1.25;
  overflow-wrap: anywhere;
}

.data-fields__type {
  color: var(--color-text-secondary);
  font-size: 11px;
  line-height: 1;
}

.data-fields__input {
  min-width: 0;
  min-height: 36px;
  border: 0.5px solid var(--color-border-input);
  border-radius: 8px;
  background: var(--color-bg-card);
  color: var(--color-text);
  font-family: inherit;
  font-size: 13px;
  outline: none;
  padding: 8px 10px;
}

.data-fields__input:focus {
  border-color: var(--color-primary);
}

.switch-cases {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 10px;
  border: 0.5px solid var(--color-border);
  border-radius: 10px;
  background: var(--color-bg-secondary);
}

.switch-cases__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 28px;
  gap: 8px;
  align-items: center;
}

.switch-cases__input {
  min-width: 0;
  min-height: 36px;
  border: 0.5px solid var(--color-border-input);
  border-radius: 8px;
  background: var(--color-bg-card);
  color: var(--color-text);
  font-family: var(--font-mono, monospace);
  font-size: 13px;
  outline: none;
  padding: 8px 10px;
}

.switch-cases__input:focus {
  border-color: var(--color-primary);
}

.switch-cases__remove {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
}

.switch-cases__remove:hover {
  background: color-mix(in srgb, var(--color-danger) 10%, transparent);
  color: var(--color-danger);
}

.switch-cases__add {
  min-height: 32px;
  border: 0.5px dashed var(--color-border-input);
  border-radius: 8px;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  font-size: 12px;
}

.switch-cases__add:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.inspector__field :deep(.input),
.inspector__field :deep(.select),
.inspector__field :deep(.textarea),
.inspector__field :deep(.dict__input) {
  min-height: 38px;
  border-radius: 8px;
  padding: 9px 11px;
  font-size: 13px;
}

.inspector__field :deep(.textarea) {
  min-height: 76px;
}

.inspector__field :deep(.select) {
  background-position: right 12px center;
  padding-right: 34px;
}

.inspector__field :deep(.toggle) {
  min-height: 38px;
  gap: 10px;
  font-size: 13px;
}

.inspector__pick-button {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
  width: 100%;
  min-height: 58px;
  padding: 11px 12px;
  border: 0.5px solid var(--color-border-input);
  border-radius: 9px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  cursor: pointer;
  font-family: inherit;
  text-align: left;
  transition: border-color 0.15s, background 0.15s;
}

.inspector__pick-button:hover {
  border-color: color-mix(in srgb, var(--color-primary) 45%, var(--color-border-input));
  background: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg-secondary));
}

.inspector__pick-button span {
  font-size: 14px;
  font-weight: 600;
}

.inspector__pick-button small {
  color: var(--color-text-secondary);
  font-size: 12px;
}

.inspector__relations {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-top: 4px;
}

.inspector__relations-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: var(--color-text);
  font-size: 14px;
  font-weight: 600;
}

.inspector__relations-head small {
  color: var(--color-text-secondary);
  font-size: 13px;
  font-weight: 400;
}

.inspector__relation {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 42px;
  padding: 9px 11px;
  border: 0.5px solid var(--color-border-input);
  border-radius: 8px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  text-align: left;
  transition: border-color 0.15s, background 0.15s;
}

.inspector__relation:hover {
  border-color: color-mix(in srgb, var(--color-primary) 45%, var(--color-border-input));
  background: color-mix(in srgb, var(--color-primary) 6%, var(--color-bg-secondary));
}

.inspector__relation span {
  font-size: 13px;
  font-weight: 600;
}

.inspector__relation small {
  color: var(--color-text-secondary);
  font-size: 11px;
  font-family: var(--font-mono, monospace);
}
</style>
