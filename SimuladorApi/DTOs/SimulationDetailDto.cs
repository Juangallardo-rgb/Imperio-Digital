namespace SimuladorApi.DTOs
{
    public class SimulationDetailDto
    {
        public int Id { get; set; }

        public string ScenarioName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public decimal DigitalMaturity { get; set; }

        public decimal OperationalEfficiency { get; set; }

        public decimal CustomerExperience { get; set; }

        public decimal GlobalScore { get; set; }

        public string FeedbackRule { get; set; } = string.Empty;

        public string AiFeedback { get; set; } = string.Empty;

        public List<SimulationVariableDetailDto> Variables { get; set; } = new();
    }
}