namespace MazyPlatform.Service.Bot.Manager.Application.BotInstances.Commands;

using FluentValidation;

using MazyPlatform.Service.Bot.Manager.Domain.BotInstances.ValueObjects;
using MazyPlatform.Service.Bot.Manager.Domain.Common;

internal sealed class CreateBotWithoutProjectValidator : AbstractValidator<CreateBotWithoutProjectCommand>
{
    public CreateBotWithoutProjectValidator()
    {
        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("OwnerAccountId имеет неверный формат.");

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

        When(x => x.PlatformType == PlatformType.Vk, () =>
        {
            RuleFor(x => x.CommunityId)
                .NotEmpty()
                .WithErrorCode(ErrorCodes.Validation.Required)
                .WithMessage("CommunityId обязателен для платформы VK.");
        });
    }
}
