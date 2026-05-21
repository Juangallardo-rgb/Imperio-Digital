namespace SimuladorApi.Models
{
    public class ScenarioPhaseSetting
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public int? MethodologyPhaseId { get; set; }

        public MethodologyPhase? MethodologyPhase { get; set; }

        public string PhaseName { get; set; } = string.Empty;

        public string CustomName { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public decimal PhaseWeight { get; set; }

        public bool IsEnabled { get; set; } = true;

        public List<PhaseCriteriaSetting> Criteria { get; set; } = new();
    }
}