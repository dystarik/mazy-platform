namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using FluentValidation;

internal sealed class UpdateEntitySchemaValidator : AbstractValidator<UpdateEntitySchemaCommand>
{
    public UpdateEntitySchemaValidator()
    {
        RuleFor(x => x.SchemaId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("SchemaId обязателен для заполнения.")
            .Must(id => Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("SchemaId имеет неверный формат.");

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
            .WithMessage("Название поля обязательно для заполнения.")
            .MaximumLength(256)
            .WithErrorCode(ErrorCodes.Validation.TooLong)
            .WithMessage("Название поля не должно превышать 256 символов.");

        RuleFor(x => x.FieldType)
            .IsInEnum()
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Указан неверный тип поля.");
    }
}
