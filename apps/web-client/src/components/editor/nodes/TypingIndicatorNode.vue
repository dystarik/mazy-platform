<template>
  <BaseNode
    class="typing-indicator-node"
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
    <div class="typing-indicator-node__body">
      Набор текста
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
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
  'set-start': []
  delete: []
}>()

const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const mainPortStyle: CSSProperties = { top: '60px' }
</script>

<style scoped>
.typing-indicator-node {
  width: 204px;
  min-width: 204px;
  max-width: 204px;
}

.typing-indicator-node :deep(.bn__body) {
  box-sizing: border-box;
  min-height: 48px;
  justify-content: center;
  padding: 12px;
}

.typing-indicator-node__body {
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  text-align: center;
}
</style>
