namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Commands;

using FluentValidation;

internal sealed class RollbackReleaseValidator : AbstractValidator<RollbackReleaseCommand>
{
    public RollbackReleaseValidator()
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

        RuleFor(x => x.TargetVersion)
            .GreaterThan(0)
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("TargetVersion должен быть больше нуля.");
    }
}
