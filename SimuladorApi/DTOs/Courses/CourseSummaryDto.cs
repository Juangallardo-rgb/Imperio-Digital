namespace SimuladorApi.DTOs.Courses
{
    public class CourseSummaryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int StudentsCount { get; set; }

        public int ScenariosCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}