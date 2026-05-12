<template>
  <div
    class="bn"
    :class="[{
      'bn--start': isStart,
      'bn--selected': isSelected,
      'bn--pickable': isPickable,
      'bn--pick-target': isPickTarget,
      'bn--with-ports': hasOutputPorts || outputPorts.length > 0,
      'bn--read-only': isReadOnly,
    }, relationClass]"
    :style="baseStyle"
  >
    <!-- Входной хэндл -->
    <Handle
      v-if="hasInput"
      type="target"
      :position="Position.Left"
      class="bn__handle bn__handle--target"
      :style="inputStyle"
    />

    <!-- Шапка: стартовый маркер + тип узла + удаление -->
    <div class="bn__header">
      <Button
        v-if="!isStart"
        v-show="!isReadOnly"
        class="bn__action bn__action--set-start"
        title="Сделать стартовым"
        label="◯"
        @mousedown.stop
        @pointerdown.stop
        @click.stop="emit('set-start')"
      />
      <span
        v-if="isStart"
        class="bn__action bn__action--start"
        title="Стартовый узел"
      >●</span>
      <span class="bn__title">{{ label }}</span>
      <Button
        v-show="!isReadOnly"
        class="bn__action bn__action--delete"
        title="Удалить"
        label="×"
        @mousedown.stop
        @pointerdown.stop
        @click.stop="emit('delete')"
      />
    </div>

    <!-- Тело -->
    <div
      class="bn__body"
      @wheel.stop
      @keydown.stop
    >
      <slot />
    </div>

    <!-- Порты: либо стандартный output, либо кастомные через slot -->
    <slot name="ports">
      <EditorNodePortLayer
        v-if="outputPorts.length > 0"
        :ports="outputPorts"
      />
      <Handle
        v-else-if="hasOutput"
        type="source"
        :position="Position.Right"
        :class="['bn__handle', 'bn__handle--source', outputClass]"
        :style="outputStyle"
      />
    </slot>
  </div>
</template>

<script setup lang="ts">
import { computed, type CSSProperties } from 'vue'
import { Handle, Position } from '@vue-flow/core'
import Button from 'primevue/button'
import EditorNodePortLayer, {
  type EditorNodePortLayerPort,
} from '@/components/editor/EditorNodePortLayer.vue'

const props = withDefaults(
  defineProps<{
    label: string
    isStart: boolean
    isSelected: boolean
    isReadOnly?: boolean
    isPickable?: boolean
    isPickTarget?: boolean
    isRelated?: boolean
    relationTone?: 'edit' | 'delete' | 'source' | 'jump' | null
    hasInput?: boolean
    hasOutput?: boolean
    hasOutputPorts?: boolean
    accentColor?: string
    outputClass?: string
    inputStyle?: CSSProperties
    outputStyle?: CSSProperties
    outputPorts?: EditorNodePortLayerPort[]
  }>(),
  {
    isReadOnly: false,
    isPickable: false,
    isPickTarget: false,
    isRelated: false,
    relationTone: null,
    hasInput: true,
    hasOutput: true,
    hasOutputPorts: false,
    accentColor: 'var(--color-primary)',
    outputClass: '',
    inputStyle: undefined,
    outputStyle: undefined,
    outputPorts: () => [],
  },
)

const emit = defineEmits<{
  'set-start': []
  delete: []
}>()

const baseStyle = computed<CSSProperties>(() => ({
  '--node-accent': props.accentColor,
}) as CSSProperties)

const relationClass = computed(() => {
  if (!props.isRelated) return ''
  return `bn--related-${props.relationTone ?? 'edit'}`
})
</script>

