import apiClient from './client'
import type {
  ChangePasswordRequest,
  ChangePasswordResponse,
  SetPasswordRequest,
  SetPasswordResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  ConfirmResetPasswordRequest,
} from '@/types/api'

export const passwordApi = {
  changePassword(data: ChangePasswordRequest): Promise<ChangePasswordResponse> {
    return apiClient.post('/api/v1/password/change', data).then((r) => r.data)
  },

  setPassword(data: SetPasswordRequest): Promise<SetPasswordResponse> {
    return apiClient.post('/api/v1/password/set', data).then((r) => r.data)
  },

  resetPassword(data: ResetPasswordRequest): Promise<ResetPasswordResponse> {
    return apiClient.post('/api/v1/password/reset', data).then((r) => r.data)
  },

  confirmResetPassword(data: ConfirmResetPasswordRequest): Promise<void> {
    return apiClient.post('/api/v1/password/reset/confirm', data).then((r) => r.data)
  },
}
