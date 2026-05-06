namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

internal sealed class GetMfaMethodsValidator : AbstractValidator<GetMfaMethodsQuery>
{
    public GetMfaMethodsValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(GetMfaMethodsQuery.UserAccountId));
    }
}
