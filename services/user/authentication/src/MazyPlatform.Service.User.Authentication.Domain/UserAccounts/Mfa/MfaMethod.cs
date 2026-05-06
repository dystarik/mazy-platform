namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

/// <summary>
/// Сущность, представляющая один метод многофакторной аутентификации аккаунта.
/// </summary>
/// <remarks>
/// Дочерняя сущность <see cref="MfaSettings"/>. Создаётся через <see cref="Create"/>
/// и подтверждается через <see cref="Confirm"/>. Оба метода доступны только внутри сборки домена.
/// Тип метода определяется полезной нагрузкой <see cref="Payload"/>.
/// </remarks>
/// <seealso cref="IMfaPayload"/>
/// <seealso cref="MfaMethodType"/>
public sealed class MfaMethod : Entity
{
    private MfaMethod() { }

    /// <summary>
    /// Идентификатор родительских <see cref="MfaSettings"/>.
    /// </summary>
    public required Guid MfaSettingsId { get; init; }

    /// <summary>
    /// Полезная нагрузка, содержащая специфичные для метода данные (секрет TOTP или email).
    /// </summary>
    public required IMfaPayload Payload { get; init; }

    /// <summary>
    /// Тип метода MFA, определяется <see cref="IMfaPayload.Type"/>.
    /// </summary>
    public MfaMethodType Type => Payload.Type;

    /// <summary>
    /// Временная метка UTC подтверждения метода.
    /// Равна <see langword="null"/>, если метод ещё не подтверждён.
    /// </summary>
    public DateTimeOffset? ConfirmedAt { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если <see cref="ConfirmedAt"/> не равно <see langword="null"/>.
    /// </summary>
    public bool IsConfirmed => ConfirmedAt is not null;

    /// <summary>
    /// Создаёт новый неподтверждённый метод MFA.
    /// </summary>
    /// <param name="mfaSettingsId">Идентификатор родительских <see cref="MfaSettings"/>.</param>
    /// <param name="payload">Полезная нагрузка метода.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>Новый <see cref="MfaMethod"/> в статусе ожидания подтверждения.</returns>
    internal static MfaMethod Create(Guid mfaSettingsId, IMfaPayload payload, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new MfaMethod
        {
            Id = Guid.NewGuid(),
            MfaSettingsId = mfaSettingsId,
            Payload = payload,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Переводит метод в статус подтверждённого.
    /// </summary>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <remarks>
    /// Идемпотентный метод: повторный вызов для уже подтверждённого метода игнорируется.
    /// </remarks>
    internal void Confirm(DateTimeOffset now)
    {
        if (IsConfirmed)
            return;

        MarkAsUpdated(now);
        ConfirmedAt = now;
    }
}
