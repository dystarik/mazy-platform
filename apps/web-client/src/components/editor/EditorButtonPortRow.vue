<template>
  <div class="editor-button-port-row" :style="rowStyle">
    <slot />
    <Handle
      v-if="showHandle && handleId"
      :id="handleId"
      type="source"
      :position="Position.Right"
      class="editor-button-port-row__handle"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import { Handle, Position } from '@vue-flow/core'

const props = withDefaults(defineProps<{
  handleId?: string
  showHandle?: boolean
  gridRows?: number
}>(), {
  handleId: '',
  showHandle: false,
  gridRows: 2,
})

const rowStyle = computed<CSSProperties>(() => ({
  '--editor-button-port-row-height': `${normalizeGridRows(props.gridRows) * 12}px`,
} as CSSProperties))

function normalizeGridRows(value: number): number {
  if (!Number.isFinite(value)) return 2
  return Math.max(1, Math.round(value))
}
</script>

<style scoped>
.editor-button-port-row {
  --editor-button-port-row-height: 24px;
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  align-items: center;
  min-width: 0;
  height: var(--editor-button-port-row-height);
  min-height: var(--editor-button-port-row-height);
  max-height: var(--editor-button-port-row-height);
  overflow: visible;
}

.editor-button-port-row__handle {
  box-sizing: border-box;
  position: absolute !important;
  right: -16px;
  top: 50%;
  width: 8px !important;
  height: 8px !important;
  transform: translateY(-50%) !important;
  background: var(--color-primary) !important;
  border: 1px solid var(--color-bg-card) !important;
  border-radius: 50%;
  pointer-events: auto;
}
</style>
