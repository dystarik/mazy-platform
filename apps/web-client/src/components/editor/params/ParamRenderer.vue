<template>
  <StringParam
    v-if="normalizedType === 'string' || normalizedType === 'int'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <BoolParam
    v-else-if="normalizedType === 'bool'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <EnumParam
    v-else-if="normalizedType === 'enum'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <StringListParam
    v-else-if="normalizedType === 'stringlist'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <StringDictionaryParam
    v-else-if="normalizedType === 'stringdictionary'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <ObjectParam
    v-else-if="normalizedType === 'object'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <ObjectListParam
    v-else-if="normalizedType === 'objectlist'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <ObjectMatrixParam
    v-else-if="normalizedType === 'objectmatrix'"
    :schema="schema"
    :value="value"
    @update="emit('update', $event)"
  />
  <span v-else class="unknown">{{ schema.type }}</span>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { NodeParamItem } from '@/types/api'
import { normalizeNodeParamType } from '@/components/editor/editorTypes'
import StringParam from './StringParam.vue'
import BoolParam from './BoolParam.vue'
import EnumParam from './EnumParam.vue'
import StringListParam from './StringListParam.vue'
import StringDictionaryParam from './StringDictionaryParam.vue'
import ObjectParam from './ObjectParam.vue'
import ObjectListParam from './ObjectListParam.vue'
import ObjectMatrixParam from './ObjectMatrixParam.vue'

const props = defineProps<{
  schema: NodeParamItem
  value: unknown
}>()

const emit = defineEmits<{ update: [value: unknown] }>()
const normalizedType = computed(() => normalizeNodeParamType(props.schema.type))
</script>

<style scoped>
.unknown {
  font-size: 10px;
  color: var(--color-text-secondary);
  opacity: 0.5;
  font-family: var(--font-mono, monospace);
}
</style>
