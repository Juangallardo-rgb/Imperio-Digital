namespace SimuladorApi.DTOs.DesignThinking
{
    public class SubmitPhaseResultDto
    {
        public int AttemptId { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public string Feedback { get; set; } = string.Empty;

        public string NextPhaseName { get; set; } = string.Empty;

        public bool IsLastPhase { get; set; }
    }
}