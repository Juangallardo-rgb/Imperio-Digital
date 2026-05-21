namespace SimuladorApi.Models
{
    public class Methodology
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<MethodologyPhase> Phases { get; set; } = new();

        public List<Scenario> Scenarios { get; set; } = new();
    }
}