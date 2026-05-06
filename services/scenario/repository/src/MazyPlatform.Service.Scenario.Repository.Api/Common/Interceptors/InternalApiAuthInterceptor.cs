namespace MazyPlatform.Service.Scenario.Repository.Api.Common.Interceptors;

using System.Security.Cryptography;
using System.Text;

using Grpc.Core;
using Grpc.Core.Interceptors;

using MazyPlatform.Service.Scenario.Repository.Api.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// gRPC interceptor для проверки сервисного токена в metadata входящего запроса.
/// </summary>
/// <remarks>
/// Применяется только к internal gRPC-сервисам, доступным внутри docker-сети.
/// Ожидает заголовок <c>x-internal-token</c> со значением, совпадающим с настройкой <see cref="InternalApiOptions.AccessToken"/>.
/// При несовпадении или отсутствии заголовка возвращает <see cref="StatusCode.Unauthenticated"/>.
/// </remarks>
internal sealed class InternalApiAuthInterceptor(IOptions<InternalApiOptions> options) : Interceptor
{
    private const string TokenHeader = "x-internal-token";
    private readonly string _expectedToken = options.Value.AccessToken;

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthenticated(context);
        return continuation(request, context);
    }

    private void EnsureAuthenticated(ServerCallContext context)
    {
        var token = context.RequestHeaders.GetValue(TokenHeader);

        if (string.IsNullOrEmpty(token) || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token),
                Encoding.UTF8.GetBytes(_expectedToken)))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or missing internal token."));
        }
    }
}
