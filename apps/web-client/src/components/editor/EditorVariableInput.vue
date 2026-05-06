<template>
  <div class="editor-variable-input">
    <EditorOverflowTooltip
      :value="modelValue"
      :disabled="suggestionsOpen"
      target-selector=".editor-variable-input__control"
      :known-variables="knownVariables"
      :variable-scope="variableScope"
    >
      <div
        class="editor-variable-input__highlight"
        aria-hidden="true"
      >
        <span
          v-for="segment in highlightSegments"
          :key="segment.id"
          :class="`editor-variable-input__segment editor-variable-input__segment--${segment.state}`"
        >{{ segment.text }}</span>
      </div>
      <InputText
        ref="inputRef"
        class="editor-variable-input__control"
        :value="modelValue"
        :readonly="readonly"
        :disabled="disabled"
        :placeholder="placeholder"
        @input="handleInput"
        @focus="refreshSuggestions"
        @blur="closeSuggestionsLater"
        @click="refreshSuggestions"
        @keyup="handleKeyup"
        @keydown="handleKeydown"
        @change="event => emit('change', event)"
      />
    </EditorOverflowTooltip>
    <Teleport to="body">
      <div
        v-if="suggestionsOpen"
        class="editor-variable-input__suggestions nodrag"
        :style="suggestionsStyle"
        @mousedown.prevent
      >
        <button
          v-for="(suggestion, index) in visibleSuggestions"
          :key="suggestion"
          class="editor-variable-input__suggestion"
          :class="{ 'editor-variable-input__suggestion--active': index === activeSuggestionIndex }"
          type="button"
          @mouseenter="activeSuggestionIndex = index"
          @click.stop="insertVariable(suggestion)"
        >
          {{ suggestion }}
        </button>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, type CSSProperties } from 'vue'
import InputText from 'primevue/inputtext'
import EditorOverflowTooltip from '@/components/editor/EditorOverflowTooltip.vue'
import {
  buildVariableHighlightSegments,
  variableSuggestions,
  type VariableScope,
} from '@/components/editor/variableHighlight'
import { getTextControlCaretPosition } from '@/components/editor/caretPosition'

const props = withDefaults(defineProps<{
  modelValue: string
  readonly?: boolean
  disabled?: boolean
  placeholder?: string
  knownVariables?: string[]
  variableScope?: VariableScope
}>(), {
  readonly: false,
  disabled: false,
  placeholder: '',
  knownVariables: () => [],
  variableScope: undefined,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  change: [event: Event]
}>()

const highlightSegments = computed(() =>
  buildVariableHighlightSegments(props.modelValue, currentScope.value),
)
const inputRef = ref<InputRef | null>(null)
const variableQuery = ref<string | null>(null)
const suggestionsStyle = ref<CSSProperties>({})
const activeSuggestionIndex = ref(0)
const currentScope = computed(() => props.variableScope ?? props.knownVariables)
const suggestionVariables = computed(() => variableSuggestions(currentScope.value))
const suggestionsOpen = computed(() => visibleSuggestions.value.length > 0)
const visibleSuggestions = computed(() => {
  if (props.readonly || props.disabled || variableQuery.value === null) return []
  const query = variableQuery.value.toLowerCase()
  return suggestionVariables.value
    .filter(variable => variable.toLowerCase().includes(query))
    .slice(0, 8)
})

function handleInput(event: Event): void {
  const input = event.target as HTMLInputElement
  emit('update:modelValue', input.value)
  void nextTick(refreshSuggestions)
}

function handleKeydown(event: KeyboardEvent): void {
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
  const input = getInputElement()
  if (!input || props.readonly || props.disabled) {
    variableQuery.value = null
    return
  }

  const cursor = input.selectionStart ?? input.value.length
  const nextQuery = readVariableQuery(input.value, cursor)
  if (nextQuery !== variableQuery.value) {
    activeSuggestionIndex.value = 0
  }
  variableQuery.value = nextQuery
  updateSuggestionsPosition(input)
}

function closeSuggestionsLater(): void {
  window.setTimeout(() => {
    variableQuery.value = null
  }, 120)
}

function insertVariable(variable: string): void {
  const input = getInputElement()
  if (!input) return

  const cursor = input.selectionStart ?? input.value.length
  const openBraceIndex = input.value.lastIndexOf('{', cursor - 1)
  if (openBraceIndex < 0) return

  const nextValue = `${input.value.slice(0, openBraceIndex)}{${variable}}${input.value.slice(cursor)}`
  const nextCursor = openBraceIndex + variable.length + 2
  emit('update:modelValue', nextValue)
  variableQuery.value = null
  void nextTick(() => {
    const nextInput = getInputElement()
    nextInput?.focus()
    nextInput?.setSelectionRange(nextCursor, nextCursor)
  })
}

function updateSuggestionsPosition(input: HTMLInputElement): void {
  const caret = getTextControlCaretPosition(input)
  const width = Math.min(240, window.innerWidth - 16)
  suggestionsStyle.value = {
    left: `${Math.min(window.innerWidth - width - 8, caret.left + 48)}px`,
    top: `${Math.min(window.innerHeight - 180, caret.top)}px`,
    width: `${width}px`,
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

type InputRef = HTMLInputElement | { $el?: Element }

function getInputElement(): HTMLInputElement | null {
  const value = inputRef.value
  if (!value) return null
  if (value instanceof HTMLInputElement) return value
  if (value.$el instanceof HTMLInputElement) return value.$el
  return value.$el instanceof Element ? value.$el.querySelector('input') : null
}
</script>

<style scoped>
.editor-variable-input {
  position: relative;
  width: 100%;
  height: 24px;
  min-width: 0;
}

.editor-variable-input__highlight,
.editor-variable-input :deep(.editor-variable-input__control) {
  box-sizing: border-box;
  width: 100%;
  min-width: 0;
  height: 24px;
  border: 1px solid var(--color-border-input);
  border-radius: 5px;
  font: inherit;
  font-size: 11px;
  line-height: 16px;
  padding: 3px 8px;
}

.editor-variable-input__highlight {
  position: absolute;
  inset: 0;
  z-index: 0;
  overflow: hidden;
  background: var(--color-bg);
  color: var(--color-text);
  pointer-events: none;
  text-align: left;
  white-space: pre;
}

.editor-variable-input :deep(.editor-variable-input__control) {
  position: relative;
  z-index: 1;
  background: transparent !important;
  color: transparent !important;
  caret-color: var(--color-text);
  -webkit-text-fill-color: transparent;
  outline: none;
}

.editor-variable-input :deep(.editor-variable-input__control::placeholder) {
  color: var(--color-text-secondary);
  -webkit-text-fill-color: var(--color-text-secondary);
  opacity: 1;
}

.editor-variable-input :deep(.editor-variable-input__control:focus) {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.editor-variable-input__segment--known {
  color: var(--color-primary);
  font-weight: 600;
}

.editor-variable-input__segment--future {
  color: #b7791f;
  font-weight: 600;
}

.editor-variable-input__segment--missing {
  color: var(--color-danger);
  font-weight: 600;
}

.editor-variable-input__suggestions {
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

.editor-variable-input__suggestion {
  display: block;
  width: 100%;
  border: 0;
  background: transparent;
  color: var(--color-text);
  cursor: pointer;
  font: inherit;
  padding: 5px 10px;
  text-align: left;
}

.editor-variable-input__suggestion:hover,
.editor-variable-input__suggestion--active {
  background: var(--color-bg-secondary);
  color: var(--color-primary);
}
</style>
