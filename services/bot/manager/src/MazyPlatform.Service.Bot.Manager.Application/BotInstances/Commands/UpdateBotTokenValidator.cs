namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using FluentValidation;

internal sealed class UpdateBotTokenValidator : AbstractValidator<UpdateBotTokenCommand>
{
    public UpdateBotTokenValidator()
    {
        RuleFor(x => x.BotInstanceId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("BotInstanceId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("BotInstanceId имеет неверный формат.");

        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("OwnerAccountId имеет неверный формат.");

        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("Токен доступа обязателен для заполнения.");
    }
}
