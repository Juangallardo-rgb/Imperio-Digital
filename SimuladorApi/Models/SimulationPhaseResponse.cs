namespace SimuladorApi.Models
{
    public class SimulationPhaseResponse
    {
        public int Id { get; set; }

        public int SimulationAttemptId { get; set; }

        public SimulationAttempt? SimulationAttempt { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public decimal Score { get; set; } = 0;

        public string Feedback { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public List<SimulationAnswer> Answers { get; set; } = new();
    }
}