<template>
  <div
    class="editor-grid-textarea"
    :style="{ height: `${height}px` }"
  >
    <EditorOverflowTooltip
      :value="modelValue"
      :disabled="suggestionsOpen"
      target-selector=".editor-grid-textarea__control"
      :known-variables="knownVariables"
      :variable-scope="variableScope"
    >
      <div
        class="editor-grid-textarea__highlight"
        :style="{ height: `${height}px` }"
        aria-hidden="true"
      >
        <span
          v-for="segment in highlightSegments"
          :key="segment.id"
          :class="`editor-grid-textarea__segment editor-grid-textarea__segment--${segment.state}`"
        >{{ segment.text }}</span>
      </div>
      <Textarea
        ref="textareaRef"
        class="editor-grid-textarea__control"
        rows="1"
        :value="modelValue"
        :readonly="readonly"
        :disabled="disabled"
        :placeholder="placeholder"
        :style="{ height: `${height}px` }"
        @input="handleInput"
        @focus="refreshSuggestions"
        @blur="closeSuggestionsLater"
        @click="refreshSuggestions"
        @keyup="handleKeyup"
        @keydown="handleKeydown"
      />
    </EditorOverflowTooltip>
    <Teleport to="body">
      <div
        v-if="suggestionsOpen"
        class="editor-grid-textarea__suggestions nodrag"
        :style="suggestionsStyle"
        @mousedown.prevent
      >
        <button
          v-for="(suggestion, index) in visibleSuggestions"
          :key="suggestion.value"
          class="editor-grid-textarea__suggestion"
          :class="{ 'editor-grid-textarea__suggestion--active': index === activeSuggestionIndex }"
          type="button"
          @mouseenter="activeSuggestionIndex = index"
          @click.stop="insertVariable(suggestion)"
        >
          <span class="editor-grid-textarea__suggestion-value">{{ suggestion.value }}</span>
          <span
            v-if="suggestion.detail"
            class="editor-grid-textarea__suggestion-detail"
          >{{ suggestion.detail }}</span>
        </button>
      </div>
    </Teleport>
    <button
      v-if="!readonly && !disabled"
      class="editor-grid-textarea__resize nodrag"
      type="button"
      title="Изменить высоту"
      @mousedown.stop.prevent
      @pointerdown.stop.prevent="startResize"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch, type CSSProperties } from 'vue'
import Textarea from 'primevue/textarea'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import {
  buildVariableHighlightSegments,
  variableSuggestionItems,
  type VariableSuggestion,
  type VariableScope,
} from '@/components/editor/variableHighlight'
import { getTextControlCaretPosition } from '@/components/editor/caretPosition'
import { useTextInputHistory } from '@/components/editor/textInputHistory'

const GRID_SIZE = 12

