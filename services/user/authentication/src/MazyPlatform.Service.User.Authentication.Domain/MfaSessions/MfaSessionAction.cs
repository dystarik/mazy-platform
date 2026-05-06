namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions;

/// <summary>
/// Целевое действие, для защиты которого создаётся MFA-сессия.
/// </summary>
/// <remarks>
/// Влияет на количество требуемых факторов: высокорисковые действия (например,
/// <see cref="GenerateBackupCodes"/>, <see cref="DeleteMfaMethod"/>, <see cref="ResetPassword"/>)
/// требуют двух факторов, если доступны минимум два метода MFA.
/// </remarks>
/// <seealso cref="MfaSession"/>
public enum MfaSessionAction
{
    /// <summary>
    /// Вход в систему с паролем при наличии настроенного MFA.
    /// </summary>
    Login = 1,

    /// <summary>
    /// Генерация нового набора резервных кодов MFA (высокорисковое действие).
    /// </summary>
    GenerateBackupCodes = 2,

    /// <summary>
    /// Смена пароля при наличии настроенного MFA.
    /// </summary>
    ChangePassword = 3,

    /// <summary>
    /// Удаление метода MFA (высокорисковое действие).
    /// </summary>
    DeleteMfaMethod = 4,

    /// <summary>
    /// Сброс пароля при наличии настроенного MFA (высокорисковое действие).
    /// </summary>
    ResetPassword = 5,
}
