<template>
  <BaseNode
    class="http-request-node"
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
    <div class="http-request-node__body">
      <div
        class="http-request-node__request nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <label class="http-request-node__method">
          <span class="http-request-node__label">Метод</span>
          <Select
            class="http-request-node__select http-request-node__select--method"
            :model-value="method"
            :options="methodOptions"
            :disabled="isReadOnly"
            overlay-class="editor-node-select-overlay"
            @update:model-value="value => updateParam('method', stringValue(value))"
          />
        </label>

        <label class="http-request-node__url">
          <span class="http-request-node__label">URL</span>
          <EditorOverflowTooltip :value="url" :known-variables="knownVariables" :variable-scope="variableScope">
            <InputText
              class="http-request-node__input"
              :model-value="url"
              :readonly="isReadOnly"
              placeholder="https://api.example.com"
              @update:model-value="value => updateParam('url', stringValue(value))"
            />
          </EditorOverflowTooltip>
        </label>
      </div>

      <label
        v-if="method !== 'GET'"
        class="http-request-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <div class="http-request-node__section-head">
          <span class="http-request-node__label">Тело</span>
          <Select
            class="http-request-node__select http-request-node__select--body"
            :model-value="bodyType"
            :options="bodyTypeOptions"
            option-label="label"
            option-value="value"
            :disabled="isReadOnly"
            overlay-class="editor-node-select-overlay"
            @update:model-value="value => updateParam('bodyType', stringValue(value))"
          />
        </div>
        <EditorGridTextarea
          class="http-request-node__textarea"
          :model-value="body"
          :readonly="isReadOnly"
          :placeholder="bodyPlaceholder"
          :min-rows="2"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('body', value)"
        />
      </label>

      <div
        class="http-request-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <div class="http-request-node__section-head">
          <span class="http-request-node__label">Заголовки</span>
          <Button
            v-if="!isReadOnly"
            class="http-request-node__head-add"
            type="button"
            title="Добавить заголовок"
            label="+"
            @click.stop="addHeader"
          />
        </div>
        <div
          v-for="([key, value]) in headerEntries"
          :key="key"
          class="http-request-node__dict-row"
        >
          <EditorOverflowTooltip :value="key" :known-variables="knownVariables" :variable-scope="variableScope">
            <InputText
              class="http-request-node__dict-input"
              :model-value="key"
              :readonly="isReadOnly"
              placeholder="Header"
              @change="event => renameHeader(key, (event.target as HTMLInputElement).value)"
            />
          </EditorOverflowTooltip>
          <EditorOverflowTooltip :value="value" :known-variables="knownVariables" :variable-scope="variableScope">
            <InputText
              class="http-request-node__dict-input"
              :model-value="value"
              :readonly="isReadOnly"
              placeholder="value"
              @update:model-value="nextValue => updateHeaderValue(key, stringValue(nextValue))"
            />
          </EditorOverflowTooltip>
          <Button
            v-if="!isReadOnly"
            class="http-request-node__icon-button"
            type="button"
            title="Удалить заголовок"
            label="×"
            @click.stop="removeHeader(key)"
          />
        </div>
      </div>

      <label
        class="http-request-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="http-request-node__label">Сохранить ответ</span>
        <EditorOverflowTooltip :value="responseBodyVariable" :known-variables="knownVariables" :variable-scope="variableScope">
          <InputText
            class="http-request-node__input"
            :model-value="responseBodyVariable"
            :readonly="isReadOnly"
            placeholder="response_body"
            @update:model-value="value => updateParam('responseBodyVariable', stringValue(value))"
          />
        </EditorOverflowTooltip>
      </label>

      <label
        class="http-request-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="http-request-node__label">Сохранить статус</span>
        <EditorOverflowTooltip :value="responseStatusVariable" :known-variables="knownVariables" :variable-scope="variableScope">
          <InputText
            class="http-request-node__input"
            :model-value="responseStatusVariable"
            :readonly="isReadOnly"
            placeholder="Не сохранять"
            @update:model-value="value => updateParam('responseStatusVariable', stringValue(value))"
          />
        </EditorOverflowTooltip>
      </label>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import { getEditorNodeLayoutMetrics } from '@/components/editor/editorLayoutMetrics'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'

type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
type BodyType = 'json' | 'form' | 'raw'

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

