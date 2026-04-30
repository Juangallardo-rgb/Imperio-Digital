namespace SimuladorApi.DTOs.DesignThinking
{
    public class UpdatePhaseSettingDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public decimal PhaseWeight { get; set; }
    }
}