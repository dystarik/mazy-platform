namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Commands;

using FluentValidation;

internal sealed class DeleteEntitySchemaValidator : AbstractValidator<DeleteEntitySchemaCommand>
{
    public DeleteEntitySchemaValidator()
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
    }
}
