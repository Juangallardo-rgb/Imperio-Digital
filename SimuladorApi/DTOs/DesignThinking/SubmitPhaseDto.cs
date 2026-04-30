namespace SimuladorApi.DTOs.DesignThinking
{
    public class SubmitPhaseDto
    {
        public List<int> SelectedOptionIds { get; set; } = new();

        public string TextAnswer { get; set; } = string.Empty;
    }
}