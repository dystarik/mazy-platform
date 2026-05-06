export const AuthErrorCodes = {
  InternalError: 'AUTH.INTERNAL_ERROR',
  Unauthorized: 'AUTH.UNAUTHORIZED',
  // Email зарегистрирован, но не подтверждён — возвращается при логине и регистрации
  EmailVerificationPending: 'AUTH.EMAIL_VERIFICATION_PENDING',

  Validation: {
    Required: 'AUTH.VALIDATION.REQUIRED',
    Invalid: 'AUTH.VALIDATION.INVALID',
  },

  Email: {
    Required: 'AUTH.EMAIL.REQUIRED',
    TooShort: 'AUTH.EMAIL.TOO_SHORT',
    TooLong: 'AUTH.EMAIL.TOO_LONG',
    InvalidFormat: 'AUTH.EMAIL.INVALID_FORMAT',
  },

  Password: {
    Required: 'AUTH.PASSWORD.REQUIRED',
    Invalid: 'AUTH.PASSWORD.INVALID',
    TooShort: 'AUTH.PASSWORD.TOO_SHORT',
    TooLong: 'AUTH.PASSWORD.TOO_LONG',
    NoUpperCase: 'AUTH.PASSWORD.NO_UPPER_CASE',
    NoLowerCase: 'AUTH.PASSWORD.NO_LOWER_CASE',
    NoDigit: 'AUTH.PASSWORD.NO_DIGIT',
    NoSpecialChar: 'AUTH.PASSWORD.NO_SPECIAL_CHAR',
  },

  OneTimePassword: {
    NotFound: 'AUTH.ONE_TIME_PASSWORD.NOT_FOUND',
    Expired: 'AUTH.ONE_TIME_PASSWORD.EXPIRED',
    InvalidCode: 'AUTH.ONE_TIME_PASSWORD.INVALID_CODE',
    TooManyAttempts: 'AUTH.ONE_TIME_PASSWORD.TOO_MANY_ATTEMPTS',
  },

  Registration: {
    EmailAlreadyInUse: 'AUTH.REGISTRATION.EMAIL_ALREADY_IN_USE',
  },

  UserAccount: {
    NotFound: 'AUTH.USER_ACCOUNT.NOT_FOUND',
    ExternalProviderAlreadyLinked: 'AUTH.USER_ACCOUNT.EXTERNAL_PROVIDER_ALREADY_LINKED',
    ExternalProviderNotLinked: 'AUTH.USER_ACCOUNT.EXTERNAL_PROVIDER_NOT_LINKED',
    CannotUnlinkLastAuthenticationMethod: 'AUTH.USER_ACCOUNT.CANNOT_UNLINK_LAST_AUTHENTICATION_METHOD',
    MfaMethodAlreadyExists: 'AUTH.USER_ACCOUNT.MFA_METHOD_ALREADY_EXISTS',
    MfaMethodNotFound: 'AUTH.USER_ACCOUNT.MFA_METHOD_NOT_FOUND',
    TotpCodeInvalid: 'AUTH.USER_ACCOUNT.TOTP_CODE_INVALID',
    BackupCodeInvalid: 'AUTH.USER_ACCOUNT.BACKUP_CODE_INVALID',
    NoConfirmedMfaMethod: 'AUTH.USER_ACCOUNT.NO_CONFIRMED_MFA_METHOD',
  },

  MfaSession: {
    NotFound: 'AUTH.MFA_SESSION.NOT_FOUND',
    AlreadyCompleted: 'AUTH.MFA_SESSION.ALREADY_COMPLETED',
    Expired: 'AUTH.MFA_SESSION.EXPIRED',
    FactorAlreadyUsed: 'AUTH.MFA_SESSION.FACTOR_ALREADY_USED',
    PrimaryFactorRequired: 'AUTH.MFA_SESSION.PRIMARY_FACTOR_REQUIRED',
  },
} as const
