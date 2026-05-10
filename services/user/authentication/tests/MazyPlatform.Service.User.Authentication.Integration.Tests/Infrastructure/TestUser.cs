namespace MazyPlatform.Service.User.Authentication.Integration.Tests.Infrastructure;

using System.IdentityModel.Tokens.Jwt;

using MazyPlatform.Contracts.User.Grpc.Authentication;

public sealed record TestUser(
    string Email,
    string Password,
    string AccessToken,
    string RefreshToken,
    Guid UserAccountId,
    Guid RefreshTokenId)
{
    public static TestUser FromTokens(string email, string password, TokenPair tokens)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userAccountId = Guid.Parse(jwt.Claims.Single(c => string.Equals(c.Type, JwtRegisteredClaimNames.Sub, StringComparison.Ordinal)).Value);
        var refreshTokenId = Guid.Parse(jwt.Claims.Single(c => string.Equals(c.Type, JwtRegisteredClaimNames.Jti, StringComparison.Ordinal)).Value);

        return new TestUser(
            email,
            password,
            tokens.AccessToken,
            tokens.RefreshToken,
            userAccountId,
            refreshTokenId);
    }
}
