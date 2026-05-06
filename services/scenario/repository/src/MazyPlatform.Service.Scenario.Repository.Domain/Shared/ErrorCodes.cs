namespace MazyPlatform.Service.Scenario.Repository.Domain.Shared;

public static class ErrorCodes
{
    public static class Validation
    {
        public const string Required = "validation.required";
        public const string Invalid = "validation.invalid";
        public const string TooLong = "validation.too_long";
    }

    public static class Project
    {
        public const string NotFound = "project.not_found";
        public const string AccessDenied = "project.access_denied";
        public const string PlatformMismatch = "project.platform_mismatch";
    }

    public static class ScenarioGraph
    {
        public const string NotFound = "scenario_graph.not_found";
        public const string VersionNotFound = "scenario_graph.version_not_found";
        public const string CannotDeleteCurrentVersion = "scenario_graph.cannot_delete_current_version";
        public const string VersionInUse = "scenario_graph.version_in_use";
        public const string NoReleasedVersion = "scenario_graph.no_released_version";
        public const string PromotionFailed = "scenario_graph.promotion_failed";
        public const string ValidationFailed = "scenario_graph.validation_failed";
    }

    public static class EntitySchema
    {
        public const string NotFound = "entity_schema.not_found";
        public const string AccessDenied = "entity_schema.access_denied";
    }

    public static class UserAccount
    {
        public const string NotFound = "user_account.not_found";
    }
}
