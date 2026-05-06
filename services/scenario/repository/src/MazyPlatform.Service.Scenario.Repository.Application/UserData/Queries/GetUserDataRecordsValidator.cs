namespace MazyPlatform.Service.Scenario.Repository.Application.UserData.Queries;

using FluentValidation;

internal sealed class GetUserDataRecordsValidator : AbstractValidator<GetUserDataRecordsQuery>
{
    public GetUserDataRecordsValidator()
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

        RuleFor(x => x.SchemaId)
            .Must(id => string.IsNullOrWhiteSpace(id) || Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("SchemaId имеет неверный формат.");

        RuleFor(x => x.BotId)
            .Must(id => string.IsNullOrWhiteSpace(id) || Guid.TryParse(id, out _))
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("BotId имеет неверный формат.");

        RuleFor(x => x.ScenarioVersion)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ScenarioVersion.HasValue)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("ScenarioVersion не может быть отрицательной.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(0, 200)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("PageSize должен быть от 0 до 200.");

        RuleFor(x => x.PageOffset)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("PageOffset не может быть отрицательным.");
    }
}
