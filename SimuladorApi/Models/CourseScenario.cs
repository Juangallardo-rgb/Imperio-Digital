using SimuladorApi.Models;

public class CourseScenario
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public int ScenarioId { get; set; }

    public Scenario? Scenario { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}