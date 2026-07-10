namespace SimuladorApi.DTOs.DesignThinking
{
    public class SimulationResultsDto
    {
        public int AttemptId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = string.Empty;

        public string MethodologyName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal FinalScore { get; set; }

        public string FinalFeedback { get; set; } = string.Empty;

        public List<PhaseScoreDto> PhaseScores { get; set; } = new();

        public List<PhaseAnswerReviewDto> PhaseReviews { get; set; } = new();

        public List<KpiResultDto> KpiResults { get; set; } = new();
    }

    public class PhaseScoreDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public string Feedback { get; set; } = string.Empty;
    }

    public class PhaseAnswerReviewDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public decimal SelectionScore { get; set; }

        public string SelectionFeedback { get; set; } = string.Empty;

        public string TextAnswer { get; set; } = string.Empty;

        public decimal TextAnswerScore { get; set; }

        public string TextAnswerFeedback { get; set; } = string.Empty;

        public List<OptionAnswerReviewDto> Options { get; set; } = new();
    }

    public class OptionAnswerReviewDto
    {
        public int OptionId { get; set; }

        public string OptionType { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public decimal Score { get; set; }

        public bool WasSelected { get; set; }

        public bool IsCorrect { get; set; }
    }

    public class KpiResultDto
    {
        public string KpiName { get; set; } = string.Empty;

        public decimal InitialValue { get; set; }

        public decimal FinalValue { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}
