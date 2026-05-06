<template>
  <Textarea
    class="textarea"
    rows="2"
    :model-value="listValue.join('\n')"
    :placeholder="schema.isRequired ? 'Обязательное' : '—'"
    @click.stop
    @mousedown.stop
    @update:model-value="emit('update', ($event ?? '').split('\n').filter(Boolean))"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Textarea from 'primevue/textarea'
import type { NodeParamItem } from '@/types/api'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: string[]] }>()

const listValue = computed(() => (props.value as string[]) ?? [])
</script>

<style scoped>
.textarea {
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
  resize: vertical;
  transition: border-color 0.15s;
}
.textarea:focus {
  border-color: var(--node-color, var(--color-primary));
}
</style>
