namespace SimuladorApi.DTOs.Courses
{
    public class CourseResultsDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public int StudentsCount { get; set; }

        public int FinishedAttempts { get; set; }

        public decimal AverageScore { get; set; }

        public List<StudentCourseResultDto> Students { get; set; } = new();
    }

    public class StudentCourseResultDto
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public List<StudentSimulationResultItemDto> Simulations { get; set; } = new();
    }

    public class StudentSimulationResultItemDto
    {
        public int AttemptId { get; set; }

        public int ScenarioId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal FinalScore { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }
    }
}