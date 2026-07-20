namespace SimuladorApi.Models
{
    public class Scenario
    {
        public int Id { get; set; }

        // Nombre anterior, lo mantenemos para no romper pantallas actuales
        public string Name { get; set; } = string.Empty;

        // Nuevo campo principal del escenario
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyType { get; set; } = string.Empty;

        public string Problem { get; set; } = string.Empty;

        public string TargetUser { get; set; } = string.Empty;

        public string Constraints { get; set; } = string.Empty;

        public string Methodology { get; set; } = "DesignThinking";

        public int? MethodologyId { get; set; }

        public Methodology? MethodologyCatalog { get; set; }

        public string Difficulty { get; set; } = "Media";

        public DateTime? AvailableFrom { get; set; }

        public DateTime? AvailableUntil { get; set; }

        public int MaxAttemptsPerStudent { get; set; } = 1;

        public bool AllowLateAttempts { get; set; } = false;

        public bool IsPublished { get; set; } = false;

        public string CreationMode { get; set; } = "Legacy";

        public bool GeneratedByAi { get; set; }

        public string? AiProvider { get; set; }

        public string? AiModel { get; set; }

        public string? AiPromptVersion { get; set; }

        public DateTime? AiGeneratedAt { get; set; }

        public int CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Flujo anterior: lo dejamos para no romper lo ya hecho
        public List<ScenarioVariable> Variables { get; set; } = new();

        // Nuevo flujo Design Thinking
        public List<ScenarioPhaseSetting> PhaseSettings { get; set; } = new();

        public List<ScenarioOption> Options { get; set; } = new();

        public List<SimulationAttempt> SimulationAttempts { get; set; } = new();

        public List<AiGenerationRecord> AiGenerationRecords { get; set; } = new();
    }
}
