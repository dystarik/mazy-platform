namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;

/// <summary>
/// Результат успешной регистрации пользователя по паролю.
/// </summary>
/// <param name="UserAccount">Новый агрегат аккаунта с поднятым <see cref="UserAccountRegisteredByPasswordDomainEvent"/>.</param>
/// <param name="Otp">
/// Одноразовый пароль типа <see cref="OtpType.EmailConfirmation"/> для подтверждения email.
/// Содержит <see cref="OneTimePasswordCreatedDomainEvent"/> с открытым кодом для доставки.
/// </param>
/// <remarks>
/// Оба агрегата необходимо сохранить в рамках одной транзакции,
/// после чего диспетчер событий доставит OTP-код пользователю.
/// </remarks>
public sealed record RegisterByPasswordOutput(UserAccount UserAccount, OneTimePassword Otp);
