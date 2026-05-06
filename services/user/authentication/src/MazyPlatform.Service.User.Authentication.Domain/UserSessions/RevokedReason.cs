namespace MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Причина аннулирования пользовательской сессии.
/// </summary>
/// <seealso cref="UserSession"/>
public enum RevokedReason
{
    /// <summary>
    /// Пользователь явно вышел из системы.
    /// </summary>
    Logout = 1,

    /// <summary>
    /// Сессия аннулирована из-за смены пароля пользователем.
    /// </summary>
    PasswordChanged = 2,
}
