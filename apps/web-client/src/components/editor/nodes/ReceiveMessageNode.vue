<template>
  <BaseNode
    class="receive-message-node"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div class="receive-message-node__body">
      <label class="receive-message-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="receive-message-node__label">Сохранить текст</span>
        <InputText
          class="receive-message-node__input"
          :model-value="messageTextVariable"
          :readonly="isReadOnly"
          placeholder="message_text"
          @update:model-value="value => updateParam('messageTextVariable', stringValue(value))"
        />
      </label>

      <label class="receive-message-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="receive-message-node__label">Проверка</span>
        <Select
          class="receive-message-node__select"
          :model-value="validatorSelectValue"
          :options="validatorOptions"
          option-label="label"
          option-value="value"
          overlay-class="editor-node-select-overlay"
          :disabled="isReadOnly"
          @update:model-value="updateValidatorType"
        />
      </label>

      <label
        class="receive-message-node__option nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <Checkbox
          binary
          :model-value="deleteAfterReceive"
          :disabled="isReadOnly"
          @update:model-value="value => updateParam('deleteAfterReceive', Boolean(value))"
        />
        <span>Удалять после получения</span>
      </label>

      <label
        v-if="validatorType === 'regex'"
        class="receive-message-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="receive-message-node__label">Паттерн</span>
        <InputText
          class="receive-message-node__input"
          :model-value="validationPattern"
          :readonly="isReadOnly"
          placeholder="^.+$"
          @update:model-value="value => updateValidationParam('pattern', stringValue(value))"
        />
      </label>

      <div
        v-if="validatorType === 'number'"
        class="receive-message-node__range nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <label class="receive-message-node__field">
          <span class="receive-message-node__label">Мин.</span>
          <InputText
            class="receive-message-node__input"
            :model-value="validationMin"
            :readonly="isReadOnly"
            placeholder="0"
            @update:model-value="value => updateValidationParam('min', stringValue(value))"
          />
        </label>
        <label class="receive-message-node__field">
          <span class="receive-message-node__label">Макс.</span>
          <InputText
            class="receive-message-node__input"
            :model-value="validationMax"
            :readonly="isReadOnly"
            placeholder="100"
            @update:model-value="value => updateValidationParam('max', stringValue(value))"
          />
        </label>
      </div>

      <label
        v-if="validatorType"
        class="receive-message-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="receive-message-node__label">Ошибка</span>
        <EditorGridTextarea
          class="receive-message-node__textarea"
          :model-value="errorMessage"
          :readonly="isReadOnly"
          placeholder="Напишите сообщение об ошибке"
          :min-rows="2"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('errorMessage', value)"
        />
      </label>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Checkbox from 'primevue/checkbox'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
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
  knownVariables?: string[]
  variableScope?: VariableScope
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const mainPortStyle: CSSProperties = { top: '72px' }
const noValidatorValue = '__none__'
const messageTextVariable = computed(() => stringValue(props.params.messageTextVariable))
const deleteAfterReceive = computed(() => props.params.deleteAfterReceive === true)
const validatorType = computed(() => stringValue(props.params.validatorType))
const validatorSelectValue = computed(() => validatorType.value || noValidatorValue)
const validationParams = computed(() => isRecord(props.params.validationParams) ? props.params.validationParams : {})
const validationPattern = computed(() => stringValue(validationParams.value.pattern))
const validationMin = computed(() => stringValue(validationParams.value.min))
const validationMax = computed(() => stringValue(validationParams.value.max))
const errorMessage = computed(() => stringValue(props.params.errorMessage))
const validatorOptions = [
  { label: 'Без проверки', value: noValidatorValue },
  { label: 'Телефон', value: 'phone' },
  { label: 'Email', value: 'email' },
  { label: 'Число', value: 'number' },
  { label: 'Regex', value: 'regex' },
]

function updateParam(key: string, value: unknown): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function updateValidatorType(value: unknown): void {
  const nextValue = stringValue(value)
  updateParam('validatorType', nextValue === noValidatorValue ? '' : nextValue)
}

function updateValidationParam(key: 'pattern' | 'min' | 'max', value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'validationParams', {
    ...validationParams.value,
    [key]: value,
  })
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
.receive-message-node {
  width: 192px;
  min-width: 192px;
  max-width: 192px;
}

.receive-message-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.receive-message-node__field,
.receive-message-node__range {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 6px;
}

.receive-message-node__field:not(:first-child),
.receive-message-node__range {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 6px;
}

.receive-message-node__range {
  flex-direction: row;
  gap: 6px;
}

.receive-message-node__range .receive-message-node__field {
  flex: 1;
  min-width: 0;
  border-top: 0;
  padding: 0;
}

.receive-message-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: left;
}

.receive-message-node__input,
.receive-message-node__select {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 5px 8px;
}

.receive-message-node__input,
.receive-message-node__select {
  height: 24px;
}

.receive-message-node__input:focus,
.receive-message-node__select:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.receive-message-node__select :deep(.p-select-label) {
  padding: 3px 8px;
  font-size: 11px;
  line-height: 16px;
}

.receive-message-node__select :deep(.p-select-dropdown) {
  width: 24px;
}

.receive-message-node__option {
  display: flex;
  align-items: center;
  gap: 6px;
  min-height: 24px;
  padding: 0 0 6px;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.receive-message-node__option:not(:first-child) {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 6px;
}

.receive-message-node__option :deep(.p-checkbox) {
  width: 14px;
  height: 14px;
  flex: 0 0 14px;
}

.receive-message-node__option :deep(.p-checkbox-box) {
  width: 14px;
  height: 14px;
  border-radius: 4px;
}
</style>
