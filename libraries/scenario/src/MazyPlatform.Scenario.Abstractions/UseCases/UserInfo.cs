namespace MazyPlatform.Scenario.Abstractions.UseCases;

/// <summary>
/// Информация о пользователе мессенджера.
/// </summary>
/// <param name="FirstName">Имя.</param>
/// <param name="LastName">Фамилия (может быть null).</param>
/// <param name="Username">Никнейм/логин (может быть null).</param>
/// <param name="AvatarUrl">URL аватара (может быть null).</param>
public sealed record UserInfo(
    string FirstName,
    string? LastName = null,
    string? Username = null,
    string? AvatarUrl = null);
