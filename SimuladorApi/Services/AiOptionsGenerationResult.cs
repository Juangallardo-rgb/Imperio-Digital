using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    internal static class AiOptionsGenerationErrorCodes
    {
        public const string OpenRouterTimeout = "OPENROUTER_TIMEOUT";
        public const string OpenRouterHttpError = "OPENROUTER_HTTP_ERROR";
        public const string OpenRouterEmptyResponse = "OPENROUTER_EMPTY_RESPONSE";
        public const string AiInvalidJson = "AI_INVALID_JSON";
        public const string AiInvalidSchema = "AI_INVALID_SCHEMA";
        public const string BpmMissingPhases = "BPM_MISSING_PHASES";
        public const string BpmInvalidPhaseNames = "BPM_INVALID_PHASE_NAMES";
        public const string DbSaveError = "DB_SAVE_ERROR";
        public const string UnknownError = "UNKNOWN_ERROR";
    }

    internal sealed class AiOptionsGenerationResult
    {
        public bool Success { get; init; }

        public string UserMessage { get; init; } = string.Empty;

        public string TechnicalReason { get; init; } = string.Empty;

        public string ErrorCode { get; init; } = string.Empty;

        public List<ScenarioOption> Options { get; init; } = new();

        public List<string> ExpectedPhases { get; init; } = new();

        public List<string> ReceivedPhases { get; init; } = new();

        public List<string> MissingPhases { get; init; } = new();

        public TimeSpan Duration { get; init; }

        public bool OpenRouterResponded { get; init; }

        public int? OpenRouterStatusCode { get; init; }

        public string RequestedModel { get; init; } = string.Empty;

        public string? EffectiveModel { get; init; }

        public string PromptVersion { get; init; } = string.Empty;

        public int RetryCount { get; init; }

        public string PromptHash { get; init; } = string.Empty;

        public string MethodologyCode { get; init; } = string.Empty;

        public string? FailedPhaseName { get; init; }

        public Guid CorrelationId { get; init; }

        public List<string> ValidationErrors { get; init; } = new();

        public string ResponseFormat { get; init; } = "none";
    }
}
