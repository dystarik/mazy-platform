<template>
  <div v-if="messages.length" class="form-error-list">
    <Message
      v-for="(message, index) in messages"
      :key="index"
      severity="error"
      size="small"
      variant="simple"
    >
      {{ message }}
    </Message>
  </div>
</template>

<script setup lang="ts">
import Message from 'primevue/message'

defineProps<{
  messages: string[]
}>()
</script>

<style scoped>
.form-error-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
</style>
