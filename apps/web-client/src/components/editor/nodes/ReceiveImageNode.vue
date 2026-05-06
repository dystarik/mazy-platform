<template>
  <BaseNode
    class="receive-image-node"
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
    <div class="receive-image-node__body">
      <label class="receive-image-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="receive-image-node__label">Сохранить URL</span>
        <InputText
          class="receive-image-node__input"
          :model-value="imageUrlVariable"
          :readonly="isReadOnly"
          placeholder="image_url"
          @update:model-value="value => updateParam('imageUrlVariable', stringValue(value))"
        />
      </label>

      <label class="receive-image-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="receive-image-node__label">Сохранить подпись</span>
        <InputText
          class="receive-image-node__input"
          :model-value="captionVariable"
          :readonly="isReadOnly"
          placeholder="caption"
          @update:model-value="value => updateParam('captionVariable', stringValue(value))"
        />
      </label>
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

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const mainPortStyle: CSSProperties = { top: '72px' }
const imageUrlVariable = computed(() => stringValue(props.params.imageUrlVariable))
const captionVariable = computed(() => stringValue(props.params.captionVariable))

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
.receive-image-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.receive-image-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.receive-image-node__field {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.receive-image-node__field:not(:first-child) {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.receive-image-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.receive-image-node__input {
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

.receive-image-node__input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}
</style>
