<template>
  <div class="settings">
    <SettingsNav
      :items="navItems"
      :active-section="activeSection"
      @select="scrollTo"
    />

    <div ref="contentRef" class="settings__content">
      <section id="connections" class="settings__section">
        <h2 class="settings__section-title">Подключения</h2>

        <ProviderConnectionCard
          :linked="isYandexLinked"
          :loading="providerLoading"
          :error="providerError"
          @link="handleLinkYandex"
          @unlink="handleUnlinkYandex"
        />
      </section>

      <section id="security" class="settings__section">
        <h2 class="settings__section-title">Безопасность</h2>

        <PasswordSettingsCard
          v-model:current-password="currentPassword"
          v-model:new-password="newPassword"
          mode="change"
          :errors="passwordErrors"
          :loading="passwordLoading"
          :success="passwordSuccess"
          @submit="handleChangePassword"
        />

        <PasswordSettingsCard
          v-model:new-password="setPasswordValue"
          mode="set"
          :errors="setPasswordErrors"
          :loading="setPasswordLoading"
          :success="setPasswordSuccess"
          @submit="handleSetPassword"
        />

        <MfaSettingsSection
          :has-totp="hasTotp"
          :has-email="hasEmail"
          :has-any-factor="factorsStore.factors.length > 0"
          :loading="mfaLoading"
          @add-totp="startAddTotp"
          @add-email="mfaPageState = 'add-email-form'"
          @remove-factor="startRemoveFactor"
          @regenerate="startRegenerate"
        />
      </section>

      <section id="sessions" class="settings__section">
        <h2 class="settings__section-title">Сессии</h2>

        <SessionsCard
          :sessions="sortedSessions"
          :loading="sessionsStore.isLoading"
          :error="sessionsError"
          :theme="theme"
          @open-logout-all="showSessionsModal = true"
          @logout-session="handleLogoutSession"
          @logout-current="handleLogoutCurrent"
        />
      </section>
    </div>

    <MfaFlowModal
      v-model:code="mfaCode"
      v-model:email-for-factor="emailForFactor"
      :page-state="mfaPageState"
      :errors="mfaErrors"
      :loading="mfaLoading"
      :used-factors="usedFactors"
      :remaining-factors="remainingFactors"
      :qr-data-url="qrDataUrl"
      :backup-codes="factorsStore.backupCodes"
      :copied="copied"
      :step="mfaStore.step"
      :selected-factor="mfaStore.selectedFactor"
      :theme="theme"
      :factor-label="factorLabel"
      :factor-hint="factorHint"
      @add-email="startAddEmail"
      @verify-totp="confirmTotp"
      @verify-email="confirmEmail"
      @select-factor="handleMfaSelectFactor"
      @verify-challenge="handleMfaVerify"
      @resend-email="mfaStore.sendEmailCode"
      @copy-backup-codes="copyBackupCodes"
      @cancel="cancelMfaFlow"
    />

    <SessionsModal
      v-model:open="showSessionsModal"
      :action="sessionsAction"
      @logout-others="handleLogoutOthers"
      @logout-all="handleLogoutAll"
    />
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { linkedProvidersApi, passwordApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'
import { useSettingsMfaFlow } from '@/composables/useSettingsMfaFlow'
import { useSettingsScrollSpy } from '@/composables/useSettingsScrollSpy'
import { useTheme } from '@/composables/useTheme'
import MfaFlowModal from '@/components/settings/MfaFlowModal.vue'
import MfaSettingsSection from '@/components/settings/MfaSettingsSection.vue'
import PasswordSettingsCard from '@/components/settings/PasswordSettingsCard.vue'
import ProviderConnectionCard from '@/components/settings/ProviderConnectionCard.vue'
import SessionsCard from '@/components/settings/SessionsCard.vue'
import SessionsModal from '@/components/settings/SessionsModal.vue'
import SettingsNav from '@/components/settings/SettingsNav.vue'
import { useAuthStore } from '@/stores/auth.store'
import { useFactorsStore } from '@/stores/factors.store'
import { useMfaStore } from '@/stores/mfa.store'
import { useSessionsStore } from '@/stores/sessions.store'
import type { ExternalProvider } from '@/types/api'
import link2Icon from '@/assets/icons/link-2.svg'
import monitorIcon from '@/assets/icons/monitor.svg'
import shieldIcon from '@/assets/icons/shield.svg'

const router = useRouter()
const authStore = useAuthStore()
const sessionsStore = useSessionsStore()
const factorsStore = useFactorsStore()
const mfaStore = useMfaStore()
const { theme } = useTheme()

const contentRef = ref<HTMLElement | null>(null)

const navItems = [
  {
    id: 'connections',
    label: 'Подключения',
    icon: link2Icon,
  },
  {
    id: 'security',
    label: 'Безопасность',
    icon: shieldIcon,
  },
  {
    id: 'sessions',
    label: 'Сессии',
    icon: monitorIcon,
  },
]

const {
  activeSection,
  scrollTo,
  onScroll,
  mount: mountScrollSpy,
  unmount: unmountScrollSpy,
} = useSettingsScrollSpy(contentRef, navItems)

const linkedProviders = ref<ExternalProvider[]>([])
const providerLoading = ref(false)
const providerError = ref<string | null>(null)

const isYandexLinked = computed(() =>
  linkedProviders.value.includes('EXTERNAL_PROVIDER_YANDEX')
)

async function loadProviders(): Promise<void> {
  try {
    const response = await linkedProvidersApi.getLinkedProviders()
    linkedProviders.value = response.providers ?? []
  } catch {
    // ignore
  }
}

function handleLinkYandex(): void {
  const clientId = import.meta.env.VITE_YANDEX_CLIENT_ID
  const redirectUri = `${window.location.origin}/auth/callback/yandex`
  const url = new URL('https://oauth.yandex.ru/authorize')
  url.searchParams.set('response_type', 'code')
  url.searchParams.set('client_id', clientId)
  url.searchParams.set('redirect_uri', redirectUri)
  url.searchParams.set('state', 'link')
  window.location.href = url.toString()
}

async function handleUnlinkYandex(): Promise<void> {
  providerLoading.value = true
  providerError.value = null
  try {
    await linkedProvidersApi.unlinkProvider('EXTERNAL_PROVIDER_YANDEX')
    linkedProviders.value = linkedProviders.value.filter((p) => p !== 'EXTERNAL_PROVIDER_YANDEX')
  } catch (e) {
    providerError.value = parseApiError(e)[0] ?? null
  } finally {
    providerLoading.value = false
  }
}

const currentPassword = ref('')
const newPassword = ref('')
const passwordErrors = ref<string[]>([])
const passwordLoading = ref(false)
const passwordSuccess = ref(false)
const setPasswordValue = ref('')
const setPasswordErrors = ref<string[]>([])
const setPasswordLoading = ref(false)
const setPasswordSuccess = ref(false)

async function handleChangePassword(): Promise<void> {
  passwordErrors.value = []
  passwordSuccess.value = false
  if (!currentPassword.value || !newPassword.value) {
    passwordErrors.value = ['Заполните текущий и новый пароль']
    return
  }
  passwordLoading.value = true
  try {
    const response = await passwordApi.changePassword({
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
    })
    if (response.success) {
      passwordSuccess.value = true
      currentPassword.value = ''
      newPassword.value = ''
    } else if (response.challenge) {
      startPasswordChallenge('change-password', response.challenge)
    }
  } catch (e) {
    passwordErrors.value = parseApiError(e)
  } finally {
    passwordLoading.value = false
  }
}

async function handleSetPassword(): Promise<void> {
  setPasswordErrors.value = []
  setPasswordSuccess.value = false
  if (!setPasswordValue.value) {
    setPasswordErrors.value = ['Введите новый пароль']
    return
  }
  setPasswordLoading.value = true
  try {
    const response = await passwordApi.setPassword({
      newPassword: setPasswordValue.value,
    })
    if (response.success) {
      setPasswordSuccess.value = true
      setPasswordValue.value = ''
    } else if (response.challenge) {
      startPasswordChallenge('set-password', response.challenge)
    }
  } catch (e) {
    setPasswordErrors.value = parseApiError(e)
  } finally {
    setPasswordLoading.value = false
  }
}

const {
  mfaPageState,
  mfaCode,
  mfaErrors,
  mfaLoading,
  usedFactors,
  qrDataUrl,
  emailForFactor,
  copied,
  hasTotp,
  hasEmail,
  remainingFactors,
  startPasswordChallenge,
  factorLabel,
  factorHint,
  copyBackupCodes,
  startAddTotp,
  confirmTotp,
  startAddEmail,
  confirmEmail,
  startRemoveFactor,
  startRegenerate,
  handleMfaSelectFactor,
  handleMfaVerify,
  cancelMfaFlow,
} = useSettingsMfaFlow({
  currentPassword,
  newPassword,
  passwordSuccess,
  setPasswordValue,
  setPasswordSuccess,
})

const sessionsError = ref<string | null>(null)
const showSessionsModal = ref(false)
const sessionsAction = ref<'others' | 'all' | null>(null)

const sortedSessions = computed(() =>
  [...sessionsStore.sessions].sort((a, b) => (b.isCurrent ? 1 : 0) - (a.isCurrent ? 1 : 0))
)

async function handleLogoutSession(refreshTokenId: string): Promise<void> {
  try {
    sessionsError.value = null
    await sessionsStore.logout(refreshTokenId)
  } catch (e) {
    sessionsError.value = parseApiError(e)[0] ?? 'Не удалось завершить сессию'
  }
}

async function handleLogoutOthers(): Promise<void> {
  sessionsAction.value = 'others'
  try {
    sessionsError.value = null
    await sessionsStore.logoutAll(true)
    showSessionsModal.value = false
  } catch (e) {
    sessionsError.value = parseApiError(e)[0] ?? 'Не удалось завершить другие сессии'
    showSessionsModal.value = false
  } finally {
    sessionsAction.value = null
  }
}

async function handleLogoutCurrent(refreshTokenId: string): Promise<void> {
  try {
    sessionsError.value = null
    await sessionsStore.logout(refreshTokenId)
    authStore.accessToken = null
    await router.push({ name: 'login' })
  } catch (e) {
    sessionsError.value = parseApiError(e)[0] ?? 'Не удалось завершить текущую сессию'
  }
}

async function handleLogoutAll(): Promise<void> {
  sessionsAction.value = 'all'
  try {
    sessionsError.value = null
    await sessionsStore.logoutAll(false)
    authStore.accessToken = null
    showSessionsModal.value = false
    await router.push({ name: 'login' })
  } catch (e) {
    sessionsError.value = parseApiError(e)[0] ?? 'Не удалось завершить все сессии'
    showSessionsModal.value = false
  } finally {
    sessionsAction.value = null
  }
}

onMounted(async () => {
  mountScrollSpy()

  await Promise.allSettled([
    loadProviders(),
    factorsStore.loadFactors(),
    sessionsStore.loadSessions(),
  ])

  onScroll()
})

onBeforeUnmount(() => {
  unmountScrollSpy()
})
</script>

<style>
@import '@/assets/settings.css';
</style>

<style scoped>
.settings {
  display: flex;
  gap: 0;
  min-height: 100%;
  overflow: visible;
  max-width: 1040px;
  margin: 0 auto;
  width: 100%;
}

.settings__content {
  flex: 1;
  overflow: visible;
  padding: 32px 36px 48px;
  display: flex;
  flex-direction: column;
  gap: 40px;
}

.settings__section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.settings__section + .settings__section {
  border-top: 0.5px solid var(--color-border);
  padding-top: 32px;
}

@media (max-width: 700px) {
  .settings__content {
    padding: 24px 16px 32px;
  }
}
</style>
