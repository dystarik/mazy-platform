<template>
  <BaseNode
    class="condition-node"
    :label="label"
    :is-start="isStart"
    :is-selected="isSelected"
    :is-read-only="isReadOnly"
    :is-pickable="isPickable"
    :is-pick-target="isPickTarget"
    :is-related="isRelated"
    :accent-color="accentColor"
    :has-output="false"
    :input-style="mainPortStyle"
    @set-start="emit('set-start')"
    @delete="emit('delete')"
  >
    <div
      class="condition-node__body nodrag"
      @mousedown.stop
      @pointerdown.stop
      @click.stop
    >
      <div class="condition-node__expression">
        <EditorOverflowTooltip :value="leftOperand" :known-variables="knownVariables" :variable-scope="variableScope">
          <InputText
            class="condition-node__input"
            :model-value="leftOperand"
            :readonly="isReadOnly"
            placeholder="left"
            @update:model-value="value => updateParam('leftOperand', stringValue(value))"
          />
        </EditorOverflowTooltip>
        <Select
          class="condition-node__operator"
          :model-value="operator"
          :options="operatorOptions"
          option-label="label"
          option-value="value"
          overlay-class="editor-node-select-overlay"
          :disabled="isReadOnly"
          @update:model-value="value => updateParam('operator', stringValue(value))"
        />
        <EditorOverflowTooltip :value="rightOperand" :known-variables="knownVariables" :variable-scope="variableScope">
          <InputText
            class="condition-node__input"
            :model-value="rightOperand"
            :readonly="isReadOnly"
            placeholder="right"
            @update:model-value="value => updateParam('rightOperand', stringValue(value))"
          />
        </EditorOverflowTooltip>
      </div>
    </div>

    <template #ports>
      <div class="condition-node__ports">
        <div
          v-for="port in ports"
          :key="port.id"
          class="condition-node__port"
        >
          <span class="condition-node__port-label">{{ port.label }}</span>
          <Handle
            :id="port.id"
            type="source"
            :position="Position.Right"
            class="condition-node__handle"
          />
        </div>
      </div>
    </template>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import { Handle, Position } from '@vue-flow/core'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
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

const mainPortStyle: CSSProperties = { top: '60px' }
const ports = [
  { id: 'true', label: 'true' },
  { id: 'false', label: 'false' },
]
const operatorOptions = [
  { label: '=', value: '==' },
  { label: '!=', value: '!=' },
  { label: '>', value: '>' },
  { label: '>=', value: '>=' },
  { label: '<', value: '<' },
  { label: '<=', value: '<=' },
  { label: 'contains', value: 'contains' },
  { label: 'startsWith', value: 'startsWith' },
]
const accentColor = computed(() => getNodeAccentColor(props.nodeType))
const leftOperand = computed(() => stringValue(props.params.leftOperand))
const operator = computed(() => stringValue(props.params.operator, '=='))
const rightOperand = computed(() => stringValue(props.params.rightOperand))

function updateParam(key: string, value: string): void {
  if (props.isReadOnly) return
  emit('update-param', key, value)
}

function stringValue(value: unknown, fallback = ''): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : fallback
}
</script>

<style scoped>
.condition-node {
  width: 348px;
  min-width: 348px;
  max-width: 348px;
}

.condition-node :deep(.bn__body) {
  min-height: 72px;
}

.condition-node__body {
  padding: 0 48px 0 0;
}

.condition-node__expression {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 108px minmax(0, 1fr);
  gap: 6px;
  align-items: center;
  min-height: 24px;
}

.condition-node__input,
.condition-node__operator {
  box-sizing: border-box;
  min-width: 0;
  width: 100%;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  outline: none;
  padding: 4px 6px;
}

.condition-node__operator {
  padding: 0;
}

.condition-node__operator :deep(.p-select-label) {
  padding: 3px 4px 3px 8px;
  font-size: 11px;
  line-height: 16px;
}

.condition-node__operator :deep(.p-select-dropdown) {
  width: 22px;
}

.condition-node__input:focus,
.condition-node__operator:focus {
  border-color: var(--color-primary);
}

.condition-node__ports {
  position: absolute;
  top: 48px;
  right: 12px;
  width: 36px;
  pointer-events: none;
}

.condition-node__port {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  height: 24px;
}

.condition-node__port-label {
  max-width: 28px;
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--color-text-secondary);
  font-family: var(--font-mono, monospace);
  font-size: 10px;
  white-space: nowrap;
}

.condition-node__handle {
  box-sizing: border-box;
  position: absolute !important;
  right: -16px;
  top: 50%;
  width: 8px;
  height: 8px;
  transform: translateY(-50%) !important;
  background: var(--color-primary);
  border: 1px solid var(--color-bg-card);
  border-radius: 50%;
  pointer-events: auto;
}
</style>
