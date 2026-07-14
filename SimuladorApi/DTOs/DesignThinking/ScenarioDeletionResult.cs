namespace SimuladorApi.DTOs.DesignThinking
{
    public enum ScenarioDeletionStatus
    {
        Deleted,
        NotFound,
        Forbidden,
        Failed
    }

    public class ScenarioDeletionResult
    {
        public ScenarioDeletionStatus Status { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
