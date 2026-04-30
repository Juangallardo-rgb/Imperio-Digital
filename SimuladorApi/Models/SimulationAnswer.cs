namespace SimuladorApi.Models
{
    public class SimulationAnswer
    {
        public int Id { get; set; }

        public int SimulationPhaseResponseId { get; set; }

        public SimulationPhaseResponse? SimulationPhaseResponse { get; set; }

        public string QuestionType { get; set; } = string.Empty;

        public string SelectedOptionIdsJson { get; set; } = string.Empty;

        public string TextAnswer { get; set; } = string.Empty;

        public decimal Score { get; set; } = 0;

        public string Feedback { get; set; } = string.Empty;
    }
}