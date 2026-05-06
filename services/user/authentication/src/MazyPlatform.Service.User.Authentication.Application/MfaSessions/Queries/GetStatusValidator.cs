namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Queries;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class GetStatusValidator : AbstractValidator<GetStatusQuery>
{
    public GetStatusValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(GetStatusQuery.MfaSessionId));

        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(GetStatusQuery.UserAccountId));
    }
}
