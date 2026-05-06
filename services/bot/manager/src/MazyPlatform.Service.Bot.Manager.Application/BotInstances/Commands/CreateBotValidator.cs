namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using FluentValidation;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;

internal sealed class CreateBotValidator : AbstractValidator<CreateBotCommand>
{
    public CreateBotValidator()
    {
        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("OwnerAccountId имеет неверный формат.");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ProjectId имеет неверный формат.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("Название бота обязательно для заполнения.")
            .MaximumLength(256)
            .WithErrorCode(ErrorCodes.Validation.TooLong)
            .WithMessage("Название бота не должно превышать 256 символов.");

        RuleFor(x => x.PlatformType)
            .IsInEnum()
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Указан неверный тип платформы.");

        RuleFor(x => x.AccessToken)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("Токен доступа обязателен для заполнения.");

        RuleFor(x => x.ScenarioVersion)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ScenarioVersion должен быть больше нуля.");

        RuleFor(x => x.ScenarioVersionUpdateMode)
            .IsInEnum()
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Указан неверный режим обновления версии сценария.");

        When(x => x.PlatformType == PlatformType.Vk, () =>
        {
            RuleFor(x => x.CommunityId)
                .NotEmpty()
                .WithErrorCode(ErrorCodes.Validation.Required)
                .WithMessage("CommunityId обязателен для платформы VK.");
        });
    }
}
