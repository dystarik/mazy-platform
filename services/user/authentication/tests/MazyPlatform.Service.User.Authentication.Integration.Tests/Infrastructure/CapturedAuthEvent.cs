namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

public sealed record CapturedAuthEvent(
    string RoutingKey,
    DateTimeOffset CapturedAt,
    Guid? UserAccountId,
    string? Email,
    string? ConfirmationCode,
    string? Code,
    string? ResetCode,
    string Json);
