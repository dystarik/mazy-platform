namespace MazyPlatform.Scenario.Nodes.Integration;

using System.Text.Json;

using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Helpers;
using MazyPlatform.Scenario.Nodes.Base;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Дескриптор узла HTTP-запроса.
/// </summary>
public sealed class HttpRequestNodeDescriptor : NodeDescriptorBase
{
    /// <inheritdoc />
    public override string Type => "http_request";

    /// <inheritdoc />
    public override IReadOnlyList<NodeParamSchema> Schema { get; } =
    [
        new(
            "url",
            NodeParamType.String,
            IsRequired: true,
            Description: "URL внешнего сервиса."),
        new(
            "method",
            NodeParamType.Enum,
            IsRequired: false,
            Description: "HTTP-метод запроса.",
            EnumValues: ["GET", "POST", "PUT", "PATCH", "DELETE"]),
        new(
            "responseBodyVariable",
            NodeParamType.String,
            IsRequired: true,
            Description: "Имя переменной для сохранения тела ответа."),
        new(
            "body",
            NodeParamType.String,
            IsRequired: false,
            Description: "Тело запроса. Формат интерпретации зависит от bodyType: для 'json' — JSON-строка, для 'form' — пары вида key=value&key2=value2, для 'raw' — произвольный текст."),
        new(
            "bodyType",
            NodeParamType.Enum,
            IsRequired: false,
            Description: "Тип тела запроса. По умолчанию JSON.",
            EnumValues: ["json", "form", "raw"]),
        new(
            "headers",
            NodeParamType.StringDictionary,
            IsRequired: false,
            Description: "HTTP-заголовки запроса."),
        new(
            "responseStatusVariable",
            NodeParamType.String,
            IsRequired: false,
            Description: "Имя переменной для сохранения HTTP-статус-кода ответа."),
    ];

    /// <inheritdoc />
    public override INode Create(Guid id, JsonElement parameters, IServiceProvider services) =>
        new HttpRequestNode(
            id,
            JsonParamHelper.GetRequiredString(parameters, "url"),
            JsonParamHelper.GetOptionalString(parameters, "method") ?? "GET",
            JsonParamHelper.GetRequiredString(parameters, "responseBodyVariable"),
            services.GetRequiredService<IHttpClientFactory>(),
            JsonParamHelper.GetOptionalString(parameters, "body"),
            JsonParamHelper.GetOptionalStringDictionary(parameters, "headers"),
            JsonParamHelper.GetOptionalString(parameters, "responseStatusVariable"),
            JsonParamHelper.GetOptionalString(parameters, "bodyType") ?? "json");
}
