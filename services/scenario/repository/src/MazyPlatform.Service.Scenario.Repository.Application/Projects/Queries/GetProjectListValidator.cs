namespace MazyPlatform.Service.Scenario.Repository.Application.Projects.Queries;

using FluentValidation;

internal sealed class GetProjectListValidator : AbstractValidator<GetProjectListQuery>
{
    public GetProjectListValidator()
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
