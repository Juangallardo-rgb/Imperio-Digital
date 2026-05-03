namespace SimuladorApi.DTOs.DesignThinking
{
    public class CurrentSimulationDto
    {
        public int AttemptId { get; set; }

        public int ScenarioId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CurrentPhaseName { get; set; } = string.Empty;

        public int CurrentPhaseOrder { get; set; }

        public List<string> CompletedPhases { get; set; } = new();

        public List<ScenarioOptionDetailDto> CurrentPhaseOptions { get; set; } = new();

        // NUEVO: estado visible de la simulación
        public decimal InitialBudget { get; set; }

        public decimal RemainingBudget { get; set; }

        public decimal InitialTimeWeeks { get; set; }

        public decimal RemainingTimeWeeks { get; set; }

        public decimal RiskLevel { get; set; }

        public string CurrentKpisJson { get; set; } = string.Empty;

        public string DecisionTraceJson { get; set; } = string.Empty;

        public string TriggeredEventsJson { get; set; } = string.Empty;
    }
}