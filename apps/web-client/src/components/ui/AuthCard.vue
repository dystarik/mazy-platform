<template>
  <div class="auth-shell">
    <section class="auth-card">
      <AppLogo class="auth-card__logo" />

      <header v-if="title || $slots.subtitle" class="auth-card__header">
        <h1 v-if="title">{{ title }}</h1>
        <p v-if="$slots.subtitle">
          <slot name="subtitle" />
        </p>
      </header>

      <slot />

      <slot name="footer" />
    </section>
  </div>
</template>

<script setup lang="ts">
import AppLogo from '@/components/ui/AppLogo.vue'

defineProps<{
  title?: string
}>()
</script>

<style scoped>
.auth-shell {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.auth-card {
  background: var(--color-bg-card);
  border: 0.5px solid var(--color-border);
  border-radius: 16px;
  padding: 32px;
  width: 100%;
  max-width: 380px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.auth-card__logo {
  align-self: flex-start;
}

.auth-card__header h1 {
  font-size: 22px;
  font-weight: 500;
  color: var(--color-text);
  margin: 0 0 4px;
}

.auth-card__header p {
  font-size: 13px;
  color: var(--color-text-secondary);
  margin: 0;
}

.auth-card__header :deep(a) {
  color: var(--color-primary);
  text-decoration: none;
}

.auth-card :deep(.auth-card__form) {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.auth-card :deep(.p-inputtext),
.auth-card :deep(.p-password),
.auth-card :deep(.p-password-input),
.auth-card :deep(.p-button) {
  width: 100%;
}
</style>
