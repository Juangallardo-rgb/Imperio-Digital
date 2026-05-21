namespace SimuladorApi.Models
{
    public class MethodologyPhase
    {
        public int Id { get; set; }

        public int MethodologyId { get; set; }

        public Methodology? Methodology { get; set; }

        public string Name { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal DefaultWeight { get; set; }

        public string ActivityType { get; set; } = "SelectionAndText";

        public int DefaultMaxSelections { get; set; } = 3;

        public bool IsActive { get; set; } = true;

        public List<MethodologyPhaseCriteria> Criteria { get; set; } = new();
    }
}