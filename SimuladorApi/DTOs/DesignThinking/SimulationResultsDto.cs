namespace SimuladorApi.DTOs.DesignThinking
{
    public class SimulationResultsDto
    {
        public int AttemptId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal FinalScore { get; set; }

        public string FinalFeedback { get; set; } = string.Empty;

        public List<PhaseScoreDto> PhaseScores { get; set; } = new();

        public List<KpiResultDto> KpiResults { get; set; } = new();
    }

    public class PhaseScoreDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public string Feedback { get; set; } = string.Empty;
    }

    public class KpiResultDto
    {
        public string KpiName { get; set; } = string.Empty;

        public decimal InitialValue { get; set; }

        public decimal FinalValue { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}