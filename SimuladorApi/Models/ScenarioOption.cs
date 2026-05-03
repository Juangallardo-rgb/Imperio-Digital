namespace SimuladorApi.Models
{
    public class ScenarioOption
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public string OptionType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public decimal Score { get; set; } = 0;

        public bool IsCorrect { get; set; } = false;

        public string ImpactJson { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        // NUEVO: metadata para cartas de decisión
        public decimal Cost { get; set; } = 0;

        public decimal TimeCost { get; set; } = 0;

        public decimal RiskImpact { get; set; } = 0;

        public string TagsJson { get; set; } = string.Empty;

        public int MaxSelections { get; set; } = 0;

        public string ExpectedImpactLevel { get; set; } = string.Empty;

        public string ExpectedEffortLevel { get; set; } = string.Empty;

        public string ExpectedViabilityLevel { get; set; } = string.Empty;
    }
}