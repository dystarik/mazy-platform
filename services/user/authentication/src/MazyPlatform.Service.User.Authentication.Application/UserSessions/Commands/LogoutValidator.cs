namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal class LogoutValidator : AbstractValidator<LogoutCommand>
{
    public LogoutValidator()
    {
        RuleFor(x => x.RefreshTokenId)
            .RequiredGuid(nameof(LogoutCommand.RefreshTokenId));
    }
}
