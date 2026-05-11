namespace MazyPlatform.Service.User.Authentication.Domain.Shared;

/// <summary>
/// Централизованный реестр кодов ошибок домена аутентификации.
/// </summary>
/// <remarks>
/// Все строковые коды начинаются с префикса <c>AUTH.</c> и используются при создании
/// объектов <see cref="Error"/> внутри агрегатов и доменных сервисов.
/// Коды передаются в gRPC-ответы клиентам через слой API.
/// </remarks>
public sealed class ErrorCodes
{
    /// <summary>
    /// Коды ошибок, специфичные для сервиса аутентификации.
    /// </summary>
    public static class Auth
    {
        /// <summary>Внутренняя ошибка сервера.</summary>
        public const string InternalError = "AUTH.INTERNAL_ERROR";

        /// <summary>Запрос не авторизован (неверные учётные данные или истёкший токен).</summary>
        public const string Unauthorized = "AUTH.UNAUTHORIZED";

        /// <summary>Email зарегистрирован, но не подтверждён; ожидается подтверждение.</summary>
        public const string EmailVerificationPending = "AUTH.EMAIL_VERIFICATION_PENDING";

        /// <summary>
        /// Общие коды ошибок валидации входных данных.
        /// </summary>
        public static class Validation
        {
            /// <summary>Обязательное поле не заполнено.</summary>
            public const string Required = "AUTH.VALIDATION.REQUIRED";

            /// <summary>Значение не соответствует ожидаемому формату.</summary>
            public const string Invalid = "AUTH.VALIDATION.INVALID";
        }

        /// <summary>
        /// Коды ошибок объекта-значения <see cref="Email"/>.
        /// </summary>
        public static class Email
        {
            /// <summary>Email не передан.</summary>
            public const string Required = "AUTH.EMAIL.REQUIRED";

            /// <summary>Email короче <see cref="Email.MinLength"/> символов.</summary>
            public const string TooShort = "AUTH.EMAIL.TOO_SHORT";

            /// <summary>Email длиннее <see cref="Email.MaxLength"/> символов.</summary>
            public const string TooLong = "AUTH.EMAIL.TOO_LONG";

            /// <summary>Email не соответствует допустимому формату.</summary>
            public const string InvalidFormat = "AUTH.EMAIL.INVALID_FORMAT";
        }

        /// <summary>
        /// Коды ошибок объекта-значения <see cref="Password"/>.
        /// </summary>
        public static class Password
        {
            /// <summary>Пароль не передан.</summary>
            public const string Required = "AUTH.PASSWORD.REQUIRED";

            /// <summary>Пароль не соответствует требованиям безопасности.</summary>
            public const string Invalid = "AUTH.PASSWORD.INVALID";

            /// <summary>Пароль короче <see cref="Password.MinLength"/> символов.</summary>
            public const string TooShort = "AUTH.PASSWORD.TOO_SHORT";

            /// <summary>Пароль длиннее <see cref="Password.MaxLength"/> символов.</summary>
            public const string TooLong = "AUTH.PASSWORD.TOO_LONG";

            /// <summary>Пароль не содержит заглавных букв.</summary>
            public const string NoUpperCase = "AUTH.PASSWORD.NO_UPPER_CASE";

            /// <summary>Пароль не содержит строчных букв.</summary>
            public const string NoLowerCase = "AUTH.PASSWORD.NO_LOWER_CASE";

            /// <summary>Пароль не содержит цифр.</summary>
            public const string NoDigit = "AUTH.PASSWORD.NO_DIGIT";

            /// <summary>Пароль не содержит специальных символов.</summary>
            public const string NoSpecialChar = "AUTH.PASSWORD.NO_SPECIAL_CHAR";
        }

        /// <summary>
        /// Коды ошибок объекта-значения <see cref="PasswordHash"/>.
        /// </summary>
        public static class PasswordHash
        {
            /// <summary>Хэш пароля имеет недопустимый формат.</summary>
            public const string Invalid = "AUTH.PASSWORD_HASH.INVALID";
        }

        /// <summary>
        /// Коды ошибок объекта-значения <see cref="OtpCodeHash"/>.
        /// </summary>
        public static class OtpCodeHash
        {
            /// <summary>Хэш OTP-кода имеет недопустимый формат.</summary>
            public const string Invalid = "AUTH.OTP_CODE_HASH.INVALID";
        }

        /// <summary>
        /// Коды ошибок агрегата <see cref="OneTimePassword"/>.
        /// </summary>
        public static class OneTimePassword
        {
            /// <summary>Одноразовый пароль не найден.</summary>
            public const string NotFound = "AUTH.ONE_TIME_PASSWORD.NOT_FOUND";

            /// <summary>Срок действия одноразового пароля истёк.</summary>
            public const string Expired = "AUTH.ONE_TIME_PASSWORD.EXPIRED";

            /// <summary>Введён неверный код.</summary>
            public const string InvalidCode = "AUTH.ONE_TIME_PASSWORD.INVALID_CODE";

            /// <summary>Одноразовый пароль уже был использован.</summary>
            public const string AlreadyUsed = "AUTH.ONE_TIME_PASSWORD.ALREADY_USED";

