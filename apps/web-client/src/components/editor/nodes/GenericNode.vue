<template>
  <BaseNode
    :class="nodeClasses"
    :style="nodeStyle"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="outputPorts.length === 0"
    :has-output-ports="outputPorts.length > 0"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div
      class="gn-preview"
      :class="{ 'gn-preview--with-ports': outputPorts.length > 0 }"
      :style="portPreviewStyle"
    >
      <input
        v-if="isGroupNode && !isReadOnly"
        class="gn-preview__title-input nodrag nopan"
        :value="groupTitle"
        placeholder="Название группы"
        @input="updateGroupTitle"
        @pointerdown.stop
        @mousedown.stop
        @click.stop
        @keydown.stop
      >
      <p v-else class="gn-preview__main">{{ preview.main }}</p>
      <p v-if="preview.detail" class="gn-preview__detail">{{ preview.detail }}</p>
      <div v-if="preview.chips.length" class="gn-preview__chips">
        <span v-for="chip in preview.chips.slice(0, 4)" :key="chip" class="gn-preview__chip">
          {{ chip }}
        </span>
        <span v-if="preview.chips.length > 4" class="gn-preview__chip">
          +{{ preview.chips.length - 4 }}
        </span>
      </div>
    </div>

    <template v-if="outputPorts.length > 0" #ports>
      <div class="gn__ports">
        <div
          v-for="port in outputPorts"
          :key="port.id"
          class="gn__port"
        >
          <span class="gn__port-label">{{ port.label }}</span>
          <Handle
            type="source"
            :position="Position.Right"
            :id="port.id"
            class="gn__port-handle"
          />
        </div>
      </div>
    </template>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import { Handle, Position } from '@vue-flow/core'
import type { NodeParamItem } from '@/types/api'
import { GROUP_NODE_TYPE, getNodeOutputPorts } from '@/components/editor/editorTypes'
import { getNodePreview } from '@/components/editor/nodes/nodeUi'
import { getNodeAccentColor } from '@/components/editor/nodes/nodeMeta'
import BaseNode from './BaseNode.vue'

const props = defineProps<{
  /** Технический тип узла */
  nodeType: string
  /** Значения параметров текущего узла */
  params: Record<string, unknown>
  /** Описание параметров из каталога */
  catalogParams: NodeParamItem[]
  /** Узел отмечен как стартовый */
  isStart: boolean
  /** Узел выделен */
  isSelected: boolean
  /** Режим просмотра без редактирования */
  isReadOnly?: boolean
  /** Узел можно выбрать как источник messageIdVariable */
  isPickable?: boolean
  /** Узел ожидает выбора источника messageIdVariable */
  isPickTarget?: boolean
  /** Узел связан с выбранным сообщением */
  isRelated?: boolean
  /** Заголовок в шапке */
  label: string
}>()

const emit = defineEmits<{
  'update-param': [key: string, value: unknown]
  'set-start': []
  delete: []
}>()

const outputPorts = computed(() => getNodeOutputPorts(props.nodeType, props.params))
const preview = computed(() => getNodePreview(props.nodeType, props.params))
const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const isSwitchNode = computed(() => props.nodeType === 'switch')
const isGroupNode = computed(() => props.nodeType === GROUP_NODE_TYPE)
const groupTitle = computed(() => {
  const value = props.params.title
  return typeof value === 'string' ? cleanGroupTitle(value) : ''
})
const nodeClasses = computed(() => ({
  'gn-node--switch': isSwitchNode.value,
  'gn-node--group': isGroupNode.value,
}))
const nodeStyle = computed<CSSProperties>(() => {
  if (!isSwitchNode.value) return {}

  const longestPort = outputPorts.value.reduce(
    (max, port) => Math.max(max, port.label.length),
    0,
  )
  const rawWidth = Math.min(264, Math.max(216, 192 + Math.min(longestPort, 12) * 4))
  const width = Math.round(rawWidth / 12) * 12

  return {
    width: `${width}px`,
    minWidth: '216px',
    maxWidth: '264px',
  }
})
const portPreviewStyle = computed<CSSProperties>(() => {
  if (outputPorts.value.length === 0) return {}

  if (isSwitchNode.value) {
    return {
      minHeight: `${Math.max(84, outputPorts.value.length * 24 + 24)}px`,
    }
  }

  return {
    minHeight: `${Math.max(96, outputPorts.value.length * 24 + 24)}px`,
  }
})

