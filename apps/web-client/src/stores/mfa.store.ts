import { defineStore } from 'pinia'
import { ref } from 'vue'
import { mfaApi } from '@/api'
import type { MfaChallenge, MfaFactorType, MfaSessionAction } from '@/types/api'

type MfaStep = 'select_factor' | 'enter_code' | 'completed'
type MfaMode = 'unauthenticated' | 'authenticated'
type MfaContext =
  | 'login'
  | 'reset_password'
  | 'change_password'
  | 'set_password'
  | 'remove_factor'
  | 'regenerate_backup_codes'

export const useMfaStore = defineStore('mfa', () => {
  // Состояние
  const mfaSessionId = ref<string | null>(null)
  const availableFactors = ref<MfaFactorType[]>([])
  const requiredFactorCount = ref(0)
  const selectedFactor = ref<MfaFactorType | null>(null)
  const step = ref<MfaStep>('select_factor')
  const mode = ref<MfaMode>('unauthenticated')
  const context = ref<MfaContext | null>(null)
  const pendingEmail = ref<string | null>(null)
  const pendingPassword = ref<string | null>(null)

  // Инициализация из challenge (гостевой флоу)
  function startFromChallenge(challenge: MfaChallenge, mfaContext: MfaContext) {
    mfaSessionId.value = challenge.mfaSessionId!
    availableFactors.value = challenge.availableFactors ?? []
    requiredFactorCount.value = challenge.requiredFactorCount ?? 1
    selectedFactor.value = null
    step.value = 'select_factor'
    mode.value = 'unauthenticated'
    context.value = mfaContext
  }

  // Инициализация для авторизованного флоу
  async function startAuthenticated(action: MfaSessionAction, mfaContext: MfaContext) {
    const response = await mfaApi.startSession({ action })
    mfaSessionId.value = response.mfaSessionId!
    availableFactors.value = response.availableFactors ?? []
    requiredFactorCount.value = response.requiredFactorCount ?? 1
    selectedFactor.value = null
    step.value = 'select_factor'
    mode.value = 'authenticated'
    context.value = mfaContext
  }

  // Выбор фактора
  async function selectFactor(factor: MfaFactorType) {
    selectedFactor.value = factor
    step.value = 'enter_code'

    // Если Email — автоматически отправляем код
    if (factor === 'MFA_FACTOR_TYPE_EMAIL') {
      await sendEmailCode()
    }
  }

  // Отправка email кода
  async function sendEmailCode() {
    if (!mfaSessionId.value) return

    if (mode.value === 'unauthenticated') {
      await mfaApi.sendUnauthenticatedEmailCode(mfaSessionId.value, {
        mfaSessionId: mfaSessionId.value,
      })
    } else {
      await mfaApi.sendEmailCode(mfaSessionId.value, {
        mfaSessionId: mfaSessionId.value,
      })
    }
  }

  // Верификация кода
  async function verifyCode(code: string): Promise<boolean> {
    if (!mfaSessionId.value || !selectedFactor.value) return false

    let isCompleted = false

    if (mode.value === 'unauthenticated') {
      const response = await mfaApi.verifyUnauthenticatedCode(mfaSessionId.value, {
        mfaSessionId: mfaSessionId.value,
        factor: selectedFactor.value,
        code,
      })
      isCompleted = response.isCompleted ?? false
    } else {
      const response = await mfaApi.verifyCode(mfaSessionId.value, {
        mfaSessionId: mfaSessionId.value,
        factor: selectedFactor.value,
        code,
      })
      isCompleted = response.isCompleted ?? false
    }

    if (isCompleted) {
      step.value = 'completed'
    }

    return isCompleted
  }

  // Сброс
  function reset() {
    mfaSessionId.value = null
    availableFactors.value = []
    requiredFactorCount.value = 0
    selectedFactor.value = null
    step.value = 'select_factor'
    mode.value = 'unauthenticated'
    context.value = null
  }

  return {
    mfaSessionId,
    availableFactors,
    requiredFactorCount,
    selectedFactor,
    step,
    mode,
    context,
    startFromChallenge,
    startAuthenticated,
    selectFactor,
    sendEmailCode,
    verifyCode,
    reset,
    pendingEmail,
    pendingPassword,
  }
})
