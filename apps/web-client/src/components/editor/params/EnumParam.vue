<template>
  <Select
    class="select"
    :model-value="(value as string) ?? ''"
    :options="options"
    option-label="label"
    option-value="value"
    @click.stop
    @mousedown.stop
    @update:model-value="emit('update', $event)"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Select from 'primevue/select'
import type { NodeParamItem } from '@/types/api'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: string] }>()

const options = computed(() => [
  { label: '— выбрать —', value: '' },
  ...(props.schema.enumValues ?? []).map(opt => ({ label: opt, value: opt })),
])
</script>

<style scoped>
.select {
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
  cursor: pointer;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='10' height='6' viewBox='0 0 10 6'%3E%3Cpath d='M1 1l4 4 4-4' stroke='%23666' stroke-width='1.5' fill='none' stroke-linecap='round'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 8px center;
  padding-right: 24px;
  transition: border-color 0.15s;
}
.select:focus {
  border-color: var(--node-color, var(--color-primary));
}
</style>
