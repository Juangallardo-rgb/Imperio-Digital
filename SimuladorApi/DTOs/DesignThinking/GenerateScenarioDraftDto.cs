namespace SimuladorApi.DTOs.DesignThinking
{
    public class GenerateScenarioDraftDto
    {
        public string Methodology { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = string.Empty;

        public string? Topic { get; set; }

        public string? CompanyType { get; set; }

        public string? Difficulty { get; set; }

        public string? AdditionalInstructions { get; set; }

        public string ResolveMethodologyCode() =>
            !string.IsNullOrWhiteSpace(MethodologyCode)
                ? MethodologyCode.Trim()
                : Methodology.Trim();
    }
}
