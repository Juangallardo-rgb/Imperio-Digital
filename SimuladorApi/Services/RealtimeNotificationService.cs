using Microsoft.AspNetCore.SignalR;
using SimuladorApi.Hubs;

namespace SimuladorApi.Services
{
    public class RealtimeNotificationService : IRealtimeNotificationService
    {
        private readonly IHubContext<RealtimeHub> _hubContext;

        public RealtimeNotificationService(
            IHubContext<RealtimeHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyCoursesChangedAsync(
            string action,
            int courseId)
        {
            return _hubContext.Clients.All.SendAsync(
                "CoursesChanged",
                new
                {
                    action,
                    courseId,
                    occurredAtUtc = DateTime.UtcNow
                });
        }

        public Task NotifyEnrollmentsChangedAsync(
            int courseId,
            int studentId)
        {
            return _hubContext.Clients.All.SendAsync(
                "EnrollmentsChanged",
                new
                {
                    courseId,
                    studentId,
                    occurredAtUtc = DateTime.UtcNow
                });
        }

        public Task NotifyCourseScenariosChangedAsync(
            int courseId,
            int scenarioId)
        {
            return _hubContext.Clients.All.SendAsync(
                "CourseScenariosChanged",
                new
                {
                    courseId,
                    scenarioId,
                    occurredAtUtc = DateTime.UtcNow
                });
        }

        public Task NotifyResultsChangedAsync(
            int? courseId,
            int studentId,
            int attemptId)
        {
            return _hubContext.Clients.All.SendAsync(
                "ResultsChanged",
                new
                {
                    courseId,
                    studentId,
                    attemptId,
                    occurredAtUtc = DateTime.UtcNow
                });
        }
    }
}