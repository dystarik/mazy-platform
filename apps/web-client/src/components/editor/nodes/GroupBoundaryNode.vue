<template>
  <BaseNode
    class="gbn"
    :class="`gbn--${boundaryKind}`"
    :label="label"
    :is-start="false"
    :is-selected="isSelected"
    :is-read-only="true"
    :accent-color="boundaryKind === 'entry' ? '#2ecc71' : 'var(--color-primary)'"
    :style="nodeLayoutStyle"
    :has-input="boundaryKind === 'exit'"
    :has-output="boundaryKind === 'entry'"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
  >
    <div class="gbn__body">
      <span class="gbn__caption">{{ boundaryKind === 'entry' ? 'Внутренний вход' : 'Внутренний выход' }}</span>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import { getEditorNodeLayoutMetrics } from '@/components/editor/editorLayoutMetrics'
import BaseNode from './BaseNode.vue'

const props = defineProps<{
  nodeType: string
  isSelected: boolean
  label: string
}>()

const boundaryKind = computed(() => props.nodeType === 'group_exit_marker' ? 'exit' : 'entry')
const layoutMetrics = computed(() => getEditorNodeLayoutMetrics({ type: props.nodeType, params: {} }))
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutMetrics.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutMetrics.value.width}px`,
  '--node-min-height': `${layoutMetrics.value.height}px`,
}) as CSSProperties)
</script>

<style scoped>
.gbn :deep(.bn__header) {
  height: 24px;
}

.gbn :deep(.bn__title) {
  font-size: 11px;
}

.gbn :deep(.bn__body) {
  min-height: 24px;
  justify-content: center;
  padding: 6px 10px;
}

.gbn__body {
  display: flex;
  justify-content: center;
}

.gbn__caption {
  color: var(--color-text-secondary);
  font-size: 9px;
  line-height: 12px;
  text-align: center;
}

.gbn--entry :deep(.bn__handle--source),
.gbn--exit :deep(.bn__handle--target) {
  background: var(--node-accent);
}
</style>
