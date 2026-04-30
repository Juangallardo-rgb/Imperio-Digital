namespace SimuladorApi.DTOs.DesignThinking
{
    public class CreateScenarioOptionDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public string OptionType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public decimal Score { get; set; } = 0;

        public bool IsCorrect { get; set; } = false;

        public string ImpactJson { get; set; } = string.Empty;

        public int OrderIndex { get; set; }
    }
}