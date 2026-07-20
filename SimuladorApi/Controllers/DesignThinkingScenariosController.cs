using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Services;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/design-thinking/scenarios")]
    public class DesignThinkingScenariosController : ControllerBase
    {
        private readonly ScenarioService _scenarioService;

        public DesignThinkingScenariosController(ScenarioService scenarioService)
        {
            _scenarioService = scenarioService;
        }

        [Authorize(Roles = "Docente")]
        [HttpPost]
        public async Task<IActionResult> CreateScenario(CreateDesignThinkingScenarioDto request)
        {
            var teacherId = GetUserId();

            try
            {
                var result = await _scenarioService.CreateDesignThinkingScenarioAsync(request, teacherId);

                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (AiContentGenerationException exception)
            {
                return StatusCode(
                    exception.StatusCode == StatusCodes.Status429TooManyRequests
                        ? StatusCodes.Status503ServiceUnavailable
                        : StatusCodes.Status502BadGateway,
                    BuildAiErrorResponse(exception));
            }
            catch
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "No se pudo crear el escenario. Intenta nuevamente."
                );
            }
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyScenarios()
        {
            var teacherId = GetUserId();

            var scenarios = await _scenarioService.GetMyScenariosAsync(teacherId);

            return Ok(scenarios);
        }

        [Authorize]
        [HttpGet("published")]
        public async Task<IActionResult> GetPublishedScenarios()
        {
            var scenarios = await _scenarioService.GetPublishedScenariosAsync();

            return Ok(scenarios);
        }

        [Authorize(Roles = "Docente")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetScenarioById(int id)
        {
            var userId = GetUserId();

            var scenario = await _scenarioService.GetScenarioDetailAsync(id, userId, true);

            if (scenario == null)
                return NotFound("Escenario no encontrado.");

            return Ok(scenario);
        }

        [Authorize(Roles = "Docente")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateScenario(int id, UpdateDesignThinkingScenarioDto request)
        {
            var teacherId = GetUserId();

            var result = await _scenarioService.UpdateScenarioAsync(id, teacherId, request);

            if (result == null)
                return NotFound("Escenario no encontrado.");

            return Ok(result);
        }

        [Authorize(Roles = "Docente")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScenario(int id)
        {
            var teacherId = GetUserId();
            var result = await _scenarioService.DeleteScenarioAsync(id, teacherId);

            return result.Status switch
            {
                ScenarioDeletionStatus.Deleted => Ok(result.Message),
                ScenarioDeletionStatus.NotFound => NotFound(result.Message),
                ScenarioDeletionStatus.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden,
                    result.Message),
                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    result.Message)
            };
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("{id}/options")]
        public async Task<IActionResult> AddOption(int id, CreateScenarioOptionDto request)
        {
            var teacherId = GetUserId();

            var result = await _scenarioService.AddScenarioOptionAsync(id, teacherId, request);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPut("{id}/phase-settings")]
        public async Task<IActionResult> UpdatePhaseSettings(int id, UpdatePhaseSettingsDto request)
        {
            var teacherId = GetUserId();

            var result = await _scenarioService.UpdatePhaseSettingsAsync(id, teacherId, request);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishScenario(int id)
        {
            var teacherId = GetUserId();

            var result = await _scenarioService.PublishScenarioAsync(id, teacherId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("{id}/generate-ai-content")]
        public async Task<IActionResult> GenerateBaseContent(int id)
        {
            var teacherId = GetUserId();

            var result = await _scenarioService.RegenerateBaseOptionsAsync(id, teacherId);

            if (!result.Success)
                return StatusCode(result.StatusCode, new { message = result.Message });

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("generate-draft")]
        public async Task<IActionResult> GenerateScenarioDraft(GenerateScenarioDraftDto request)
        {
            try
            {
                var result = await _scenarioService.GenerateScenarioDraftAsync(
                    request,
                    GetUserId(),
                    HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
            catch (AiContentGenerationException exception)
            {
                return StatusCode(
                    exception.StatusCode == StatusCodes.Status429TooManyRequests
                        ? StatusCodes.Status503ServiceUnavailable
                        : StatusCodes.Status502BadGateway,
                    BuildAiErrorResponse(exception));
            }
        }

        private static object BuildAiErrorResponse(AiContentGenerationException exception) => new
        {
            code = exception.PhaseName is null
                ? exception.ErrorCode
                : "AI_PHASE_GENERATION_FAILED",
            message = exception.Message,
            phaseName = exception.PhaseName,
            methodologyCode = exception.MethodologyCode,
            correlationId = exception.CorrelationId,
            validationErrors = exception.ValidationErrors,
            detail = BuildAiErrorDetail(exception)
        };

        private static string? BuildAiErrorDetail(AiContentGenerationException exception)
        {
            if (exception.PhaseName is null)
            {
                return null;
            }

            return exception.StatusCode switch
            {
                StatusCodes.Status402PaymentRequired =>
                    "La cuenta de OpenRouter no tiene créditos disponibles para el modelo configurado.",
                StatusCodes.Status429TooManyRequests =>
                    "OpenRouter limitó las solicitudes del modelo. No se modificó ni guardó parcialmente el escenario.",
                _ when exception.ErrorCode is "invalid_json" or "empty_json_result" or
                    "empty_response" or "truncated_response" or "AI_INVALID_SCHEMA" =>
                    "OpenRouter devolvió contenido que no cumplió la estructura esperada. La fase se reintentó automáticamente.",
                _ => "OpenRouter no pudo completar esta fase después de los reintentos automáticos."
            };
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }


    }
}
