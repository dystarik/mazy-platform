namespace MazyPlatform.Service.Bot.Manager.Domain.Common;

public static class ErrorCodes
{
    public static class BotInstance
    {
        public const string NotFound = "bot.not_found";
        public const string AccessDenied = "bot.access_denied";
        public const string ProjectNotFound = "bot.project_not_found";
        public const string ScenarioVersionNotFound = "bot.scenario_version_not_found";
        public const string PlatformMismatch = "bot.platform_mismatch";
        public const string AlreadyActive = "bot.already_active";
        public const string AlreadyInactive = "bot.already_inactive";
        public const string CannotActivateUnbound = "bot.cannot_activate_unbound";
        public const string AlreadyBound = "bot.already_bound";
        public const string CannotChangeVersionWhenUnbound = "bot.cannot_change_version_when_unbound";
        public const string AlreadyUnbound = "bot.already_unbound";
        public const string InvalidScenarioVersionUpdateMode = "bot.invalid_scenario_version_update_mode";
    }

    public static class Validation
    {
        public const string Required = "validation.required";
        public const string Invalid = "validation.invalid";
        public const string TooLong = "validation.too_long";
    }
}