<style scoped>
/* ── Каркас ───────────────────────────────────────────────────────────────── */
.bn {
  --node-grid: 12px;
  --node-half-grid: 6px;
  --node-control-height: 24px;
  --node-port-size: 8px;
  --node-width: calc(var(--node-grid) * 17);
  --node-wide-width: calc(var(--node-grid) * 36);
  --node-header-height: calc(var(--node-grid) * 3);
  --node-body-padding-x: var(--node-grid);
  --node-body-padding-y: var(--node-grid);
  --node-border-color: var(--color-border);
  box-sizing: border-box;
  background: var(--color-bg-card);
  border: 0;
  border-radius: 6px;
  outline: 1px solid var(--node-border-color);
  outline-offset: 0;
  width: var(--node-width);
  min-width: var(--node-width);
  max-width: var(--node-width);
  min-height: var(--node-min-height, auto);
  overflow: visible;
  cursor: grab;
  position: relative;
  transition: border-color 0.15s;
}
.bn:active {
  cursor: grabbing;
}
.bn--read-only,
.bn--read-only:active {
  cursor: default;
}
.bn--with-ports {
  width: var(--node-wide-width);
  min-width: var(--node-wide-width);
  max-width: var(--node-wide-width);
}
.bn:hover {
  --node-border-color: var(--color-text-secondary);
}
.bn--selected {
  --node-border-color: var(--color-primary);
  box-shadow:
    0 0 0 2px color-mix(in srgb, var(--color-primary) 88%, transparent),
    0 0 0 5px color-mix(in srgb, var(--color-primary) 14%, transparent);
}
.bn--start {
  --node-border-color: #2ecc71;
}
.bn--selected.bn--start {
  --node-border-color: #2ecc71;
  box-shadow:
    0 0 0 2px #2ecc71,
    0 0 0 5px color-mix(in srgb, var(--color-primary) 14%, transparent);
}
.bn--pickable {
  --node-border-color: color-mix(in srgb, var(--color-primary) 70%, var(--color-border));
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary) 14%, transparent);
}
.bn--pick-target {
  --node-border-color: color-mix(in srgb, var(--color-primary) 84%, var(--color-border));
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-primary) 12%, transparent);
}
.bn--related-edit {
  --node-border-color: color-mix(in srgb, #f59f00 72%, var(--color-border));
  box-shadow:
    0 0 0 2px color-mix(in srgb, #f59f00 88%, transparent),
    0 0 0 5px color-mix(in srgb, #f59f00 14%, transparent);
}
.bn--related-delete {
  --node-border-color: color-mix(in srgb, #e03131 78%, var(--color-border));
  box-shadow:
    0 0 0 2px color-mix(in srgb, #e03131 88%, transparent),
    0 0 0 5px color-mix(in srgb, #e03131 14%, transparent);
}
.bn--related-source {
  --node-border-color: color-mix(in srgb, #2ecc71 82%, var(--color-border));
  box-shadow:
    0 0 0 2px color-mix(in srgb, #2ecc71 88%, transparent),
    0 0 0 5px color-mix(in srgb, #2ecc71 14%, transparent);
}
.bn--related-jump {
  --node-border-color: color-mix(in srgb, #845ef7 78%, var(--color-border));
  box-shadow:
    0 0 0 2px color-mix(in srgb, #845ef7 88%, transparent),
    0 0 0 5px color-mix(in srgb, #845ef7 14%, transparent);
}

/* ── Шапка ────────────────────────────────────────────────────────────────── */
.bn__header {
  display: flex;
  align-items: center;
  gap: 6px;
  box-sizing: border-box;
  height: var(--node-header-height);
  padding: 0 var(--node-grid);
  box-shadow: inset 0 -1px 0 var(--color-border);
}
.bn__title {
  font-size: 11px;
  font-weight: 500;
  color: var(--color-text);
  flex: 1;
  font-family: var(--font-mono, monospace);
  text-align: center;
}
.bn__action {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  border: none;
  background: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  border-radius: 3px;
  padding: 0;
  font-size: 12px;
  line-height: 1;
  transition: color 0.15s, background 0.15s;
}
.bn__action:hover { color: var(--color-text); }
.bn__action--set-start {
  color: var(--color-text-secondary);
}
.bn__action--start { color: #2ecc71; cursor: default; }
.bn__action--delete:hover {
  color: var(--color-danger);
  background: color-mix(in srgb, var(--color-danger) 12%, transparent);
}

/* ── Тело ────────────────────────────────────────────────────────────────── */
.bn__body {
  padding: var(--node-body-padding-y) var(--node-body-padding-x);
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: var(--node-half-grid);
}
.bn--read-only .bn__body {
  cursor: default;
}

/* ── Единый вид PrimeVue-контролов внутри узлов ─────────────────────────── */
.bn :deep(.p-inputtext),
.bn :deep(.p-textarea),
.bn :deep(.p-select) {
  box-sizing: border-box;
  min-width: 0;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg);
  color: var(--color-text);
  font-family: inherit;
  font-size: 11px;
  line-height: 18px;
  outline: none;
  box-shadow: none;
}

.bn :deep(.p-inputtext),
.bn :deep(.p-textarea) {
  font-family: var(--font-mono, monospace);
  line-height: 18px;
}

.bn :deep(.p-inputtext) {
  height: var(--node-control-height);
  padding: 2px 8px;
}

.bn :deep(.p-textarea) {
  min-height: var(--node-control-height);
  padding: 2px 8px;
}

.bn :deep(.p-select) {
  height: var(--node-control-height);
  padding: 0;
}

.bn :deep(.p-select-label) {
  padding: 2px 8px;
  font-size: 11px;
  line-height: 18px;
  text-align: left;
}

.bn :deep(.p-select-dropdown) {
  width: var(--node-control-height);
}

.bn :deep(.p-inputtext:enabled:focus),
.bn :deep(.p-textarea:enabled:focus),
.bn :deep(.p-select:not(.p-disabled).p-focus) {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.bn :deep(.p-button:not(.bn__action)) {
  min-width: 0;
  border-radius: 5px;
  font-family: inherit;
  font-size: 11px;
  line-height: 16px;
}

.bn :deep(.p-button:not(.bn__action) .p-button-label) {
  font-weight: inherit;
  line-height: inherit;
}

/* ── Хэндлы ──────────────────────────────────────────────────────────────── */
.bn__handle {
  box-sizing: border-box;
  width: var(--node-port-size);
  height: var(--node-port-size);
  background: var(--color-text-secondary);
  border: 1px solid var(--color-bg-card);
  border-radius: 50%;
}
.bn__handle--target,
.bn__handle--source {
  top: calc(var(--node-header-height) + var(--node-grid) * 3);
}
.bn__handle--target {
  background: var(--color-primary);
}

.bn :deep(input),
.bn :deep(textarea),
.bn :deep(select),
.bn :deep(button),
.bn :deep(.p-inputtext),
.bn :deep(.p-textarea),
.bn :deep(.p-select),
.bn :deep(.editor-variable-input__control),
.bn :deep(.editor-variable-input__highlight) {
  box-sizing: border-box !important;
}
</style>
