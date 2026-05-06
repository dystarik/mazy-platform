<template>
  <div class="matrix">
    <div v-for="(row, rowIndex) in matrixValue" :key="rowIndex" class="matrix__row">
      <div class="matrix__row-header">
        <span class="matrix__row-title">Ряд {{ rowIndex + 1 }}</span>
        <Button class="matrix__row-del" label="×" text severity="danger" @click.stop="removeRow(rowIndex)" />
      </div>

      <div class="matrix__cells">
        <div v-for="(item, itemIndex) in row" :key="itemIndex" class="matrix__cell">
          <div class="matrix__cell-header">
            <span class="matrix__cell-title">Кнопка {{ itemIndex + 1 }}</span>
            <Button class="matrix__cell-del" label="×" text severity="danger" @click.stop="removeItem(rowIndex, itemIndex)" />
          </div>
          <ObjectParam
            :schema="schema"
            :value="item"
            @update="(val) => updateItem(rowIndex, itemIndex, val)"
          />
        </div>
      </div>

      <Button class="matrix__add-item" label="+ Добавить элемент" text severity="secondary" @click.stop="addItem(rowIndex)" />
    </div>

    <Button class="matrix__add-row" label="+ Добавить ряд" text severity="secondary" @click.stop="addRow" />
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import type { NodeParamItem } from '@/types/api'
import ObjectParam from './ObjectParam.vue'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: Record<string, unknown>[][]] }>()

const matrixValue = computed(() => (props.value as Record<string, unknown>[][]) ?? [])

function addRow(): void {
  emit('update', [...matrixValue.value, []])
}

function removeRow(rowIndex: number): void {
  const next = [...matrixValue.value]
  next.splice(rowIndex, 1)
  emit('update', next)
}

function addItem(rowIndex: number): void {
  const next = matrixValue.value.map(row => [...row])
  next[rowIndex] = [...(next[rowIndex] ?? []), {}]
  emit('update', next)
}

function removeItem(rowIndex: number, itemIndex: number): void {
  const next = matrixValue.value.map(row => [...row])
  next[rowIndex]?.splice(itemIndex, 1)
  emit('update', next)
}

function updateItem(rowIndex: number, itemIndex: number, value: Record<string, unknown>): void {
  const next = matrixValue.value.map(row => [...row])
  const row = [...(next[rowIndex] ?? [])]
  row[itemIndex] = value
  next[rowIndex] = row
  emit('update', next)
}
</script>

<style scoped>
.matrix {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.matrix__row {
  border: 0.5px solid var(--color-border);
  border-radius: 6px;
  padding: 8px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.matrix__row-header,
.matrix__cell-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.matrix__row-title,
.matrix__cell-title {
  font-size: 9px;
  font-weight: 600;
  color: var(--color-text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.matrix__cells {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.matrix__cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.matrix__row-del,
.matrix__cell-del {
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
.matrix__row-del:hover,
.matrix__cell-del:hover {
  color: var(--color-danger);
}
.matrix__add-item,
.matrix__add-row {
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
.matrix__add-item:hover,
.matrix__add-row:hover {
  color: var(--node-color, var(--color-primary));
  border-color: var(--node-color, var(--color-primary));
}
</style>
