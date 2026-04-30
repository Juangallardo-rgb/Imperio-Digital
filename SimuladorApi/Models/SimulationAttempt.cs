namespace SimuladorApi.Models
{
    public class SimulationAttempt
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public int StudentId { get; set; }

        public User? Student { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? FinishedAt { get; set; }

        public decimal FinalScore { get; set; } = 0;

        public string FinalFeedback { get; set; } = string.Empty;

        public string Status { get; set; } = "InProgress";

        public List<SimulationPhaseResponse> PhaseResponses { get; set; } = new();

        public List<SimulationKpiResult> KpiResults { get; set; } = new();
    }
}