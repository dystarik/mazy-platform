import { defineStore } from 'pinia'
import { ref } from 'vue'
import { mfaApi } from '@/api'
import { useAuthStore } from './auth.store'
import type { MfaFactorType, TotpSetup, EmailMfaSetup } from '@/types/api'

export const useFactorsStore = defineStore('factors', () => {
  const factors = ref<MfaFactorType[]>([])
  const backupCodes = ref<string[]>([])
  const totpSetup = ref<TotpSetup | null>(null)
  const emailSetup = ref<EmailMfaSetup | null>(null)
  const isLoading = ref(false)

  async function loadFactors(): Promise<void> {
    isLoading.value = true
    try {
      const response = await mfaApi.getFactors()
      factors.value = response.factors ?? []
    } finally {
      isLoading.value = false
    }
  }

  async function addFactor(factor: MfaFactorType, email?: string): Promise<void> {
    const response = await mfaApi.addFactor({ factor, email })
    totpSetup.value = response.totp ?? null
    emailSetup.value = response.email ?? null
    // Если email-фактор добавлен без верификации (та же почта) — сразу получаем backup-коды
    if (response.email?.success?.backupCodes) {
      backupCodes.value = response.email.success.backupCodes
    }
  }

  async function confirmFactor(
    factor: MfaFactorType,
    code: string,
    otpId?: string
  ): Promise<void> {
    const request =
      factor === 'MFA_FACTOR_TYPE_TOTP'
        ? { factor, totp: { code } }
        : { factor, email: { otpId, code } }

    const response = await mfaApi.confirmFactor(request)
    backupCodes.value = response.backupCodes ?? []

    // Обновляем факторы в auth store
    const authStore = useAuthStore()
    await authStore.loadMfaFactors()
    await loadFactors()
  }

  async function removeFactor(
    factor: MfaFactorType,
    mfaSessionId: string
  ): Promise<void> {
    await mfaApi.removeFactor({ factor, mfaSessionId })
    await loadFactors()

    const authStore = useAuthStore()
    await authStore.loadMfaFactors()
  }

  async function regenerateBackupCodes(mfaSessionId: string): Promise<void> {
    const response = await mfaApi.regenerateBackupCodes({ mfaSessionId })
    backupCodes.value = response.backupCodes ?? []
  }

  function clearBackupCodes(): void {
    backupCodes.value = []
  }

  function clearSetup(): void {
    totpSetup.value = null
    emailSetup.value = null
  }

  return {
    factors,
    backupCodes,
    totpSetup,
    emailSetup,
    isLoading,
    loadFactors,
    addFactor,
    confirmFactor,
    removeFactor,
    regenerateBackupCodes,
    clearBackupCodes,
    clearSetup,
  }
})
