namespace SimuladorApi.Models
{
    public class ScenarioPhaseSetting
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public decimal PhaseWeight { get; set; }

        public List<PhaseCriteriaSetting> Criteria { get; set; } = new();
    }
}