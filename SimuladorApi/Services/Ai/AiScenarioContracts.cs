using System.Text.Json.Serialization;

namespace SimuladorApi.Services.Ai;

public sealed class AiScenarioDraftContent
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string Problem { get; set; } = string.Empty;
    public string TargetUser { get; set; } = string.Empty;
    public string Constraints { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string LearningObjective { get; set; } = string.Empty;
    public string MethodologyCode { get; set; } = string.Empty;
}

public sealed class AiPhaseOptionsContent
{
    public string PhaseName { get; set; } = string.Empty;
    public List<AiScenarioOptionContent> Options { get; set; } = new();
}

public sealed class AiScenarioOptionContent
{
    public string OptionType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsBestOption { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public Dictionary<string, decimal> Impact { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public decimal Cost { get; set; }
    public decimal TimeCost { get; set; }
    public decimal RiskImpact { get; set; }
    public int MaxSelections { get; set; }
    public string ExpectedImpactLevel { get; set; } = string.Empty;
    public string ExpectedEffortLevel { get; set; } = string.Empty;
    public string ExpectedViabilityLevel { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}

public sealed record AiValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static AiValidationResult Valid { get; } = new(true, Array.Empty<string>());
}
