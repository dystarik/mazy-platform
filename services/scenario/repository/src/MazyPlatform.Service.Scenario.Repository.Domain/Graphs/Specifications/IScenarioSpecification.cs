namespace MazyPlatform.Service.Scenario.Repository.Domain.Graphs.Specifications;

public interface IScenarioSpecification
{
    Result IsSatisfiedBy(string graphJson);
}
