namespace MazyPlatform.Service.User.Authentication.Domain.UserAccounts;

using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Events;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.LinkedProviders;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Агрегат учётной записи пользователя сервиса аутентификации.
/// </summary>
/// <remarks>
/// Является корнем агрегата. Управляет email, паролем, верификацией email
/// и настройками многофакторной аутентификации через <see cref="MfaSettings"/>,
/// а также привязками внешних провайдеров через <see cref="UserLinkedProviders"/>.
/// Создание нового аккаунта возможно через <see cref="RegisterByPassword"/>
/// и <see cref="RegisterByExternalProvider"/>.
/// </remarks>
/// <seealso cref="MfaSettings"/>
/// <seealso cref="UserLinkedProviders"/>
/// <seealso cref="IUserAccountRepository"/>
public sealed class UserAccount : AggregateRoot
{
    private UserAccount() { }

    /// <summary>
    /// Настройки многофакторной аутентификации, привязанные к данному аккаунту.
    /// </summary>
    public required MfaSettings MfaSettings { get; init; }

    /// <summary>
    /// Набор привязанных внешних провайдеров аккаунта.
    /// </summary>
    public required UserLinkedProviders UserLinkedProviders { get; init; }

    /// <summary>
    /// Email-адрес аккаунта в нижнем регистре.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Временная метка UTC подтверждения email.
    /// Равна <see langword="null"/>, если email ещё не подтверждён.
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если <see cref="EmailVerifiedAt"/> не равно <see langword="null"/>.
    /// </summary>
    public bool IsEmailVerified => EmailVerifiedAt is not null;

    /// <summary>
    /// Хэш пароля пользователя.
    /// Равен <see langword="null"/>, если пароль не установлен.
    /// </summary>
    public PasswordHash? PasswordHash { get; private set; }

    /// <summary>
    /// Временная метка UTC последней установки или смены пароля.
    /// Равна <see langword="null"/>, если пароль никогда не устанавливался.
    /// </summary>
    public DateTimeOffset? PasswordSetAt { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если <see cref="PasswordHash"/> не равен <see langword="null"/>.
    /// </summary>
    public bool HasPassword => PasswordHash is not null;

    /// <summary>
    /// Создаёт новый аккаунт через регистрацию по паролю.
    /// </summary>
    /// <param name="email">Валидированный email нового пользователя.</param>
    /// <param name="passwordHash">Хэш пароля, вычисленный через <see cref="IPasswordHasher"/>.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// Новый <see cref="UserAccount"/> с поднятым <see cref="UserAccountRegisteredByPasswordDomainEvent"/>
    /// и инициализированными пустыми <see cref="MfaSettings"/> и <see cref="UserLinkedProviders"/>.
    /// </returns>
    public static UserAccount RegisterByPassword(Email email, PasswordHash passwordHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);

        var id = Guid.NewGuid();
        var userAccount = new UserAccount
        {
            Id = id,
            Email = email,
            PasswordHash = passwordHash,
            PasswordSetAt = now,
            CreatedAt = now,
            MfaSettings = MfaSettings.Create(id, now),
            UserLinkedProviders = UserLinkedProviders.Create(id, now),
        };

        userAccount.AddDomainEvent(new UserAccountRegisteredByPasswordDomainEvent(now, userAccount.Id, userAccount.Email));
        return userAccount;
    }

    /// <summary>
    /// Создаёт новый аккаунт через внешний провайдер и сразу привязывает его.
    /// </summary>
    /// <param name="email">Валидированный email пользователя.</param>
    /// <param name="provider">Value Object внешнего провайдера (тип + email), через который выполняется регистрация.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// Новый <see cref="UserAccount"/> с привязанным провайдером
    /// и поднятым <see cref="UserAccountRegisteredByExternalProviderDomainEvent"/>.
    /// </returns>
    public static UserAccount RegisterByExternalProvider(Email email, ExternalProvider provider, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(email);

        var id = Guid.NewGuid();
        var userAccount = new UserAccount
        {
            Id = id,
            Email = email,
            EmailVerifiedAt = now,
            CreatedAt = now,
            MfaSettings = MfaSettings.Create(id, now),
            UserLinkedProviders = UserLinkedProviders.Create(id, now),
        };

        userAccount.UserLinkedProviders.LinkProvider(provider, now);
        userAccount.AddDomainEvent(new UserAccountRegisteredByExternalProviderDomainEvent(now, userAccount.Id, userAccount.Email, provider.Type));
        userAccount.AddDomainEvent(new UserAccountEmailVerifiedDomainEvent(now, userAccount.Id, userAccount.Email));
        return userAccount;
    }

