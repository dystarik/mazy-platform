namespace MazyPlatform.Scenario.Nodes.Integration;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Abstractions.UseCases;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла получения данных пользователя.
/// </summary>
public sealed class GetUserInfoNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "get_user_info";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "prefix",
            NodeParamType.String,
            IsRequired: false,
            Description: "Префикс для имён переменных, в которые сохраняются данные пользователя (по умолчанию \"user\")."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new GetUserInfoNode(
            id,
            JsonParamHelper.GetOptionalString(parameters, "prefix") ?? "user",
            services.GetRequiredService<IGetUserInfoUseCase>());
}
