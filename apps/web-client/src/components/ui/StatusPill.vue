<template>
  <Tag class="status-pill" :class="classes" :severity="severity">
    <slot>{{ label }}</slot>
  </Tag>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Tag from 'primevue/tag'

const props = withDefaults(defineProps<{
  label?: string
  variant?: 'active' | 'success' | 'inactive' | 'muted' | 'danger'
  size?: 'default' | 'small'
}>(), {
  label: '',
  variant: 'active',
  size: 'default',
})

const classes = computed(() => ({
  'status-pill--inactive': props.variant === 'inactive',
  'status-pill--muted': props.variant === 'muted',
  'status-pill--small': props.size === 'small',
}))

const severity = computed(() => {
  switch (props.variant) {
    case 'success': return 'success'
    case 'danger': return 'danger'
    case 'inactive':
    case 'muted': return 'secondary'
    default: return 'info'
  }
})
</script>

<style scoped>
.status-pill {
  min-height: 24px;
  min-width: 76px;
  font-size: 12px;
  line-height: 1.2;
  justify-content: center;
  white-space: nowrap;
}

.status-pill--small {
  min-height: 21px;
  font-size: 11px;
}

.status-pill--muted {
  opacity: 0.78;
}

.status-pill--inactive {
  border: 1px solid color-mix(in srgb, #dc2626 42%, var(--color-border));
  background: color-mix(in srgb, #dc2626 10%, var(--color-bg-card));
  color: #b91c1c;
}
</style>