const methodOptions: HttpMethod[] = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE']
const bodyTypeOptions: Array<{ value: BodyType; label: string; placeholder: string }> = [
  { value: 'json', label: 'JSON', placeholder: '{ "key": "{variable}" }' },
  { value: 'form', label: 'Form', placeholder: 'key=value&key2={variable}' },
  { value: 'raw', label: 'Raw', placeholder: 'Текст тела запроса' },
]

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const layoutMetrics = computed(() => getEditorNodeLayoutMetrics({ type: props.nodeType, params: props.params }))
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutMetrics.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutMetrics.value.width}px`,
  '--node-wide-width': `${layoutMetrics.value.width}px`,
  '--node-min-height': `${layoutMetrics.value.height}px`,
}) as CSSProperties)
const method = computed<HttpMethod>(() => readMethod(props.params.method))
const url = computed(() => stringParam('url'))
const body = computed(() => stringParam('body'))
const bodyType = computed<BodyType>(() => readBodyType(props.params.bodyType))
const responseBodyVariable = computed(() => stringParam('responseBodyVariable'))
const responseStatusVariable = computed(() => stringParam('responseStatusVariable'))
const headerEntries = computed(() => Object.entries(readHeaders()))
const bodyPlaceholder = computed(() =>
  bodyTypeOptions.find(option => option.value === bodyType.value)?.placeholder ?? '',
)

function updateParam(key: string, value: unknown): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function addHeader(): void {
  if (props.isReadOnly) return
  const headers = readHeaders()
  let index = Object.keys(headers).length + 1
  let key = `Header-${index}`
  while (key in headers) {
    index += 1
    key = `Header-${index}`
  }
  emit('update-param', 'headers', { ...headers, [key]: '' })
}

function renameHeader(oldKey: string, nextKey: string): void {
  if (props.isReadOnly) return
  const trimmed = nextKey.trim()
  if (!trimmed || trimmed === oldKey) return
  const headers = readHeaders()
  const value = headers[oldKey] ?? ''
  delete headers[oldKey]
  emit('update-param', 'headers', { ...headers, [trimmed]: value })
}

function updateHeaderValue(key: string, value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'headers', { ...readHeaders(), [key]: value })
}

function removeHeader(key: string): void {
  if (props.isReadOnly) return
  const headers = readHeaders()
  delete headers[key]
  emit('update-param', 'headers', headers)
}

function readHeaders(): Record<string, string> {
  const value = props.params.headers
  if (!isRecord(value)) return {}
  return Object.fromEntries(
    Object.entries(value)
      .filter(([key]) => key.trim().length > 0)
      .map(([key, itemValue]) => [key, stringValue(itemValue)]),
  )
}

function stringParam(key: string): string {
  return stringValue(props.params[key])
}

function readMethod(value: unknown): HttpMethod {
  return methodOptions.includes(value as HttpMethod) ? value as HttpMethod : 'GET'
}

function readBodyType(value: unknown): BodyType {
  return ['json', 'form', 'raw'].includes(String(value)) ? value as BodyType : 'json'
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
.http-request-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.http-request-node__request {
  display: grid;
  grid-template-columns: 84px minmax(0, 1fr);
  gap: 12px;
  padding: 0 0 12px;
}

.http-request-node__field,
.http-request-node__method,
.http-request-node__url {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 0;
}

.http-request-node__field {
  padding: 0 0 12px;
}

.http-request-node__field + .http-request-node__field,
.http-request-node__request + .http-request-node__field {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.http-request-node__label {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.http-request-node__input,
.http-request-node__dict-input,
.http-request-node__select {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 12px;
  outline: none;
}

.http-request-node__input,
.http-request-node__dict-input,
.http-request-node__select {
  height: 24px;
  padding: 3px 8px;
}

.http-request-node__input:focus,
.http-request-node__dict-input:focus,
.http-request-node__select:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.http-request-node__select--method {
  font-family: var(--font-mono, monospace);
  font-weight: 600;
}

.http-request-node__select--body {
  width: 72px;
  font-size: 11px;
}

.http-request-node__select :deep(.p-select-label) {
  padding: 3px 8px;
  font-size: 11px;
  line-height: 16px;
}

.http-request-node__select :deep(.p-select-dropdown) {
  width: 24px;
}

.http-request-node__section-head {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.http-request-node__head-add,
.http-request-node__icon-button {
  border: 0;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  line-height: 1;
  padding: 0;
}

.http-request-node__head-add {
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 4px;
  background: var(--color-bg);
  font: inherit;
}

.http-request-node__head-add:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.http-request-node__dict-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) 24px;
  align-items: center;
  gap: 6px;
}

.http-request-node__dict-row + .http-request-node__dict-row {
  margin-top: 0;
}

.http-request-node__icon-button {
  width: 24px;
  height: 24px;
  border-radius: 3px;
}

.http-request-node__icon-button:hover {
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
  color: var(--color-danger);
}
</style>
