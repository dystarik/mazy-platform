<template>
  <BaseNode
    class="send-image-node"
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
    <div class="send-image-node__body">
      <label class="send-image-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="send-image-node__label">URL изображения</span>
        <EditorVariableInput
          class="send-image-node__variable-input"
          :model-value="imageUrl"
          :readonly="isReadOnly"
          placeholder="https://..."
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('imageUrl', stringValue(value))"
        />
      </label>

      <label class="send-image-node__field nodrag" @mousedown.stop @pointerdown.stop>
        <span class="send-image-node__label">Подпись</span>
        <EditorGridTextarea
          class="send-image-node__textarea"
          :model-value="caption"
          :readonly="isReadOnly"
          placeholder="Подпись к фото"
          :min-rows="2"
          :known-variables="knownVariables"
          :variable-scope="variableScope"
          @update:model-value="value => updateParam('caption', value)"
        />
      </label>

    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import type { NodeParamItem } from '@/types/api'
import EditorGridTextarea from '@/components/editor/EditorGridTextarea.vue'
import EditorVariableInput from '@/components/editor/EditorVariableInput.vue'
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
const imageUrl = computed(() => stringValue(props.params.imageUrl))
const caption = computed(() => stringValue(props.params.caption))

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
.send-image-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.send-image-node__body {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.send-image-node__field {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0 0 12px;
}

.send-image-node__field:not(:first-child) {
  box-shadow: inset 0 1px 0 var(--color-border);
  padding-top: 12px;
}

.send-image-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.send-image-node__input {
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

.send-image-node__input {
  height: 24px;
}

.send-image-node__variable-input {
  width: 100%;
  min-width: 0;
}

.send-image-node__textarea {
  width: 100%;
  min-width: 0;
}

.send-image-node__input:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}
</style>
