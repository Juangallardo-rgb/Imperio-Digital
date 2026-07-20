namespace SimuladorApi.Models
{
    public class SimulationAnswer
    {
        public int Id { get; set; }

        public int SimulationPhaseResponseId { get; set; }

        public SimulationPhaseResponse? SimulationPhaseResponse { get; set; }

        public string QuestionType { get; set; } = string.Empty;

        public string SelectedOptionIdsJson { get; set; } = string.Empty;

        public string SelectedOptionsSnapshotJson { get; set; } = string.Empty;

        public string TextAnswer { get; set; } = string.Empty;

        public decimal Score { get; set; } = 0;

        public string Feedback { get; set; } = string.Empty;

        public string TextEvaluationStatus { get; set; } = string.Empty;

        public string TextEvaluationJson { get; set; } = string.Empty;

        public string? TextEvaluationProvider { get; set; }

        public string? TextEvaluationModel { get; set; }

        public string? TextEvaluationPromptVersion { get; set; }

        public DateTime? TextEvaluatedAt { get; set; }
    }
}
