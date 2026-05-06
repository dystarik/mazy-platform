namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class LogoutAllValidator : AbstractValidator<LogoutAllCommand>
{
    public LogoutAllValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(LogoutAllCommand.UserAccountId));

        RuleFor(x => x.CurrentRefreshTokenId)
            .RequiredGuid(nameof(LogoutAllCommand.CurrentRefreshTokenId));
    }
}
