using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.DTOs.Courses;
using SimuladorApi.Models;
using SimuladorApi.DTOs.DesignThinking;

namespace SimuladorApi.Services
{
    public class CourseService
    {
        private readonly AppDbContext _context;

        public CourseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CourseDetailDto> CreateCourseAsync(int teacherId, CreateCourseDto request)
        {
            var course = new Course
            {
                Name = request.Name,
                Description = request.Description,
                Code = GenerateCourseCode(),
                TeacherId = teacherId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return await GetCourseDetailAsync(course.Id, teacherId)
                   ?? throw new Exception("No se pudo recuperar el curso creado.");
        }

        public async Task<List<CourseSummaryDto>> GetMyCoursesAsync(int teacherId)
        {
            return await _context.Courses
                .Where(c => c.TeacherId == teacherId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CourseSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    StudentsCount = c.Enrollments.Count,
                    ScenariosCount = c.CourseScenarios.Count
                })
                .ToListAsync();
        }

        public async Task<CourseDetailDto?> GetCourseDetailAsync(int courseId, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .Include(c => c.CourseScenarios)
                    .ThenInclude(cs => cs.Scenario)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return null;

            return MapCourseDetail(course);
        }

        public async Task<CourseDetailDto?> UpdateCourseAsync(
            int courseId,
            int teacherId,
            UpdateCourseDto request)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return null;

            course.Name = request.Name;
            course.Description = request.Description;
            course.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return await GetCourseDetailAsync(course.Id, teacherId);
        }

