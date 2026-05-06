<template>
  <AppModal
    :open="open"
    title="Завершить сессии"
    description="Выберите, какие сессии завершить. Завершённые сессии потребуют повторного входа."
    @update:open="$emit('update:open', $event)"
  >
    <div class="sessions-modal__actions">
      <Button
        :label="action === 'others' ? 'Завершаю...' : 'Все, кроме текущей'"
        severity="secondary"
        outlined
        :loading="action === 'others'"
        :disabled="action !== null && action !== 'others'"
        @click="$emit('logoutOthers')"
      />
      <Button
        :label="action === 'all' ? 'Завершаю...' : 'Все сессии, включая текущую'"
        severity="danger"
        :loading="action === 'all'"
        :disabled="action !== null && action !== 'all'"
        @click="$emit('logoutAll')"
      />
    </div>

    <template #footer>
      <Button label="Отмена" text severity="secondary" @click="$emit('update:open', false)" />
    </template>
  </AppModal>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import AppModal from '@/components/ui/AppModal.vue'

defineProps<{
  open: boolean
  action: 'others' | 'all' | null
}>()

defineEmits<{
  'update:open': [open: boolean]
  logoutOthers: []
  logoutAll: []
}>()
</script>

<style scoped>
.sessions-modal__actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

</style>
