namespace MazyPlatform.Service.User.Authentication.Domain.Services;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

/// <summary>
/// Доменный сервис регистрации нового пользователя по паролю.
/// </summary>
/// <remarks>
/// Если аккаунт с таким email уже существует, но не подтверждён и старше 24 часов —
/// он удаляется, а новый создаётся на его месте.
/// </remarks>
public sealed class RegisterByPasswordService(
    IUserAccountRepository userAccountRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ICodeHasher codeHasher) : IDomainService
{
    /// <summary>
    /// Регистрирует новый аккаунт и создаёт OTP для подтверждения email.
    /// </summary>
    /// <param name="email">Валидированный email нового пользователя.</param>
    /// <param name="password">Валидированный открытый пароль.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>
    /// <see cref="Result{T}"/> с <see cref="RegisterByPasswordOutput"/> при успехе.
    /// сбой с кодом <see cref="ErrorCodes.Auth.Registration.EmailAlreadyInUse"/>, если email занят подтверждённым аккаунтом.
    /// сбой с кодом <see cref="ErrorCodes.Auth.Registration.EmailVerificationPending"/>, если аккаунт с таким email
    /// был создан менее 24 часов назад и ещё не подтверждён.
    /// </returns>
    public async Task<Result<RegisterByPasswordOutput>> ExecuteAsync(Email email, Password password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var now = timeProvider.GetUtcNow();

        var existingUserAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUserAccount is not null)
        {
            if (existingUserAccount.IsEmailVerified)
                return Error.Conflict(ErrorCodes.Auth.Registration.EmailAlreadyInUse, "Пользователь с таким email уже существует.");

            if ((now - existingUserAccount.CreatedAt) < TimeSpan.FromHours(24))
                return Error.Conflict(ErrorCodes.Auth.EmailVerificationPending, "Email уже зарегистрирован, ожидается подтверждение.");

            userAccountRepository.Delete(existingUserAccount);
        }

        var isMfaEmailInUse = await userAccountRepository.IsMfaEmailInUseAsync(email, cancellationToken);
        if (isMfaEmailInUse)
            return Error.Conflict(ErrorCodes.Auth.Registration.EmailAlreadyUsedAsMfaEmail, "Email уже используется как MFA email другого аккаунта.");

        var passwordHash = PasswordHash.FromTrusted(passwordHasher.Hash(password.Value));

        var newUserAccount = UserAccount.RegisterByPassword(email, passwordHash, now);
        var otp = OneTimePassword.Create(newUserAccount.Id, OtpType.EmailConfirmation, codeHasher, now);

        return new RegisterByPasswordOutput(newUserAccount, otp);
    }
}
