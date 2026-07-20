namespace SimuladorApi.DTOs.DesignThinking
{
    public class GeneratedScenarioDraftDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string CompanyType { get; set; } = string.Empty;

        public string Problem { get; set; } = string.Empty;

        public string TargetUser { get; set; } = string.Empty;

        public string Constraints { get; set; } = string.Empty;

        public string Difficulty { get; set; } = "Media";

        public string LearningObjective { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = string.Empty;

        public Guid GenerationId { get; set; }

        public bool GeneratedByAi { get; set; }

        public string Provider { get; set; } = string.Empty;

        public string RequestedModel { get; set; } = string.Empty;

        public string? EffectiveModel { get; set; }

        public string PromptVersion { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }
    }
}
