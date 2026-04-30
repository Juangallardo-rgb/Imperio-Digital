namespace SimuladorApi.DTOs.DesignThinking
{
    public class SimulationHistoryItemDto
    {
        public int AttemptId { get; set; }

        public int ScenarioId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public decimal FinalScore { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}