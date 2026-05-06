<template>
  <div class="empty-state">
    <img
      v-if="icon"
      class="empty-state__icon"
      :class="{ 'empty-state__icon--dark': theme === 'dark' }"
      :src="icon"
      alt=""
    />
    <p class="empty-state__title">{{ title }}</p>
    <p v-if="description" class="empty-state__description">{{ description }}</p>
    <slot />
  </div>
</template>

<script setup lang="ts">
import { useTheme } from '@/composables/useTheme'

defineProps<{
  title: string
  description?: string
  icon?: string
}>()

const { theme } = useTheme()
</script>

<style scoped>
.empty-state {
  min-height: 280px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 12px;
  color: var(--color-text-secondary);
  text-align: center;
}

.empty-state__icon {
  width: 42px;
  height: 42px;
  opacity: 0.55;
}

.empty-state__icon--dark {
  filter: invert(1) brightness(1.35);
}

.empty-state__title {
  margin: 0;
  color: var(--color-text);
  font-size: 15px;
  font-weight: 500;
}

.empty-state__description {
  margin: 0;
  max-width: 360px;
  font-size: 13px;
  line-height: 1.5;
}
</style>
