<template>
  <div class="editor-node-port-layer">
    <Handle
      v-for="port in ports"
      :id="port.id"
      :key="port.id"
      type="source"
      :position="Position.Right"
      class="editor-node-port-layer__handle"
      :style="portStyle(port)"
      :title="port.label || port.id"
    />
  </div>
</template>

<script setup lang="ts">
import { type CSSProperties } from 'vue'
import { Handle, Position } from '@vue-flow/core'

export interface EditorNodePortLayerPort {
  id: string
  label?: string
  y: number
}

defineProps<{
  ports: EditorNodePortLayerPort[]
}>()

function portStyle(port: EditorNodePortLayerPort): CSSProperties {
  return {
    top: `${normalizePortY(port.y)}px`,
    right: '0px',
    transform: 'translate(50%, -50%)',
  }
}

function normalizePortY(value: number): number {
  return Number.isFinite(value) ? value : 0
}
</script>

<style scoped>
.editor-node-port-layer {
  position: absolute;
  inset: 0;
  overflow: visible;
  pointer-events: none;
}

.editor-node-port-layer__handle {
  box-sizing: border-box;
  position: absolute !important;
  width: var(--node-port-size, 8px) !important;
  height: var(--node-port-size, 8px) !important;
  background: var(--color-primary) !important;
  border: 1px solid var(--color-bg-card) !important;
  border-radius: 50%;
  pointer-events: auto;
}
</style>
