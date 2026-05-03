namespace SimuladorApi.DTOs.Courses
{
    public class AssignScenarioToCoursesDto
    {
        public int ScenarioId { get; set; }

        public List<int> CourseIds { get; set; } = new();

        public bool AssignToAll { get; set; } = false;
    }
}