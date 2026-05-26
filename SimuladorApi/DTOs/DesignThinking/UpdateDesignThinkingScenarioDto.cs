namespace SimuladorApi.DTOs.DesignThinking
{
    public class UpdateDesignThinkingScenarioDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyType { get; set; } = string.Empty;

        public string Problem { get; set; } = string.Empty;

        public string TargetUser { get; set; } = string.Empty;

        public string Constraints { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = "DesignThinking";

        public string Difficulty { get; set; } = "Media";

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableUntil { get; set; }

        public int MaxAttemptsPerStudent { get; set; } = 1;

        public bool AllowLateAttempts { get; set; } = false;
    }
}