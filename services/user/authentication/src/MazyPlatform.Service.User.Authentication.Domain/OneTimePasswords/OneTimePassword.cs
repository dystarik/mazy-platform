namespace MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords;

using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Enums;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.Events;
using MazyPlatform.Service.User.Authentication.Domain.OneTimePasswords.ValueObjects;
using MazyPlatform.Service.User.Authentication.Domain.Shared;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Generators;
using MazyPlatform.Service.User.Authentication.Domain.Shared.Hashing;

/// <summary>
/// Агрегат одноразового пароля для верификации операций пользователя.
/// </summary>
/// <remarks>
/// Используется для подтверждения email, прохождения Email-фактора MFA и сброса пароля.
/// Каждый экземпляр одноразовый: после верификации через <see cref="Verify"/> его нельзя использовать повторно.
/// Создание возможно только через фабрику <see cref="Create"/>, которая генерирует код,
/// хэширует его и поднимает <see cref="OneTimePasswordCreatedDomainEvent"/>.
/// </remarks>
/// <seealso cref="OtpType"/>
/// <seealso cref="IOneTimePasswordRepository"/>
public sealed class OneTimePassword : AggregateRoot
{
    /// <summary>Длина числового OTP-кода в цифрах.</summary>
    public const int CodeLength = 6;

    /// <summary>Максимальное количество неудачных попыток верификации кода.</summary>
    public const int MaxAttempts = 5;

    private OneTimePassword() { }

    /// <summary>
    /// Идентификатор аккаунта, которому принадлежит данный OTP.
    /// </summary>
    public required Guid UserAccountId { get; init; }

    /// <summary>
    /// Хэш числового кода, используемый при верификации.
    /// </summary>
    public required OtpCodeHash Code { get; init; }

    /// <summary>
    /// Временная метка UTC истечения срока действия кода.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Тип одноразового пароля, определяющий его назначение и срок действия.
    /// </summary>
    public required OtpType Type { get; init; }

    /// <summary>
    /// Способ доставки кода, вычисляется из <see cref="Type"/> через <see cref="OtpTypeExtensions.GetDeliveryMethod"/>.
    /// </summary>
    public DeliveryMethod Method => Type.GetDeliveryMethod();

    /// <summary>
    /// Количество неудачных попыток ввода кода.
    /// </summary>
    public int FailedAttempts { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если <see cref="VerifiedAt"/> не равно <see langword="null"/>.
    /// </summary>
    public bool IsVerified => VerifiedAt is not null;

    /// <summary>
    /// Временная метка UTC успешной верификации кода.
    /// Равна <see langword="null"/>, если код ещё не верифицирован.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>
    /// Возвращает <see langword="true"/>, если OTP был аннулирован.
    /// </summary>
    public bool IsInvalidated => InvalidatedAt is not null;

    /// <summary>
    /// Временная метка UTC аннулирования OTP.
    /// </summary>
    public DateTimeOffset? InvalidatedAt { get; private set; }

    /// <summary>
    /// Единственная точка создания нового одноразового пароля.
    /// </summary>
    /// <param name="userAccountId">Идентификатор аккаунта, для которого создаётся OTP.</param>
    /// <param name="type">Тип OTP, определяющий назначение и срок действия.</param>
    /// <param name="codeHasher">Хэшер для шифрования сгенерированного кода.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// Новый <see cref="OneTimePassword"/> с поднятым <see cref="OneTimePasswordCreatedDomainEvent"/>,
    /// содержащим открытый код для доставки пользователю.
    /// </returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="codeHasher"/> равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Если <paramref name="userAccountId"/> является пустым Guid.</exception>
    public static OneTimePassword Create(Guid userAccountId, OtpType type, ICodeHasher codeHasher, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(codeHasher);
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("userAccountId не может быть пустым.", nameof(userAccountId));

        var plainCode = NumericCodeGenerator.Generate(CodeLength);
        var codeHash = OtpCodeHash.FromTrusted(codeHasher.Hash(plainCode));

        var otp = new OneTimePassword()
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            Code = codeHash,
            Type = type,
            ExpiresAt = now.Add(GetLifetime(type)),
            CreatedAt = now,
        };

        otp.AddDomainEvent(new OneTimePasswordCreatedDomainEvent(now, otp.Id, otp.UserAccountId, plainCode, otp.Type, otp.ExpiresAt));
        return otp;
    }

    /// <summary>
    /// Верифицирует введённый пользователем код.
    /// </summary>
    /// <param name="hasher">Хэшер для проверки кода по сохранённому хэшу.</param>
    /// <param name="code">Открытый числовой код, введённый пользователем.</param>
    /// <param name="now">Текущая временная метка UTC, передаётся для тестируемости.</param>
    /// <returns>
    /// <see cref="Result"/> с успехом, если код верен или уже был верифицирован ранее;
    /// сбой с кодом <see cref="ErrorCodes.Auth.OneTimePassword.TooManyAttempts"/>, если превышен лимит попыток (<see cref="MaxAttempts"/>);
    /// сбой с кодом <see cref="ErrorCodes.Auth.OneTimePassword.Expired"/>, если срок действия истёк;
    /// сбой с кодом <see cref="ErrorCodes.Auth.OneTimePassword.InvalidCode"/>, если код неверен (счётчик попыток увеличивается).
    /// </returns>
    public Result Verify(ICodeHasher hasher, string code, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hasher);

        if (IsVerified)
            return Result.Success();

        if (FailedAttempts >= MaxAttempts)
            return Error.Validation(ErrorCodes.Auth.OneTimePassword.TooManyAttempts, "Превышено максимальное количество попыток ввода кода.");

        if (ExpiresAt < now)
            return Error.Unauthorized(ErrorCodes.Auth.OneTimePassword.Expired, "Срок действия кода истек.");

        if (!hasher.Verify(code, Code.Value))
        {
            FailedAttempts++;
            MarkAsUpdated(now);
            return Error.Unauthorized(ErrorCodes.Auth.OneTimePassword.InvalidCode, "Неверный код.");
        }

        VerifiedAt = now;
        MarkAsUpdated(now);

        return Result.Success();
    }

    /// <summary>
    /// Аннулирует OTP (например, при повторной отправке кода).
    /// </summary>
    /// <param name="now">Текущая временная метка UTC.</param>
    /// <remarks>
    /// Идемпотентный метод: повторный вызов для уже аннулированного OTP игнорируется.
    /// </remarks>
    public void Invalidate(DateTimeOffset now)
    {
        if (IsInvalidated)
            return;

        InvalidatedAt = now;
        MarkAsUpdated(now);
    }

    private static TimeSpan GetLifetime(OtpType type) => type switch
    {
        OtpType.EmailConfirmation => TimeSpan.FromMinutes(15),
        OtpType.MfaEmail => TimeSpan.FromMinutes(5),
        OtpType.PasswordReset => TimeSpan.FromMinutes(15),
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
