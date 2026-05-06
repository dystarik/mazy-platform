<template>
  <Button
    class="icon-button"
    :class="{ 'icon-button--danger': danger }"
    :type="type"
    :title="label"
    :aria-label="label"
    :disabled="disabled"
    text
    severity="secondary"
    @click="$emit('click', $event)"
  >
    <img
      class="icon-button__img"
      :class="{ 'icon-button__img--dark': theme === 'dark' }"
      :src="icon"
      alt=""
    />
  </Button>
</template>

<script setup lang="ts">
import Button from 'primevue/button'
import { useTheme } from '@/composables/useTheme'

withDefaults(defineProps<{
  icon: string
  label: string
  danger?: boolean
  disabled?: boolean
  type?: 'button' | 'submit' | 'reset'
}>(), {
  danger: false,
  disabled: false,
  type: 'button',
})

defineEmits<{
  click: [event: MouseEvent]
}>()

const { theme } = useTheme()
</script>

<style scoped>
.icon-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  min-width: 32px;
  border: 0.5px solid var(--color-border);
  border-radius: 8px;
  background: transparent;
  color: var(--color-text-secondary);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s, opacity 0.15s;
  flex-shrink: 0;
  padding: 0;
}

.icon-button:hover:not(:disabled) {
  background: var(--color-bg-secondary);
  border-color: color-mix(in srgb, var(--color-primary) 20%, var(--color-border));
}

.icon-button--danger:hover:not(:disabled) {
  border-color: color-mix(in srgb, var(--color-danger) 40%, var(--color-border));
  background: color-mix(in srgb, var(--color-danger) 7%, transparent);
}

.icon-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.icon-button__img {
  width: 14px;
  height: 14px;
  display: block;
}

.icon-button__img--dark {
  filter: invert(1) brightness(1.35);
}
</style>
