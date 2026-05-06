namespace MazyPlatform.Scenario.Abstractions.Validation;

/// <summary>
/// Валидатор пользовательского ввода в диалоге.
/// Например: телефон, email, ФИО.
/// </summary>
/// <remarks>
/// Для типизированной обработки параметров наследуйся от
/// <c>InputValidatorBase&lt;TParams&gt;</c> в <c>MazyPlatform.Scenario.Validators.Input</c>:
/// опиши свой record параметров, реализуй <c>ParseParams</c> и <c>ValidateTyped</c>.
/// Прямая реализация интерфейса допустима, но требует ручного парсинга словаря строк
/// в каждом вызове <see cref="IsValid"/>.
/// </remarks>
public interface IInputValidator
{
    /// <summary>
    /// Строковый ключ валидатора (например, "phone", "email", "regex").
    /// Указывается в параметрах узла receive_message.
    /// </summary>
    string ValidatorType { get; }

    /// <summary>
    /// Проверяет введённый текст.
    /// </summary>
    /// <param name="input">Текст от пользователя.</param>
    /// <param name="parameters">
    /// Дополнительные параметры (например, паттерн для regex).
    /// </param>
    /// <returns>true, если ввод валиден.</returns>
    bool IsValid(string input, IReadOnlyDictionary<string, string>? parameters = null);
}
