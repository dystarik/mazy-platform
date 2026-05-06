import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primevue/themes/aura'
import { definePreset } from '@primeuix/themes'
import App from './App.vue'
import router from './router'
import { useAuthStore } from '@/stores/auth.store'
import '@/assets/main.css'

const MazyPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#e6f7ff',
      100: '#c9eefb',
      200: '#95ddf2',
      300: '#5ec8e6',
      400: '#0096c7',
      500: '#0077b6',
      600: '#00669d',
      700: '#023e8a',
      800: '#02326f',
      900: '#022653',
      950: '#01162f',
    },
    colorScheme: {
      light: {
        primary: {
          color: '#0077b6',
          contrastColor: '#ffffff',
          hoverColor: '#0096c7',
          activeColor: '#00669d',
        },
        highlight: {
          background: 'color-mix(in srgb, #0077b6 12%, transparent)',
          focusBackground: 'color-mix(in srgb, #0077b6 18%, transparent)',
          color: '#023e8a',
          focusColor: '#023e8a',
        },
      },
      dark: {
        primary: {
          color: '#023e8a',
          contrastColor: '#ffffff',
          hoverColor: '#0077b6',
          activeColor: '#0096c7',
        },
        highlight: {
          background: 'color-mix(in srgb, #0077b6 28%, transparent)',
          focusBackground: 'color-mix(in srgb, #0077b6 36%, transparent)',
          color: '#ffffff',
          focusColor: '#ffffff',
        },
      },
    },
  },
})

// Применяем тему до рендера — нет мерцания
const savedTheme = localStorage.getItem('theme')
const systemDark = window.matchMedia('(prefers-color-scheme: dark)').matches
if (savedTheme === 'dark' || (!savedTheme && systemDark)) {
  document.documentElement.classList.add('dark')
} else {
  document.documentElement.classList.remove('dark')
}

const app = createApp(App)

app.use(createPinia())

// Инициализируем auth ДО регистрации router, чтобы guard видел актуальный токен
const authStore = useAuthStore()
await authStore.init()

app.use(router)
app.use(PrimeVue, {
  theme: {
    preset: MazyPreset,
    options: {
      darkModeSelector: '.dark',
    },
  },
})

app.mount('#app')
