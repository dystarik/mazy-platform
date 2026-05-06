namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.UserSessions;

/// <summary>
/// Результат успешного завершения регистрации.
/// </summary>
/// <param name="UserSession">Новая сессия пользователя.</param>
/// <param name="RefreshToken">Открытый токен обновления для передачи клиенту.</param>
/// <remarks>
/// <see cref="UserSession"/> необходимо сохранить в базе.
/// <see cref="RefreshToken"/> нигде не хранится в открытом виде — его следует немедленно передать клиенту.
/// </remarks>
public sealed record CompleteRegistrationOutput(UserSession UserSession, string RefreshToken);
