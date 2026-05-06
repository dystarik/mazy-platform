<template>
  <BaseNode
    class="user-info-node"
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
    <div class="user-info-node__body">
      <label
        class="user-info-node__field nodrag"
        @mousedown.stop
        @pointerdown.stop
      >
        <span class="user-info-node__label">Префикс</span>
        <EditorOverflowTooltip :value="prefix">
          <InputText
            class="user-info-node__input"
            :model-value="prefix"
            :readonly="isReadOnly"
            placeholder="user"
            @update:model-value="value => updatePrefix(stringValue(value))"
          />
        </EditorOverflowTooltip>
      </label>

      <div class="user-info-node__variables">
        <span class="user-info-node__label">Будут доступны</span>
        <div class="user-info-node__chips">
          <span
            v-for="variable in variables"
            :key="variable"
            class="user-info-node__chip"
          >
            {{ variable }}
          </span>
        </div>
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import InputText from 'primevue/inputtext'
import type { NodeParamItem } from '@/types/api'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
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

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const prefix = computed(() => stringValue(props.params.prefix) || 'user')
const variables = computed(() => [
  `${prefix.value}_first_name`,
  `${prefix.value}_last_name`,
  `${prefix.value}_username`,
  `${prefix.value}_avatar_url`,
])
const mainPortStyle: CSSProperties = { top: '72px' }

function updatePrefix(value: string): void {
  if (props.isReadOnly) return
  emit('update-param', 'prefix', value)
}

function stringValue(value: unknown): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : ''
}
</script>

<style scoped>
.user-info-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.user-info-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.user-info-node__field,
.user-info-node__variables {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.user-info-node__variables {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.user-info-node__label {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.user-info-node__input {
  width: 100%;
  box-sizing: border-box;
  border: 1px solid var(--color-border);
  border-radius: 5px;
  background: var(--color-bg-muted);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  height: 24px;
  line-height: 16px;
  padding: 3px 8px;
}

.user-info-node__input:focus {
  border-color: var(--color-primary);
  outline: none;
}

.user-info-node__chips {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.user-info-node__chip {
  overflow: hidden;
  border-radius: 5px;
  background: var(--color-bg-muted);
  color: var(--color-text-secondary);
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  height: 24px;
  line-height: 24px;
  padding: 0 8px;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
