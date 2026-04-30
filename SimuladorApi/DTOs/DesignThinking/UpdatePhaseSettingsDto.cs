namespace SimuladorApi.DTOs.DesignThinking
{
    public class UpdatePhaseSettingsDto
    {
        public List<UpdatePhaseSettingDto> Phases { get; set; } = new();
    }
}