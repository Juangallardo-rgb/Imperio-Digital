namespace SimuladorApi.Models
{
    public class SimulationVariableValue
    {
        public int Id { get; set; }

        public int SimulationId { get; set; }

        public Simulation? Simulation { get; set; }

        public int ScenarioVariableId { get; set; }

        public ScenarioVariable? ScenarioVariable { get; set; }

        public decimal Value { get; set; }
    }
}