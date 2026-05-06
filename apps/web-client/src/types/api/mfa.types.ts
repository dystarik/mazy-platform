import type { components } from './schema'

// Факторы
export type GetFactorsResponse = components['schemas']['GetFactorsResponse']
export type AddFactorRequest = components['schemas']['AddFactorRequest']
export type AddFactorResponse = components['schemas']['AddFactorResponse']
export type RemoveFactorRequest = components['schemas']['RemoveFactorRequest']
export type TotpSetup = components['schemas']['TotpSetup']
export type EmailMfaSetup = components['schemas']['EmailMfaSetup']
export type EmailMfaSetupSuccess = components['schemas']['EmailMfaSetupSuccess']

// Подтверждение фактора
export type ConfirmFactorRequest = components['schemas']['ConfirmFactorRequest']
export type ConfirmFactorResponse = components['schemas']['ConfirmFactorResponse']
export type ConfirmTotp = components['schemas']['ConfirmTotp']
export type ConfirmEmail = components['schemas']['ConfirmEmail']

// Backup коды
export type RegenerateBackupCodesRequest = components['schemas']['RegenerateBackupCodesRequest']
export type RegenerateBackupCodesResponse = components['schemas']['RegenerateBackupCodesResponse']

// MFA сессии (авторизованные)
export type StartRequest = components['schemas']['StartRequest']
export type StartResponse = components['schemas']['StartResponse']
export type GetStatusResponse = components['schemas']['GetStatusResponse']
export type SendEmailCodeRequest = components['schemas']['SendEmailCodeRequest']
export type VerifyCodeRequest = components['schemas']['VerifyCodeRequest']
export type VerifyCodeResponse = components['schemas']['VerifyCodeResponse']

// MFA сессии (неавторизованные)
export type SendUnauthenticatedEmailCodeRequest = components['schemas']['SendUnauthenticatedEmailCodeRequest']
export type VerifyUnauthenticatedCodeRequest = components['schemas']['VerifyUnauthenticatedCodeRequest']
export type VerifyUnauthenticatedCodeResponse = components['schemas']['VerifyUnauthenticatedCodeResponse']
