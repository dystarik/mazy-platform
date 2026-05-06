<template>
  <aside class="settings-nav">
    <p class="settings-nav__title">Настройки</p>
    <nav>
      <button
        v-for="item in items"
        :key="item.id"
        class="settings-nav__item"
        :class="{ 'settings-nav__item--active': activeSection === item.id }"
        type="button"
        @click="$emit('select', item.id)"
      >
        <img
          class="settings-nav__icon"
          :class="{ 'settings-nav__icon--dark': theme === 'dark' }"
          :src="item.icon"
          :alt="item.label"
        />
        {{ item.label }}
      </button>
    </nav>
  </aside>
</template>

<script setup lang="ts">
import { useTheme } from '@/composables/useTheme'

export interface SettingsNavItem {
  id: string
  label: string
  icon: string
}

defineProps<{
  items: SettingsNavItem[]
  activeSection: string
}>()

defineEmits<{
  select: [id: string]
}>()

const { theme } = useTheme()
</script>

<style scoped>
.settings-nav {
  position: sticky;
  top: 0;
  align-self: stretch;
  width: 200px;
  flex-shrink: 0;
  padding: 32px 12px;
  border-right: 0.5px solid var(--color-border);
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.settings-nav__title {
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--color-text-secondary);
  margin: 0 0 12px 8px;
}

.settings-nav__item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 8px 10px;
  background: none;
  border: none;
  border-radius: 8px;
  font-size: 13px;
  font-family: 'Inter', sans-serif;
  color: var(--color-text-secondary);
  cursor: pointer;
  text-align: left;
  transition: background 0.15s, color 0.15s;
  line-height: 1.3;
}

.settings-nav__item:hover,
.settings-nav__item--active {
  background: var(--color-bg-secondary);
  color: var(--color-text);
}

.settings-nav__icon {
  width: 15px;
  height: 15px;
  opacity: 0.75;
}

.settings-nav__icon--dark {
  filter: invert(1) brightness(1.25);
  opacity: 0.95;
}

@media (max-width: 860px) {
  .settings-nav {
    position: static;
    width: 100%;
    padding: 18px 20px 0;
    border-right: none;
    border-bottom: 0.5px solid var(--color-border);
  }

  .settings-nav nav {
    display: flex;
    gap: 6px;
    overflow-x: auto;
    padding-bottom: 12px;
  }

  .settings-nav__title {
    margin-left: 0;
  }

  .settings-nav__item {
    width: auto;
    flex-shrink: 0;
  }
}
</style>
