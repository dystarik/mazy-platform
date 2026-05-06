namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Результат выполнения <see cref="LoginByPasswordService.ExecuteAsync"/>.
/// </summary>
/// <remarks>
/// Дискриминированный union: либо требуется прохождение MFA (<see cref="RequiresMfa"/> равно <see langword="true"/>),
/// либо вход завершён успешно (<see cref="RequiresMfa"/> равно <see langword="false"/>).
/// Используйте <see cref="RequiresMfa"/> перед обращением к <see cref="MfaRequiredData"/> или <see cref="SuccessData"/>.
/// </remarks>
public sealed record LoginByPasswordOutput
{
    /// <summary>
    /// Инициализирует результат с требованием прохождения MFA.
    /// </summary>
    /// <param name="mfaSession">Новая MFA-сессия для входа.</param>
    /// <param name="availableFactor">Список доступных типов MFA-методов у аккаунта.</param>
    public LoginByPasswordOutput(MfaSession mfaSession, IReadOnlyCollection<MfaMethodType> availableFactor)
    {
        ArgumentNullException.ThrowIfNull(mfaSession);
        RequiresMfa = true;
        MfaRequiredData = new MfaRequired(mfaSession, availableFactor);
    }

    /// <summary>
    /// Инициализирует результат успешного входа без MFA или с уже пройденным MFA.
    /// </summary>
    /// <param name="userAccount">Аутентифицированный аккаунт.</param>
    /// <param name="userSession">Новая сессия пользователя.</param>
    /// <param name="refreshToken">Открытый токен обновления для передачи клиенту.</param>
    public LoginByPasswordOutput(UserAccount userAccount, UserSession userSession, string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(userSession);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        RequiresMfa = false;
        SuccessData = new Success(userAccount, userSession, refreshToken);
    }

    /// <summary>
    /// Если <see langword="true"/> — требуется прохождение MFA, <see cref="MfaRequiredData"/> не равно <see langword="null"/>.
    /// Если <see langword="false"/> — вход завершён, <see cref="SuccessData"/> не равно <see langword="null"/>.
    /// </summary>
    [MemberNotNullWhen(false, nameof(SuccessData))]
    [MemberNotNullWhen(true, nameof(MfaRequiredData))]
    public bool RequiresMfa { get; init; }

    /// <summary>
    /// Данные для MFA-шага входа. Не равно <see langword="null"/> только при <see cref="RequiresMfa"/> равном <see langword="true"/>.
    /// </summary>
    public MfaRequired? MfaRequiredData { get; init; }

    /// <summary>
    /// Данные успешного входа. Не равно <see langword="null"/> только при <see cref="RequiresMfa"/> равном <see langword="false"/>.
    /// </summary>
    public Success? SuccessData { get; init; }

    /// <summary>
    /// Данные о необходимости прохождения MFA: созданная сессия и доступные методы.
    /// </summary>
    /// <param name="MfaSession">Новая MFA-сессия с действием <see cref="MfaSessionAction.Login"/>.</param>
    /// <param name="AvailableFactor">Доступные типы MFA-методов для отображения пользователю.</param>
    public sealed record MfaRequired(MfaSession MfaSession, IReadOnlyCollection<MfaMethodType> AvailableFactor);

    /// <summary>
    /// Данные успешного входа: аккаунт, сессия и токен обновления.
    /// </summary>
    /// <param name="UserAccount">Аутентифицированный аккаунт.</param>
    /// <param name="UserSession">Новая сессия для сохранения в базе.</param>
    /// <param name="RefreshToken">Открытый токен обновления для передачи клиенту.</param>
    public sealed record Success(UserAccount UserAccount, UserSession UserSession, string RefreshToken);
}
