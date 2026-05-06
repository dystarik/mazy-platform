namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Queries;

using FluentValidation;

internal sealed class GetBotCredentialsValidator : AbstractValidator<GetBotCredentialsQuery>
{
    public GetBotCredentialsValidator()
    {
        RuleFor(x => x.BotInstanceId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("BotInstanceId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("BotInstanceId имеет неверный формат.");
    }
}
