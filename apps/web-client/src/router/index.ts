import { createRouter, createWebHistory } from 'vue-router'
import { authGuard, guestGuard, mfaGuard } from './guards'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // Гостевые маршруты
    {
      path: '/auth',
      meta: { guard: 'guest' },
      children: [
        {
          path: 'login',
          name: 'login',
          component: () => import('@/views/auth/LoginView.vue'),
        },
        {
          path: 'register',
          name: 'register',
          component: () => import('@/views/auth/RegisterView.vue'),
        },
        {
          path: 'register/confirm',
          name: 'register-confirm',
          component: () => import('@/views/auth/RegisterConfirmView.vue'),
        },
        {
          path: 'mfa',
          name: 'mfa-verify',
          component: () => import('@/views/auth/MfaVerifyView.vue'),
          beforeEnter: mfaGuard,
        },
        {
          path: 'password/reset',
          name: 'password-reset',
          component: () => import('@/views/auth/ResetPasswordView.vue'),
        },
        {
          path: 'password/reset/confirm',
          name: 'password-reset-confirm',
          component: () => import('@/views/auth/ResetPasswordConfirmView.vue'),
        },
      ],
    },

    // Callback от внешних провайдеров — без guard, работает и для гостей и для авторизованных
    {
      path: '/auth/callback/yandex',
      name: 'yandex-callback',
      component: () => import('@/views/auth/YandexCallbackView.vue'),
    },

    // Лендинг — публичный, без guard
    {
      path: '/',
      name: 'landing',
      component: () => import('@/views/LandingView.vue'),
    },

    // Основное приложение
    {
      path: '/app',
      meta: { guard: 'auth' },
      children: [
        {
          path: '',
          redirect: { name: 'projects' },
        },
        {
          path: 'projects',
          name: 'projects',
          component: () => import('@/views/projects/ProjectsListView.vue'),
        },
        {
          path: 'projects/:id',
          name: 'project',
          component: () => import('@/views/projects/ProjectView.vue'),
        },
        {
          path: 'projects/:id/user-data',
          name: 'project-user-data',
          component: () => import('@/views/projects/ProjectUserDataView.vue'),
        },
        {
          path: 'projects/:id/scenarios/:sid',
          name: 'scenario-editor',
          component: () => import('@/views/scenarios/ScenarioEditorView.vue'),
        },
        {
          path: 'bots',
          name: 'bots',
          component: () => import('@/views/bots/BotsView.vue'),
        },
      ],
    },

    // Настройки
    {
      path: '/settings',
      name: 'settings',
      meta: { guard: 'auth' },
      component: () => import('@/views/settings/SettingsView.vue'),
    },

    // Редирект с корня
    {
      path: '/:pathMatch(.*)*',
      redirect: { name: 'landing' },
    },
  ],
})

// Глобальный guard
router.beforeEach((to) => {
  if (to.meta.guard === 'auth') return authGuard()
  if (to.meta.guard === 'guest') return guestGuard()
})

export default router
