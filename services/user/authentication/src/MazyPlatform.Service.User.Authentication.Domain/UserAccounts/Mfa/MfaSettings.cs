namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

/// <summary>
/// Сущность, агрегирующая все настройки многофакторной аутентификации аккаунта.
/// </summary>
/// <remarks>
/// Дочерняя сущность агрегата <see cref="UserAccount"/>. Управляет жизненным циклом
/// методов MFA (<see cref="MfaMethod"/>) и резервных кодов (<see cref="BackupCodeHash"/>).
/// Мутирующие методы доступны только внутри сборки домена и вызываются через <see cref="UserAccount"/>.
/// Фабрика <see cref="Create"/> вызывается только из <see cref="UserAccount.RegisterByPassword"/>.
/// </remarks>
/// <seealso cref="MfaMethod"/>
/// <seealso cref="BackupCode"/>
public sealed class MfaSettings : Entity
{
    private readonly List<BackupCodeHash> _backupCodes = [];

    private readonly List<MfaMethod> _mfaMethods = [];

    private MfaSettings() { }

    /// <summary>
    /// Идентификатор аккаунта, которому принадлежат данные настройки.
    /// </summary>
    public required Guid UserAccountId { get; init; }

    /// <summary>
    /// Хэши активных резервных кодов. Становится непустым после первого подтверждения метода MFA.
    /// </summary>
    public IReadOnlyCollection<BackupCodeHash> BackupCodes => _backupCodes.AsReadOnly();

    /// <summary>
    /// Все зарегистрированные методы MFA (подтверждённые и ожидающие подтверждения).
    /// </summary>
    public IReadOnlyCollection<MfaMethod> MfaMethods => _mfaMethods.AsReadOnly();

    /// <summary>
    /// Типы всех зарегистрированных методов MFA.
    /// </summary>
    public IReadOnlyCollection<MfaMethodType> AvailableMfaMethodTypes => [.. _mfaMethods.Where(mfa => mfa.IsConfirmed).Select(mfa => mfa.Type)];

    /// <summary>
    /// Полезная нагрузка TOTP-метода, если он добавлен; иначе <see langword="null"/>.
    /// </summary>
    public TotpPayload? TotpMethod => _mfaMethods.SingleOrDefault(mfa => mfa.Type is MfaMethodType.Totp)?.Payload as TotpPayload;

    /// <summary>
    /// Полезная нагрузка Email-метода, если он добавлен; иначе <see langword="null"/>.
    /// </summary>
    public EmailPayload? EmailMethod => _mfaMethods.SingleOrDefault(mfa => mfa.Type is MfaMethodType.Email)?.Payload as EmailPayload;

    /// <summary>
    /// Возвращает <see langword="true"/>, если хотя бы один метод MFA находится в статусе подтверждённого.
    /// </summary>
    public bool HasConfirmedMfaMethod => _mfaMethods.Exists(mfa => mfa.IsConfirmed);

