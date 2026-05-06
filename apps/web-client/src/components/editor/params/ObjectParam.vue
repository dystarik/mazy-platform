<template>
  <div class="obj">
    <div v-for="field in schema.fields ?? []" :key="field.key" class="obj__field">
      <label class="obj__label">
        {{ formatKey(field.key ?? '') }}
        <span v-if="field.isRequired" class="obj__req">*</span>
      </label>
      <ParamRenderer
        :schema="field"
        :value="objValue[field.key ?? '']"
        @update="(val) => updateField(field.key ?? '', val)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { NodeParamItem } from '@/types/api'
import ParamRenderer from './ParamRenderer.vue'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: Record<string, unknown>] }>()

const objValue = computed(() => (props.value as Record<string, unknown>) ?? {})

function formatKey(key: string): string {
  const labels: Record<string, string> = {
    label: 'Текст кнопки',
  }
  if (labels[key]) return labels[key]

  return key.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase())
}

function updateField(fieldKey: string, val: unknown): void {
  emit('update', { ...objValue.value, [fieldKey]: val })
}
</script>

<style scoped>
.obj {
  border: 0.5px solid var(--color-border);
  border-radius: 6px;
  padding: 6px 8px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  background: color-mix(in srgb, var(--color-bg) 90%, var(--node-color, transparent));
}
.obj__field {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.obj__label {
  font-size: 9px;
  font-weight: 600;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.obj__req {
  color: var(--color-danger);
  margin-left: 2px;
}
</style>
