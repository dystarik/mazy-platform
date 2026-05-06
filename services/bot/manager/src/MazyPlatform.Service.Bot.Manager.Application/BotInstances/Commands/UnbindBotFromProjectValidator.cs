namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using FluentValidation;

internal sealed class UnbindBotFromProjectValidator : AbstractValidator<UnbindBotFromProjectCommand>
{
    public UnbindBotFromProjectValidator()
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
    }
}
