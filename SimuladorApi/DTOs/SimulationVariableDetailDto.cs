namespace SimuladorApi.DTOs
{
    public class SimulationVariableDetailDto
    {
        public int ScenarioVariableId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Methodology { get; set; } = string.Empty;

        public string Phase { get; set; } = string.Empty;

        public string TargetKpi { get; set; } = string.Empty;

        public decimal Value { get; set; }
    }
}