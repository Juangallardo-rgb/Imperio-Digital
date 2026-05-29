namespace SimuladorApi.DTOs.Courses
{
    public class TeacherDashboardAnalyticsDto
    {
        public TeacherDashboardSummaryDto Summary { get; set; } = new();

        public List<CourseAverageDto> CourseAverages { get; set; } = new();

        public List<MethodologyAverageDto> MethodologyAverages { get; set; } = new();

        public List<CompletionStatusDto> CompletionStatus { get; set; } = new();

        public List<LowPerformanceCourseDto> LowPerformanceCourses { get; set; } = new();
    }

    public class TeacherDashboardSummaryDto
    {
        public int CoursesCount { get; set; }

        public int StudentsCount { get; set; }

        public int ActiveStudentsCount { get; set; }

        public int ScenariosCount { get; set; }

        public int TotalAttempts { get; set; }

        public int FinishedAttempts { get; set; }

        public decimal AverageScore { get; set; }

        public decimal CompletionRate { get; set; }

        public string BestCourseName { get; set; } = "Sin datos";

        public decimal BestCourseScore { get; set; }

        public string WorstCourseName { get; set; } = "Sin datos";

        public decimal WorstCourseScore { get; set; }

        public string TopMethodologyName { get; set; } = "Sin datos";

        public decimal TopMethodologyScore { get; set; }

        public int RiskCoursesCount { get; set; }
    }

    public class CourseAverageDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public int StudentsCount { get; set; }

        public int SimulationsCount { get; set; }

        public decimal AverageScore { get; set; }
    }

    public class MethodologyAverageDto
    {
        public string MethodologyCode { get; set; } = string.Empty;

        public string MethodologyName { get; set; } = string.Empty;

        public int SimulationsCount { get; set; }

        public decimal AverageScore { get; set; }
    }

    public class CompletionStatusDto
    {
        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    public class LowPerformanceCourseDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public int SimulationsCount { get; set; }

        public decimal AverageScore { get; set; }
    }
}