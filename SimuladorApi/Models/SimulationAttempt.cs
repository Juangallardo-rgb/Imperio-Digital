namespace SimuladorApi.Models
{
    public class SimulationAttempt
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public int StudentId { get; set; }

        public User? Student { get; set; }

        public int? CourseId { get; set; }

        public Course? Course { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? FinishedAt { get; set; }

        public decimal FinalScore { get; set; } = 0;

        public string FinalFeedback { get; set; } = string.Empty;

        public string Status { get; set; } = "InProgress";

        // NUEVO: fase actual persistente
        public string CurrentPhase { get; set; } = "Empatizar";

        // NUEVO: recursos de la simulación
        public decimal InitialBudget { get; set; } = 100;

        public decimal RemainingBudget { get; set; } = 100;

        public decimal InitialTimeWeeks { get; set; } = 8;

        public decimal RemainingTimeWeeks { get; set; } = 8;

        public decimal RiskLevel { get; set; } = 20;

        // NUEVO: KPIs y trazabilidad
        public string InitialKpisJson { get; set; } = string.Empty;

        public string CurrentKpisJson { get; set; } = string.Empty;

        public string DecisionTraceJson { get; set; } = string.Empty;

        public string TriggeredEventsJson { get; set; } = string.Empty;

        public List<SimulationPhaseResponse> PhaseResponses { get; set; } = new();

        public List<SimulationKpiResult> KpiResults { get; set; } = new();
    }
}