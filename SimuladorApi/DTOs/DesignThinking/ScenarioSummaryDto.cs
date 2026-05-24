namespace SimuladorApi.DTOs.DesignThinking
{
    public class ScenarioSummaryDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyType { get; set; } = string.Empty;

        public string Problem { get; set; } = string.Empty;

        public string TargetUser { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public bool IsPublished { get; set; }

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableUntil { get; set; }

        public int MaxAttemptsPerStudent { get; set; }

        public bool AllowLateAttempts { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}