const props = withDefaults(defineProps<{
  modelValue: string
  readonly?: boolean
  disabled?: boolean
  placeholder?: string
  minRows?: number
  height?: number
  knownVariables?: string[]
  variableScope?: VariableScope
}>(), {
  readonly: false,
  disabled: false,
  placeholder: '',
  minRows: 2,
  height: undefined,
  knownVariables: () => [],
  variableScope: undefined,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:height': [value: number]
}>()

const textareaRef = ref<TextareaRef | null>(null)
const height = ref(normalizeHeight(props.height))
const currentScope = computed(() => props.variableScope ?? props.knownVariables)
const highlightSegments = computed(() =>
  buildVariableHighlightSegments(props.modelValue, currentScope.value),
)
const variableQuery = ref<string | null>(null)
const suggestionsStyle = ref<CSSProperties>({})
const activeSuggestionIndex = ref(0)
const suggestionsOpen = computed(() => visibleSuggestions.value.length > 0)
const visibleSuggestions = computed(() => {
  if (props.readonly || props.disabled || variableQuery.value === null) return []
  return variableSuggestionItems(currentScope.value, variableQuery.value)
    .slice(0, 8)
})
const { handleHistoryKeydown, rememberHistoryValue } = useTextInputHistory(
  () => props.modelValue,
  getTextareaElement,
  value => emit('update:modelValue', value),
)
let resizeStartY = 0
let resizeStartHeight = 0
let isResizing = false

watch(() => props.height, (value) => {
  if (isResizing) return
  height.value = normalizeHeight(value)
})

onBeforeUnmount(() => {
  stopResize()
})

function handleInput(event: Event): void {
  const textarea = event.target as HTMLTextAreaElement
  rememberHistoryValue(textarea)
  emit('update:modelValue', textarea.value)
  void nextTick(refreshSuggestions)
}

function handleKeydown(event: KeyboardEvent): void {
  if (handleHistoryKeydown(event)) return
  if (!suggestionsOpen.value) return
  if (event.key === 'Escape') {
    variableQuery.value = null
    event.preventDefault()
    return
  }
  if (event.key === 'ArrowDown') {
    activeSuggestionIndex.value = Math.min(visibleSuggestions.value.length - 1, activeSuggestionIndex.value + 1)
    event.preventDefault()
    return
  }
  if (event.key === 'ArrowUp') {
    activeSuggestionIndex.value = Math.max(0, activeSuggestionIndex.value - 1)
    event.preventDefault()
    return
  }
  if ((event.key === 'Enter' || event.key === 'Tab') && visibleSuggestions.value[0]) {
    insertVariable(visibleSuggestions.value[activeSuggestionIndex.value] ?? visibleSuggestions.value[0])
    event.preventDefault()
  }
}

function handleKeyup(event: KeyboardEvent): void {
  if (['ArrowDown', 'ArrowUp', 'Enter', 'Tab', 'Escape'].includes(event.key)) return
  refreshSuggestions()
}

function refreshSuggestions(): void {
  const textarea = getTextareaElement()
  if (!textarea || props.readonly || props.disabled) {
    variableQuery.value = null
    return
  }

  const cursor = textarea.selectionStart ?? textarea.value.length
  const nextQuery = readVariableQuery(textarea.value, cursor)
  if (nextQuery !== variableQuery.value) {
    activeSuggestionIndex.value = 0
  }
  variableQuery.value = nextQuery
  updateSuggestionsPosition(textarea)
}

function closeSuggestionsLater(): void {
  window.setTimeout(() => {
    variableQuery.value = null
  }, 120)
}

function insertVariable(suggestion: VariableSuggestion): void {
  const textarea = getTextareaElement()
  if (!textarea) return

  const cursor = textarea.selectionStart ?? textarea.value.length
  const openBraceIndex = textarea.value.lastIndexOf('{', cursor - 1)
  if (openBraceIndex < 0) return

  const insertValue = suggestion.insertValue ?? suggestion.value
  const suffix = suggestion.closeBrace === false ? '' : '}'
  const nextValue = `${textarea.value.slice(0, openBraceIndex)}{${insertValue}${suffix}${textarea.value.slice(cursor)}`
  const nextCursor = openBraceIndex + insertValue.length + 1 + suffix.length
  emit('update:modelValue', nextValue)
  variableQuery.value = suggestion.closeBrace === false ? insertValue : null
  void nextTick(() => {
    const nextTextarea = getTextareaElement()
    nextTextarea?.focus()
    nextTextarea?.setSelectionRange(nextCursor, nextCursor)
    if (suggestion.closeBrace === false) {
      refreshSuggestions()
    }
  })
}

function startResize(event: PointerEvent): void {
  isResizing = true
  resizeStartY = event.clientY
  resizeStartHeight = height.value

  window.addEventListener('pointermove', resize)
  window.addEventListener('pointerup', stopResize, { once: true })
}

function updateSuggestionsPosition(textarea: HTMLTextAreaElement): void {
  const caret = getTextControlCaretPosition(textarea)
  const width = Math.min(240, window.innerWidth - 16)
  suggestionsStyle.value = {
    left: `${Math.min(window.innerWidth - width - 8, caret.left + 48)}px`,
    top: `${Math.min(window.innerHeight - 180, caret.top)}px`,
    width: `${width}px`,
  }
}

function resize(event: PointerEvent): void {
  height.value = snapHeight(resizeStartHeight + event.clientY - resizeStartY)
  void nextTick(resetScroll)
}

function stopResize(): void {
  window.removeEventListener('pointermove', resize)
  if (isResizing) {
    emit('update:height', height.value)
  }
  isResizing = false
  resetScroll()
}

function normalizeHeight(value: unknown): number {
  return typeof value === 'number' && Number.isFinite(value)
    ? snapHeight(value)
    : minHeight()
}

function snapHeight(value: number): number {
  return Math.max(minHeight(), Math.round(value / GRID_SIZE) * GRID_SIZE)
}

function minHeight(): number {
  return GRID_SIZE * Math.max(1, props.minRows)
}

function resetScroll(element = getTextareaElement()): void {
  if (element) {
    element.scrollTop = 0
  }
}

function readVariableQuery(value: string, cursor: number): string | null {
  const openBraceIndex = value.lastIndexOf('{', cursor - 1)
  if (openBraceIndex < 0) return null
  const closeBraceIndex = value.lastIndexOf('}', cursor - 1)
  if (closeBraceIndex > openBraceIndex) return null
  const query = value.slice(openBraceIndex + 1, cursor)
  return query.includes('{') ? '' : query
}

type TextareaRef = HTMLTextAreaElement | { $el?: Element }

function getTextareaElement(): HTMLTextAreaElement | null {
  const value = textareaRef.value
  if (!value) return null
  if (value instanceof HTMLTextAreaElement) return value
  if (value.$el instanceof HTMLTextAreaElement) return value.$el
  return value.$el instanceof Element ? value.$el.querySelector('textarea') : null
}
</script>

<style scoped>
.editor-grid-textarea {
  position: relative;
  width: 100%;
  min-width: 0;
}

.editor-grid-textarea :deep(.editor-grid-textarea__control) {
  box-sizing: border-box;
  position: relative;
  z-index: 1;
  width: 100%;
  min-width: 0;
  height: 100% !important;
  min-height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: transparent !important;
  color: transparent !important;
  caret-color: var(--color-text);
  -webkit-text-fill-color: transparent;
  font: inherit;
  font-size: 11px;
  line-height: 16px;
  outline: none;
  overflow: hidden !important;
  padding: 3px 28px 3px 8px !important;
  resize: none !important;
}

.editor-grid-textarea__highlight {
  box-sizing: border-box;
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  background: var(--color-bg-secondary);
  color: var(--color-text);
  font: inherit;
  font-size: 11px;
  line-height: 16px;
  padding: 3px 28px 3px 8px;
  pointer-events: none;
  text-align: left;
  white-space: pre-wrap;
  overflow-wrap: break-word;
}

.editor-grid-textarea__segment--known {
  color: var(--color-primary);
}

.editor-grid-textarea__segment--future {
  color: #b7791f;
}

.editor-grid-textarea__segment--missing {
  color: var(--color-danger);
}

.editor-grid-textarea :deep(.editor-grid-textarea__control::placeholder) {
  color: var(--color-text-secondary);
  -webkit-text-fill-color: var(--color-text-secondary);
  opacity: 1;
}

.editor-grid-textarea :deep(.editor-grid-textarea__control:focus) {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.editor-grid-textarea__resize {
  position: absolute;
  right: 4px;
  bottom: 4px;
  z-index: 2;
  width: 14px;
  height: 14px;
  border: 0;
  background:
    linear-gradient(135deg, transparent 0 58%, var(--color-text-secondary) 58% 64%, transparent 64% 100%),
    linear-gradient(135deg, transparent 0 73%, var(--color-text-secondary) 73% 79%, transparent 79% 100%);
  cursor: ns-resize;
  opacity: 0.7;
  padding: 0;
  pointer-events: auto;
}

.editor-grid-textarea__resize:hover {
  opacity: 1;
}

.editor-grid-textarea__suggestions {
  position: fixed;
  z-index: 10000;
  box-sizing: border-box;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  background: var(--color-bg-card);
  font-size: 12px;
  line-height: 18px;
  box-shadow: 0 8px 24px rgba(15, 23, 42, 0.16);
}

.editor-grid-textarea__suggestion {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  border: 0;
  background: transparent;
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  padding: 5px 10px;
  text-align: left;
}

.editor-grid-textarea__suggestion-value {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}

.editor-grid-textarea__suggestion-detail {
  flex-shrink: 0;
  border-radius: 999px;
  background: var(--color-bg-secondary);
  color: var(--color-text-secondary);
  font-size: 10px;
  line-height: 14px;
  padding: 1px 6px;
}

.editor-grid-textarea__suggestion:hover,
.editor-grid-textarea__suggestion--active {
  background: var(--color-bg-secondary);
  color: var(--color-primary);
}
</style>
