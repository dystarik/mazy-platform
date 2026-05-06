<template>
  <div class="objlist">
    <div v-for="(item, idx) in listValue" :key="idx" class="objlist__item">
      <div class="objlist__item-header">
        <span class="objlist__item-num"># {{ idx + 1 }}</span>
        <Button class="objlist__del" label="×" text severity="danger" @click.stop="removeItem(idx)" />
      </div>
      <ObjectParam
        :schema="schema"
        :value="item"
        @update="(val) => updateItem(idx, val)"
      />
    </div>
    <Button class="objlist__add" label="+ Добавить" text severity="secondary" @click.stop="addItem" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import type { NodeParamItem } from '@/types/api'
import ObjectParam from './ObjectParam.vue'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: Record<string, unknown>[]] }>()

const listValue = computed(() => (props.value as Record<string, unknown>[]) ?? [])

function addItem(): void {
  emit('update', [...listValue.value, {}])
}

function removeItem(idx: number): void {
  const list = [...listValue.value]
  list.splice(idx, 1)
  emit('update', list)
}

function updateItem(idx: number, val: Record<string, unknown>): void {
  const list = [...listValue.value]
  list[idx] = val
  emit('update', list)
}
</script>

<style scoped>
.objlist {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.objlist__item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.objlist__item-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.objlist__item-num {
  font-size: 9px;
  font-weight: 600;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.objlist__del {
  flex-shrink: 0;
  background: none;
  border: none;
  color: var(--color-text-secondary);
  cursor: pointer;
  font-size: 14px;
  line-height: 1;
  padding: 0 2px;
  transition: color 0.15s;
}
.objlist__del:hover { color: var(--color-danger); }
.objlist__add {
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
.objlist__add:hover {
  color: var(--node-color, var(--color-primary));
  border-color: var(--node-color, var(--color-primary));
}
</style>
