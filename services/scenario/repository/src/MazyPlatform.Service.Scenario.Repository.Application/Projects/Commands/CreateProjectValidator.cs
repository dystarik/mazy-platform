namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Commands;

using FluentValidation;

internal sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
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
            .WithMessage("Название проекта обязательно для заполнения.")
            .MaximumLength(256)
            .WithErrorCode(ErrorCodes.Validation.TooLong)
            .WithMessage("Название проекта не должно превышать 256 символов.");

        RuleFor(x => x.PlatformType)
            .IsInEnum()
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Указан неверный тип платформы.");
    }
}
