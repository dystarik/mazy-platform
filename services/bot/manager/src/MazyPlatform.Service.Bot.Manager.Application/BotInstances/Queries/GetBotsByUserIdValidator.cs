namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using FluentValidation;

internal sealed class GetBotsByUserIdValidator : AbstractValidator<GetBotsByUserIdQuery>
{
    public GetBotsByUserIdValidator()
    {
        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("OwnerAccountId имеет неверный формат.");
    }
}
