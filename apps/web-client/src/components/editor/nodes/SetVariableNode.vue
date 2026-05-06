<template>
  <BaseNode
    class="set-variable-node"
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
    <div
      class="set-variable-node__body nodrag"
      @mousedown.stop
      @pointerdown.stop
      @click.stop
    >
      <InputText
        class="set-variable-node__input"
        :model-value="variableName"
        :readonly="isReadOnly"
        placeholder="variable"
        @update:model-value="value => updateParam('variable', stringValue(value))"
      />
      <span class="set-variable-node__equals">=</span>
      <InputText
        class="set-variable-node__input"
        :model-value="variableValue"
        :readonly="isReadOnly"
        placeholder="value"
        @update:model-value="value => updateParam('value', stringValue(value))"
      />
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import InputText from 'primevue/inputtext'
import type { NodeParamItem } from '@/types/api'
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
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  delete: []
}>()

const mainPortStyle: CSSProperties = { top: '60px' }
const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const variableName = computed(() => stringValue(props.params.variable))
const variableValue = computed(() => stringValue(props.params.value))

function updateParam(key: string, value: string): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}
</script>

<style scoped>
.set-variable-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.set-variable-node__body {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
  gap: 6px;
  align-items: center;
  min-height: 24px;
  padding: 0;
}

.set-variable-node__input {
  box-sizing: border-box;
  min-width: 0;
  width: 100%;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 4px 6px;
}

.set-variable-node__input:focus {
  border-color: var(--color-primary);
}

.set-variable-node__equals {
  color: var(--color-text-secondary);
  font-family: var(--font-mono, monospace);
  font-size: 11px;
}
</style>
