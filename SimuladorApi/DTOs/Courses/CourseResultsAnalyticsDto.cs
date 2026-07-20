using SimuladorApi.DTOs.DesignThinking;

namespace SimuladorApi.DTOs.Courses
{
    public class CourseResultsAnalyticsDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = string.Empty;

        public string CourseCode { get; set; } = string.Empty;

        public int TotalStudents { get; set; }

        public int TotalScenarios { get; set; }

        public List<CourseScenarioAnalyticsDto> Scenarios { get; set; } = new();
    }

    public class CourseScenarioAnalyticsDto
    {
        public int ScenarioId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = string.Empty;

        public string MethodologyName { get; set; } = string.Empty;

        public int TotalStudents { get; set; }

        public int StartedStudents { get; set; }

        public int CompletedStudents { get; set; }

        public int InProgressStudents { get; set; }

        public decimal CompletionRate { get; set; }

        public decimal? AverageScore { get; set; }

        public string StrongestPhase { get; set; } = string.Empty;

        public string PhaseToReinforce { get; set; } = string.Empty;

        public List<PhaseAnalyticsDto> PhaseAnalytics { get; set; } = new();

        public List<StudentScenarioResultDto> Students { get; set; } = new();
    }

    public class PhaseAnalyticsDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public decimal? AverageScore { get; set; }

        public int StudentsEvaluated { get; set; }

        public int ReinforcementCount { get; set; }

        public int DevelopingCount { get; set; }

        public int GoodPerformanceCount { get; set; }
    }

    public class StudentScenarioResultDto
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public int AttemptCount { get; set; }

        public int? LatestAttemptId { get; set; }

        public int? ReportAttemptId { get; set; }

        public string LatestAttemptStatus { get; set; } = "NotStarted";

        public DateTime? LatestAttemptStartedAt { get; set; }

        public DateTime? LatestAttemptFinishedAt { get; set; }

        public decimal? LatestCompletedScore { get; set; }

        public List<StudentPhaseResultDto> PhaseResults { get; set; } = new();
    }

    public class StudentPhaseResultDto
    {
        public string PhaseName { get; set; } = string.Empty;

        public int PhaseOrder { get; set; }

        public decimal Score { get; set; }
    }

    public class TeacherAttemptReportDto
    {
        public int AttemptId { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public int ScenarioId { get; set; }

        public string ScenarioTitle { get; set; } = string.Empty;

        public string MethodologyCode { get; set; } = string.Empty;

        public string MethodologyName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public decimal? FinalScore { get; set; }

        public string FinalFeedback { get; set; } = string.Empty;

        public string StrongestPhase { get; set; } = string.Empty;

        public string PhaseToReinforce { get; set; } = string.Empty;

        public bool IsCompleteReport { get; set; }

        public List<PhaseScoreDto> PhaseScores { get; set; } = new();

        public List<PhaseAnswerReviewDto> PhaseReviews { get; set; } = new();

        public List<KpiResultDto> KpiResults { get; set; } = new();

        public List<TeacherAttemptSummaryDto> Attempts { get; set; } = new();
    }

    public class TeacherAttemptSummaryDto
    {
        public int AttemptId { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public decimal? FinalScore { get; set; }
    }
}
