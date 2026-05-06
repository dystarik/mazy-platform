import QRCode from 'qrcode'
import { computed, ref, type Ref } from 'vue'
import { passwordApi } from '@/api'
import { parseApiError } from '@/composables/useApiError'
import { useFactorsStore } from '@/stores/factors.store'
import { useMfaStore } from '@/stores/mfa.store'
import type { MfaChallenge, MfaFactorType } from '@/types/api'

export type MfaPageState =
  | null
  | 'add-email-form'
  | 'mfa-challenge'
  | 'add-totp-qr'
  | 'add-email-code'
  | 'backup-codes'

type PendingAction = 'remove' | 'regen' | 'change-password' | 'set-password' | null

interface PasswordRefs {
  currentPassword: Ref<string>
  newPassword: Ref<string>
  passwordSuccess: Ref<boolean>
  setPasswordValue: Ref<string>
  setPasswordSuccess: Ref<boolean>
}

export function useSettingsMfaFlow(passwordRefs: PasswordRefs) {
  const factorsStore = useFactorsStore()
  const mfaStore = useMfaStore()

  const mfaPageState = ref<MfaPageState>(null)
  const pendingAction = ref<PendingAction>(null)
  const pendingRemoveFactor = ref<MfaFactorType | null>(null)
  const mfaCode = ref('')
  const mfaErrors = ref<string[]>([])
  const mfaLoading = ref(false)
  const usedFactors = ref<MfaFactorType[]>([])
  const qrDataUrl = ref<string | null>(null)
  const emailForFactor = ref('')
  const copied = ref(false)

  const hasTotp = computed(() => factorsStore.factors.includes('MFA_FACTOR_TYPE_TOTP'))
  const hasEmail = computed(() => factorsStore.factors.includes('MFA_FACTOR_TYPE_EMAIL'))
  const remainingFactors = computed(() =>
    mfaStore.availableFactors.filter((f) => !usedFactors.value.includes(f)),
  )

  function startPasswordChallenge(
    action: 'change-password' | 'set-password',
    challenge: MfaChallenge,
  ): void {
    pendingAction.value = action
    mfaStore.startFromChallenge(
      challenge,
      action === 'change-password' ? 'change_password' : 'set_password',
    )
    mfaPageState.value = 'mfa-challenge'
    usedFactors.value = []
    mfaCode.value = ''
  }

  function factorLabel(factor: MfaFactorType): string {
    switch (factor) {
      case 'MFA_FACTOR_TYPE_TOTP': return 'Приложение-аутентификатор'
      case 'MFA_FACTOR_TYPE_EMAIL': return 'Отправить код на почту'
      case 'MFA_FACTOR_TYPE_BACKUP_CODE': return 'Резервный код'
      default: return factor
    }
  }

  function factorHint(factor: MfaFactorType | null): string {
    switch (factor) {
      case 'MFA_FACTOR_TYPE_TOTP': return 'Введите код из приложения-аутентификатора'
      case 'MFA_FACTOR_TYPE_EMAIL': return 'Код отправлен на ваш email'
      case 'MFA_FACTOR_TYPE_BACKUP_CODE': return 'Введите один из резервных кодов'
      default: return ''
    }
  }

  async function copyBackupCodes(): Promise<void> {
    await navigator.clipboard.writeText(factorsStore.backupCodes.join('\n'))
    copied.value = true
    setTimeout(() => (copied.value = false), 2000)
  }

  async function startAddTotp(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      await factorsStore.addFactor('MFA_FACTOR_TYPE_TOTP')
      if (factorsStore.totpSetup?.provisioningUri) {
        qrDataUrl.value = await QRCode.toDataURL(factorsStore.totpSetup.provisioningUri, {
          margin: 1,
          width: 180,
        })
      }
      mfaPageState.value = 'add-totp-qr'
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  async function confirmTotp(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      await factorsStore.confirmFactor('MFA_FACTOR_TYPE_TOTP', mfaCode.value)
      goToBackupCodesOrDone()
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  async function startAddEmail(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      const email = emailForFactor.value.trim() || undefined
      await factorsStore.addFactor('MFA_FACTOR_TYPE_EMAIL', email)

      if (factorsStore.emailSetup?.success) {
        await factorsStore.loadFactors()
        goToBackupCodesOrDone()
      } else {
        mfaPageState.value = 'add-email-code'
      }
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  async function confirmEmail(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      const otpId = factorsStore.emailSetup?.otpId ?? ''
      await factorsStore.confirmFactor('MFA_FACTOR_TYPE_EMAIL', mfaCode.value, otpId)
      goToBackupCodesOrDone()
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  async function startRemoveFactor(factor: MfaFactorType): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      pendingAction.value = 'remove'
      pendingRemoveFactor.value = factor
      await mfaStore.startAuthenticated('MFA_SESSION_ACTION_REMOVE_FACTOR', 'remove_factor')
      mfaPageState.value = 'mfa-challenge'
      usedFactors.value = []
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
      pendingAction.value = null
      pendingRemoveFactor.value = null
    } finally {
      mfaLoading.value = false
    }
  }

  async function startRegenerate(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      pendingAction.value = 'regen'
      await mfaStore.startAuthenticated('MFA_SESSION_ACTION_REGENERATE_BACKUP_CODES', 'regenerate_backup_codes')
      mfaPageState.value = 'mfa-challenge'
      usedFactors.value = []
      mfaCode.value = ''
    } catch (e) {
      mfaErrors.value = parseApiError(e)
      pendingAction.value = null
    } finally {
      mfaLoading.value = false
    }
  }

  async function handleMfaSelectFactor(factor: MfaFactorType): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      await mfaStore.selectFactor(factor)
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  async function handleMfaVerify(): Promise<void> {
    mfaLoading.value = true
    mfaErrors.value = []
    try {
      const completed = await mfaStore.verifyCode(mfaCode.value)
      mfaCode.value = ''

      if (!completed) {
        usedFactors.value.push(mfaStore.selectedFactor!)
        mfaStore.step = 'select_factor'
        return
      }

      const sessionId = mfaStore.mfaSessionId!

      if (pendingAction.value === 'change-password') {
        await passwordApi.changePassword({
          currentPassword: passwordRefs.currentPassword.value,
          newPassword: passwordRefs.newPassword.value,
          mfaSessionId: sessionId,
        })
        passwordRefs.passwordSuccess.value = true
        passwordRefs.currentPassword.value = ''
        passwordRefs.newPassword.value = ''
        mfaPageState.value = null
      } else if (pendingAction.value === 'set-password') {
        await passwordApi.setPassword({
          newPassword: passwordRefs.setPasswordValue.value,
          mfaSessionId: sessionId,
        })
        passwordRefs.setPasswordSuccess.value = true
        passwordRefs.setPasswordValue.value = ''
        mfaPageState.value = null
      } else if (pendingAction.value === 'remove' && pendingRemoveFactor.value) {
        await factorsStore.removeFactor(pendingRemoveFactor.value, sessionId)
        mfaPageState.value = null
      } else if (pendingAction.value === 'regen') {
        await factorsStore.regenerateBackupCodes(sessionId)
        goToBackupCodesOrDone()
      }

      mfaStore.reset()
      usedFactors.value = []
      pendingAction.value = null
      pendingRemoveFactor.value = null
    } catch (e) {
      mfaErrors.value = parseApiError(e)
    } finally {
      mfaLoading.value = false
    }
  }

  function goToBackupCodesOrDone(): void {
    mfaPageState.value = factorsStore.backupCodes.length > 0 ? 'backup-codes' : null
  }

  function cancelMfaFlow(): void {
    mfaStore.reset()
    factorsStore.clearSetup()
    factorsStore.clearBackupCodes()
    mfaPageState.value = null
    mfaCode.value = ''
    mfaErrors.value = []
    emailForFactor.value = ''
    pendingAction.value = null
    pendingRemoveFactor.value = null
    usedFactors.value = []
    qrDataUrl.value = null
  }

  return {
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
  }
}
