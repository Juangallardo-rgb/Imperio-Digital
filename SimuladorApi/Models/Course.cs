namespace SimuladorApi.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int TeacherId { get; set; }

        public User? Teacher { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<CourseEnrollment> Enrollments { get; set; } = new();

        public List<CourseScenario> CourseScenarios { get; set; } = new();
    }
}