namespace MazyPlatform.Service.Bot.Manager.Domain.BotInstances;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Credentials;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.Events;
using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;

/// <summary>
/// Агрегатный корень «Экземпляр бота» — конфигурация бота в конкретном мессенджере,
/// привязанного к проекту и сценарию.
/// </summary>
public sealed class BotInstance : AggregateRoot
{
    private BotInstance() { }

    /// <summary>Идентификатор владельца (аккаунта пользователя).</summary>
    public required Guid OwnerAccountId { get; init; }

    /// <summary>Идентификатор проекта.</summary>
    public Guid? ProjectId { get; private set; }

    /// <summary>Отображаемое имя бота.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Учётные данные платформы (токен, идентификатор сообщества и т.д.).</summary>
    public IBotCredentials Credentials { get; private set; } = null!;

    /// <summary>Версия привязанного сценария.</summary>
    public int? ScenarioVersion { get; private set; }

    /// <summary>Текущий статус бота.</summary>
    public BotStatus Status { get; private set; }

    /// <summary>Тип платформы — вычисляется из <see cref="Credentials"/>.</summary>
    public PlatformType PlatformType => Credentials.PlatformType;

    /// <summary>
    /// Создаёт новый экземпляр бота со статусом <see cref="BotStatus.Inactive"/>
    /// и публикует <see cref="BotInstanceCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="ownerAccountId">Идентификатор владельца.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="name">Отображаемое имя бота.</param>
    /// <param name="credentials">Учётные данные платформы.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>Созданный агрегат экземпляра бота.</returns>
    public static BotInstance Create(
        Guid ownerAccountId,
        Guid? projectId,
        string name,
        IBotCredentials credentials,
        int? scenarioVersion,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var botInstance = new BotInstance
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = ownerAccountId,
            ProjectId = projectId,
            Name = name,
            Credentials = credentials,
            ScenarioVersion = scenarioVersion,
            Status = BotStatus.Inactive,
            CreatedAt = now,
        };

        botInstance.AddDomainEvent(new BotInstanceCreatedDomainEvent(now, botInstance.Id, projectId, ownerAccountId, credentials.PlatformType));

