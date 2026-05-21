using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs.Courses;
using SimuladorApi.Services;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly CourseService _courseService;

        public CoursesController(CourseService courseService)
        {
            _courseService = courseService;
        }

        [Authorize(Roles = "Docente")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse(CreateCourseDto request)
        {
            var teacherId = GetUserId();

            var result = await _courseService.CreateCourseAsync(teacherId, request);

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("{courseId}/attempts/{attemptId}/results")]
        public async Task<IActionResult> GetAttemptResultsForTeacher(int courseId, int attemptId)
        {
            var teacherId = GetUserId();

            var result = await _courseService.GetAttemptResultsForTeacherAsync(
                courseId,
                attemptId,
                teacherId
            );

            if (result == null)
                return NotFound("Resultados no encontrados para este curso.");

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyCourses()
        {
            var teacherId = GetUserId();

            var result = await _courseService.GetMyCoursesAsync(teacherId);

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCourseDetail(int id)
        {
            var teacherId = GetUserId();

            var result = await _courseService.GetCourseDetailAsync(id, teacherId);

            if (result == null)
                return NotFound("Curso no encontrado.");

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCourse(int id, UpdateCourseDto request)
        {
            var teacherId = GetUserId();

            var result = await _courseService.UpdateCourseAsync(id, teacherId, request);

            if (result == null)
                return NotFound("Curso no encontrado.");

            return Ok(result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableCourses()
        {
            var studentId = GetUserId();

            var result = await _courseService.GetAvailableCoursesAsync(studentId);

            return Ok(result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("enrolled")]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var studentId = GetUserId();

            var result = await _courseService.GetEnrolledCoursesAsync(studentId);

            return Ok(result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpPost("{courseId}/enroll")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var studentId = GetUserId();

            var result = await _courseService.EnrollAsync(courseId, studentId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("{courseId}/student-detail")]
        public async Task<IActionResult> GetStudentCourseDetail(int courseId)
        {
            var studentId = GetUserId();

            var result = await _courseService.GetStudentCourseDetailAsync(courseId, studentId);

            if (result == null)
                return NotFound("Curso no encontrado o no estás inscrito.");

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("{courseId}/scenarios/{scenarioId}")]
        public async Task<IActionResult> AssignScenarioToCourse(int courseId, int scenarioId)
        {
            var teacherId = GetUserId();

            var result = await _courseService.AssignScenarioToCourseAsync(courseId, scenarioId, teacherId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("assign-scenario")]
        public async Task<IActionResult> AssignScenarioToCourses(AssignScenarioToCoursesDto request)
        {
            var teacherId = GetUserId();

            var result = await _courseService.AssignScenarioToCoursesAsync(teacherId, request);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("{courseId}/results")]
        public async Task<IActionResult> GetCourseResults(int courseId)
        {
            var teacherId = GetUserId();

            var result = await _courseService.GetCourseResultsAsync(courseId, teacherId);

            if (result == null)
                return NotFound("Curso no encontrado.");

            return Ok(result);
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }
}