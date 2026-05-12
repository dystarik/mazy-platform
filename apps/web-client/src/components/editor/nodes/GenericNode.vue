<template>
  <BaseNode
    :class="nodeClasses"
    :style="nodeLayoutStyle"
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
    :output-ports="outputPorts"
    :input-style="mainPortStyle"
    :output-style="mainPortStyle"
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

    <div v-if="outputPorts.length > 0" class="gn__ports">
      <div
        v-for="port in outputPorts"
        :key="port.id"
        class="gn__port"
        :style="portLabelStyle(port)"
      >
        <span class="gn__port-label">{{ port.label }}</span>
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import type { NodeParamItem } from '@/types/api'
import { GROUP_NODE_TYPE } from '@/components/editor/editorTypes'
import {
  describeEditorNodeLayout,
  type EditorNodeLayoutPort,
} from '@/components/editor/editorNodeLayoutContract'
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

const layoutContract = computed(() => describeEditorNodeLayout({ type: props.nodeType, params: props.params }))
const outputPorts = computed(() => layoutContract.value.outputPorts)
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
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutContract.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutContract.value.width}px`,
  '--node-wide-width': `${layoutContract.value.width}px`,
  '--node-min-height': `${layoutContract.value.height}px`,
}) as CSSProperties)
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

function portLabelStyle(port: EditorNodeLayoutPort): CSSProperties {
  return {
    top: `${port.y}px`,
  }
}

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
  inset: 0;
  width: 152px;
  pointer-events: none;
}
.gn-node--switch .gn__ports {
  right: 12px;
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
  position: absolute;
  right: 12px;
  height: 24px;
  min-height: 24px;
  width: 152px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  transform: translateY(-50%);
  pointer-events: none;
}
.gn-node--switch .gn__port {
  width: 60px;
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
</style>