        public async Task<List<CourseSummaryDto>> GetAvailableCoursesAsync(int studentId)
        {
            var enrolledCourseIds = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _context.Courses
                .Where(c => c.IsActive && !enrolledCourseIds.Contains(c.Id))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CourseSummaryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Code = c.Code,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    StudentsCount = c.Enrollments.Count,
                    ScenariosCount = c.CourseScenarios.Count
                })
                .ToListAsync();
        }

        public async Task<List<CourseSummaryDto>> GetEnrolledCoursesAsync(int studentId)
        {
            return await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)
                    .ThenInclude(c => c!.Enrollments)
                .Include(e => e.Course)
                    .ThenInclude(c => c!.CourseScenarios)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new CourseSummaryDto
                {
                    Id = e.Course!.Id,
                    Name = e.Course.Name,
                    Description = e.Course.Description,
                    Code = e.Course.Code,
                    IsActive = e.Course.IsActive,
                    CreatedAt = e.Course.CreatedAt,
                    StudentsCount = e.Course.Enrollments.Count,
                    ScenariosCount = e.Course.CourseScenarios.Count
                })
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> EnrollAsync(int courseId, int studentId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null)
                return (false, "Curso no encontrado o inactivo.");

            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);

            if (alreadyEnrolled)
                return (false, "Ya estás inscrito en este curso.");

            var enrollment = new CourseEnrollment
            {
                CourseId = courseId,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return (true, "Inscripción realizada correctamente.");
        }

        public async Task<(bool Success, string Message)> AssignScenarioToCourseAsync(
            int courseId,
            int scenarioId,
            int teacherId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return (false, "Curso no encontrado.");

            var scenario = await _context.Scenarios
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            var exists = await _context.CourseScenarios
                .AnyAsync(cs => cs.CourseId == courseId && cs.ScenarioId == scenarioId);

            if (exists)
                return (false, "El escenario ya está asignado a este curso.");

            _context.CourseScenarios.Add(new CourseScenario
            {
                CourseId = courseId,
                ScenarioId = scenarioId,
                AssignedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return (true, "Escenario asignado correctamente.");
        }

        public async Task<(bool Success, string Message)> AssignScenarioToCoursesAsync(
            int teacherId,
            AssignScenarioToCoursesDto request)
        {
            var scenario = await _context.Scenarios
                .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            List<int> targetCourseIds;

            if (request.AssignToAll)
            {
                targetCourseIds = await _context.Courses
                    .Where(c => c.TeacherId == teacherId && c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();
            }
            else
            {
                targetCourseIds = await _context.Courses
                    .Where(c => c.TeacherId == teacherId && request.CourseIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            if (!targetCourseIds.Any())
                return (false, "No hay cursos válidos para asignar.");

            var existing = await _context.CourseScenarios
                .Where(cs => cs.ScenarioId == request.ScenarioId && targetCourseIds.Contains(cs.CourseId))
                .Select(cs => cs.CourseId)
                .ToListAsync();

            var newAssignments = targetCourseIds
                .Where(courseId => !existing.Contains(courseId))
                .Select(courseId => new CourseScenario
                {
                    CourseId = courseId,
                    ScenarioId = request.ScenarioId,
                    AssignedAt = DateTime.UtcNow
                })
                .ToList();

            if (!newAssignments.Any())
                return (false, "El escenario ya estaba asignado a los cursos seleccionados.");

            _context.CourseScenarios.AddRange(newAssignments);
            await _context.SaveChangesAsync();

            return (true, $"Escenario asignado a {newAssignments.Count} curso(s).");
        }

        public async Task<CourseDetailDto?> GetStudentCourseDetailAsync(int courseId, int studentId)
        {
            var enrolled = await _context.CourseEnrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);

            if (!enrolled)
                return null;

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .Include(c => c.CourseScenarios)
                    .ThenInclude(cs => cs.Scenario)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return null;

            return MapCourseDetail(course);
        }

        public async Task<CourseResultsDto?> GetCourseResultsAsync(int courseId, int teacherId)
        {
            var course = await _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return null;

            var studentIds = course.Enrollments.Select(e => e.StudentId).ToList();

            var attempts = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                .Where(a => a.CourseId == courseId && studentIds.Contains(a.StudentId))
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            var finishedAttempts = attempts.Where(a => a.Status == "Finished").ToList();

            return new CourseResultsDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                StudentsCount = course.Enrollments.Count,
                FinishedAttempts = finishedAttempts.Count,
                AverageScore = finishedAttempts.Any()
                    ? Math.Round(finishedAttempts.Average(a => a.FinalScore), 2)
                    : 0,
                Students = course.Enrollments.Select(e => new StudentCourseResultDto
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.Name ?? "",
                    StudentEmail = e.Student?.Email ?? "",
                    Simulations = attempts
                        .Where(a => a.StudentId == e.StudentId)
                        .Select(a => new StudentSimulationResultItemDto
                        {
                            AttemptId = a.Id,
                            ScenarioId = a.ScenarioId,
                            ScenarioTitle = a.Scenario?.Title ?? "",
                            Status = a.Status,
                            FinalScore = a.FinalScore,
                            StartedAt = a.StartedAt,
                            FinishedAt = a.FinishedAt
                        })
                        .ToList()
                }).ToList()
            };
        }

        public async Task<SimulationResultsDto?> GetAttemptResultsForTeacherAsync(
    int courseId,
    int attemptId,
    int teacherId)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (!courseExists)
                return null;

            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                .Include(a => a.PhaseResponses)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.CourseId == courseId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            var phaseOrder = new List<string>
    {
        "Empatizar",
        "Definir",
        "Idear",
        "Prototipar",
        "Evaluar"
    };

            return new SimulationResultsDto
            {
                AttemptId = attempt.Id,
                ScenarioTitle = attempt.Scenario.Title,
                Status = attempt.Status,
                FinalScore = attempt.FinalScore,
                FinalFeedback = attempt.FinalFeedback,
                PhaseScores = attempt.PhaseResponses
                    .OrderBy(p => phaseOrder.IndexOf(p.PhaseName))
                    .Select(p => new PhaseScoreDto
                    {
                        PhaseName = p.PhaseName,
                        Score = p.Score,
                        Feedback = p.Feedback
                    })
                    .ToList(),
                KpiResults = attempt.KpiResults
                    .Select(k => new KpiResultDto
                    {
                        KpiName = k.KpiName,
                        InitialValue = k.InitialValue,
                        FinalValue = k.FinalValue,
                        Unit = k.Unit
                    })
                    .ToList()
            };
        }

        private static CourseDetailDto MapCourseDetail(Course course)
        {
            return new CourseDetailDto
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                Code = course.Code,
                IsActive = course.IsActive,
                CreatedAt = course.CreatedAt,
                Students = course.Enrollments.Select(e => new CourseStudentDto
                {
                    StudentId = e.StudentId,
                    Name = e.Student?.Name ?? "",
                    Email = e.Student?.Email ?? "",
                    EnrolledAt = e.EnrolledAt
                }).ToList(),
                Scenarios = course.CourseScenarios.Select(cs => new CourseScenarioDto
                {
                    ScenarioId = cs.ScenarioId,
                    Title = cs.Scenario?.Title ?? "",
                    Difficulty = cs.Scenario?.Difficulty ?? "",
                    IsPublished = cs.Scenario?.IsPublished ?? false,
                    AssignedAt = cs.AssignedAt
                }).ToList()
            };
        }

        private static string GenerateCourseCode()
        {
            var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"IMP-{random}";
        }
    }
}