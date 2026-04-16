namespace SimuladorApi.Models
{
    public class Scenario
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public List<ScenarioVariable> Variables { get; set; } = new();
    }
}