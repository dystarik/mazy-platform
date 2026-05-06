namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Queries;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class GetSessionsValidator : AbstractValidator<GetSessionsQuery>
{
    public GetSessionsValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(GetSessionsQuery.UserAccountId));

        RuleFor(x => x.RefreshTokenId)
            .RequiredGuid(nameof(GetSessionsQuery.RefreshTokenId));
    }
}