    /// <summary>
    /// Добавляет новый метод многофакторной аутентификации.
    /// </summary>
    /// <param name="payload">Полезная нагрузка добавляемого MFA-метода (TOTP или Email).</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see langword="bool"/>, указывающим требуется ли верификация:
    /// <see langword="true"/> — требуется подтверждение (TOTP или email отличается от текущего);
    /// <see langword="false"/> — метод добавлен без подтверждения (email совпадает с текущим).
    /// Сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodAlreadyExists"/>, если подтверждённый метод уже существует.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountMfaMethodAddedDomainEvent"/> при успехе.
    /// Если метод уже добавлен, но не подтверждён, он заменяется новым.
    /// </remarks>
    public Result<bool> AddMfaMethod(IMfaPayload payload, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var addMfaMethodR = MfaSettings.AddMfaMethod(payload, now);
        if (addMfaMethodR.IsFailure)
            return addMfaMethodR.Errors;

        MarkAsUpdated(now);
        AddDomainEvent(new UserAccountMfaMethodAddedDomainEvent(now, Id, Email, payload.Type));

        return payload is not EmailPayload emailPayload || emailPayload.Email != Email;
    }

    /// <summary>
    /// Удаляет метод многофакторной аутентификации указанного типа.
    /// </summary>
    /// <param name="type">Тип удаляемого MFA-метода.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если метод удалён;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodNotFound"/>, если метод не существует.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountMfaMethodRemovedDomainEvent"/> при успехе.
    /// </remarks>
    public Result RemoveMfaMethod(MfaMethodType type, DateTimeOffset now)
    {
        var removeMfaMethodR = MfaSettings.RemoveMfaMethod(type, now);
        return CompleteMutation(removeMfaMethodR, now, new UserAccountMfaMethodRemovedDomainEvent(now, Id, Email, type));
    }

    /// <summary>
    /// Подтверждает метод многофакторной аутентификации и генерирует резервные коды, если это первый подтверждённый метод.
    /// </summary>
    /// <param name="type">Тип подтверждаемого MFA-метода.</param>
    /// <param name="hasher">Хэшер для шифрования сгенерированных резервных кодов.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с коллекцией открытых <see cref="BackupCode"/> при первом подтверждённом методе;
    /// пустая коллекция, если хотя бы один метод уже был подтверждён ранее;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.MfaMethodNotFound"/>, если метод не существует.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountMfaMethodConfirmedDomainEvent"/> при успехе.
    /// Открытые коды возвращаются единственный раз и должны быть сразу переданы пользователю.
    /// </remarks>
    public Result<IReadOnlyCollection<BackupCode>> ConfirmMfaMethod(MfaMethodType type, IBackupCodeHasher hasher, DateTimeOffset now)
    {
        var confirmMfaMethodR = MfaSettings.ConfirmMfaMethod(type, hasher, now);
        return CompleteMutation(confirmMfaMethodR, now, new UserAccountMfaMethodConfirmedDomainEvent(now, Id, Email, type));
    }

    /// <summary>
    /// Проверяет и списывает резервный код MFA.
    /// </summary>
    /// <param name="code">Открытый резервный код, введённый пользователем.</param>
    /// <param name="hasher">Хэшер для верификации кода по сохранённым хэшам.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если код найден и списан;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.BackupCodeInvalid"/>, если код недействителен.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountBackupCodeUsedDomainEvent"/> при успехе.
    /// Каждый код одноразовый и удаляется после использования.
    /// </remarks>
    public Result UseBackupCode(BackupCode code, IBackupCodeHasher hasher, DateTimeOffset now)
    {
        var useBackupCodeR = MfaSettings.UseBackupCode(code, hasher, now);
        return CompleteMutation(useBackupCodeR, now, new UserAccountBackupCodeUsedDomainEvent(now, Id, Email));
    }

    /// <summary>
    /// Генерирует новый набор резервных кодов MFA, заменяя старые.
    /// </summary>
    /// <param name="hasher">Хэшер для шифрования новых резервных кодов.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с коллекцией из 10 открытых <see cref="BackupCode"/> при успехе;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.NoConfirmedMfaMethod"/>, если нет подтверждённых методов MFA.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountBackupCodesGeneratedDomainEvent"/> при успехе.
    /// Все ранее сохранённые резервные коды аннулируются.
    /// </remarks>
    public Result<IReadOnlyCollection<BackupCode>> GenerateNewBackupCodes(IBackupCodeHasher hasher, DateTimeOffset now)
    {
        var generateNewBackupCodesR = MfaSettings.GenerateNewBackupCodes(hasher, now);
        return CompleteMutation(generateNewBackupCodesR, now, new UserAccountBackupCodesGeneratedDomainEvent(now, Id, Email));
    }

    /// <summary>
    /// Изменяет пароль пользователя.
    /// </summary>
    /// <param name="newPasswordHash">Хэш нового пароля, вычисленный через <see cref="IPasswordHasher"/>.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом — всегда, если <paramref name="newPasswordHash"/> не равен <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountPasswordChangedDomainEvent"/> при успехе.
    /// Обновляет <see cref="PasswordSetAt"/> на текущее значение <paramref name="now"/>.
    /// </remarks>
    public Result ChangePassword(PasswordHash newPasswordHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newPasswordHash);

