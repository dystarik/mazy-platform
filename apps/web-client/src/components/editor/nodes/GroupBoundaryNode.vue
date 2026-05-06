<template>
  <BaseNode
    class="gbn"
    :class="`gbn--${boundaryKind}`"
    :label="label"
    :is-start="false"
    :is-selected="isSelected"
    :is-read-only="true"
    :accent-color="boundaryKind === 'entry' ? '#2ecc71' : 'var(--color-primary)'"
    :has-input="boundaryKind === 'exit'"
    :has-output="boundaryKind === 'entry'"
  >
    <div class="gbn__body">
      <span class="gbn__caption">{{ boundaryKind === 'entry' ? 'Внутренний вход' : 'Внутренний выход' }}</span>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseNode from './BaseNode.vue'

const props = defineProps<{
  nodeType: string
  isSelected: boolean
  label: string
}>()

const boundaryKind = computed(() => props.nodeType === 'group_exit_marker' ? 'exit' : 'entry')
</script>

<style scoped>
.gbn {
  --node-width: 120px;
}

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
  top: 36px;
  background: var(--node-accent);
}
</style>