    /// <summary>
    /// Создаёт новые пустые настройки MFA для заданного аккаунта.
    /// </summary>
    /// <param name="userAccountId">Идентификатор родительского <see cref="UserAccount"/>.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>Новый экземпляр <see cref="MfaSettings"/> без методов и резервных кодов.</returns>
    public static MfaSettings Create(Guid userAccountId, DateTimeOffset now)
    {
        return new MfaSettings
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Добавляет или заменяет метод MFA.
    /// </summary>
    /// <param name="payload">Полезная нагрузка нового метода.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если метод добавлен;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodAlreadyExists"/>, если подтверждённый метод данного типа уже существует.
    /// </returns>
    /// <remarks>
    /// Если существует неподтверждённый метод того же типа, он заменяется новым.
    /// </remarks>
    internal Result AddMfaMethod(IMfaPayload payload, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var existingMethod = _mfaMethods.Find(mfa => mfa.Type == payload.Type);
        if (existingMethod is not null)
        {
            if (existingMethod.IsConfirmed)
                return Error.Conflict(ErrorCodes.Auth.UserAccount.MfaMethodAlreadyExists, "Указанный метод MFA уже добавлен.");

            _mfaMethods.Remove(existingMethod);
        }

        _mfaMethods.Add(MfaMethod.Create(Id, payload, now));
        MarkAsUpdated(now);

        return Result.Success();
    }

    /// <summary>
    /// Удаляет метод MFA указанного типа.
    /// </summary>
    /// <param name="type">Тип удаляемого метода.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если метод удалён;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodNotFound"/>, если метод не найден.
    /// </returns>
    internal Result RemoveMfaMethod(MfaMethodType type, DateTimeOffset now)
    {
        var mfaMethod = _mfaMethods.Find(mfa => mfa.Type == type);

        if (mfaMethod is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");

        _mfaMethods.Remove(mfaMethod);
        MarkAsUpdated(now);

        return Result.Success();
    }

    /// <summary>
    /// Подтверждает метод MFA и при необходимости генерирует новые резервные коды.
    /// </summary>
    /// <param name="type">Тип подтверждаемого метода.</param>
    /// <param name="hasher">Хэшер резервных кодов.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с набором открытых <see cref="BackupCode"/> при первом подтверждённом методе;
    /// пустая коллекция, если ранее уже был подтверждён хотя бы один метод;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodNotFound"/>, если метод не найден.
    /// </returns>
    internal Result<IReadOnlyCollection<BackupCode>> ConfirmMfaMethod(MfaMethodType type, IBackupCodeHasher hasher, DateTimeOffset now)
    {
        var mfaMethod = _mfaMethods.Find(mfa => mfa.Type == type);

        if (mfaMethod is null)
            return Error.NotFound(ErrorCodes.Auth.UserAccount.MfaMethodNotFound, "Метод MFA не найден.");

        var hasConfirmedMfaMethod = HasConfirmedMfaMethod;
        mfaMethod.Confirm(now);
        MarkAsUpdated(now);

        if (hasConfirmedMfaMethod)
            return Result.Success<IReadOnlyCollection<BackupCode>>([]);

        var rawCodes = BackupCode.GenerateBackupCodes(10);
        _backupCodes.Clear();
        _backupCodes.AddRange(rawCodes.Select(c => BackupCodeHash.FromTrusted(hasher.Hash(c.Code))));

        return Result.Success(rawCodes);
    }

    /// <summary>
    /// Проверяет и списывает резервный код.
    /// </summary>
    /// <param name="code">Открытый резервный код.</param>
    /// <param name="hasher">Хэшер для верификации кода по сохранённым хэшам.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если код найден и удалён;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.BackupCodeInvalid"/>, если код не соответствует ни одному хэшу.
    /// </returns>
    internal Result UseBackupCode(BackupCode code, IBackupCodeHasher hasher, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(hasher);

        var backupCode = _backupCodes.SingleOrDefault(c => hasher.Verify(code.Code, c.Value));

        if (backupCode is null)
            return Error.Unauthorized(ErrorCodes.Auth.UserAccount.BackupCodeInvalid, "Неверный резервный код.");

        _backupCodes.Remove(backupCode);
        MarkAsUpdated(now);
        return Result.Success();
    }

    /// <summary>
    /// Генерирует новый набор из 10 резервных кодов, заменяя старые.
    /// </summary>
    /// <param name="hasher">Хэшер для шифрования новых кодов.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с 10 новыми открытыми <see cref="BackupCode"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod"/>, если нет ни одного подтверждённого метода MFA.
    /// </returns>
    internal Result<IReadOnlyCollection<BackupCode>> GenerateNewBackupCodes(IBackupCodeHasher hasher, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        if (!HasConfirmedMfaMethod)
            return Error.Conflict(ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod, "Нет подтверждённого MFA-метода.");

        var rawCodes = BackupCode.GenerateBackupCodes(10);
        _backupCodes.Clear();
        _backupCodes.AddRange(rawCodes.Select(c => BackupCodeHash.FromTrusted(hasher.Hash(c.Code))));
        MarkAsUpdated(now);
        return Result.Success(rawCodes);
    }
}