            /// <summary>Одноразовый пароль был аннулирован.</summary>
            public const string Invalidated = "AUTH.ONE_TIME_PASSWORD.INVALIDATED";

            /// <summary>Превышено максимальное число попыток (<see cref="Domain.OneTimePasswords.OneTimePassword.MaxAttempts"/>).</summary>
            public const string TooManyAttempts = "AUTH.ONE_TIME_PASSWORD.TOO_MANY_ATTEMPTS";
        }

        /// <summary>
        /// Коды ошибок процесса регистрации.
        /// </summary>
        public static class Registration
        {
            /// <summary>Email уже занят подтверждённым аккаунтом.</summary>
            public const string EmailAlreadyInUse = "AUTH.REGISTRATION.EMAIL_ALREADY_IN_USE";

            /// <summary>Email уже используется как MFA email другого аккаунта.</summary>
            public const string EmailAlreadyUsedAsMfaEmail = "AUTH.REGISTRATION.EMAIL_ALREADY_USED_AS_MFA_EMAIL";
        }

        /// <summary>
        /// Коды ошибок валидации информации об устройстве пользователя.
        /// </summary>
        public static class DeviceInfo
        {
            /// <summary>IP-адрес имеет недопустимый формат.</summary>
            public const string InvalidIpAddress = "AUTH.DEVICE_INFO.INVALID_IP_ADDRESS";

            /// <summary>User-Agent превышает допустимую длину.</summary>
            public const string UserAgentTooLong = "AUTH.DEVICE_INFO.USER_AGENT_TOO_LONG";
        }

        /// <summary>
        /// Коды ошибок агрегата <see cref="UserAccount"/>.
        /// </summary>
        public static class UserAccount
        {
            /// <summary>Аккаунт пользователя не найден.</summary>
            public const string NotFound = "AUTH.USER_ACCOUNT.NOT_FOUND";

            /// <summary>Указанный внешний провайдер уже привязан к аккаунту.</summary>
            public const string ExternalProviderAlreadyLinked = "AUTH.USER_ACCOUNT.EXTERNAL_PROVIDER_ALREADY_LINKED";

            /// <summary>Указанный внешний провайдер не привязан к аккаунту.</summary>
            public const string ExternalProviderNotLinked = "AUTH.USER_ACCOUNT.EXTERNAL_PROVIDER_NOT_LINKED";

            /// <summary>Указанный метод MFA уже добавлен и подтверждён.</summary>
            public const string MfaMethodAlreadyExists = "AUTH.USER_ACCOUNT.MFA_METHOD_ALREADY_EXISTS";

            /// <summary>Указанный метод MFA не найден.</summary>
            public const string MfaMethodNotFound = "AUTH.USER_ACCOUNT.MFA_METHOD_NOT_FOUND";

            /// <summary>Введён неверный TOTP-код.</summary>
            public const string TotpCodeInvalid = "AUTH.USER_ACCOUNT.TOTP_CODE_INVALID";

            /// <summary>Введён неверный резервный код.</summary>
            public const string BackupCodeInvalid = "AUTH.USER_ACCOUNT.BACKUP_CODE_INVALID";

            /// <summary>Нет ни одного подтверждённого метода MFA; операция требует подтверждённого метода.</summary>
            public const string NoConfirmedMfaMethod = "AUTH.USER_ACCOUNT.NO_CONFIRMED_MFA_METHOD";

            /// <summary>Невозможно отвязать последний способ аутентификации; аккаунт должен иметь хотя бы один способ входа.</summary>
            public const string CannotUnlinkLastAuthenticationMethod = "AUTH.USER_ACCOUNT.CANNOT_UNLINK_LAST_AUTHENTICATION_METHOD";

            /// <summary>Локальный пароль уже установлен; для изменения нужно использовать flow смены пароля.</summary>
            public const string PasswordAlreadySet = "AUTH.USER_ACCOUNT.PASSWORD_ALREADY_SET";
        }

        /// <summary>
        /// Коды ошибок агрегата <see cref="MfaSession"/>.
        /// </summary>
        public static class MfaSession
        {
            /// <summary>MFA-сессия не найдена.</summary>
            public const string NotFound = "AUTH.MFA_SESSION.NOT_FOUND";

            /// <summary>MFA-сессия уже завершена.</summary>
            public const string AlreadyCompleted = "AUTH.MFA_SESSION.ALREADY_COMPLETED";

            /// <summary>Срок действия MFA-сессии истёк.</summary>
            public const string Expired = "AUTH.MFA_SESSION.EXPIRED";

            /// <summary>Указанный фактор MFA уже использован в этой сессии.</summary>
            public const string FactorAlreadyUsed = "AUTH.MFA_SESSION.FACTOR_ALREADY_USED";

            /// <summary>
            /// Операция требует прохождения основного фактора MFA;
            /// сессия была завершена только через резервный код.
            /// </summary>
            public const string PrimaryFactorRequired = "AUTH.MFA_SESSION.PRIMARY_FACTOR_REQUIRED";
        }
    }
}
