<template>
  <Dialog
    v-model:visible="visibleModel"
    modal
    :header="title"
    :closable="showClose"
    :dismissable-mask="closeOnOverlay"
    :style="{ width: modalWidth }"
    class="app-dialog"
  >
    <template v-if="$slots.header" #header>
      <slot name="header" />
    </template>

    <p v-if="description" class="app-dialog__description">{{ description }}</p>
    <div class="app-dialog__body">
      <slot />
    </div>

    <template v-if="$slots.footer" #footer>
      <slot name="footer" />
    </template>
  </Dialog>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Dialog from 'primevue/dialog'

const props = withDefaults(defineProps<{
  open: boolean
  title?: string
  description?: string
  size?: 'sm' | 'md' | 'lg'
  closeOnOverlay?: boolean
  showClose?: boolean
}>(), {
  size: 'md',
  closeOnOverlay: true,
  showClose: true,
})

const emit = defineEmits<{
  'update:open': [value: boolean]
}>()

const visibleModel = computed({
  get: () => props.open,
  set: (value: boolean) => emit('update:open', value),
})

const modalWidth = computed(() => {
  if (props.size === 'sm') return 'min(100%, 360px)'
  if (props.size === 'lg') return 'min(100%, 620px)'
  return 'min(100%, 440px)'
})
</script>

<style scoped>
.app-dialog__description {
  margin: 6px 0 0;
  color: var(--color-text-secondary);
  font-size: 13px;
  line-height: 1.5;
}

.app-dialog__body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

:deep(.p-dialog-footer) .p-button {
  width: auto;
  min-width: 120px;
}
</style>
