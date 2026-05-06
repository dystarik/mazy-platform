namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

using FluentValidation;

internal sealed class GetEntitySchemaValidator : AbstractValidator<GetEntitySchemaQuery>
{
    public GetEntitySchemaValidator()
    {
        RuleFor(x => x.SchemaId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("SchemaId обязателен для заполнения.");

        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.");
    }
}
