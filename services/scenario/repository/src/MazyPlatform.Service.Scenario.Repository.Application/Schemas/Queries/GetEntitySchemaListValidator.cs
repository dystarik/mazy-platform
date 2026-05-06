namespace MazyPlatform.Service.Scenario.Repository.Application.Schemas.Queries;

using FluentValidation;

internal sealed class GetEntitySchemaListValidator : AbstractValidator<GetEntitySchemaListQuery>
{
    public GetEntitySchemaListValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("ProjectId обязателен для заполнения.");

        RuleFor(x => x.OwnerAccountId)
            .NotEmpty()
            .WithErrorCode(ErrorCodes.Validation.Required)
            .WithMessage("OwnerAccountId обязателен для заполнения.");
    }
}
