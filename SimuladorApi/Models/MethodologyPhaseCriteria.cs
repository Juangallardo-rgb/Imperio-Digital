namespace SimuladorApi.Models
{
    public class MethodologyPhaseCriteria
    {
        public int Id { get; set; }

        public int MethodologyPhaseId { get; set; }

        public MethodologyPhase? MethodologyPhase { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal DefaultWeight { get; set; }

        public string EvaluationType { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}