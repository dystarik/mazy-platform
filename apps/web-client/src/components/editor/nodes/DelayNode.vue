<template>
  <BaseNode
    class="delay-node"
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
    <label
      class="delay-node__body nodrag"
      @mousedown.stop
      @pointerdown.stop
      @click.stop
    >
      <InputText
        class="delay-node__input"
        type="text"
        inputmode="numeric"
        :model-value="String(seconds)"
        :readonly="isReadOnly"
        @update:model-value="value => updateSeconds(String(value ?? ''))"
      />
      <span class="delay-node__suffix">сек.</span>
    </label>
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
const seconds = computed(() => {
  const value = props.params.seconds
  if (typeof value === 'number') return value
  if (typeof value === 'string') return value
  return 0
})

function updateSeconds(value: string): void {
  if (props.isReadOnly) return
  const normalized = value.replace(/[^\d.]/g, '')
  emit('update-param', 'seconds', normalized === '' ? '' : Number(normalized))
}
</script>

<style scoped>
.delay-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.delay-node__body {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 24px;
  padding: 0;
}

.delay-node__input {
  box-sizing: border-box;
  min-width: 0;
  width: 76px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 4px 6px;
  text-align: right;
}

.delay-node__input:focus {
  border-color: var(--color-primary);
}

.delay-node__suffix {
  color: var(--color-text-secondary);
  font-size: 11px;
}
</style>
