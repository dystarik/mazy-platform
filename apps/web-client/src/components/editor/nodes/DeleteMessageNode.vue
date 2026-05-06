<template>
  <BaseNode
    class="delete-message-node"
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
      class="delete-message-node__target nodrag"
      :class="{ 'delete-message-node__target--missing': hasMissingTarget }"
      @mousedown.stop
      @pointerdown.stop
    >
      <span class="delete-message-node__label">{{ messageSourceLabel ? 'Удалить' : 'Сообщение' }}</span>
      <div class="delete-message-node__target-control">
        <Button
          class="delete-message-node__target-button"
          type="button"
          :disabled="isReadOnly"
          :label="targetLabel"
          @click.stop="emit('pick-message')"
        />
        <Button
          v-if="hasTargetNode && !isReadOnly"
          class="delete-message-node__clear-target"
          type="button"
          title="Отменить выбор"
          label="×"
          @click.stop="clearTarget"
        />
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import Button from 'primevue/button'
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
  messageSourceLabel?: string | null
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  'pick-message': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const mainPortStyle: CSSProperties = { top: '72px' }
const hasMissingTarget = computed(() =>
  Boolean(props.params.targetMessageNodeId) && !props.messageSourceLabel,
)
const hasTargetNode = computed(() => Boolean(props.params.targetMessageNodeId))
const targetLabel = computed(() => {
  if (props.messageSourceLabel) return props.messageSourceLabel
  if (hasMissingTarget.value) return 'Сообщение удалено'
  return 'Выбрать сообщение'
})

function clearTarget(): void {
  if (props.isReadOnly) return
  emit('update-param', 'targetMessageNodeId', '')
  emit('update-param', 'messageIdVariable', '')
}

</script>

<style scoped>
.delete-message-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.delete-message-node__target {
  display: flex;
  flex-direction: column;
  gap: 0;
  padding: 0;
}

.delete-message-node__label {
  align-self: flex-start;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
}

.delete-message-node__target-control {
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 0;
}

.delete-message-node__target-button {
  box-sizing: border-box;
  display: flex;
  align-items: center;
  flex: 1;
  min-width: 0;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  font-size: 11px;
  padding: 3px 8px;
  text-align: left;
}

.delete-message-node__clear-target {
  box-sizing: border-box;
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text-secondary);
  cursor: pointer;
  font: inherit;
  line-height: 1;
  padding: 0;
}

.delete-message-node__clear-target:hover {
  border-color: var(--color-danger);
  color: var(--color-danger);
}

.delete-message-node__target-button:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.delete-message-node__target-button:disabled {
  cursor: default;
}

.delete-message-node__target--missing .delete-message-node__target-button {
  border-color: var(--color-danger);
  color: var(--color-danger);
}
</style>