function updateGroupTitle(event: Event): void {
  const input = event.target as HTMLInputElement | null
  emit('update-param', 'title', cleanGroupTitle(input?.value ?? ''))
}

function cleanGroupTitle(value: string): string {
  return value
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(line => line && !['Вход', 'Выход'].includes(line))
    .join(' ')
    .trim()
}
</script>

<style scoped>
.gn-preview {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 190px;
}
.gn-preview--with-ports {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  padding-right: 158px;
}
.gn-node--switch.bn--with-ports {
  width: 216px;
  min-width: 216px;
  max-width: 264px;
}
.gn-node--switch .gn-preview--with-ports {
  padding-right: 64px;
}
.gn-node--switch .gn-preview__main {
  font-size: 11px;
}
.gn-node--group :deep(.bn__body) {
  min-height: 72px;
}
.gn-node--group :deep(.bn__handle--target),
.gn-node--group :deep(.bn__handle--source) {
  top: calc(var(--node-header-height) + var(--node-grid) * 4);
  width: 10px;
  height: 10px;
  background: var(--color-primary);
  border: 2px solid var(--color-bg-card);
}
.gn-node--group .gn-preview {
  justify-content: center;
  min-height: 48px;
}
.gn-preview__main {
  margin: 0;
  color: var(--color-text);
  font-size: 12px;
  font-weight: 500;
  line-height: 12px;
  overflow-wrap: anywhere;
}
.gn-preview__title-input {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  height: 24px;
  border: 1px solid transparent;
  border-radius: 4px;
  background: transparent;
  color: var(--color-text);
  font: inherit;
  font-size: 12px;
  font-weight: 500;
  line-height: 12px;
  text-align: center;
  outline: none;
  padding: 0 6px;
}
.gn-preview__title-input:hover {
  background: var(--color-bg-secondary);
}
.gn-preview__title-input:focus {
  border-color: var(--color-primary);
  background: var(--color-bg);
  box-shadow: none;
}
.gn-preview__detail {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 12px;
  overflow-wrap: anywhere;
}
.gn-preview__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.gn-preview__chip {
  max-width: 150px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  border-radius: 999px;
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font-size: 9px;
  line-height: 12px;
  padding: 0 6px;
}

.gn__ports {
  position: absolute;
  top: 72px;
  right: 12px;
  width: 152px;
  display: flex;
  flex-direction: column;
  pointer-events: none;
}
.gn-node--switch .gn__ports {
  top: 72px;
  right: 12px;
  bottom: 12px;
  width: 60px;
}
.gn-node--switch .gn__port {
  min-height: 24px;
}
.gn-node--switch .gn__port-label {
  max-width: 44px;
  padding-right: 16px;
}
.gn__port {
  position: relative;
  height: 24px;
  min-height: 24px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  pointer-events: none;
}
.gn__port-label {
  padding-right: 18px;
  max-width: 132px;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 10px;
  color: var(--color-text-secondary);
  font-family: var(--font-mono, monospace);
  white-space: nowrap;
}
.gn__port-handle {
  box-sizing: border-box;
  position: absolute !important;
  right: -16px;
  top: 50%;
  transform: translateY(-50%) !important;
  width: 8px;
  height: 8px;
  background: var(--color-text-secondary);
  border: 1px solid var(--color-bg-card);
  border-radius: 50%;
  pointer-events: auto;
}
</style>
