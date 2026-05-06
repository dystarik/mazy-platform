namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using FluentValidation;

internal sealed class CreateEntitySchemaValidator : AbstractValidator<CreateEntitySchemaCommand>
{
    public CreateEntitySchemaValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ProjectId имеет неверный формат.");

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
            .WithMessage("Название схемы обязательно для заполнения.")
            .MaximumLength(256)
            .WithErrorCode(ErrorCodes.Validation.TooLong)
            .WithMessage("Название схемы не должно превышать 256 символов.");
    }
}
