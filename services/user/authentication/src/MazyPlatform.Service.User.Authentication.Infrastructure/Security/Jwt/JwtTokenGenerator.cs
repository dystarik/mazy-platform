namespace MazyPlatform.Service.User.Authentication.Infrastructure.Security.Jwt;

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using MazyPlatform.Service.User.Authentication.Application.Common.Abstractions;
using MazyPlatform.Service.User.Authentication.Domain.UserAccounts.ValueObjects;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Реализация <see cref="IJwtTokenGenerator"/> — генерирует JWT с подписью HMAC-SHA256.
/// </summary>
/// <remarks>
/// Токен содержит утверждения: <c>sub</c> (идентификатор пользователя), <c>email</c>,
/// <c>jti</c> (идентификатор refresh-токена, позволяет связать access- и refresh-токены)
/// и <c>iat</c> (время выпуска в Unix-секундах).
/// Срок жизни задаётся параметром <see cref="JwtOptions.ExpirationMinutes"/>.
/// </remarks>
internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtSettings;
    private readonly SymmetricSecurityKey _securityKey;

    /// <summary>
    /// Инициализирует генератор, вычисляя <see cref="SymmetricSecurityKey"/> из <see cref="JwtOptions.SecretKey"/>.
    /// </summary>
    /// <param name="jwtSettings">Параметры JWT из конфигурации.</param>
    public JwtTokenGenerator(IOptions<JwtOptions> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        _securityKey = new SymmetricSecurityKey(keyBytes);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Если <paramref name="userAccountId"/> или <paramref name="refreshTokenId"/> равен <see cref="Guid.Empty"/>.
    /// </exception>
    /// <exception cref="ArgumentNullException">Если <paramref name="email"/> равен <see langword="null"/>.</exception>
    public string Generate(Guid userAccountId, Guid refreshTokenId, Email email)
    {
        if (userAccountId == Guid.Empty)
            throw new ArgumentException("userAccountId не может быть пустым.", nameof(userAccountId));
        if (refreshTokenId == Guid.Empty)
            throw new ArgumentException("refreshTokenId не может быть пустым.", nameof(refreshTokenId));
        ArgumentNullException.ThrowIfNull(email);

        var signingCredentials = new SigningCredentials(
            _securityKey,
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()),
            new(JwtRegisteredClaimNames.Email, email.Value),
            new(JwtRegisteredClaimNames.Jti, refreshTokenId.ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
