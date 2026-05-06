namespace MazyPlatform.Service.User.Authentication.Application.MfaSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;

internal sealed class SendUnauthenticatedEmailCodeValidator : AbstractValidator<SendUnauthenticatedEmailCodeCommand>
{
    public SendUnauthenticatedEmailCodeValidator()
    {
        RuleFor(x => x.MfaSessionId)
            .RequiredGuid(nameof(SendUnauthenticatedEmailCodeCommand.MfaSessionId));
    }
}
