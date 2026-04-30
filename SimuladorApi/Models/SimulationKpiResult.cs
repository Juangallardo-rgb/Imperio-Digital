namespace SimuladorApi.Models
{
    public class SimulationKpiResult
    {
        public int Id { get; set; }

        public int SimulationAttemptId { get; set; }

        public SimulationAttempt? SimulationAttempt { get; set; }

        public string KpiName { get; set; } = string.Empty;

        public decimal InitialValue { get; set; }

        public decimal FinalValue { get; set; }

        public string Unit { get; set; } = string.Empty;
    }
}