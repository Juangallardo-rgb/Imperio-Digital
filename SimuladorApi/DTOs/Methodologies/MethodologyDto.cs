namespace SimuladorApi.DTOs.Methodologies
{
    public class MethodologyDto
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<MethodologyPhaseDto> Phases { get; set; } = new();
    }

    public class MethodologyPhaseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal DefaultWeight { get; set; }

        public int DefaultMaxSelections { get; set; }

        public List<MethodologyPhaseCriteriaDto> Criteria { get; set; } = new();
    }

    public class MethodologyPhaseCriteriaDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal DefaultWeight { get; set; }

        public string EvaluationType { get; set; } = string.Empty;
    }
}