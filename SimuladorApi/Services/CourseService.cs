using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.DTOs.Courses;
using SimuladorApi.Models;
using SimuladorApi.DTOs.DesignThinking;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SimuladorApi.Services
{
    public class CourseService
    {
        private const int MaxImportRows = 200;
        private const long MaxCsvFileBytes = 512 * 1024;
        private const decimal DevelopingPerformanceThreshold = 60;
        private const decimal GoodPerformanceThreshold = 80;

        private readonly AppDbContext _context;
        private readonly IRealtimeNotificationService _realtime;

        public CourseService(
            AppDbContext context,
            IRealtimeNotificationService realtime)
        {
            _context = context;
            _realtime = realtime;
        }

        public async Task<CourseDetailDto> CreateCourseAsync(int teacherId, CreateCourseDto request)
        {
            var course = new Course
            {
                Name = request.Name,
                Description = request.Description,
                Code = await GenerateUniqueCourseCodeAsync(),
                TeacherId = teacherId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            await _realtime.NotifyCoursesChangedAsync(
                 "Created",
                 course.Id
             );

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
            await _realtime.NotifyCoursesChangedAsync(
                "Updated",
                course.Id
            );

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
            await _realtime.NotifyEnrollmentsChangedAsync(
                courseId,
                studentId
            );

            return (true, "Inscripción realizada correctamente.");
        }

        public async Task<(bool Success, string Message)> JoinByCodeAsync(
            int studentId,
            JoinCourseByCodeDto request)
        {
            var code = NormalizeCourseCode(request.Code);

            if (string.IsNullOrWhiteSpace(code))
                return (false, "El código de curso no es válido.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Code == code);

            if (course == null)
                return (false, "El código de curso no es válido.");

            if (!course.IsActive)
                return (false, "El curso no está disponible.");

            return await EnrollAsync(course.Id, studentId);
        }

        public async Task<(bool Success, string Message, ImportStudentsResultDto? Result)> ImportStudentsAsync(
            int courseId,
            int teacherId,
            IFormFile? file)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return (false, "Curso no encontrado.", null);

            var fileValidation = ValidateCsvFile(file);

            if (!fileValidation.Success)
                return (false, fileValidation.Message, null);

            string csvContent;

            using (var reader = new StreamReader(
                file!.OpenReadStream(),
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            var parseResult = ParseStudentCsv(csvContent);

            if (!parseResult.Success)
                return (false, parseResult.Message, null);

            var validRows = parseResult.ValidRows;
            var result = parseResult.Result;

            if (!validRows.Any())
            {
                result.FailedRows = result.Errors.Count;
                return (true, "No se encontraron filas válidas para importar.", result);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var emails = validRows.Select(r => r.Email).ToList();

                var existingUsers = await _context.Users
                    .Where(u => emails.Contains(u.Email.ToLower()))
                    .ToDictionaryAsync(u => u.Email.ToLower(), u => u);

                var enrolledStudentIds = (await _context.CourseEnrollments
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.StudentId)
                    .ToListAsync())
                    .ToHashSet();

                foreach (var row in validRows)
                {
                    if (existingUsers.TryGetValue(row.Email, out var existingUser))
                    {
                        if (existingUser.Role != "Estudiante")
                        {
                            result.Errors.Add(new ImportStudentErrorDto
                            {
                                RowNumber = row.RowNumber,
                                Name = row.Name,
                                Email = row.Email,
                                Message = "El correo pertenece a una cuenta docente."
                            });
                            continue;
                        }

                        if (enrolledStudentIds.Contains(existingUser.Id))
                        {
                            result.AlreadyEnrolled++;
                            continue;
                        }

                        _context.CourseEnrollments.Add(new CourseEnrollment
                        {
                            CourseId = courseId,
                            StudentId = existingUser.Id,
                            EnrolledAt = DateTime.UtcNow
                        });

                        enrolledStudentIds.Add(existingUser.Id);
                        result.ExistingStudentsEnrolled++;
                        continue;
                    }

                    var temporaryPassword = GenerateTemporaryPassword();

                    var newUser = new User
                    {
                        Name = row.Name,
                        Email = row.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
                        Role = "Estudiante",
                        MustChangePassword = true
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    _context.CourseEnrollments.Add(new CourseEnrollment
                    {
                        CourseId = courseId,
                        StudentId = newUser.Id,
                        EnrolledAt = DateTime.UtcNow
                    });

                    existingUsers[row.Email] = newUser;
                    enrolledStudentIds.Add(newUser.Id);
                    result.NewUsersCreated++;

                    result.Credentials.Add(new TemporaryCredentialDto
                    {
                        Name = newUser.Name,
                        Email = newUser.Email,
                        TemporaryPassword = temporaryPassword,
                        CourseCode = course.Code
                    });
                }

                result.FailedRows = result.Errors.Count;
                result.ValidRows =
                    result.NewUsersCreated +
                    result.ExistingStudentsEnrolled +
                    result.AlreadyEnrolled;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            try
            {
                await _realtime.NotifyEnrollmentsChangedAsync(courseId, 0);
            }
            catch
            {
                // La importación ya fue guardada; una falla de SignalR no debe revertirla.
            }

            return (true, "Importación completada.", result);
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
            await _realtime.NotifyCourseScenariosChangedAsync(
                courseId,
                scenarioId
            );

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
            await Task.WhenAll(
                newAssignments.Select(assignment =>
                    _realtime.NotifyCourseScenariosChangedAsync(
                        assignment.CourseId,
                        assignment.ScenarioId
                    )
                )
            );

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
                .Include(a => a.Student)
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
                            ScenarioTitle = string.IsNullOrWhiteSpace(a.Scenario?.Title)
                                ? a.Scenario?.Name ?? ""
                                : a.Scenario.Title,
                            Methodology = a.Scenario?.Methodology ?? "",
                            MethodologyName = GetMethodologyName(a.Scenario?.Methodology ?? ""),
                            Status = a.Status,
                            FinalScore = a.FinalScore,
                            StartedAt = a.StartedAt,
                            FinishedAt = a.FinishedAt
                        })
                        .ToList()
                }).ToList()
            };
        }

        public async Task<CourseResultsAnalyticsDto?> GetCourseResultsAnalyticsAsync(
            int courseId,
            int teacherId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .Include(c => c.CourseScenarios)
                    .ThenInclude(cs => cs.Scenario)
                        .ThenInclude(s => s!.PhaseSettings)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (course == null)
                return null;

            var studentIds = course.Enrollments
                .Select(enrollment => enrollment.StudentId)
                .ToList();

            var scenarioIds = course.CourseScenarios
                .Select(assignment => assignment.ScenarioId)
                .ToList();

            var attempts = studentIds.Count == 0 || scenarioIds.Count == 0
                ? new List<SimulationAttempt>()
                : await _context.SimulationAttempts
                    .AsNoTracking()
                    .Include(attempt => attempt.PhaseResponses)
                    .Where(attempt =>
                        attempt.CourseId == courseId &&
                        studentIds.Contains(attempt.StudentId) &&
                        scenarioIds.Contains(attempt.ScenarioId))
                    .ToListAsync();

            var enrolledStudents = course.Enrollments
                .OrderBy(enrollment => enrollment.Student?.Name ?? string.Empty)
                .ThenBy(enrollment => enrollment.Student?.Email ?? string.Empty)
                .ToList();

            var scenarioAnalytics = course.CourseScenarios
                .Where(assignment => assignment.Scenario != null)
                .OrderBy(assignment => assignment.AssignedAt)
                .Select(assignment =>
                {
                    var scenario = assignment.Scenario!;
                    var scenarioAttempts = attempts
                        .Where(attempt => attempt.ScenarioId == scenario.Id)
                        .ToList();

                    var phaseDefinitions = GetScenarioPhaseDefinitions(
                        scenario,
                        scenarioAttempts.SelectMany(attempt => attempt.PhaseResponses)
                    );

                    var latestAttemptsByStudent = scenarioAttempts
                        .GroupBy(attempt => attempt.StudentId)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderByDescending(attempt => attempt.StartedAt)
                                .ThenByDescending(attempt => attempt.Id)
                                .First()
                        );

                    var latestCompletedByStudent = scenarioAttempts
                        .Where(attempt => IsFinished(attempt.Status))
                        .GroupBy(attempt => attempt.StudentId)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderByDescending(attempt => attempt.FinishedAt ?? attempt.StartedAt)
                                .ThenByDescending(attempt => attempt.Id)
                                .First()
                        );

                    var phaseAnalytics = phaseDefinitions
                        .Select(phase =>
                        {
                            var scores = latestCompletedByStudent.Values
                                .Select(attempt => FindPhaseResponse(attempt, phase.MatchName))
                                .Where(response => response != null)
                                .Select(response => response!.Score)
                                .ToList();

                            return new PhaseAnalyticsDto
                            {
                                PhaseName = phase.DisplayName,
                                PhaseOrder = phase.Order,
                                AverageScore = scores.Count > 0
                                    ? Math.Round(scores.Average(), 2)
                                    : null,
                                StudentsEvaluated = scores.Count,
                                ReinforcementCount = scores.Count(score =>
                                    score < DevelopingPerformanceThreshold),
                                DevelopingCount = scores.Count(score =>
                                    score >= DevelopingPerformanceThreshold &&
                                    score < GoodPerformanceThreshold),
                                GoodPerformanceCount = scores.Count(score =>
                                    score >= GoodPerformanceThreshold)
                            };
                        })
                        .ToList();

                    var phasesWithResults = phaseAnalytics
                        .Where(phase => phase.AverageScore.HasValue)
                        .ToList();

                    var strongestPhase = phasesWithResults
                        .OrderByDescending(phase => phase.AverageScore)
                        .ThenBy(phase => phase.PhaseOrder)
                        .FirstOrDefault();

                    var phaseToReinforce = phasesWithResults
                        .OrderBy(phase => phase.AverageScore)
                        .ThenBy(phase => phase.PhaseOrder)
                        .FirstOrDefault();

                    var studentResults = enrolledStudents
                        .Select(enrollment =>
                        {
                            var studentAttempts = scenarioAttempts
                                .Where(attempt => attempt.StudentId == enrollment.StudentId)
                                .OrderByDescending(attempt => attempt.StartedAt)
                                .ThenByDescending(attempt => attempt.Id)
                                .ToList();

                            var latestAttempt = studentAttempts.FirstOrDefault();
                            latestCompletedByStudent.TryGetValue(
                                enrollment.StudentId,
                                out var latestCompletedAttempt
                            );

                            return new StudentScenarioResultDto
                            {
                                StudentId = enrollment.StudentId,
                                StudentName = enrollment.Student?.Name ?? string.Empty,
                                StudentEmail = enrollment.Student?.Email ?? string.Empty,
                                AttemptCount = studentAttempts.Count,
                                LatestAttemptId = latestAttempt?.Id,
                                ReportAttemptId = latestCompletedAttempt?.Id ?? latestAttempt?.Id,
                                LatestAttemptStatus = latestAttempt?.Status ?? "NotStarted",
                                LatestAttemptStartedAt = latestAttempt?.StartedAt,
                                LatestAttemptFinishedAt = latestAttempt?.FinishedAt,
                                LatestCompletedScore = latestCompletedAttempt?.FinalScore,
                                PhaseResults = latestCompletedAttempt == null
                                    ? new List<StudentPhaseResultDto>()
                                    : phaseDefinitions
                                        .Select(phase => new
                                        {
                                            Phase = phase,
                                            Response = FindPhaseResponse(
                                                latestCompletedAttempt,
                                                phase.MatchName
                                            )
                                        })
                                        .Where(item => item.Response != null)
                                        .Select(item => new StudentPhaseResultDto
                                        {
                                            PhaseName = item.Phase.DisplayName,
                                            PhaseOrder = item.Phase.Order,
                                            Score = item.Response!.Score
                                        })
                                        .ToList()
                            };
                        })
                        .ToList();

                    var startedStudents = latestAttemptsByStudent.Count;
                    var completedStudents = latestCompletedByStudent.Count;
                    var inProgressStudents = latestAttemptsByStudent.Values
                        .Count(attempt => !IsFinished(attempt.Status));

                    return new CourseScenarioAnalyticsDto
                    {
                        ScenarioId = scenario.Id,
                        ScenarioTitle = string.IsNullOrWhiteSpace(scenario.Title)
                            ? scenario.Name
                            : scenario.Title,
                        MethodologyCode = scenario.Methodology,
                        MethodologyName = GetMethodologyName(scenario.Methodology),
                        TotalStudents = course.Enrollments.Count,
                        StartedStudents = startedStudents,
                        CompletedStudents = completedStudents,
                        InProgressStudents = inProgressStudents,
                        CompletionRate = startedStudents > 0
                            ? Math.Round(
                                (decimal)completedStudents / startedStudents * 100,
                                2
                            )
                            : 0,
                        AverageScore = latestCompletedByStudent.Count > 0
                            ? Math.Round(
                                latestCompletedByStudent.Values.Average(attempt =>
                                    attempt.FinalScore),
                                2
                            )
                            : null,
                        StrongestPhase = strongestPhase?.PhaseName ?? string.Empty,
                        PhaseToReinforce = phaseToReinforce?.PhaseName ?? string.Empty,
                        PhaseAnalytics = phaseAnalytics,
                        Students = studentResults
                    };
                })
                .ToList();

            return new CourseResultsAnalyticsDto
            {
                CourseId = course.Id,
                CourseName = course.Name,
                CourseCode = course.Code,
                TotalStudents = course.Enrollments.Count,
                TotalScenarios = scenarioAnalytics.Count,
                Scenarios = scenarioAnalytics
            };
        }

        public async Task<TeacherDashboardAnalyticsDto> GetTeacherDashboardAnalyticsAsync(int teacherId)
        {
            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.CourseScenarios)
                    .ThenInclude(cs => cs.Scenario)
                .Where(c => c.TeacherId == teacherId)
                .ToListAsync();

            var courseIds = courses.Select(c => c.Id).ToList();

            var attempts = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                .Include(a => a.Student)
                .Where(a => a.CourseId.HasValue && courseIds.Contains(a.CourseId.Value))
                .ToListAsync();

            var finishedAttempts = attempts
                .Where(a => IsFinished(a.Status))
                .ToList();

            var totalAttempts = attempts.Count;
            var finishedCount = finishedAttempts.Count;

            var studentsCount = courses
                .SelectMany(c => c.Enrollments)
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            var activeStudentsCount = finishedAttempts
                .Select(a => a.StudentId)
                .Distinct()
                .Count();

            var scenariosCount = await _context.Scenarios
                .CountAsync(s => s.CreatedByUserId == teacherId);

            var globalAverage = finishedAttempts.Any()
                ? Math.Round(finishedAttempts.Average(a => a.FinalScore), 2)
                : 0;

            var completionRate = totalAttempts > 0
                ? Math.Round(((decimal)finishedCount / totalAttempts) * 100, 2)
                : 0;

            var courseAverages = courses.Select(course =>
            {
                var courseFinishedAttempts = finishedAttempts
                    .Where(a => a.CourseId == course.Id)
                    .ToList();

                var average = courseFinishedAttempts.Any()
                    ? Math.Round(courseFinishedAttempts.Average(a => a.FinalScore), 2)
                    : 0;

                return new CourseAverageDto
                {
                    CourseId = course.Id,
                    CourseName = course.Name,
                    StudentsCount = course.Enrollments.Count,
                    SimulationsCount = courseFinishedAttempts.Count,
                    AverageScore = average
                };
            }).ToList();

            var coursesWithResults = courseAverages
                .Where(c => c.SimulationsCount > 0)
                .ToList();

            var bestCourse = coursesWithResults
                .OrderByDescending(c => c.AverageScore)
                .FirstOrDefault();

            var worstCourse = coursesWithResults
                .OrderBy(c => c.AverageScore)
                .FirstOrDefault();

            var methodologyAverages = BuildMethodologyAverages(finishedAttempts);

            var topMethodology = methodologyAverages
                .Where(m => m.SimulationsCount > 0)
                .OrderByDescending(m => m.AverageScore)
                .FirstOrDefault();

            var lowPerformanceCourses = courseAverages
                .Where(c => c.SimulationsCount > 0 && c.AverageScore < 70)
                .Select(c => new LowPerformanceCourseDto
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    SimulationsCount = c.SimulationsCount,
                    AverageScore = c.AverageScore
                })
                .ToList();

            return new TeacherDashboardAnalyticsDto
            {
                Summary = new TeacherDashboardSummaryDto
                {
                    CoursesCount = courses.Count,
                    StudentsCount = studentsCount,
                    ActiveStudentsCount = activeStudentsCount,
                    ScenariosCount = scenariosCount,
                    TotalAttempts = totalAttempts,
                    FinishedAttempts = finishedCount,
                    AverageScore = globalAverage,
                    CompletionRate = completionRate,
                    BestCourseName = bestCourse?.CourseName ?? "Sin datos",
                    BestCourseScore = bestCourse?.AverageScore ?? 0,
                    WorstCourseName = worstCourse?.CourseName ?? "Sin datos",
                    WorstCourseScore = worstCourse?.AverageScore ?? 0,
                    TopMethodologyName = topMethodology?.MethodologyName ?? "Sin datos",
                    TopMethodologyScore = topMethodology?.AverageScore ?? 0,
                    RiskCoursesCount = lowPerformanceCourses.Count
                },
                CourseAverages = courseAverages,
                MethodologyAverages = methodologyAverages,
                CompletionStatus = new List<CompletionStatusDto>
        {
            new CompletionStatusDto
            {
                Name = "Finalizadas",
                Value = finishedCount
            },
            new CompletionStatusDto
            {
                Name = "En progreso",
                Value = Math.Max(0, totalAttempts - finishedCount)
            }
        },
                LowPerformanceCourses = lowPerformanceCourses
            };
        }
        public async Task<TeacherAttemptReportDto?> GetAttemptResultsForTeacherAsync(
            int courseId,
            int attemptId,
            int teacherId)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == courseId && c.TeacherId == teacherId);

            if (!courseExists)
                return null;

            var attempt = await _context.SimulationAttempts
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Student)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.PhaseResponses)
                    .ThenInclude(response => response.Answers)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.CourseId == courseId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            var isCompleteReport = IsFinished(attempt.Status);
            var phaseDefinitions = GetScenarioPhaseDefinitions(
                attempt.Scenario,
                attempt.PhaseResponses
            );

            var phaseScores = attempt.PhaseResponses
                .OrderBy(response => GetPhaseSortOrder(
                    response.PhaseName,
                    phaseDefinitions
                ))
                .Select(response => new PhaseScoreDto
                {
                    PhaseName = GetPhaseDisplayName(
                        response.PhaseName,
                        phaseDefinitions
                    ),
                    Score = response.Score,
                    Feedback = response.Feedback
                })
                .ToList();

            var phaseReviews = isCompleteReport
                ? attempt.PhaseResponses
                    .OrderBy(response => GetPhaseSortOrder(
                        response.PhaseName,
                        phaseDefinitions
                    ))
                    .Select(response =>
                    {
                        var selectionAnswer = response.Answers.FirstOrDefault(answer =>
                            string.Equals(
                                answer.QuestionType,
                                "Selection",
                                StringComparison.OrdinalIgnoreCase
                            ));

                        var textAnswer = response.Answers.FirstOrDefault(answer =>
                            string.Equals(
                                answer.QuestionType,
                                "Text",
                                StringComparison.OrdinalIgnoreCase
                            ));

                        var options = BuildCourseOptionReviews(
                            selectionAnswer,
                            attempt.Scenario.Options,
                            response.PhaseName);

                        return new PhaseAnswerReviewDto
                        {
                            PhaseName = GetPhaseDisplayName(
                                response.PhaseName,
                                phaseDefinitions
                            ),
                            SelectionScore = selectionAnswer?.Score ?? 0,
                            SelectionFeedback = selectionAnswer?.Feedback ?? string.Empty,
                            TextAnswer = textAnswer?.TextAnswer ?? string.Empty,
                            TextAnswerScore = textAnswer?.Score ?? 0,
                            TextAnswerFeedback = textAnswer?.Feedback ?? string.Empty,
                            Options = options
                        };
                    })
                    .ToList()
                : new List<PhaseAnswerReviewDto>();

            var relatedAttempts = await _context.SimulationAttempts
                .AsNoTracking()
                .Where(relatedAttempt =>
                    relatedAttempt.CourseId == courseId &&
                    relatedAttempt.StudentId == attempt.StudentId &&
                    relatedAttempt.ScenarioId == attempt.ScenarioId)
                .OrderByDescending(relatedAttempt => relatedAttempt.StartedAt)
                .ThenByDescending(relatedAttempt => relatedAttempt.Id)
                .ToListAsync();

            var strongestPhase = phaseScores
                .OrderByDescending(phase => phase.Score)
                .FirstOrDefault();

            var phaseToReinforce = phaseScores
                .OrderBy(phase => phase.Score)
                .FirstOrDefault();

            return new TeacherAttemptReportDto
            {
                AttemptId = attempt.Id,
                StudentId = attempt.StudentId,
                StudentName = attempt.Student?.Name ?? string.Empty,
                StudentEmail = attempt.Student?.Email ?? string.Empty,
                ScenarioId = attempt.ScenarioId,
                ScenarioTitle = string.IsNullOrWhiteSpace(attempt.Scenario.Title)
                    ? attempt.Scenario.Name
                    : attempt.Scenario.Title,
                MethodologyCode = attempt.Scenario.Methodology,
                MethodologyName = GetMethodologyName(attempt.Scenario.Methodology),
                Status = attempt.Status,
                StartedAt = attempt.StartedAt,
                FinishedAt = attempt.FinishedAt,
                FinalScore = isCompleteReport ? attempt.FinalScore : null,
                FinalFeedback = isCompleteReport
                    ? attempt.FinalFeedback
                    : string.Empty,
                StrongestPhase = strongestPhase?.PhaseName ?? string.Empty,
                PhaseToReinforce = phaseToReinforce?.PhaseName ?? string.Empty,
                IsCompleteReport = isCompleteReport,
                PhaseScores = phaseScores,
                PhaseReviews = phaseReviews,
                KpiResults = attempt.KpiResults
                    .Select(k => new KpiResultDto
                    {
                        KpiName = k.KpiName,
                        InitialValue = k.InitialValue,
                        FinalValue = k.FinalValue,
                        Unit = k.Unit
                    })
                    .ToList(),
                Attempts = relatedAttempts
                    .Select(relatedAttempt => new TeacherAttemptSummaryDto
                    {
                        AttemptId = relatedAttempt.Id,
                        Status = relatedAttempt.Status,
                        StartedAt = relatedAttempt.StartedAt,
                        FinishedAt = relatedAttempt.FinishedAt,
                        FinalScore = IsFinished(relatedAttempt.Status)
                            ? relatedAttempt.FinalScore
                            : null
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
                    Title = string.IsNullOrWhiteSpace(cs.Scenario?.Title)
                        ? cs.Scenario?.Name ?? ""
                        : cs.Scenario.Title,
                    Description = cs.Scenario?.Description ?? "",
                    Difficulty = cs.Scenario?.Difficulty ?? "",
                    IsPublished = cs.Scenario?.IsPublished ?? false,
                    AssignedAt = cs.AssignedAt,
                    Methodology = cs.Scenario?.Methodology ?? "",
                    MethodologyName = GetMethodologyName(cs.Scenario?.Methodology ?? "")
                }).ToList()
            };
        }

        private static string GetMethodologyName(string methodologyCode)
        {
            return methodologyCode switch
            {
                "BPM" => "Business Process Management",
                "DigitalMaturity" => "Madurez Digital",
                "LeanStartup" => "Lean Startup",
                "DesignThinking" => "Design Thinking",
                _ => "No definida"
            };
        }

        private async Task<string> GenerateUniqueCourseCodeAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = GenerateCourseCode();

                var exists = await _context.Courses
                    .AnyAsync(c => c.Code == code);

                if (!exists)
                    return code;
            }

            throw new Exception("No se pudo generar un código único para el curso.");
        }

        private static string GenerateCourseCode()
        {
            var random = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"IMP-{random}";
        }

        private static string NormalizeCourseCode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static (bool Success, string Message) ValidateCsvFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (false, "Debe seleccionar un archivo CSV.");

            if (file.Length > MaxCsvFileBytes)
                return (false, "El archivo CSV supera el tamaño máximo permitido.");

            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                return (false, "Solo se aceptan archivos .csv.");

            return (true, string.Empty);
        }

        private static (
            bool Success,
            string Message,
            ImportStudentsResultDto Result,
            List<ImportStudentRow> ValidRows) ParseStudentCsv(string csvContent)
        {
            var result = new ImportStudentsResultDto();
            var validRows = new List<ImportStudentRow>();

            if (string.IsNullOrWhiteSpace(csvContent))
                return (false, "El archivo CSV está vacío.", result, validRows);

            List<List<string>> rows;

            try
            {
                rows = ParseCsvRows(csvContent);
            }
            catch
            {
                return (false, "El archivo CSV no tiene un formato válido.", result, validRows);
            }

            rows = rows
                .Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value)))
                .ToList();

            if (rows.Count <= 1)
                return (false, "El archivo CSV no contiene estudiantes.", result, validRows);

            var header = rows[0]
                .Select(value => NormalizeHeader(value))
                .ToList();

            var nameIndex = FindHeaderIndex(header, new[] { "name", "nombre", "fullname" });
            var emailIndex = FindHeaderIndex(header, new[] { "email", "correo" });

            if (nameIndex < 0 || emailIndex < 0)
                return (false, "El CSV debe contener las columnas name y email.", result, validRows);

            var allowedHeaders = new HashSet<string>
            {
                "name",
                "nombre",
                "fullname",
                "email",
                "correo"
            };

            if (header.Any(column => !allowedHeaders.Contains(column)))
                return (false, "El CSV solo puede contener columnas de nombre y correo.", result, validRows);

            var dataRows = rows.Skip(1).ToList();

            if (dataRows.Count > MaxImportRows)
                return (false, $"El archivo no puede contener más de {MaxImportRows} estudiantes.", result, validRows);

            var emailsInFile = new HashSet<string>();

            for (var index = 0; index < dataRows.Count; index++)
            {
                var row = dataRows[index];
                var rowNumber = index + 2;

                result.TotalRows++;

                if (row.Count > header.Count &&
                    row.Skip(header.Count).Any(value => !string.IsNullOrWhiteSpace(value)))
                {
                    AddImportError(result, rowNumber, string.Empty, string.Empty, "La fila tiene más columnas de las esperadas.");
                    continue;
                }

                var name = GetCsvValue(row, nameIndex).Trim();
                var email = GetCsvValue(row, emailIndex).Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(name))
                {
                    AddImportError(result, rowNumber, name, email, "El nombre es obligatorio.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    AddImportError(result, rowNumber, name, email, "El correo es obligatorio.");
                    continue;
                }

                if (!IsValidEmail(email))
                {
                    AddImportError(result, rowNumber, name, email, "El correo no tiene un formato válido.");
                    continue;
                }

                if (!emailsInFile.Add(email))
                {
                    AddImportError(result, rowNumber, name, email, "El correo está duplicado dentro del archivo.");
                    continue;
                }

                validRows.Add(new ImportStudentRow(rowNumber, name, email));
            }

            result.FailedRows = result.Errors.Count;

            return (true, string.Empty, result, validRows);
        }

        private static List<List<string>> ParseCsvRows(string content)
        {
            var delimiter = DetectCsvDelimiter(content);
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var current = content[index];

                if (current == '"')
                {
                    if (inQuotes &&
                        index + 1 < content.Length &&
                        content[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (current == delimiter && !inQuotes)
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if ((current == '\r' || current == '\n') && !inQuotes)
                {
                    if (current == '\r' &&
                        index + 1 < content.Length &&
                        content[index + 1] == '\n')
                    {
                        index++;
                    }

                    row.Add(field.ToString());
                    rows.Add(row);
                    row = new List<string>();
                    field.Clear();
                    continue;
                }

                field.Append(current);
            }

            if (inQuotes)
                throw new FormatException("CSV con comillas sin cerrar.");

            row.Add(field.ToString());
            rows.Add(row);

            return rows;
        }

        private static char DetectCsvDelimiter(string content)
        {
            var commaCount = 0;
            var semicolonCount = 0;
            var tabCount = 0;
            var inQuotes = false;

            for (var index = 0; index < content.Length; index++)
            {
                var current = content[index];

                if (current == '"')
                {
                    if (inQuotes &&
                        index + 1 < content.Length &&
                        content[index + 1] == '"')
                    {
                        index++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if ((current == '\r' || current == '\n') && !inQuotes)
                    break;

                if (inQuotes)
                    continue;

                if (current == ',')
                    commaCount++;
                else if (current == ';')
                    semicolonCount++;
                else if (current == '\t')
                    tabCount++;
            }

            if (semicolonCount > commaCount && semicolonCount >= tabCount)
                return ';';

            if (tabCount > commaCount)
                return '\t';

            return ',';
        }

        private static int FindHeaderIndex(List<string> header, string[] aliases)
        {
            return header.FindIndex(column => aliases.Contains(column));
        }

        private static string NormalizeHeader(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(" ", "");
        }

        private static string GetCsvValue(List<string> row, int index)
        {
            return index >= 0 && index < row.Count
                ? row[index]
                : string.Empty;
        }

        private static void AddImportError(
            ImportStudentsResultDto result,
            int rowNumber,
            string name,
            string email,
            string message)
        {
            result.Errors.Add(new ImportStudentErrorDto
            {
                RowNumber = rowNumber,
                Name = name,
                Email = email,
                Message = message
            });
        }

        private static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(
                email,
                "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$",
                RegexOptions.CultureInvariant
            );
        }

        private static List<ScenarioPhaseDefinition> GetScenarioPhaseDefinitions(
            Scenario scenario,
            IEnumerable<SimulationPhaseResponse> fallbackResponses)
        {
            var configuredPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => new ScenarioPhaseDefinition(
                    phase.PhaseName,
                    string.IsNullOrWhiteSpace(phase.CustomName)
                        ? phase.PhaseName
                        : phase.CustomName,
                    phase.PhaseOrder
                ))
                .ToList();

            if (configuredPhases.Count > 0)
                return configuredPhases;

            return fallbackResponses
                .OrderBy(response => response.SubmittedAt)
                .GroupBy(response => NormalizePhaseName(response.PhaseName))
                .Select((group, index) => new ScenarioPhaseDefinition(
                    group.First().PhaseName,
                    group.First().PhaseName,
                    index + 1
                ))
                .ToList();
        }

        private static SimulationPhaseResponse? FindPhaseResponse(
            SimulationAttempt attempt,
            string phaseName)
        {
            var normalizedPhaseName = NormalizePhaseName(phaseName);

            return attempt.PhaseResponses.FirstOrDefault(response =>
                NormalizePhaseName(response.PhaseName) == normalizedPhaseName);
        }

        private static int GetPhaseSortOrder(
            string phaseName,
            List<ScenarioPhaseDefinition> phaseDefinitions)
        {
            var normalizedPhaseName = NormalizePhaseName(phaseName);
            var phase = phaseDefinitions.FirstOrDefault(definition =>
                NormalizePhaseName(definition.MatchName) == normalizedPhaseName);

            return phase?.Order ?? int.MaxValue;
        }

        private static string GetPhaseDisplayName(
            string phaseName,
            List<ScenarioPhaseDefinition> phaseDefinitions)
        {
            var normalizedPhaseName = NormalizePhaseName(phaseName);
            var phase = phaseDefinitions.FirstOrDefault(definition =>
                NormalizePhaseName(definition.MatchName) == normalizedPhaseName);

            return phase?.DisplayName ?? phaseName;
        }

        private static HashSet<int> DeserializeSelectedOptionIds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new HashSet<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(value)?.ToHashSet()
                    ?? new HashSet<int>();
            }
            catch (JsonException)
            {
                return new HashSet<int>();
            }
        }

        private static List<OptionAnswerReviewDto> BuildCourseOptionReviews(
            SimulationAnswer? selectionAnswer,
            IReadOnlyCollection<ScenarioOption> currentOptions,
            string phaseName)
        {
            var snapshots = DeserializeSelectedOptionSnapshots(
                selectionAnswer?.SelectedOptionsSnapshotJson);
            if (snapshots.Count > 0)
            {
                var selectedIds = snapshots.Select(snapshot => snapshot.OptionId).ToHashSet();
                var selected = snapshots.Select(snapshot => new OptionAnswerReviewDto
                {
                    OptionId = snapshot.OptionId,
                    OptionType = snapshot.OptionType,
                    Text = snapshot.Text,
                    Score = snapshot.Score,
                    WasSelected = true,
                    IsCorrect = snapshot.IsCorrect,
                    ImpactJson = snapshot.ImpactJson,
                    TagsJson = snapshot.TagsJson,
                    Cost = snapshot.Cost,
                    TimeCost = snapshot.TimeCost,
                    RiskImpact = snapshot.RiskImpact
                });
                var omittedCorrect = currentOptions
                    .Where(option => NormalizePhaseName(option.PhaseName) == NormalizePhaseName(phaseName))
                    .Where(option => option.IsCorrect && !selectedIds.Contains(option.Id))
                    .OrderBy(option => option.OrderIndex)
                    .Select(option => MapCurrentOptionReview(option, false));
                return selected.Concat(omittedCorrect).ToList();
            }

            var selectedOptionIds = DeserializeSelectedOptionIds(selectionAnswer?.SelectedOptionIdsJson);
            return currentOptions
                .Where(option => NormalizePhaseName(option.PhaseName) == NormalizePhaseName(phaseName))
                .Where(option => selectedOptionIds.Contains(option.Id) || option.IsCorrect)
                .OrderBy(option => option.OrderIndex)
                .Select(option => MapCurrentOptionReview(option, selectedOptionIds.Contains(option.Id)))
                .ToList();
        }

        private static OptionAnswerReviewDto MapCurrentOptionReview(
            ScenarioOption option,
            bool wasSelected) =>
            new()
            {
                OptionId = option.Id,
                OptionType = option.OptionType,
                Text = option.Text,
                Score = option.Score,
                WasSelected = wasSelected,
                IsCorrect = option.IsCorrect,
                ImpactJson = option.ImpactJson,
                TagsJson = option.TagsJson,
                Cost = option.Cost,
                TimeCost = option.TimeCost,
                RiskImpact = option.RiskImpact,
                ExpectedImpactLevel = option.ExpectedImpactLevel,
                ExpectedEffortLevel = option.ExpectedEffortLevel,
                ExpectedViabilityLevel = option.ExpectedViabilityLevel
            };

        private static List<SelectedOptionSnapshotDto> DeserializeSelectedOptionSnapshots(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new List<SelectedOptionSnapshotDto>();
            try
            {
                return JsonSerializer.Deserialize<List<SelectedOptionSnapshotDto>>(value)
                    ?? new List<SelectedOptionSnapshotDto>();
            }
            catch (JsonException)
            {
                return new List<SelectedOptionSnapshotDto>();
            }
        }

        private static string NormalizePhaseName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string GenerateTemporaryPassword()
        {
            const string chars =
                "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!#$%";

            var bytes = RandomNumberGenerator.GetBytes(12);
            var password = new char[12];

            for (var index = 0; index < password.Length; index++)
            {
                password[index] = chars[bytes[index] % chars.Length];
            }

            return new string(password);
        }

        private record ImportStudentRow(
            int RowNumber,
            string Name,
            string Email);

        private static bool IsFinished(string status)
        {
            var normalized = status.Trim().ToLower();

            return normalized == "finished" ||
                   normalized == "finalizada" ||
                   normalized == "completed";
        }

        private sealed record ScenarioPhaseDefinition(
            string MatchName,
            string DisplayName,
            int Order);

        private static List<MethodologyAverageDto> BuildMethodologyAverages(List<SimulationAttempt> attempts)
        {
            var baseMethodologies = new List<MethodologyAverageDto>
    {
        new MethodologyAverageDto
        {
            MethodologyCode = "DesignThinking",
            MethodologyName = "Design Thinking",
            SimulationsCount = 0,
            AverageScore = 0
        },
        new MethodologyAverageDto
        {
            MethodologyCode = "BPM",
            MethodologyName = "Business Process Management",
            SimulationsCount = 0,
            AverageScore = 0
        },
        new MethodologyAverageDto
        {
            MethodologyCode = "DigitalMaturity",
            MethodologyName = "Madurez Digital",
            SimulationsCount = 0,
            AverageScore = 0
        },
        new MethodologyAverageDto
        {
            MethodologyCode = "LeanStartup",
            MethodologyName = "Lean Startup",
            SimulationsCount = 0,
            AverageScore = 0
        }
    };

            var grouped = attempts
                .Where(a => a.Scenario != null)
                .GroupBy(a => a.Scenario!.Methodology)
                .ToList();

            foreach (var group in grouped)
            {
                var methodology = baseMethodologies
                    .FirstOrDefault(m => m.MethodologyCode == group.Key);

                if (methodology == null)
                {
                    methodology = new MethodologyAverageDto
                    {
                        MethodologyCode = group.Key,
                        MethodologyName = GetMethodologyName(group.Key)
                    };

                    baseMethodologies.Add(methodology);
                }

                methodology.SimulationsCount = group.Count();
                methodology.AverageScore = Math.Round(group.Average(a => a.FinalScore), 2);
            }

            return baseMethodologies;
        }
    }
}
