namespace SimuladorApi.DTOs.Courses
{
    public class CourseDetailDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<CourseStudentDto> Students { get; set; } = new();

        public List<CourseScenarioDto> Scenarios { get; set; } = new();
    }

    public class CourseStudentDto
    {
        public int StudentId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime EnrolledAt { get; set; }
    }

    public class CourseScenarioDto
    {
        public int ScenarioId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Difficulty { get; set; } = string.Empty;

        public bool IsPublished { get; set; }

        public DateTime AssignedAt { get; set; }
    }
}