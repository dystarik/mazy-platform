namespace MazyPlatform.SharedKernel.Domain.Results;

using MazyPlatform.SharedKernel.Domain.Results.Errors;

/// <summary>
/// Результат операции со значением типа <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Тип возвращаемого значения при успешном результате.</typeparam>
/// <example>
/// <code>
/// public Result&lt;User&gt; GetUser(Guid id)
/// {
///     var user = _repository.Find(id);
///     if (user is null)
///         return Error.NotFound("User.NotFound", "Пользователь не найден");
///
///     return user;
/// }
/// </code>
/// </example>
public sealed record class Result<T> : Result
    where T : notnull
{
    /// <summary>
    /// Инициализирует успешный результат с указанным значением.
    /// </summary>
    /// <param name="value">Значение успешного результата.</param>
    public Result(T value)
        : base(ResultStatus.Success) => Value = value;

    /// <summary>
    /// Инициализирует неудачный результат с ошибками.
    /// </summary>
    /// <param name="errors">Коллекция ошибок.</param>
    public Result(ErrorCollection errors)
        : base(errors) { }

    /// <summary>
    /// Значение успешного результата.
    /// </summary>
    /// <exception cref="InvalidOperationException">Результат является неудачным.</exception>
    public T Value =>
        field is not null && IsSuccess
            ? field
            : throw new InvalidOperationException("Невозможно получить значение неудачного результата.");

    /// <summary>
    /// Извлекает значение из результата.
    /// </summary>
    /// <param name="result">Результат.</param>
    /// <returns>Значение типа <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Результат является неудачным.</exception>
    public static implicit operator T(Result<T> result) => result.Value;

    /// <summary>
    /// Преобразует значение в успешный результат.
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>
    /// Экземпляр <see cref="Result{T}"/> с <see cref="Result.IsSuccess"/> равным <see langword="true"/>.
    /// </returns>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Преобразует ошибку в неудачный результат.
    /// </summary>
    /// <param name="error">Ошибка.</param>
    /// <returns>
    /// Экземпляр <see cref="Result{T}"/> с <see cref="Result.IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    public static implicit operator Result<T>(Error error) => Failure<T>(error);

    /// <summary>
    /// Преобразует коллекцию ошибок в неудачный результат.
    /// </summary>
    /// <param name="errors">Коллекция ошибок.</param>
    /// <returns>
    /// Экземпляр <see cref="Result{T}"/> с <see cref="Result.IsFailure"/> равным <see langword="true"/>.
    /// </returns>
    public static implicit operator Result<T>(ErrorCollection errors) => Failure<T>(errors);
}
