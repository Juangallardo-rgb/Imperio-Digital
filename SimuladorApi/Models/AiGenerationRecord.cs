namespace SimuladorApi.Models;

public sealed class AiGenerationRecord
{
    public int Id { get; set; }
    public int? ScenarioId { get; set; }
    public Scenario? Scenario { get; set; }
    public int RequestedByUserId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? MethodologyCode { get; set; }
    public string Provider { get; set; } = "OpenRouter";
    public string RequestedModel { get; set; } = string.Empty;
    public string? EffectiveModel { get; set; }
    public string PromptVersion { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public string? PromptHash { get; set; }
    public string? ResponseHash { get; set; }
    public string ResponseFormat { get; set; } = "none";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
