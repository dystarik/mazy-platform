import type { components } from './schema'

// Логин по паролю
export type LoginByPasswordRequest = components['schemas']['LoginByPasswordRequest']
export type LoginByPasswordResponse = components['schemas']['LoginByPasswordResponse']

// Логин через внешний провайдер (Yandex)
export type LoginByExternalProviderRequest = components['schemas']['LoginByExternalProviderRequest']
export type LoginByExternalProviderResponse = components['schemas']['LoginByExternalProviderResponse']

// Регистрация
export type RegisterByPasswordRequest = components['schemas']['RegisterByPasswordRequest']
export type RegisterByPasswordResponse = components['schemas']['RegisterByPasswordResponse']
export type CompleteRegistrationRequest = components['schemas']['CompleteRegistrationRequest']
export type CompleteRegistrationResponse = components['schemas']['CompleteRegistrationResponse']
export type ResendConfirmationCodeRequest = components['schemas']['ResendConfirmationCodeRequest']
export type ResendConfirmationCodeResponse = components['schemas']['ResendConfirmationCodeResponse']
