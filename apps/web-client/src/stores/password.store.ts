import { defineStore } from 'pinia'
import { passwordApi } from '@/api'
import type { MfaChallenge, OtpChallenge } from '@/types/api'

export const usePasswordStore = defineStore('password', () => {
  // Смена пароля
  async function changePassword(
    currentPassword: string,
    newPassword: string,
    mfaSessionId?: string
  ): Promise<{ success: true } | { success: false; challenge: MfaChallenge }> {
    const response = await passwordApi.changePassword({
      currentPassword,
      newPassword,
      mfaSessionId,
    })

    if (response.success) {
      return { success: true }
    }

    return { success: false, challenge: response.challenge! }
  }

  // Сброс пароля
  async function resetPassword(
    email: string
  ): Promise<{ otp: OtpChallenge } | { mfa: MfaChallenge }> {
    const response = await passwordApi.resetPassword({ email })

    if (response.otp) {
      return { otp: response.otp }
    }

    return { mfa: response.mfa! }
  }

  // Подтверждение сброса пароля через OTP
  async function confirmResetPasswordByOtp(
    newPassword: string,
    otpId: string,
    otpCode: string
  ): Promise<void> {
    await passwordApi.confirmResetPassword({
      newPassword,
      otp: { otpId, code: otpCode },
    })
  }

  // Подтверждение сброса пароля через MFA
  async function confirmResetPasswordByMfa(
    newPassword: string,
    mfaSessionId: string
  ): Promise<void> {
    await passwordApi.confirmResetPassword({
      newPassword,
      mfa: { mfaSessionId },
    })
  }

  return {
    changePassword,
    resetPassword,
    confirmResetPasswordByOtp,
    confirmResetPasswordByMfa,
  }
})
