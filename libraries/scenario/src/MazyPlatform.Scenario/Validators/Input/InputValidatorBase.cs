namespace MazyPlatform.Scenario.Validators.Input;

using MazyPlatform.Scenario.Abstractions.Validation;

/// <summary>
/// Базовый класс для валидаторов с типизированным представлением параметров.
/// Парсит сырой словарь строк в типизированный record один раз и передаёт его
/// в <see cref="ValidateTyped"/>, чтобы реализация работала с конкретными
/// полями, а не с произвольными строками.
/// </summary>
/// <typeparam name="TParams">
/// Record-тип параметров валидатора. Для валидаторов без параметров используй
/// пустой record (<c>public sealed record PhoneValidatorParams;</c>).
/// </typeparam>
public abstract class InputValidatorBase<TParams> : IInputValidator
    where TParams : notnull
{
    /// <inheritdoc />
    public abstract string ValidatorType { get; }

    /// <inheritdoc />
    public bool IsValid(string input, IReadOnlyDictionary<string, string>? parameters = null)
    {
        var typed = ParseParams(parameters ?? new Dictionary<string, string>(StringComparer.Ordinal));
        return ValidateTyped(input, typed);
    }

    /// <summary>
    /// Преобразует сырой словарь параметров в типизированный record.
    /// Вызывается один раз на каждый <see cref="IsValid"/>.
    /// </summary>
    /// <param name="raw">Словарь параметров (никогда не null; если параметры
    /// не указаны вызывающим — пустой словарь).</param>
    /// <returns>Типизированные параметры.</returns>
    protected abstract TParams ParseParams(IReadOnlyDictionary<string, string> raw);

    /// <summary>
    /// Проверяет ввод с использованием типизированных параметров.
    /// </summary>
    /// <param name="input">Ввод от пользователя.</param>
    /// <param name="parameters">Параметры, разобранные в <see cref="ParseParams"/>.</param>
    /// <returns>true, если ввод валиден.</returns>
    protected abstract bool ValidateTyped(string input, TParams parameters);
}
