namespace MazyPlatform.Service.User.Authentication.Domain.Services;

/// <summary>
/// Результат верификации фактора MFA.
/// </summary>
/// <param name="IsCompleted">
/// <see langword="true"/>, если MFA-сессия полностью завершена после данного фактора;
/// <see langword="false"/>, если требуется прохождение дополнительных факторов.
/// </param>
public sealed record VerifyMfaFactorOutput(bool IsCompleted);
