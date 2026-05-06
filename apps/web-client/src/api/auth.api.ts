import apiClient from './client'
import type {
  LoginByPasswordRequest,
  LoginByPasswordResponse,
  LoginByExternalProviderRequest,
  LoginByExternalProviderResponse,
  RegisterByPasswordRequest,
  RegisterByPasswordResponse,
  CompleteRegistrationRequest,
  CompleteRegistrationResponse,
  ResendConfirmationCodeRequest,
  ResendConfirmationCodeResponse,
} from '@/types/api'

export const authApi = {
  loginByPassword(data: LoginByPasswordRequest): Promise<LoginByPasswordResponse> {
    return apiClient.post('/api/v1/auth/login/password', data).then((r) => r.data)
  },

  loginByExternalProvider(data: LoginByExternalProviderRequest): Promise<LoginByExternalProviderResponse> {
    return apiClient.post('/api/v1/auth/login/external-provider', data).then((r) => r.data)
  },

  register(data: RegisterByPasswordRequest): Promise<RegisterByPasswordResponse> {
    return apiClient.post('/api/v1/auth/registration/password', data).then((r) => r.data)
  },

  completeRegistration(data: CompleteRegistrationRequest): Promise<CompleteRegistrationResponse> {
    return apiClient.post('/api/v1/auth/registration/complete', data).then((r) => r.data)
  },

  resendConfirmationCode(data: ResendConfirmationCodeRequest): Promise<ResendConfirmationCodeResponse> {
    return apiClient.post('/api/v1/auth/registration/resend', data).then((r) => r.data)
  },
}
