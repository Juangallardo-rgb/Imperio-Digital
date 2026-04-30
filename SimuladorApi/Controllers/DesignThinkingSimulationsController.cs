using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Services;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/design-thinking/simulations")]
    public class DesignThinkingSimulationsController : ControllerBase
    {
        private readonly SimulationService _simulationService;

        public DesignThinkingSimulationsController(SimulationService simulationService)
        {
            _simulationService = simulationService;
        }

        [Authorize(Roles = "Estudiante")]
        [HttpPost("start")]
        public async Task<IActionResult> StartSimulation(StartSimulationDto request)
        {
            var studentId = GetUserId();

            var result = await _simulationService.StartSimulationAsync(studentId, request);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(new
            {
                message = result.Message,
                attemptId = result.AttemptId
            });
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("{attemptId}/current")]
        public async Task<IActionResult> GetCurrentSimulation(int attemptId)
        {
            var studentId = GetUserId();

            var result = await _simulationService.GetCurrentSimulationAsync(attemptId, studentId);

            if (result == null)
                return NotFound("Simulación no encontrada.");

            return Ok(result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpPost("{attemptId}/phase/{phaseName}/submit")]
        public async Task<IActionResult> SubmitPhase(
            int attemptId,
            string phaseName,
            SubmitPhaseDto request)
        {
            var studentId = GetUserId();

            var result = await _simulationService.SubmitPhaseAsync(
                attemptId,
                studentId,
                phaseName,
                request
            );

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpPost("{attemptId}/finish")]
        public async Task<IActionResult> FinishSimulation(int attemptId)
        {
            var studentId = GetUserId();

            var result = await _simulationService.FinishSimulationAsync(attemptId, studentId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("{attemptId}/results")]
        public async Task<IActionResult> GetResults(int attemptId)
        {
            var studentId = GetUserId();

            var result = await _simulationService.GetResultsAsync(attemptId, studentId);

            if (result == null)
                return NotFound("Resultados no encontrados.");

            return Ok(result);
        }

        [Authorize(Roles = "Estudiante")]
        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var studentId = GetUserId();

            var result = await _simulationService.GetMyHistoryAsync(studentId);

            return Ok(result);
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }
    }
}