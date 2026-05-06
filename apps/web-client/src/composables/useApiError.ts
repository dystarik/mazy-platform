import { AuthErrorCodes } from '@/types/domain/error-codes'

const errorMessages: Record<string, string> = {
  // Общие
  [AuthErrorCodes.InternalError]: 'Внутренняя ошибка сервера',
  [AuthErrorCodes.Unauthorized]: 'Неверный email или пароль',
  [AuthErrorCodes.EmailVerificationPending]: 'Email зарегистрирован, но не подтверждён',

  // Email
  [AuthErrorCodes.Email.Required]: 'Email обязателен',
  [AuthErrorCodes.Email.TooShort]: 'Email слишком короткий',
  [AuthErrorCodes.Email.TooLong]: 'Email слишком длинный',
  [AuthErrorCodes.Email.InvalidFormat]: 'Неверный формат email',

  // Пароль
  [AuthErrorCodes.Password.Required]: 'Пароль обязателен',
  [AuthErrorCodes.Password.Invalid]: 'Неверный пароль',
  [AuthErrorCodes.Password.TooShort]: 'Пароль должен быть не менее 8 символов',
  [AuthErrorCodes.Password.TooLong]: 'Пароль слишком длинный',
  [AuthErrorCodes.Password.NoUpperCase]: 'Пароль должен содержать заглавную букву',
  [AuthErrorCodes.Password.NoLowerCase]: 'Пароль должен содержать строчную букву',
  [AuthErrorCodes.Password.NoDigit]: 'Пароль должен содержать цифру',
  [AuthErrorCodes.Password.NoSpecialChar]: 'Пароль должен содержать специальный символ',

  // OTP
  [AuthErrorCodes.OneTimePassword.NotFound]: 'Код не найден',
  [AuthErrorCodes.OneTimePassword.Expired]: 'Срок действия кода истёк',
  [AuthErrorCodes.OneTimePassword.InvalidCode]: 'Неверный код',
  [AuthErrorCodes.OneTimePassword.TooManyAttempts]: 'Слишком много попыток, запросите новый код',

  // Регистрация
  [AuthErrorCodes.Registration.EmailAlreadyInUse]: 'Этот email уже зарегистрирован',

  // Аккаунт
  [AuthErrorCodes.UserAccount.NotFound]: 'Пользователь не найден',
  [AuthErrorCodes.UserAccount.ExternalProviderAlreadyLinked]: 'Провайдер уже привязан',
  [AuthErrorCodes.UserAccount.ExternalProviderNotLinked]: 'Провайдер не привязан',
  [AuthErrorCodes.UserAccount.CannotUnlinkLastAuthenticationMethod]: 'Нельзя отвязать последний способ входа',
  [AuthErrorCodes.UserAccount.MfaMethodAlreadyExists]: 'Этот метод MFA уже добавлен',
  [AuthErrorCodes.UserAccount.MfaMethodNotFound]: 'Метод MFA не найден',
  [AuthErrorCodes.UserAccount.TotpCodeInvalid]: 'Неверный TOTP-код',
  [AuthErrorCodes.UserAccount.BackupCodeInvalid]: 'Неверный резервный код',
  [AuthErrorCodes.UserAccount.NoConfirmedMfaMethod]: 'Нет подтверждённого метода MFA',

  // MFA сессия
  [AuthErrorCodes.MfaSession.NotFound]: 'MFA-сессия не найдена',
  [AuthErrorCodes.MfaSession.AlreadyCompleted]: 'MFA-сессия уже завершена',
  [AuthErrorCodes.MfaSession.Expired]: 'MFA-сессия истекла, начните заново',
  [AuthErrorCodes.MfaSession.FactorAlreadyUsed]: 'Этот фактор уже использован',
  [AuthErrorCodes.MfaSession.PrimaryFactorRequired]: 'Требуется основной фактор MFA',
}

interface ErrorDetail {
  reason: string
  metadata?: { message: string }
}

interface ApiErrorResponse {
  code?: number
  message?: string
  details?: ErrorDetail[]
}

export function parseApiError(error: unknown): string[] {
  if (!(error instanceof Error)) return ['Произошла ошибка']

  const axiosError = error as { response?: { data?: ApiErrorResponse } }
  const data = axiosError.response?.data

  if (!data) return [error.message]

  // Если есть details — маппим каждый reason на текст
  if (data.details && data.details.length > 0) {
    const messages = data.details
      .map((d) => errorMessages[d.reason] ?? d.metadata?.message ?? d.reason)
      .filter(Boolean)

    if (messages.length > 0) return messages
  }

  // Иначе общее сообщение
  return [data.message ?? 'Произошла ошибка']
}

const loginErrorCodes = new Set<string>([
  AuthErrorCodes.Unauthorized,
  AuthErrorCodes.UserAccount.NotFound,
  AuthErrorCodes.Password.Required,
  AuthErrorCodes.Password.Invalid,
  AuthErrorCodes.Password.TooShort,
  AuthErrorCodes.Password.TooLong,
  AuthErrorCodes.Password.NoUpperCase,
  AuthErrorCodes.Password.NoLowerCase,
  AuthErrorCodes.Password.NoDigit,
  AuthErrorCodes.Password.NoSpecialChar,
  AuthErrorCodes.Email.Required,
  AuthErrorCodes.Email.InvalidFormat,
])

export function parseLoginError(error: unknown): string[] {
  const errors = parseApiError(error)

  const axiosError = error as { response?: { data?: ApiErrorResponse } }
  const details = axiosError.response?.data?.details ?? []

  const allAreLoginErrors = details.every(d => loginErrorCodes.has(d.reason))

  if (allAreLoginErrors) {
    return ['Неверный email или пароль']
  }

  return errors
}
