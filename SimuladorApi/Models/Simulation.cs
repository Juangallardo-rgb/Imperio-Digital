namespace SimuladorApi.Models
{
    public class Simulation
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal DigitalMaturity { get; set; }

        public decimal OperationalEfficiency { get; set; }

        public decimal CustomerExperience { get; set; }

        public decimal GlobalScore { get; set; }

        public string FeedbackRule { get; set; } = string.Empty;

        public string AiFeedback { get; set; } = string.Empty;

        public List<SimulationVariableValue> VariableValues { get; set; } = new();
    }
}