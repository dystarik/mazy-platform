namespace MazyPlatform.Service.User.Authentication.Application.UserAccounts.Queries;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class GetLinkedProvidersValidator : AbstractValidator<GetLinkedProvidersQuery>
{
    public GetLinkedProvidersValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(GetLinkedProvidersQuery.UserAccountId));
    }
}
