namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using FluentValidation;

internal sealed class GetNodeCatalogValidator : AbstractValidator<GetNodeCatalogQuery>
{
    public GetNodeCatalogValidator()
    {
        RuleFor(x => x.PlatformType)
            .IsInEnum()
            .WithErrorCode(ErrorCodes.Validation.Invalid)
            .WithMessage("Указан неверный тип платформы.");
    }
}
