namespace MazyPlatform.Scenario.Nodes.Integration;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел получения информации о пользователе мессенджера.
/// Сохраняет имя, фамилию, никнейм и аватар в переменные сессии.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла получения информации о пользователе.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="prefix">
/// Префикс для переменных. Данные сохраняются как:
/// {prefix}_first_name, {prefix}_last_name, {prefix}_username, {prefix}_avatar_url.
/// </param>
/// <param name="getUserInfoUseCase">Юзкейс получения информации.</param>
public sealed class GetUserInfoNode(Guid nodeId, string prefix, IGetUserInfoUseCase getUserInfoUseCase) : NodeBase(nodeId, "get_user_info", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var platformUserId = context.Session.PlatformUserId;
        var userInfo = await getUserInfoUseCase.GetAsync(platformUserId, context.BotToken, cancellationToken);

        context.Session.Variables[$"{prefix}_first_name"] = userInfo.FirstName;
        context.Session.Variables[$"{prefix}_last_name"] = userInfo.LastName;
        context.Session.Variables[$"{prefix}_username"] = userInfo.Username;
        context.Session.Variables[$"{prefix}_avatar_url"] = userInfo.AvatarUrl;

        return NodeResult.Continue();
    }
}
