namespace MazyPlatform.SharedKernel.Domain.Results.Errors;

using System.Collections;

/// <summary>
/// Неизменяемая коллекция ошибок <see cref="Error"/>.
/// </summary>
/// <remarks>
/// Коллекция материализуется в массив при создании для стабильного перечисления.
/// </remarks>
/// <example>
/// <code>
/// // Из одной ошибки (неявное преобразование)
/// ErrorCollection errors = Error.Validation("Field.Invalid", "Ошибка");
///
/// // Из нескольких ошибок
/// var errors = new ErrorCollection([
///     Error.Validation("Name.Empty", "Имя обязательно"),
///     Error.Validation("Email.Invalid", "Неверный формат email")
/// ]);
/// </code>
/// </example>
public sealed record class ErrorCollection : IEnumerable<Error>
{
    private readonly Error[] _errors;

    /// <summary>
    /// Инициализирует коллекцию ошибок.
    /// </summary>
    /// <param name="errors">Последовательность ошибок.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> равен <see langword="null"/>.</exception>
    public ErrorCollection(IEnumerable<Error> errors) =>
        _errors = errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors));

    /// <summary>
    /// Преобразует ошибку в коллекцию из одного элемента.
    /// </summary>
    /// <param name="error">Ошибка.</param>
    /// <returns>
    /// Экземпляр <see cref="ErrorCollection"/> с одной ошибкой.
    /// </returns>
    public static implicit operator ErrorCollection(Error error) => new([error]);

    /// <summary>
    /// Преобразует массив ошибок в коллекцию.
    /// </summary>
    /// <param name="errors">Массив ошибок.</param>
    /// <returns>
    /// Экземпляр <see cref="ErrorCollection"/> с указанными ошибками.
    /// </returns>
    public static implicit operator ErrorCollection(Error[] errors) => new(errors.AsEnumerable());

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(ErrorCollection)} {{ Errors = [{string.Join(", ", _errors)}] }}";

    /// <inheritdoc/>
    public IEnumerator<Error> GetEnumerator() => ((IEnumerable<Error>)_errors).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
