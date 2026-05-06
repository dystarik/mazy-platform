namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs;

using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Events;
using MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

public sealed class ScenarioGraph : AggregateRoot
{
    private const string _emptyDraft = "{}";

    private readonly List<ScenarioVersion> _versions = [];

    private ScenarioGraph() { }

    /// <summary>
    /// Идентификатор проекта, к которому относится граф сценария.
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// Текущий черновик графа сценария в формате JSON.
    /// Может изменяться через <see cref="UpdateDraft"/>.
    /// </summary>
    public string DraftJson { get; private set; } = _emptyDraft;

    /// <summary>
    /// Номер версии, которая сейчас является активным релизом.
    /// <see langword="null"/>, если ни один релиз ещё не опубликован.
    /// Указывает на одну из записей в <see cref="Versions"/>.
    /// </summary>
    public int? CurrentReleaseVersion { get; private set; }

    /// <summary>
    /// Все опубликованные версии сценария (включая текущий релиз).
    /// </summary>
    public IReadOnlyCollection<ScenarioVersion> Versions => _versions.AsReadOnly();

    /// <summary>
    /// Создаёт новый граф сценария для указанного проекта.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта, которому принадлежит граф.</param>
    /// <param name="now">Текущий момент времени; используется как <c>CreatedAt</c>.</param>
    /// <returns>Новый экземпляр <see cref="ScenarioGraph"/> с пустым черновиком и без опубликованных версий.</returns>
    public static ScenarioGraph Create(Guid projectId, DateTimeOffset now)
    {
        return new ScenarioGraph
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Обновляет черновик графа сценария.
    /// Публикует <see cref="ScenarioDraftUpdatedDomainEvent"/>.
    /// </summary>
    /// <param name="graphJson">Новый JSON черновика. Не может быть пустой строкой или состоять только из пробелов.</param>
    /// <param name="now">Текущий момент времени; используется как <c>UpdatedAt</c>.</param>
    public void UpdateDraft(string graphJson, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(graphJson);

        DraftJson = graphJson;
        MarkAsUpdated(now);
        AddDomainEvent(new ScenarioDraftUpdatedDomainEvent(now, Id, ProjectId));
    }

    /// <summary>
    /// Публикует текущий черновик как новую версию релиза.
    /// Новая версия становится активной (<see cref="CurrentReleaseVersion"/>).
    /// Публикует <see cref="ScenarioReleaseChangedDomainEvent"/>.
    /// </summary>
    /// <param name="specifications">
    /// Список бизнес-правил, которым должен удовлетворять черновик перед публикацией.
    /// Проверяются последовательно; при первом нарушении метод возвращает ошибку.
    /// </param>
    /// <param name="now">Текущий момент времени; проставляется в новый снапшот и <c>UpdatedAt</c>.</param>
    /// <returns>
    /// <see cref="Result.Success()"/>, если черновик прошёл все проверки и версия создана.<br/>
    /// Ошибка валидации первого нарушенного правила, если хотя бы одна спецификация не выполнена.
    /// </returns>
    public Result Promote(IReadOnlyList<IScenarioSpecification> specifications, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(specifications);

        foreach (var specification in specifications)
        {
            var result = specification.IsSatisfiedBy(DraftJson);
            if (result.IsFailure)
                return result;
        }

        var nextVersion = (_versions.Count == 0) ? 1 : _versions.Max(v => v.Version) + 1;

        _versions.Add(ScenarioVersion.CreateSnapshot(Id, DraftJson, nextVersion, now));
        CurrentReleaseVersion = nextVersion;
        MarkAsUpdated(now);

        AddDomainEvent(new ScenarioReleaseChangedDomainEvent(now, Id, ProjectId, nextVersion));
        return Result.Success();
    }

    /// <summary>
    /// Откатывает активный релиз на указанную версию из истории.
    /// Если целевая версия уже является активной, метод завершается успешно без изменений (идемпотентность).
    /// Публикует <see cref="ScenarioReleaseChangedDomainEvent"/>, если версия фактически изменилась.
    /// </summary>
    /// <param name="targetVersion">Номер версии из <see cref="Versions"/>, на которую нужно откатиться.</param>
    /// <param name="now">Текущий момент времени; используется как <c>UpdatedAt</c>.</param>
    /// <returns>
    /// <see cref="Result.Success()"/>, если откат выполнен или целевая версия уже активна.<br/>
    /// <see cref="Error.NotFound"/> с кодом <see cref="ErrorCodes.ScenarioGraph.VersionNotFound"/>, если версия с указанным номером не существует.
    /// </returns>
    public Result Rollback(int targetVersion, DateTimeOffset now)
    {
        if (!_versions.Exists(v => v.Version == targetVersion))
            return Error.NotFound(ErrorCodes.ScenarioGraph.VersionNotFound, $"Версия '{targetVersion}' не найдена.");

        if (CurrentReleaseVersion == targetVersion)
            return Result.Success(); // идемпотентность

        CurrentReleaseVersion = targetVersion;
        MarkAsUpdated(now);

        AddDomainEvent(new ScenarioReleaseChangedDomainEvent(now, Id, ProjectId, targetVersion));
        return Result.Success();
    }

    /// <summary>
    /// Удаляет версию из истории релизов.
    /// Если удаляется текущий релиз, активной становится максимальная оставшаяся версия.
    /// Если версий больше нет, активный релиз сбрасывается.
    /// Публикует <see cref="ScenarioVersionDeletedDomainEvent"/> и событие изменения релиза, если текущий релиз изменился.
    /// </summary>
    /// <param name="targetVersion">Номер версии из <see cref="Versions"/>, которую нужно удалить.</param>
    /// <param name="now">Текущий момент времени; используется как <c>UpdatedAt</c>.</param>
    /// <returns>
    /// <see cref="Result.Success()"/>, если версия успешно удалена.<br/>
    /// <see cref="Error.NotFound"/> с кодом <see cref="ErrorCodes.ScenarioGraph.VersionNotFound"/>, если версия с указанным номером не существует.
    /// </returns>
    public Result DeleteVersion(int targetVersion, DateTimeOffset now)
    {
        var version = _versions.Find(v => v.Version == targetVersion);
        if (version is null)
            return Error.NotFound(ErrorCodes.ScenarioGraph.VersionNotFound, $"Версия '{targetVersion}' не найдена.");

        var isCurrentRelease = CurrentReleaseVersion == targetVersion;

        _versions.Remove(version);

        if (isCurrentRelease)
            CurrentReleaseVersion = _versions.Count == 0 ? null : _versions.Max(v => v.Version);

        MarkAsUpdated(now);

        AddDomainEvent(new ScenarioVersionDeletedDomainEvent(now, Id, ProjectId, targetVersion));

        if (isCurrentRelease && CurrentReleaseVersion is { } nextVersion)
            AddDomainEvent(new ScenarioReleaseChangedDomainEvent(now, Id, ProjectId, nextVersion));

        if (isCurrentRelease && CurrentReleaseVersion is null)
            AddDomainEvent(new ScenarioReleaseRemovedDomainEvent(now, Id, ProjectId));

        return Result.Success();
    }
}
