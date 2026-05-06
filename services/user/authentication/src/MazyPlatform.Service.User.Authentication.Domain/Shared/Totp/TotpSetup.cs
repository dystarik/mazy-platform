namespace MazyPlatform.Service.User.Authentication.Domain.Shared.Totp;

using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.Mfa.Payloads;

/// <summary>
/// Результат создания новой TOTP-настройки аутентификатора.
/// </summary>
/// <param name="Payload">
/// Полезная нагрузка с секретным ключом, передаётся в <see cref="UserAccount.AddMfaMethod"/>.
/// </param>
/// <param name="ProvisioningUri">
/// URI в формате <c>otpauth://totp/…</c>, используется для генерации QR-кода на клиенте.
/// </param>
/// <remarks>
/// Создаётся методом <see cref="ITotpService.CreateSetup"/> и возвращается в application-слой
/// для отображения QR-кода пользователю. Секрет из <paramref name="Payload"/> хранится
/// в <see cref="TotpPayload.Secret"/> и никогда не возвращается клиенту повторно.
/// </remarks>
public sealed record TotpSetup(TotpPayload Payload, string ProvisioningUri);
