<template>
  <InputText
    v-if="normalizedType === 'int'"
    class="input"
    :model-value="formatValue(value)"
    :placeholder="schema.isRequired ? 'Обязательное' : '—'"
    type="number"
    inputmode="numeric"
    :step="1"
    @click.stop
    @mousedown.stop
    @update:model-value="handleChange"
  />
  <Textarea
    v-else
    class="input input--textarea"
    rows="1"
    :model-value="formatValue(value)"
    :placeholder="schema.isRequired ? 'Обязательное' : '—'"
    @click.stop
    @mousedown.stop
    @update:model-value="handleChange"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import InputText from 'primevue/inputtext'
import Textarea from 'primevue/textarea'
import type { NodeParamItem } from '@/types/api'
import { normalizeNodeParamType } from '@/components/editor/editorTypes'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: unknown] }>()
const normalizedType = computed(() => normalizeNodeParamType(props.schema.type))

function formatValue(value: unknown): string {
  if (value === null || value === undefined) return ''
  return String(value)
}

function handleChange(value: string | undefined): void {
  const inputValue = value ?? ''
  if (normalizedType.value !== 'int') {
    emit('update', inputValue)
    return
  }

  const raw = inputValue.trim()
  if (!raw) {
    emit('update', undefined)
    return
  }

  const numberValue = Number(raw)
  emit('update', Number.isInteger(numberValue) ? numberValue : raw)
}
</script>

<style scoped>
.input {
  background: var(--color-bg);
  border: 0.5px solid var(--color-border-input);
  border-radius: 5px;
  padding: 5px 8px;
  font-size: 11px;
  font-family: inherit;
  color: var(--color-text);
  outline: none;
  width: 100%;
  box-sizing: border-box;
  transition: border-color 0.15s;
}
.input:focus {
  border-color: var(--node-color, var(--color-primary));
}
.input--textarea {
  min-height: 38px;
  field-sizing: content;
  line-height: 1.45;
  overflow: hidden;
  resize: none;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}
</style>
