import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { authApi, mfaApi, sessionsApi, saveTokens, clearTokens, apiClient } from '@/api'
import { isTokenExpired } from '@/composables/useJwt'
import { useMfaStore } from './mfa.store'
import type { MfaChallenge, MfaFactorType } from '@/types/api'


export const useAuthStore = defineStore('auth', () => {
  // Состояние
  const accessToken = ref<string | null>(sessionStorage.getItem('accessToken'))
  const mfaFactors = ref<MfaFactorType[]>([])
  const pendingOtpId = ref<string | null>(null)
  const pendingRegistrationEmail = ref<string | null>(null)

  // Computed
  const isAuthenticated = computed(() => accessToken.value !== null && !isTokenExpired(accessToken.value))
  const hasMfa = computed(() => mfaFactors.value.length > 0)

  // Загрузка MFA факторов
  async function loadMfaFactors(): Promise<void> {
    try {
      const response = await mfaApi.getFactors()
      mfaFactors.value = response.factors ?? []
    } catch {
      mfaFactors.value = []
    }
  }

  // Регистрация — сохраняет otpId и email в store
  async function register(email: string, password: string): Promise<void> {
    const response = await authApi.register({ email, password })
    pendingOtpId.value = response.otpId!
    pendingRegistrationEmail.value = email
  }

  // Повторная отправка кода подтверждения (теперь по email)
  async function resendConfirmationCode(email?: string): Promise<void> {
    const target = email ?? pendingRegistrationEmail.value
    if (!target) throw new Error('Email не найден')
    const response = await authApi.resendConfirmationCode({ email: target })
    pendingOtpId.value = response.otpId ?? null
    pendingRegistrationEmail.value = target
  }

  // Подтверждение email после регистрации
  async function completeRegistration(otpCode: string): Promise<void> {
    if (!pendingOtpId.value) throw new Error('otpId не найден')
    const response = await authApi.completeRegistration({
      otpId: pendingOtpId.value,
      otpCode,
    })
    saveTokens(response.tokens!.accessToken!, response.tokens!.refreshToken!)
    accessToken.value = response.tokens!.accessToken!
    pendingOtpId.value = null
    await loadMfaFactors()
  }

  // Логин по паролю
  async function loginByPassword(
    email: string,
    password: string,
    mfaSessionId?: string
  ): Promise<{ success: true } | { success: false; challenge: MfaChallenge }> {
    const response = await authApi.loginByPassword({ email, password, mfaSessionId })

    if (response.tokens) {
      saveTokens(response.tokens.accessToken!, response.tokens.refreshToken!)
      accessToken.value = response.tokens.accessToken!
      await loadMfaFactors()
      return { success: true }
    }

    const mfaStore = useMfaStore()
    mfaStore.pendingEmail = email
    mfaStore.pendingPassword = password
    return { success: false, challenge: response.challenge! }
  }

  // Логин через Yandex
  async function loginByYandex(code: string): Promise<void> {
    const response = await authApi.loginByExternalProvider({
      provider: 'EXTERNAL_PROVIDER_YANDEX',
      code,
    })
    saveTokens(response.tokens!.accessToken!, response.tokens!.refreshToken!)
    accessToken.value = response.tokens!.accessToken!
    await loadMfaFactors()
  }

  // Логаут
  async function logout(refreshTokenId: string): Promise<void> {
    try {
      await sessionsApi.logout({ refreshTokenId })
    } finally {
      clearTokens()
      accessToken.value = null
      mfaFactors.value = []
    }
  }

  // Инициализация при старте приложения
  async function init(): Promise<void> {
    const refreshToken = localStorage.getItem('refreshToken')
    const refreshTokenId = localStorage.getItem('refreshTokenId')

    if (!refreshToken || !refreshTokenId) return

    try {
      const response = await apiClient.post('/api/v1/sessions/refresh', {
        refreshToken,
        refreshTokenId,
      })
      const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data.tokens
      saveTokens(newAccessToken, newRefreshToken)
      accessToken.value = newAccessToken
      await loadMfaFactors()
    } catch {
      clearTokens()
    }
  }

  return {
    accessToken,
    mfaFactors,
    pendingOtpId,
    pendingRegistrationEmail,
    isAuthenticated,
    hasMfa,
    loginByPassword,
    loginByYandex,
    logout,
    loadMfaFactors,
    init,
    register,
    resendConfirmationCode,
    completeRegistration,
  }
})
