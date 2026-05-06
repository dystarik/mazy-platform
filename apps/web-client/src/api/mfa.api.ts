import apiClient from './client'
import type {
  AddFactorRequest,
  AddFactorResponse,
  ConfirmFactorRequest,
  ConfirmFactorResponse,
  RemoveFactorRequest,
  GetFactorsResponse,
  RegenerateBackupCodesRequest,
  RegenerateBackupCodesResponse,
  StartRequest,
  StartResponse,
  GetStatusResponse,
  SendEmailCodeRequest,
  SendUnauthenticatedEmailCodeRequest,
  VerifyCodeRequest,
  VerifyCodeResponse,
  VerifyUnauthenticatedCodeRequest,
  VerifyUnauthenticatedCodeResponse,
} from '@/types/api'

export const mfaApi = {
  // Факторы
  getFactors(): Promise<GetFactorsResponse> {
    return apiClient.get('/api/v1/mfa/factors').then((r) => r.data)
  },

  addFactor(data: AddFactorRequest): Promise<AddFactorResponse> {
    return apiClient.post('/api/v1/mfa/factors', data).then((r) => r.data)
  },

  confirmFactor(data: ConfirmFactorRequest): Promise<ConfirmFactorResponse> {
    return apiClient.post('/api/v1/mfa/factors/confirm', data).then((r) => r.data)
  },

  removeFactor(data: RemoveFactorRequest): Promise<void> {
    return apiClient.delete('/api/v1/mfa/factors', { data }).then((r) => r.data)
  },

  // Backup коды
  regenerateBackupCodes(data: RegenerateBackupCodesRequest): Promise<RegenerateBackupCodesResponse> {
    return apiClient.post('/api/v1/mfa/backup-codes/regenerate', data).then((r) => r.data)
  },

  // MFA сессии (авторизованные)
  startSession(data: StartRequest): Promise<StartResponse> {
    return apiClient.post('/api/v1/mfa/sessions', data).then((r) => r.data)
  },

  getSessionStatus(mfaSessionId: string): Promise<GetStatusResponse> {
    return apiClient.get(`/api/v1/mfa/sessions/${mfaSessionId}/status`).then((r) => r.data)
  },

  sendEmailCode(mfaSessionId: string, data: SendEmailCodeRequest): Promise<void> {
    return apiClient.post(`/api/v1/mfa/sessions/${mfaSessionId}/email/send`, data).then((r) => r.data)
  },

  verifyCode(mfaSessionId: string, data: VerifyCodeRequest): Promise<VerifyCodeResponse> {
    return apiClient.post(`/api/v1/mfa/sessions/${mfaSessionId}/verify`, data).then((r) => r.data)
  },

  // MFA сессии (неавторизованные)
  sendUnauthenticatedEmailCode(mfaSessionId: string, data: SendUnauthenticatedEmailCodeRequest): Promise<void> {
    return apiClient.post(`/api/v1/mfa/sessions/${mfaSessionId}/email/send-unauthenticated`, data).then((r) => r.data)
  },

  verifyUnauthenticatedCode(mfaSessionId: string, data: VerifyUnauthenticatedCodeRequest): Promise<VerifyUnauthenticatedCodeResponse> {
    return apiClient.post(`/api/v1/mfa/sessions/${mfaSessionId}/verify-unauthenticated`, data).then((r) => r.data)
  },
}
