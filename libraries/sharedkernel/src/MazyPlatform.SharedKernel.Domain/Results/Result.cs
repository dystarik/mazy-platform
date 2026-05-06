namespace MazyPlatform.SharedKernel.Domain.Results;

using System.Diagnostics.CodeAnalysis;

using MazyPlatform.SharedKernel.Domain.Results.Errors;

/// <summary>
/// Результат операции без возвращаемого значения.
/// </summary>
/// <remarks>
/// Используйте <see cref="Success"/> для успешного завершения
/// или <see cref="Failure"/> / неявное преобразование из <see cref="Error"/> для неудачи.
/// </remarks>
/// <example>
/// <code>
/// public Result DoSomething()
/// {
///     if (invalid)
///         return Error.Validation("Something.Invalid", "Описание ошибки");
///
///     return Result.Success();
/// }
/// </code>
/// </example>
public record class Result
{
    private readonly ResultStatus _status;

    /// <summary>
    /// Инициализирует результат с указанным статусом.
    /// </summary>
    /// <param name="status">Статус результата.</param>
    protected Result(ResultStatus status) => _status = status;

    /// <summary>
    /// Инициализирует неудачный результат с ошибками.
    /// </summary>
    /// <param name="error">Коллекция ошибок.</param>
    protected Result(ErrorCollection error)
        : this(ResultStatus.Failure) => Errors = error;

    /// <summary>
    /// Статус результата.
    /// </summary>
    protected enum ResultStatus
    {
        /// <summary>Операция завершилась успешно.</summary>
        Success,

        /// <summary>Операция завершилась с ошибкой.</summary>
        Failure,
    }

    /// <summary>
    /// <see langword="true"/>, если операция успешна.
    /// </summary>
    /// <remarks>
    /// Когда <see langword="false"/>, свойство <see cref="Errors"/> гарантированно не <see langword="null"/>.
    /// </remarks>
    [MemberNotNullWhen(false, nameof(Errors))]
    public bool IsSuccess => _status is ResultStatus.Success;

    /// <summary>
    /// <see langword="true"/>, если операция завершилась с ошибкой.
    /// </summary>
    /// <remarks>
    /// Когда <see langword="true"/>, свойство <see cref="Errors"/> гарантированно не <see langword="null"/>.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(Errors))]
    public bool IsFailure => _status is ResultStatus.Failure;

    /// <summary>
    /// Ошибки операции. <see langword="null"/> при успехе.
    /// </summary>
    public ErrorCollection? Errors { get; init; }

    /// <summary>
    /// Преобразует ошибку в неудачный результат.
    /// </summary>
    /// <param name="error">Ошибка.</param>
    /// <returns>
    /// Экземпляр <see cref="Result"/> с <see cref="IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    public static implicit operator Result(Error error) => Failure(error);

    /// <summary>
    /// Преобразует коллекцию ошибок в неудачный результат.
    /// </summary>
    /// <param name="errors">Коллекция ошибок.</param>
    /// <returns>
    /// Экземпляр <see cref="Result"/> с <see cref="IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    public static implicit operator Result(ErrorCollection errors) => Failure(errors);

    /// <summary>
    /// Создаёт успешный результат.
    /// </summary>
    /// <returns>
    /// Экземпляр <see cref="Result"/> с <see cref="IsSuccess"/> равным <see langword="true"/>.
    /// </returns>
    public static Result Success() => new(ResultStatus.Success);

    /// <summary>
    /// Создаёт неудачный результат.
    /// </summary>
    /// <param name="errors">Коллекция ошибок.</param>
    /// <returns>
    /// Экземпляр <see cref="Result"/> с <see cref="IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> равен <see langword="null"/>.</exception>
    public static Result Failure(ErrorCollection errors) =>
        new(errors ?? throw new ArgumentNullException(nameof(errors)));

    /// <summary>
    /// Создаёт успешный результат со значением.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="value">Значение.</param>
    /// <returns>
    /// Экземпляр <see cref="Result{T}"/> с <see cref="IsSuccess"/> равным <see langword="true"/>.
    /// </returns>
    public static Result<T> Success<T>(T value)
        where T : notnull =>
        new(value);

    /// <summary>
    /// Создаёт неудачный результат.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <param name="errors">Коллекция ошибок.</param>
    /// <returns>
    /// Экземпляр <see cref="Result{T}"/> с <see cref="IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> равен <see langword="null"/>.</exception>
    public static Result<T> Failure<T>(ErrorCollection errors)
        where T : notnull =>
        new(errors);
}
