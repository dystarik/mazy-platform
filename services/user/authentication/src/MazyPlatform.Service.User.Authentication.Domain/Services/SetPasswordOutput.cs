namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Результат выполнения <see cref="SetPasswordService.ExecuteAsync"/>.
/// </summary>
/// <remarks>
/// Дискриминированный union: либо требуется прохождение MFA, либо пароль уже установлен.
/// </remarks>
public sealed record SetPasswordOutput
{
    /// <summary>
    /// Инициализирует результат с требованием прохождения MFA перед установкой пароля.
    /// </summary>
    /// <param name="mfaSession">Новая MFA-сессия с действием <see cref="MfaSessionAction.SetPassword"/>.</param>
    /// <param name="availableFactors">Доступные типы MFA-методов у аккаунта.</param>
    public SetPasswordOutput(MfaSession mfaSession, IReadOnlyCollection<MfaMethodType> availableFactors)
    {
        ArgumentNullException.ThrowIfNull(mfaSession);

        RequiresMfa = true;
        MfaRequiredData = new MfaRequired(mfaSession, availableFactors);
    }

    /// <summary>
    /// Инициализирует результат успешной установки пароля.
    /// </summary>
    public SetPasswordOutput()
    {
        RequiresMfa = false;
    }

    /// <summary>
    /// Если <see langword="true"/> — требуется прохождение MFA, <see cref="MfaRequiredData"/> не равно <see langword="null"/>.
    /// Если <see langword="false"/> — пароль успешно установлен.
    /// </summary>
    [MemberNotNullWhen(true, nameof(MfaRequiredData))]
    public bool RequiresMfa { get; init; }

    /// <summary>
    /// Данные для MFA-шага. Не равно <see langword="null"/> только при <see cref="RequiresMfa"/> равном <see langword="true"/>.
    /// </summary>
    public MfaRequired? MfaRequiredData { get; init; }

    /// <summary>
    /// Данные о необходимости прохождения MFA перед установкой пароля.
    /// </summary>
    /// <param name="MfaSession">Новая MFA-сессия с действием <see cref="MfaSessionAction.SetPassword"/>.</param>
    /// <param name="AvailableFactors">Доступные типы MFA-методов для отображения пользователю.</param>
    public sealed record MfaRequired(MfaSession MfaSession, IReadOnlyCollection<MfaMethodType> AvailableFactors);
}
