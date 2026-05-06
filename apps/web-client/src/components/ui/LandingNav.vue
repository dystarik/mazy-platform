<template>
  <header class="landing__nav">
    <RouterLink to="/">
      <AppLogo />
    </RouterLink>

    <nav class="landing__nav-links">
      <RouterLink to="/app/projects" class="nav-link">Проекты</RouterLink>
      <RouterLink to="/app/bots" class="nav-link">Боты</RouterLink>
    </nav>

    <div class="landing__nav-actions">
      <Button
        class="theme-toggle mazy-icon-button"
        type="button"
        text
        severity="secondary"
        :aria-label="theme === 'dark' ? 'Включить светлую тему' : 'Включить тёмную тему'"
        @click="toggleTheme"
      >
        <img
          class="theme-toggle__icon"
          :class="{ 'theme-toggle__icon--sun': theme === 'dark' }"
          :src="theme === 'dark' ? sunIcon : moonIcon"
          :alt="theme === 'dark' ? 'Светлая тема' : 'Тёмная тема'"
        />
      </Button>

      <template v-if="isAuthenticated">
        <RouterLink v-slot="{ navigate }" :to="{ name: 'settings' }" custom>
          <Button label="Настройки" severity="secondary" outlined @click="navigate" />
        </RouterLink>
      </template>
      <template v-else>
        <RouterLink v-slot="{ navigate }" to="/auth/login" custom>
          <Button label="Войти" class="landing__login-button" @click="navigate" />
        </RouterLink>
      </template>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import Button from 'primevue/button'
import AppLogo from '@/components/ui/AppLogo.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useTheme } from '@/composables/useTheme'
import moonIcon from '@/assets/icons/moon.svg'
import sunIcon from '@/assets/icons/sun.svg'

const authStore = useAuthStore()
const isAuthenticated = computed(() => authStore.isAuthenticated)
const { theme, setTheme } = useTheme()

function toggleTheme(): void {
  setTheme(theme.value === 'dark' ? 'light' : 'dark')
}
</script>

<style scoped>
.landing__nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 20px;
  padding: 18px 48px;
  border-bottom: 0.5px solid var(--color-border);
  background: color-mix(in srgb, var(--color-bg-card) 88%, transparent);
  backdrop-filter: blur(14px);
}

.landing__nav-links {
  display: flex;
  align-items: center;
  gap: 4px;
}

.nav-link {
  padding: 7px 14px;
  font-size: 14px;
  color: var(--color-text-secondary);
  text-decoration: none;
  border-radius: 8px;
  transition: color 0.15s, background 0.15s;
}

.nav-link:hover {
  color: var(--color-text);
  background: var(--color-bg-secondary);
}

.landing__nav-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.landing__login-button {
  width: auto;
  min-width: 76px;
  height: 38px;
  box-sizing: border-box;
}

.theme-toggle {
  --prime-icon-button-size: 38px;
}

.theme-toggle:active {
  transform: translateY(1px);
}

.theme-toggle__icon {
  width: 16px;
  height: 16px;
  display: block;
  opacity: 0.92;
}

.theme-toggle__icon--sun {
  filter: invert(1) brightness(1.35);
}

@media (max-width: 760px) {
  .landing__nav {
    padding: 14px 18px;
    gap: 12px;
  }

  .landing__nav-links {
    display: none;
  }

  .landing__nav-actions {
    gap: 8px;
  }

  .landing__login-button,
  .landing__nav-actions :deep(.p-button) {
    min-width: 0;
    height: 36px;
  }

  .theme-toggle {
    --prime-icon-button-size: 36px;
  }
}

@media (max-width: 430px) {
  .landing__nav :deep(.logo__text) {
    display: none;
  }
}
</style>
