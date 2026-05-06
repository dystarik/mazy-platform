<template>
  <div class="dict">
    <div v-for="(val, key) in dictValue" :key="String(key)" class="dict__row">
      <InputText
        class="dict__input dict__key"
        :model-value="String(key)"
        placeholder="ключ"
        @click.stop
        @mousedown.stop
        @change.stop="renameKey(String(key), ($event.target as HTMLInputElement).value)"
      />
      <span class="dict__arrow">→</span>
      <InputText
        class="dict__input dict__val"
        :model-value="val"
        placeholder="значение"
        @click.stop
        @mousedown.stop
        @update:model-value="updateVal(String(key), $event ?? '')"
      />
      <Button class="dict__del" label="×" text severity="danger" @click.stop="removeKey(String(key))" />
    </div>
    <Button class="dict__add" label="+ Добавить" text severity="secondary" @click.stop="addKey" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import type { NodeParamItem } from '@/types/api'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: Record<string, string>] }>()

const dictValue = computed(() => (props.value as Record<string, string>) ?? {})

function addKey(): void {
  const newKey = `key_${Date.now()}`
  emit('update', { ...dictValue.value, [newKey]: '' })
}

function removeKey(key: string): void {
  const d = { ...dictValue.value }
  delete d[key]
  emit('update', d)
}

function renameKey(oldKey: string, newKey: string): void {
  if (!newKey.trim() || newKey === oldKey) return
  const d = { ...dictValue.value }
  const val = d[oldKey] ?? ''
  delete d[oldKey]
  d[newKey.trim()] = val
  emit('update', d)
}

function updateVal(key: string, val: string): void {
  emit('update', { ...dictValue.value, [key]: val })
}
</script>

<style scoped>
.dict {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.dict__row {
  display: flex;
  align-items: center;
  gap: 4px;
}
.dict__input {
  background: var(--color-bg);
  border: 0.5px solid var(--color-border-input);
  border-radius: 5px;
  padding: 5px 8px;
  font-size: 11px;
  font-family: inherit;
  color: var(--color-text);
  outline: none;
  box-sizing: border-box;
  min-width: 0;
  transition: border-color 0.15s;
}
.dict__input:focus {
  border-color: var(--node-color, var(--color-primary));
}
.dict__key { flex: 1; }
.dict__val { flex: 1; font-size: 10px; color: var(--color-text-secondary); }
.dict__arrow {
  font-size: 10px;
  color: var(--color-text-secondary);
  flex-shrink: 0;
}
.dict__del {
  flex-shrink: 0;
  background: none;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  font-size: 15px;
  line-height: 1;
  padding: 0 2px;
  transition: color 0.15s;
}
.dict__del:hover { color: var(--color-danger); }
.dict__add {
  background: none;
  border: 0.5px dashed var(--color-border-input);
  border-radius: 5px;
  color: var(--color-text-secondary);
  font-size: 11px;
  padding: 4px 8px;
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;
  text-align: center;
}
.dict__add:hover {
  color: var(--node-color, var(--color-primary));
  border-color: var(--node-color, var(--color-primary));
}
</style>
