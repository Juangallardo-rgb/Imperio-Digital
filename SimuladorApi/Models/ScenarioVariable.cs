namespace SimuladorApi.Models
{
    public class ScenarioVariable
    {
        public int Id { get; set; }

        public int ScenarioId { get; set; }

        public Scenario? Scenario { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Methodology { get; set; } = string.Empty;
        // Ejemplos:
        // MadurezDigital
        // BPM
        // DesignThinking
        // LeanStartup

        public string Phase { get; set; } = string.Empty;
        // Ejemplos:
        // Diagnostico
        // Procesos
        // Usuario
        // Iteracion

        public string TargetKpi { get; set; } = string.Empty;
        // Ejemplos:
        // DigitalMaturity
        // OperationalEfficiency
        // CustomerExperience
        // GlobalScore

        public decimal Weight { get; set; }

        public decimal BaseValue { get; set; }

        public decimal MinValue { get; set; }

        public decimal MaxValue { get; set; }
    }
}