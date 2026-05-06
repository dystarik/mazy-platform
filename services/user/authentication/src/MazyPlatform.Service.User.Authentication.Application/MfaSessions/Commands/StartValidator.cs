namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal class StartValidator : AbstractValidator<StartCommand>
{
    public StartValidator()
    {
        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(StartCommand.UserAccountId));
    }
}
