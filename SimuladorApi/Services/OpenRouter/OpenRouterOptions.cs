namespace SimuladorApi.Services.OpenRouter;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "openrouter/auto";
    public string ScenarioModel { get; init; } = "openrouter/free";
    public string EvaluationModel { get; init; } = string.Empty;
    public string FeedbackModel { get; init; } = string.Empty;
    public string SiteUrl { get; init; } = string.Empty;
    public string SiteName { get; init; } = "SimuladorApi";
    public int TimeoutSeconds { get; init; } = 180;
    public int OptionsGenerationTimeoutSeconds { get; init; } = 90;
    public int MaxRetries { get; init; } = 2;
    public int DraftValidityMinutes { get; init; } = 60;
    public string PromptVersion { get; init; } = "v1";
    public bool AllowJsonObjectFallback { get; init; }

    public string ResolveScenarioModel() => ResolveModel(ScenarioModel);
    public string ResolveEvaluationModel() => ResolveModel(EvaluationModel);
    public string ResolveFeedbackModel() => ResolveModel(FeedbackModel);

    private string ResolveModel(string preferredModel) =>
        string.IsNullOrWhiteSpace(preferredModel) ? Model : preferredModel;
}
