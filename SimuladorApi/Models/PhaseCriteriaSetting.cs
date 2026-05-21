namespace SimuladorApi.Models
{
    public class PhaseCriteriaSetting
    {
        public int Id { get; set; }

        public int ScenarioPhaseSettingId { get; set; }

        public ScenarioPhaseSetting? ScenarioPhaseSetting { get; set; }

        public int? MethodologyPhaseCriteriaId { get; set; }

        public MethodologyPhaseCriteria? MethodologyPhaseCriteria { get; set; }

        public string CriterionName { get; set; } = string.Empty;

        public decimal CriterionWeight { get; set; }

        public string EvaluationType { get; set; } = string.Empty;
    }
}