namespace SimuladorApi.Services
{
    public interface IRealtimeNotificationService
    {
        Task NotifyCoursesChangedAsync(string action, int courseId);

        Task NotifyEnrollmentsChangedAsync(
            int courseId,
            int studentId);

        Task NotifyCourseScenariosChangedAsync(
            int courseId,
            int scenarioId);

        Task NotifyResultsChangedAsync(
            int? courseId,
            int studentId,
            int attemptId);
    }
}