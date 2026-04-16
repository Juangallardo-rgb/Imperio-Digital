namespace SimuladorApi.DTOs
{
    public class CreateSimulationDto
    {
        public int ScenarioId { get; set; }

        public List<SimulationVariableInputDto> Variables { get; set; } = new();
    }
}