namespace MazyPlatform.Service.User.Authentication.Application.UserSessions.Commands;

using FluentValidation;

using MazyPlatform.Service.User.Authentication.Application.Common.Extensions;
using MazyPlatform.Service.User.Authentication.Domain.Shared;

public sealed class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
    {
        RuleFor(x => x.RefreshTokenId)
            .RequiredGuid(nameof(RefreshSessionCommand.RefreshTokenId));

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode(ErrorCodes.Auth.Validation.Invalid).WithMessage("RefreshToken обязателен для заполнения.");
    }
}
