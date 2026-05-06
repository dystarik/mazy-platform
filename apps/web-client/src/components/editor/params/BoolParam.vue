<template>
  <label class="toggle" @click.stop>
    <Checkbox v-model="checkedModel" binary @change.stop />
    <span>Включено</span>
  </label>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Checkbox from 'primevue/checkbox'
import type { NodeParamItem } from '@/types/api'

const props = defineProps<{ schema: NodeParamItem; value: unknown }>()
const emit = defineEmits<{ update: [value: boolean] }>()

const checkedModel = computed({
  get: () => Boolean(props.value),
  set: (value: boolean) => emit('update', value),
})
</script>

<style scoped>
.toggle {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 11px;
  color: var(--color-text);
  cursor: pointer;
}
.toggle input {
  accent-color: var(--node-color, var(--color-primary));
  cursor: pointer;
}
</style>
