namespace MazyPlatform.Scenario.Storage.Mongo.Exceptions;

internal sealed class DataValidationException : Exception
{
    public DataValidationException()
    {
        Errors = [];
    }

    public DataValidationException(string message)
        : base(message)
    {
        Errors = [];
    }

    public DataValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [];
    }

    public DataValidationException(IReadOnlyList<string> errors)
        : base($"Ошибка валидации данных: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
