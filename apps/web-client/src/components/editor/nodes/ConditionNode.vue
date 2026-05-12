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
    :style="nodeLayoutStyle"
    :has-output="false"
    :has-output-ports="ports.length > 0"
    :output-ports="ports"
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

    <div class="condition-node__ports">
      <div
        v-for="port in ports"
        :key="port.id"
        class="condition-node__port"
        :style="portLabelStyle(port)"
      >
        <span class="condition-node__port-label">{{ port.label }}</span>
      </div>
    </div>
  </BaseNode>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import {
  describeEditorNodeLayout,
  type EditorNodeLayoutPort,
} from '@/components/editor/editorNodeLayoutContract'
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

const layoutContract = computed(() => describeEditorNodeLayout({ type: props.nodeType, params: props.params }))
const mainPortStyle = computed<CSSProperties>(() => ({ top: `${layoutContract.value.inputPortY}px` }))
const nodeLayoutStyle = computed<CSSProperties>(() => ({
  '--node-width': `${layoutContract.value.width}px`,
  '--node-wide-width': `${layoutContract.value.width}px`,
  '--node-min-height': `${layoutContract.value.height}px`,
}) as CSSProperties)
const ports = computed(() => layoutContract.value.outputPorts)
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

function portLabelStyle(port: EditorNodeLayoutPort): CSSProperties {
  return {
    top: `${port.y}px`,
  }
}

function stringValue(value: unknown, fallback = ''): string {
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)
  return typeof value === 'string' ? value : fallback
}
</script>

<style scoped>
.condition-node :deep(.bn__body) {
  min-height: 48px;
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
  top: 0;
  right: 0;
  bottom: 0;
  width: 36px;
  pointer-events: none;
}

.condition-node__port {
  position: absolute;
  right: 12px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  height: 24px;
  width: 36px;
  transform: translateY(-50%);
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
</style>
