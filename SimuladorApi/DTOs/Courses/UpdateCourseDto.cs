namespace SimuladorApi.DTOs.Courses
{
    public class UpdateCourseDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}