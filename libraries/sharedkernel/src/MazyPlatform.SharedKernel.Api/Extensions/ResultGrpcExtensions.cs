namespace MazyPlatform.SharedKernel.Api.Extensions;

using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Google.Rpc;

using Grpc.Core;

using MazyPlatform.SharedKernel.Domain.Results;
using MazyPlatform.SharedKernel.Domain.Results.Errors;
using MazyPlatform.SharedKernel.Domain.Results.Extensions;

/// <summary>
/// Методы расширения для преобразования <see cref="Result"/> и <see cref="Result{T}"/>
/// в gRPC-ответы.
/// </summary>
public static class ResultGrpcExtensions
{
    /// <summary>
    /// Преобразует <see cref="Result{T}"/> в gRPC-ответ с помощью функции маппинга.
    /// </summary>
    /// <typeparam name="T">Тип значения результата.</typeparam>
    /// <typeparam name="TResponse">Тип protobuf-сообщения ответа.</typeparam>
    /// <param name="result">Результат выполнения операции.</param>
    /// <param name="mapper">Функция маппинга значения в protobuf-сообщение.</param>
    /// <returns>Protobuf-сообщение ответа.</returns>
    /// <exception cref="RpcException">
    /// Выбрасывается, если <paramref name="result"/> содержит ошибки.
    /// Код статуса определяется типом ошибки: <see cref="ErrorType.Validation"/> → <see cref="StatusCode.InvalidArgument"/>,
    /// <see cref="ErrorType.NotFound"/> → <see cref="StatusCode.NotFound"/>,
    /// <see cref="ErrorType.Unauthorized"/> → <see cref="StatusCode.Unauthenticated"/>,
    /// <see cref="ErrorType.Conflict"/> → <see cref="StatusCode.AlreadyExists"/>.
    /// </exception>
    public static TResponse ToGrpcResponse<T, TResponse>(this Result<T> result, Func<T, TResponse> mapper)
        where T : notnull
        where TResponse : IMessage
    {
        return result.Match(
            mapper,
            onFailure => throw onFailure.ToRpcException());
    }

    /// <summary>
    /// Преобразует <see cref="Result"/> (без значения) в <see cref="Empty"/> gRPC-ответ.
    /// </summary>
    /// <param name="result">Результат выполнения операции.</param>
    /// <returns><see cref="Empty"/> при успехе.</returns>
    /// <exception cref="RpcException">
    /// Выбрасывается, если <paramref name="result"/> содержит ошибки.
    /// </exception>
    public static Empty ToGrpcResponse(this Result result)
    {
        return result.Match(
            () => new Empty(),
            onFailure => throw onFailure.ToRpcException());
    }

    /// <summary>
    /// Преобразует коллекцию ошибок <see cref="ErrorCollection"/> в <see cref="RpcException"/>
    /// с соответствующим статус-кодом gRPC и деталями ошибок в формате <see cref="ErrorInfo"/>.
    /// </summary>
    private static RpcException ToRpcException(this ErrorCollection errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var distinctTypes = errors.Select(e => e.Type).Distinct().ToArray();

        var statusCode = distinctTypes.Length > 1
            ? StatusCode.Internal
            : distinctTypes[0] switch
            {
                ErrorType.Validation => StatusCode.InvalidArgument,
                ErrorType.NotFound => StatusCode.NotFound,
                ErrorType.Unauthorized => StatusCode.Unauthenticated,
                ErrorType.Conflict => StatusCode.AlreadyExists,
                _ => StatusCode.Internal,
            };

        var status = new Google.Rpc.Status
        {
            Code = (int)statusCode,
            Message = distinctTypes.Length > 1
                ? "Operation failed."
                : GetMessageForType(distinctTypes[0]),
        };

        foreach (var error in errors)
        {
            status.Details.Add(Any.Pack(new ErrorInfo
            {
                Reason = error.Code,
                Domain = "mazyplatform",
                Metadata = { ["message"] = error.Message },
            }));
        }

        return status.ToRpcException();
    }

    /// <summary>
    /// Возвращает человекочитаемое сообщение для указанного типа ошибки.
    /// </summary>
    /// <param name="type">Тип ошибки.</param>
    /// <returns>Строка сообщения.</returns>
    private static string GetMessageForType(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed.",
        ErrorType.NotFound => "Resource not found.",
        ErrorType.Unauthorized => "Unauthorized.",
        ErrorType.Conflict => "Conflict.",
        _ => "Internal error.",
    };
}
