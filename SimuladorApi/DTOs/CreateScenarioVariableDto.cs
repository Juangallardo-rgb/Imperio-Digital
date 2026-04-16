namespace SimuladorApi.DTOs
{
    public class CreateScenarioVariableDto
    {
        public int ScenarioId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Methodology { get; set; } = string.Empty;

        public string Phase { get; set; } = string.Empty;

        public string TargetKpi { get; set; } = string.Empty;

        public decimal Weight { get; set; }

        public decimal BaseValue { get; set; }

        public decimal MinValue { get; set; }

        public decimal MaxValue { get; set; }
    }
}