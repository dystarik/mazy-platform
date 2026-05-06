namespace MazyPlatform.Scenario.Nodes.Integration;

using System.Text;

using MazyPlatform.Scenario.Abstractions.Execution;
using MazyPlatform.Scenario.Abstractions.Nodes;
using MazyPlatform.Scenario.Nodes.Base;

/// <summary>
/// Узел вызова внешнего HTTP API.
/// Тело ответа сохраняется в переменную сессии; статус-код — опционально, в отдельную переменную.
/// </summary>
/// <remarks>
/// Инициализирует новый экземпляр узла HTTP-запроса.
/// </remarks>
/// <param name="nodeId">Идентификатор узла.</param>
/// <param name="url">URL запроса (может содержать шаблоны переменных).</param>
/// <param name="method">HTTP-метод (GET, POST, PUT, DELETE).</param>
/// <param name="responseBodyVariable">Имя переменной для сохранения тела ответа.</param>
/// <param name="httpClientFactory">Фабрика HTTP-клиентов.</param>
/// <param name="body">Тело запроса (опционально, может содержать шаблоны).</param>
/// <param name="headers">Заголовки запроса (опционально).</param>
/// <param name="responseStatusVariable">Имя переменной для сохранения HTTP-статус-кода ответа (опционально).</param>
/// <param name="bodyType">
/// Тип тела запроса: <c>"json"</c> (default, application/json),
/// <c>"form"</c> (application/x-www-form-urlencoded) или <c>"raw"</c> (text/plain).
/// </param>
public sealed class HttpRequestNode(
    Guid nodeId,
    string url,
    string method,
    string responseBodyVariable,
    IHttpClientFactory httpClientFactory,
    string? body = null,
    IReadOnlyDictionary<string, string>? headers = null,
    string? responseStatusVariable = null,
    string bodyType = "json") : NodeBase(nodeId, "http_request", isAwaiting: false)
{
    /// <inheritdoc />
    public override async Task<NodeResult> ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var resolvedUrl = context.ResolveVariables(url);
        var httpMethod = new HttpMethod(method.ToUpperInvariant());

        using var client = httpClientFactory.CreateClient("ScenarioHttp");
        using var request = new HttpRequestMessage(httpMethod, resolvedUrl);

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, context.ResolveVariables(value));
            }
        }

        if (body is not null && httpMethod != HttpMethod.Get)
        {
            var resolvedBody = context.ResolveVariables(body);
            request.Content = new StringContent(resolvedBody, Encoding.UTF8, ResolveContentType(bodyType));
        }

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            context.Session.Variables[responseBodyVariable] = await response.Content.ReadAsStringAsync(cancellationToken);

            if (responseStatusVariable is not null)
            {
                context.Session.Variables[responseStatusVariable] = (int)response.StatusCode;
            }

            return NodeResult.Continue();
        }
        catch (HttpRequestException ex)
        {
            return NodeResult.Error($"HTTP-запрос к \"{resolvedUrl}\" завершился ошибкой: {ex.Message}");
        }
    }

    private static string ResolveContentType(string bodyType) => bodyType switch
    {
        "form" => "application/x-www-form-urlencoded",
        "raw" => "text/plain",
        _ => "application/json",
    };
}
