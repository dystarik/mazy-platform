<template>
  <BaseNode
    class="switch-node"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="false"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div
      class="switch-node__summary nodrag"
      @mousedown.stop
      @pointerdown.stop
      @click.stop
    >
      <label class="switch-node__field">
        <span class="switch-node__field-label">Переключить по</span>
        <EditorOverflowTooltip :value="variableLabel" :known-variables="knownVariables" :variable-scope="variableScope">
          <InputText
            class="switch-node__input"
            :model-value="variableLabel"
            :readonly="isReadOnly"
            placeholder="variable"
            @update:model-value="value => updateVariable(text(value, ''))"
          />
        </EditorOverflowTooltip>
      </label>
    </div>

    <div class="switch-node__branches">
      <div class="switch-node__branches-head">
        <span>Ветки</span>
        <Button
          v-if="!isReadOnly"
          class="switch-node__head-add nodrag"
          type="button"
          title="Добавить ветку"
          @mousedown.stop
          @pointerdown.stop
          @click.stop="addCase"
          label="+"
        />
      </div>
      <div
        v-for="(switchCase, index) in cases"
        :key="index"
        class="switch-node__branch nodrag"
        @mousedown.stop
        @pointerdown.stop
        @click.stop
      >
        <div class="switch-node__branch-control">
          <EditorOverflowTooltip :value="switchCase.value" :known-variables="knownVariables" :variable-scope="variableScope">
            <InputText
              class="switch-node__case-input"
              :model-value="switchCase.value"
              :readonly="isReadOnly"
              placeholder="Значение"
              @update:model-value="value => updateCaseValue(index, text(value, ''))"
            />
          </EditorOverflowTooltip>
          <Button
            v-if="!isReadOnly"
            class="switch-node__remove"
            type="button"
            title="Удалить ветку"
            label="×"
            @click.stop="removeCase(index)"
          />
        </div>
        <Handle
          :id="switchCase.key"
          type="source"
          :position="Position.Right"
          class="switch-node__handle"
        />
      </div>
      <div class="switch-node__branch switch-node__branch--default">
        <span class="switch-node__branch-value">default</span>
        <Handle
          id="default"
          type="source"
          :position="Position.Right"
          class="switch-node__handle"
        />
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { Handle, Position } from '@vue-flow/core'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import type { NodeParamItem } from '@/types/api'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import type { VariableScope } from '@/components/editor/variableHighlight'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'

interface SwitchCase {
  value: string
  key: string
}

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
const variableLabel = computed(() => text(props.params.variable, 'variable'))
const cases = computed<SwitchCase[]>(() => {
  const rawCases = Array.isArray(props.params.cases) ? props.params.cases : []
  const usedKeys = new Set<string>()

  return rawCases
    .filter((item): item is Record<string, unknown> => isRecord(item))
    .map((item, index) => {
      const rawKey = text(item.branchKey, '')
      const value = text(item.value, rawKey || `case_${index + 1}`)
      const key = uniqueBranchKey(rawKey || value, usedKeys)
      return { key, value }
    })
})
function updateVariable(value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'variable', value)
}

function addCase(): void {
  if (props.isReadOnly) return
  const nextValue = `case_${cases.value.length + 1}`
  emit('update-param', 'cases', [
    ...cases.value.map(item => ({ value: item.value, branchKey: item.key })),
    { value: nextValue, branchKey: uniqueBranchKey(nextValue, new Set(cases.value.map(item => item.key))) },
  ])
}

function removeCase(index: number): void {
  if (props.isReadOnly) return
  const nextCases = cases.value.map(item => ({ value: item.value, branchKey: item.key }))
  nextCases.splice(index, 1)
  emit('update-param', 'cases', nextCases)
}

function updateCaseValue(index: number, value: string): void {
  if (props.isReadOnly) return
  const usedKeys = new Set<string>()
  emit('update-param', 'cases', cases.value.map((item, itemIndex) =>
    itemIndex === index
      ? { value, branchKey: uniqueBranchKey(value, usedKeys) }
      : { value: item.value, branchKey: uniqueBranchKey(item.key, usedKeys) },
  ))
}

function text(value: unknown, fallback: string): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  if (typeof value !== 'string') return fallback

  return value === '' ? fallback : value
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function uniqueBranchKey(value: string, usedKeys: Set<string>): string {
  const base = slugify(value) || `case_${usedKeys.size + 1}`
  let key = base
  let index = 2

  while (usedKeys.has(key)) {
    key = `${base}_${index}`
    index += 1
  }

  usedKeys.add(key)
  return key
}

function slugify(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^a-zа-яё0-9_]+/giu, '')
    .replace(/^_+|_+$/g, '')
}

</script>

<style scoped>
.switch-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.switch-node__summary {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.switch-node__field {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.switch-node__field-label {
  color: var(--color-text-secondary);
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.04em;
  line-height: 12px;
  text-transform: uppercase;
}

.switch-node__input {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 5px 8px;
}

.switch-node__case-input option {
  font-size: 14px;
}

.switch-node__input:focus {
  border-color: var(--color-primary);
}

.switch-node__branches {
  display: flex;
  flex-direction: column;
  gap: 0;
  box-shadow: inset 0 1px 0 var(--color-border);
  padding: 12px 0 0;
}

.switch-node__branches-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  min-height: 24px;
}

.switch-node__head-add {
  box-sizing: border-box;
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

.switch-node__head-add:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.switch-node__branch {
  position: relative;
  display: flex;
  align-items: center;
  height: 24px;
}

.switch-node__branch-control {
  box-sizing: border-box;
  width: 100%;
  height: 24px;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 4px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  padding: 0 5px 0 0;
}

.switch-node__branch-control:focus-within {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.switch-node__case-input {
  box-sizing: border-box;
  flex: 1;
  min-width: 0;
  height: 24px;
  border: 0;
  background: transparent;
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 5px 8px;
}

.switch-node__remove {
  box-sizing: border-box;
  flex-shrink: 0;
  width: 18px;
  height: 18px;
  border: none;
  border-radius: 3px;
  background: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  line-height: 1;
  padding: 0;
}

.switch-node__remove:hover {
  color: var(--color-danger);
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
}

.switch-node__branch-value {
  flex: 1;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--color-text);
  font-family: var(--font-mono, monospace);
  font-size: 11px;
  text-align: left;
  white-space: nowrap;
}

.switch-node__branch--default {
  padding-left: 8px;
  padding-right: 24px;
}

.switch-node__branch--default .switch-node__branch-value {
  color: var(--color-text-secondary);
}

.switch-node__handle {
  box-sizing: border-box;
  position: absolute !important;
  right: -16px;
  top: 50%;
  transform: translateY(-50%) !important;
  width: 8px;
  height: 8px;
  background: var(--color-primary);
  border: 1px solid var(--color-bg-card);
  border-radius: 50%;
  pointer-events: auto;
}
</style>