        return botInstance;
    }

    /// <summary>
    /// Активирует бота, делая его доступным для обработки сообщений.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>
    /// Успех, если активация прошла;
    /// сбой <see cref="ErrorCodes.BotInstance.AlreadyActive"/>, если бот уже активен;
    /// сбой <see cref="ErrorCodes.BotInstance.CannotActivateUnbound"/>, если бот не привязан к проекту или не задана версия сценария.
    /// </returns>
    public Result Activate(DateTimeOffset now)
    {
        if (Status is BotStatus.Active)
            return Error.Conflict(ErrorCodes.BotInstance.AlreadyActive, "Бот уже активен.");

        if (ProjectId is null || ScenarioVersion is null)
            return Error.Conflict(ErrorCodes.BotInstance.CannotActivateUnbound, "Невозможно активировать бота без привязки к проекту и версии сценария.");

        Status = BotStatus.Active;
        MarkAsUpdated(now);

        AddDomainEvent(new BotInstanceActivatedDomainEvent(
            now,
            Id,
            ProjectId.Value,
            PlatformType,
            Credentials is VkBotCredentials vk ? vk.CommunityId : null,
            ScenarioVersion.Value));

        return Result.Success();
    }

    /// <summary>
    /// Деактивирует бота.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>
    /// Успех, если деактивация прошла;
    /// сбой <see cref="ErrorCodes.BotInstance.AlreadyInactive"/>, если бот уже неактивен.
    /// </returns>
    public Result Deactivate(DateTimeOffset now)
    {
        if (Status is BotStatus.Inactive)
            return Error.Conflict(ErrorCodes.BotInstance.AlreadyInactive, "Бот уже неактивен.");

        Status = BotStatus.Inactive;
        MarkAsUpdated(now);

        AddDomainEvent(new BotInstanceDeactivatedDomainEvent(now, Id));

        return Result.Success();
    }

    /// <summary>
    /// Заменяет credentials бота. Статус не меняется — активный бот остаётся активным.
    /// </summary>
    /// <param name="newCredentials">Новые учётные данные платформы.</param>
    /// <param name="now">Текущий момент времени.</param>
    public void UpdateCredentials(IBotCredentials newCredentials, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(newCredentials);

        if (newCredentials.PlatformType != PlatformType)
            throw new InvalidOperationException("Тип платформы новых credentials не соответствует текущему.");

        Credentials = newCredentials;
        MarkAsUpdated(now);

        AddDomainEvent(new BotInstanceTokenChangedDomainEvent(now, Id));
    }

    /// <summary>
    /// Привязывает бота к проекту и версии сценария.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="scenarioVersion">Версия сценария.</param>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>
    /// Успех, если привязка выполнена;
    /// сбой <see cref="ErrorCodes.BotInstance.AlreadyBound"/>, если бот уже привязан к проекту.
    /// </returns>
    public Result BindToProject(Guid projectId, int scenarioVersion, DateTimeOffset now)
    {
        if (ProjectId is not null)
            return Error.Conflict(ErrorCodes.BotInstance.AlreadyBound, "Бот уже привязан к проекту.");

        ProjectId = projectId;
        ScenarioVersion = scenarioVersion;
        MarkAsUpdated(now);

        return Result.Success();
    }

    /// <summary>
    /// Изменяет назначенную версию сценария.
    /// </summary>
    /// <param name="newScenarioVersion">Новая версия сценария.</param>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>
    /// Успех, если версия изменена;
    /// сбой <see cref="ErrorCodes.BotInstance.CannotChangeVersionWhenUnbound"/>, если бот не привязан к проекту.
    /// </returns>
    public Result ChangeScenarioVersion(int newScenarioVersion, DateTimeOffset now)
    {
        if (ProjectId is null)
            return Error.Conflict(ErrorCodes.BotInstance.CannotChangeVersionWhenUnbound, "Невозможно изменить версию сценария у бота без привязки к проекту.");

        if (ScenarioVersion == newScenarioVersion)
            return Result.Success();

        ScenarioVersion = newScenarioVersion;
        MarkAsUpdated(now);

        AddDomainEvent(new BotInstanceScenarioVersionChangedDomainEvent(now, Id, newScenarioVersion));

        return Result.Success();
    }

    /// <summary>
    /// Отвязывает бота от проекта. Если бот был активен — деактивирует его.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    /// <returns>
    /// Успех, если отвязка прошла;
    /// сбой <see cref="ErrorCodes.BotInstance.AlreadyUnbound"/>, если бот уже не привязан к проекту.
    /// </returns>
    /// <remarks>
    /// Сбрасывает <see cref="ProjectId"/> и <see cref="ScenarioVersion"/> в <see langword="null"/>.
    /// Активные боты автоматически деактивируются (публикуется <see cref="BotInstanceDeactivatedDomainEvent"/>).
    /// Публикует <see cref="BotInstanceUnboundFromProjectDomainEvent"/>.
    /// </remarks>
    public Result UnbindFromProject(DateTimeOffset now)
    {
        if (ProjectId is null)
            return Error.Conflict(ErrorCodes.BotInstance.AlreadyUnbound, "Бот уже не привязан к проекту.");

        var formerProjectId = ProjectId.Value;

        if (Status is BotStatus.Active)
        {
            var deactivationResult = Deactivate(now);
            if (deactivationResult.IsFailure)
                return deactivationResult.Errors;
        }

        ProjectId = null;
        ScenarioVersion = null;
        MarkAsUpdated(now);

        AddDomainEvent(new BotInstanceUnboundFromProjectDomainEvent(now, Id, formerProjectId));

        return Result.Success();
    }

    /// <summary>
    /// Помечает бота как удалённого и публикует <see cref="BotInstanceDeletedDomainEvent"/>.
    /// Фактическое удаление из хранилища выполняется репозиторием.
    /// </summary>
    /// <param name="now">Текущий момент времени.</param>
    public void Delete(DateTimeOffset now)
    {
        MarkAsUpdated(now);
        AddDomainEvent(new BotInstanceDeletedDomainEvent(now, Id, ProjectId));
    }
}
