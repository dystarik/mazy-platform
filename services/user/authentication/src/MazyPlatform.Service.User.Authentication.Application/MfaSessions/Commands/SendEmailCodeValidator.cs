namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class SendEmailCodeValidator : AbstractValidator<SendEmailCodeCommand>
{
    public SendEmailCodeValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(SendEmailCodeCommand.MfaSessionId));

        RuleFor(x => x.UserAccountId)
            .RequiredGuid(nameof(SendEmailCodeCommand.UserAccountId));
    }
}
