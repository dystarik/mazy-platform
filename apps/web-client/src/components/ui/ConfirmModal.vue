<template>
  <AppModal
    :open="open"
    :title="title"
    size="sm"
    @update:open="$emit('update:open', $event)"
  >
    <p class="confirm-modal__message">{{ message }}</p>
    <FormErrorList :messages="errors" />

    <template #footer>
      <Button
        label="Отмена"
        severity="secondary"
        outlined
        :disabled="loading"
        @click="$emit('update:open', false)"
      />
      <Button
        :label="loading ? loadingLabel : confirmLabel"
        :severity="danger ? 'danger' : undefined"
        :loading="loading"
        @click="$emit('confirm')"
      />
    </template>
  </AppModal>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import AppModal from '@/components/ui/AppModal.vue'
import FormErrorList from '@/components/ui/FormErrorList.vue'

withDefaults(defineProps<{
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  loadingLabel?: string
  danger?: boolean
  loading?: boolean
  errors?: string[]
}>(), {
  confirmLabel: 'Подтвердить',
  loadingLabel: 'Выполняю...',
  danger: false,
  loading: false,
  errors: () => [],
})

defineEmits<{
  'update:open': [value: boolean]
  confirm: []
}>()
</script>

<style scoped>
.confirm-modal__message {
  margin: 0;
  color: var(--color-text-secondary);
  font-size: 14px;
  line-height: 1.5;
}

</style>
