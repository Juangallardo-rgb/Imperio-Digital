namespace SimuladorApi.DTOs.DesignThinking;

public sealed class SelectedOptionSnapshotDto
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionType { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public decimal Score { get; set; }
    public string? Rationale { get; set; }
    public string ImpactJson { get; set; } = string.Empty;
    public string TagsJson { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal TimeCost { get; set; }
    public decimal RiskImpact { get; set; }
    public DateTime CapturedAt { get; set; }
}
