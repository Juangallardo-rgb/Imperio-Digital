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

        // NUEVO: estado actualizado después de enviar fase
        public decimal RemainingBudget { get; set; }

        public decimal RemainingTimeWeeks { get; set; }

        public decimal RiskLevel { get; set; }

        public string CurrentKpisJson { get; set; } = string.Empty;

        public string TriggeredEventJson { get; set; } = string.Empty;
    }
}