        PasswordHash = newPasswordHash;
        PasswordSetAt = now;
        MarkAsUpdated(now);
        AddDomainEvent(new UserAccountPasswordChangedDomainEvent(now, Id, Email));

        return Result.Success();
    }

    /// <summary>
    /// Устанавливает первый локальный пароль пользователя.
    /// </summary>
    /// <param name="newPasswordHash">Хэш нового пароля, вычисленный через <see cref="IPasswordHasher"/>.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если у аккаунта ещё нет локального пароля;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.PasswordAlreadySet"/>, если пароль уже установлен.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountPasswordSetDomainEvent"/> при успехе.
    /// Обновляет <see cref="PasswordSetAt"/> на текущее значение <paramref name="now"/>.
    /// </remarks>
    public Result SetPassword(PasswordHash newPasswordHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newPasswordHash);

        if (HasPassword)
            return Error.Validation(ErrorCodes.Auth.UserAccount.PasswordAlreadySet, "Локальный пароль уже установлен. Используйте смену пароля.");

        PasswordHash = newPasswordHash;
        PasswordSetAt = now;

        return CompleteMutation(Result.Success(), now, new UserAccountPasswordSetDomainEvent(now, Id, Email));
    }

    /// <summary>
    /// Привязывает к аккаунту новый внешний провайдер.
    /// </summary>
    /// <param name="provider">Value Object внешнего провайдера аутентификации.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если провайдер привязан;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.ExternalProviderAlreadyLinked"/>, если провайдер уже привязан.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountExternalProviderAddedDomainEvent"/> при успехе.
    /// </remarks>
    public Result LinkProvider(ExternalProvider provider, DateTimeOffset now)
    {
        var addExternalProviderR = UserLinkedProviders.LinkProvider(provider, now);
        return CompleteMutation(addExternalProviderR, now, new UserAccountExternalProviderAddedDomainEvent(now, Id, Email, provider.Type));
    }

    /// <summary>
    /// Отвязывает внешний провайдер от аккаунта.
    /// </summary>
    /// <param name="type">Тип внешнего провайдера аутентификации.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если провайдер отвязан;
    /// сбой с кодом <see cref="ErrorCodes.Auth.UserAccount.ExternalProviderNotLinked"/>, если провайдер не был привязан.
    /// </returns>
    /// <remarks>
    /// Поднимает <see cref="UserAccountExternalProviderRemovedDomainEvent"/> при успехе.
    /// </remarks>
    public Result UnlinkProvider(ExternalProviderType type, DateTimeOffset now)
    {
        if (UserLinkedProviders.LinkedProviders.Count == 1 && UserLinkedProviders.LinkedProviders.Any(x => x.Type == type) && !HasPassword)
            return Error.Validation(ErrorCodes.Auth.UserAccount.CannotUnlinkLastAuthenticationMethod, "Невозможно отвязать последний метод аутентификации. Установите пароль или привяжите другой внешний провайдер перед отвязкой.");

        var removeExternalProviderR = UserLinkedProviders.UnlinkProvider(type, now);
        return CompleteMutation(removeExternalProviderR, now, new UserAccountExternalProviderRemovedDomainEvent(now, Id, Email, type));
    }

    /// <summary>
    /// Проверяет, привязан ли указанный внешний провайдер к аккаунту.
    /// </summary>
    /// <param name="provider">Value Object внешнего провайдера аутентификации.</param>
    /// <returns><see langword="true"/>, если провайдер присутствует в привязках; иначе <see langword="false"/>.</returns>
    public bool IsLinked(ExternalProvider provider)
    {
        return UserLinkedProviders.LinkedProviders.Contains(provider);
    }

    /// <summary>
    /// Помечает email аккаунта как подтверждённый.
    /// </summary>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <remarks>
    /// Идемпотентный метод: если email уже подтверждён — вызов игнорируется.
    /// Поднимает <see cref="UserAccountEmailVerifiedDomainEvent"/> при первом подтверждении.
    /// Доступен только внутри сборки домена; вызывается из <see cref="CompleteRegistrationService"/>.
    /// </remarks>
    internal void VerifyEmail(DateTimeOffset now)
    {
        if (IsEmailVerified)
            return;

        EmailVerifiedAt = now;
        MarkAsUpdated(now);
        AddDomainEvent(new UserAccountEmailVerifiedDomainEvent(now, Id, Email));
    }

    private Result CompleteMutation(Result result, DateTimeOffset now, DomainEventBase @event)
    {
        if (result.IsFailure)
            return result;

        MarkAsUpdated(now);
        AddDomainEvent(@event);
        return result;
    }

    private Result<T> CompleteMutation<T>(Result<T> result, DateTimeOffset now, DomainEventBase @event)
        where T : notnull
    {
        if (result.IsFailure)
            return result;

        MarkAsUpdated(now);
        AddDomainEvent(@event);
        return result;
    }
}
