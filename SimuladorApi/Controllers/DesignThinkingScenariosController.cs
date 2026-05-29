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

            var result = await _scenarioService.CreateDesignThinkingScenarioAsync(request, teacherId);

            return Ok(result);
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

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetScenarioById(int id)
        {
            var userId = GetUserId();

            var scenario = await _scenarioService.GetScenarioDetailAsync(id, userId, false);

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
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Docente")]
        [HttpPost("generate-draft")]
        public async Task<IActionResult> GenerateScenarioDraft(GenerateScenarioDraftDto request)
        {
            var result = await _scenarioService.GenerateScenarioDraftAsync(request.Methodology);

            return Ok(result);
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }


    }
}