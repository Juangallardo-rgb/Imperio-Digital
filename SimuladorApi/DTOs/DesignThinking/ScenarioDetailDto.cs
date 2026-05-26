namespace SimuladorApi.DTOs.DesignThinking
{
    public class ScenarioDetailDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyType { get; set; } = string.Empty;

        public string Problem { get; set; } = string.Empty;

        public string TargetUser { get; set; } = string.Empty;

        public string Constraints { get; set; } = string.Empty;

        public string Methodology { get; set; } = string.Empty;

        public string MethodologyName { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public bool IsPublished { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableUntil { get; set; }

        public int MaxAttemptsPerStudent { get; set; }

        public bool AllowLateAttempts { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public List<PhaseSettingDetailDto> PhaseSettings { get; set; } = new();

        public List<ScenarioOptionDetailDto> Options { get; set; } = new();
    }

    public class PhaseSettingDetailDto
    {
        public int Id { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public decimal PhaseWeight { get; set; }

        public List<PhaseCriteriaDetailDto> Criteria { get; set; } = new();
    }

    public class PhaseCriteriaDetailDto
    {
        public int Id { get; set; }

        public string CriterionName { get; set; } = string.Empty;

        public decimal CriterionWeight { get; set; }

        public string EvaluationType { get; set; } = string.Empty;
    }

    public class ScenarioOptionDetailDto
    {
        public int Id { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public string OptionType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public bool IsCorrect { get; set; }

        public string ImpactJson { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public decimal Cost { get; set; }

        public decimal TimeCost { get; set; }

        public decimal RiskImpact { get; set; }

        public string TagsJson { get; set; } = string.Empty;

        public int MaxSelections { get; set; }

        public string ExpectedImpactLevel { get; set; } = string.Empty;

        public string ExpectedEffortLevel { get; set; } = string.Empty;

        public string ExpectedViabilityLevel { get; set; } = string.Empty;
    }
}