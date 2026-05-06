namespace MazyPlatform.Service.User.Authentication.Domain.MfaSessions;

using MazyPlatform.Service.User.Authentication.Domain.MfaSessions.Events;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

/// <summary>
/// Агрегат временной сессии для прохождения многофакторной аутентификации.
/// </summary>
/// <remarks>
/// Создаётся при необходимости подтвердить высокорисковое действие через MFA.
/// Хранит список пройденных факторов и считается завершённой, когда набрано
/// требуемое количество: <see cref="IsCompleted"/>.
/// После завершения действует в течение 30 минут (<see cref="ValidUntil"/>).
/// Срок ожидания прохождения — 5 минут (<see cref="ExpiresAt"/>).
/// </remarks>
/// <seealso cref="MfaSessionAction"/>
/// <seealso cref="IMfaSessionRepository"/>
public sealed class MfaSession : AggregateRoot
{
    private readonly List<MfaMethodType> _completedFactors = [];

    private MfaSession() { }

    /// <summary>
    /// Идентификатор аккаунта, для которого создана сессия.
    /// </summary>
    public required Guid UserAccountId { get; init; }

    /// <summary>
    /// Целевое действие, которое защищает данная сессия.
    /// </summary>
    public required MfaSessionAction Action { get; init; }

    /// <summary>
    /// Требуемое количество пройденных факторов для завершения сессии.
    /// Зависит от <see cref="Action"/> и количества доступных методов MFA.
    /// </summary>
    public required int RequiredFactorCount { get; init; }

    /// <summary>
    /// Временная метка UTC истечения периода прохождения MFA (5 минут с момента создания).
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Временная метка UTC, до которой завершённая сессия действительна (30 минут с момента завершения).
    /// Равна <see langword="null"/>, если сессия ещё не завершена.
    /// </summary>
    public DateTimeOffset? ValidUntil { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если сессия завершена через резервный код.
    /// </summary>
    public bool IsCompletedByBackupCode { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если набрано достаточное количество факторов
    /// или сессия завершена через резервный код.
    /// </summary>
    public bool IsCompleted => _completedFactors.Count >= RequiredFactorCount || IsCompletedByBackupCode;

    /// <summary>
    /// Типы факторов MFA, успешно пройденных в текущей сессии.
    /// </summary>
    public IReadOnlyCollection<MfaMethodType> CompletedFactors => _completedFactors.AsReadOnly();

    /// <summary>
    /// Создаёт новую MFA-сессию для указанного аккаунта и действия.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта, проходящего MFA.</param>
    /// <param name="action">Целевое действие, для которого требуется MFA.</param>
    /// <param name="availableMethods">Типы доступных методов MFA у аккаунта.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// Новая <see cref="MfaSession"/> с поднятым <see cref="MfaSessionCreatedDomainEvent"/>.
    /// Срок ожидания — 5 минут.
    /// </returns>
    public static MfaSession Create(Guid userAccountId, MfaSessionAction action, IReadOnlyCollection<MfaMethodType> availableMethods, DateTimeOffset now)
    {
        var mfaSession = new MfaSession
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            Action = action,
            RequiredFactorCount = GetRequiredFactorCount(action, availableMethods),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(5),
        };
        mfaSession.AddDomainEvent(new MfaSessionCreatedDomainEvent(now, userAccountId, action));

        return mfaSession;
    }

    /// <summary>
    /// Фиксирует прохождение одного фактора MFA.
    /// </summary>
    /// <param name="type">Тип пройденного фактора.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если фактор зачтён;
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.AlreadyCompleted"/>, если сессия уже завершена;
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.Expired"/>, если сессия истекла;
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.FactorAlreadyUsed"/>, если данный тип уже использован.
    /// </returns>
    /// <remarks>
    /// При наборе <see cref="RequiredFactorCount"/> факторов поднимает <see cref="MfaSessionCompletedDomainEvent"/>
    /// и устанавливает <see cref="ValidUntil"/> в 30 минут от <paramref name="now"/>.
    /// </remarks>
    internal Result CompleteWithFactor(MfaMethodType type, DateTimeOffset now)
    {
        var basicValidationR = BasicValidate(now);
        if (basicValidationR.IsFailure)
            return basicValidationR;

        if (_completedFactors.Exists(t => t == type))
            return Error.Validation(ErrorCodes.Auth.MfaSession.FactorAlreadyUsed, "Указанный метод MFA уже использован.");

        _completedFactors.Add(type);
        MarkAsUpdated(now);

        if (!IsCompleted)
            return Result.Success();

        ValidUntil = now.AddMinutes(30);
        AddDomainEvent(new MfaSessionCompletedDomainEvent(now, UserAccountId, Action));
        return Result.Success();
    }

    /// <summary>
    /// Завершает сессию через резервный код.
    /// </summary>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если сессия завершена;
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.AlreadyCompleted"/>, если сессия уже завершена;
    /// сбой с кодом <see cref="ErrorCodes.Auth.MfaSession.Expired"/>, если сессия истекла.
    /// </returns>
    /// <remarks>
    /// Устанавливает <see cref="IsCompletedByBackupCode"/> в <see langword="true"/>
    /// и поднимает <see cref="MfaSessionCompletedDomainEvent"/> с флагом <c>isCompletedByBackupCode: true</c>.
    /// Операции с высоким требованием безопасности (например, <see cref="RegenerateBackupCodesService"/>)
    /// запрещают использование сессий, завершённых через резервный код.
    /// </remarks>
    internal Result CompleteWithBackupCode(DateTimeOffset now)
    {
        var basicValidationR = BasicValidate(now);
        if (basicValidationR.IsFailure)
            return basicValidationR;

        ValidUntil = now.AddMinutes(30);
        IsCompletedByBackupCode = true;
        MarkAsUpdated(now);
        AddDomainEvent(new MfaSessionCompletedDomainEvent(now, UserAccountId, Action, isCompletedByBackupCode: true));

        return Result.Success();
    }

    private static int GetRequiredFactorCount(MfaSessionAction action, IReadOnlyCollection<MfaMethodType> completedFactors)
    {
        var risk = action switch
        {
            MfaSessionAction.Login => 1,
            MfaSessionAction.GenerateBackupCodes => 2,
            MfaSessionAction.ChangePassword => 1,
            MfaSessionAction.DeleteMfaMethod => 2,
            MfaSessionAction.ResetPassword => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(action), $"Неизвестное действие для MFA сессии: {action}"),
        };

        return Math.Min(risk, completedFactors.Count);
    }

    private Result BasicValidate(DateTimeOffset now)
    {
        if (IsCompleted)
            return Error.Validation(ErrorCodes.Auth.MfaSession.AlreadyCompleted, "MFA сессия уже завершена.");

        if (ExpiresAt > now) return Result.Success();

        AddDomainEvent(new MfaSessionExpiredDomainEvent(now, UserAccountId, Action));
        return Error.Unauthorized(ErrorCodes.Auth.MfaSession.Expired, "MFA-сессия истекла.");
    }
}
