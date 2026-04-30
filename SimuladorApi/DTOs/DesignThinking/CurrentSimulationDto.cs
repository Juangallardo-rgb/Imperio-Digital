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
    }
}