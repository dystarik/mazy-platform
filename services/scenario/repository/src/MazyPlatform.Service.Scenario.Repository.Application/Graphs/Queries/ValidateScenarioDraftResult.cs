namespace MazyPlatform.Service.Scenario.Repository.Application.Graphs.Queries;

using MazyPlatform.Scenario.Abstractions.Scenarios.Validation;

public sealed record ValidateScenarioDraftResult(bool IsValid, IReadOnlyList<ScenarioValidationError> Errors);
