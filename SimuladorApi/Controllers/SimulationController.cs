using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.Data;
using SimuladorApi.DTOs;
using SimuladorApi.Models;
using SimuladorApi.Services;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimulationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly OpenRouterService _openRouterService;

        public SimulationController(AppDbContext context, OpenRouterService openRouterService)
        {
            _context = context;
            _openRouterService = openRouterService;
        }

        [Authorize(Roles = "Estudiante")]
        [HttpPost]
        public async Task<IActionResult> CreateSimulation(CreateSimulationDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("No se pudo identificar al usuario");

            var userId = int.Parse(userIdClaim.Value);

            var scenario = _context.Scenarios.FirstOrDefault(s => s.Id == request.ScenarioId);

            if (scenario == null)
                return NotFound("El escenario no existe");

            var scenarioVariables = _context.ScenarioVariables
                .Where(v => v.ScenarioId == request.ScenarioId)
                .ToList();

            if (!scenarioVariables.Any())
                return BadRequest("El escenario no tiene variables configuradas");

            if (request.Variables == null || !request.Variables.Any())
                return BadRequest("Debe enviar valores para las variables");

            foreach (var input in request.Variables)
            {
                var variable = scenarioVariables.FirstOrDefault(v => v.Id == input.ScenarioVariableId);

                if (variable == null)
                    return BadRequest($"La variable con ID {input.ScenarioVariableId} no pertenece al escenario");

                if (input.Value < variable.MinValue || input.Value > variable.MaxValue)
                    return BadRequest($"El valor de la variable '{variable.Name}' debe estar entre {variable.MinValue} y {variable.MaxValue}");
            }

            decimal digitalMaturity = CalculateKpi(scenarioVariables, request.Variables, "DigitalMaturity");
            decimal operationalEfficiency = CalculateKpi(scenarioVariables, request.Variables, "OperationalEfficiency");
            decimal customerExperience = CalculateKpi(scenarioVariables, request.Variables, "CustomerExperience");

            decimal globalScore = Math.Round(
                (digitalMaturity + operationalEfficiency + customerExperience) / 3m, 2
            );

            string feedbackRule = GenerateRuleBasedFeedback(
                digitalMaturity,
                operationalEfficiency,
                customerExperience,
                globalScore
            );

            string aiFeedback = await _openRouterService.GenerateFeedbackAsync(
                digitalMaturity,
                operationalEfficiency,
                customerExperience,
                globalScore,
                feedbackRule
            );

            var simulation = new Simulation
            {
                UserId = userId,
                ScenarioId = request.ScenarioId,
                DigitalMaturity = digitalMaturity,
                OperationalEfficiency = operationalEfficiency,
                CustomerExperience = customerExperience,
                GlobalScore = globalScore,
                FeedbackRule = feedbackRule,
                AiFeedback = aiFeedback
            };

            _context.Simulations.Add(simulation);
            _context.SaveChanges();

            var simulationValues = request.Variables.Select(v => new SimulationVariableValue
            {
                SimulationId = simulation.Id,
                ScenarioVariableId = v.ScenarioVariableId,
                Value = v.Value
            }).ToList();

            _context.SimulationVariableValues.AddRange(simulationValues);
            _context.SaveChanges();

            return Ok(new
            {
                message = "Simulación ejecutada correctamente",
                simulationId = simulation.Id,
                digitalMaturity,
                operationalEfficiency,
                customerExperience,
                globalScore,
                feedbackRule,
                aiFeedback
            });
        }

        [Authorize]
        [HttpGet("history")]
        public IActionResult GetMySimulations()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("No se pudo identificar al usuario");

            var userId = int.Parse(userIdClaim.Value);

            var simulations = _context.Simulations
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            return Ok(simulations);
        }

        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetSimulationById(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized("No se pudo identificar al usuario");

            var userId = int.Parse(userIdClaim.Value);

            var simulation = _context.Simulations
                .FirstOrDefault(s => s.Id == id && s.UserId == userId);

            if (simulation == null)
                return NotFound("La simulación no existe");

            var scenario = _context.Scenarios.FirstOrDefault(s => s.Id == simulation.ScenarioId);

            var variableValues = _context.SimulationVariableValues
                .Where(v => v.SimulationId == simulation.Id)
                .ToList();

            var scenarioVariableIds = variableValues
                .Select(v => v.ScenarioVariableId)
                .ToList();

            var scenarioVariables = _context.ScenarioVariables
                .Where(v => scenarioVariableIds.Contains(v.Id))
                .ToList();

            var result = new DTOs.SimulationDetailDto
            {
                Id = simulation.Id,
                ScenarioName = scenario?.Name ?? "Sin nombre",
                CreatedAt = simulation.CreatedAt,
                DigitalMaturity = simulation.DigitalMaturity,
                OperationalEfficiency = simulation.OperationalEfficiency,
                CustomerExperience = simulation.CustomerExperience,
                GlobalScore = simulation.GlobalScore,
                FeedbackRule = simulation.FeedbackRule,
                AiFeedback = simulation.AiFeedback,
                Variables = variableValues.Select(v =>
                {
                    var variable = scenarioVariables.FirstOrDefault(sv => sv.Id == v.ScenarioVariableId);

                    return new DTOs.SimulationVariableDetailDto
                    {
                        ScenarioVariableId = v.ScenarioVariableId,
                        Name = variable?.Name ?? "Sin nombre",
                        Methodology = variable?.Methodology ?? string.Empty,
                        Phase = variable?.Phase ?? string.Empty,
                        TargetKpi = variable?.TargetKpi ?? string.Empty,
                        Value = v.Value
                    };
                }).ToList()
            };

            return Ok(result);
        }

        private decimal CalculateKpi(
            List<ScenarioVariable> scenarioVariables,
            List<SimulationVariableInputDto> inputs,
            string targetKpi)
        {
            var variablesForKpi = scenarioVariables
                .Where(v => v.TargetKpi == targetKpi)
                .ToList();

            if (!variablesForKpi.Any())
                return 0;

            decimal weightedSum = 0;
            decimal totalWeight = 0;

            foreach (var variable in variablesForKpi)
            {
                var input = inputs.FirstOrDefault(i => i.ScenarioVariableId == variable.Id);

                if (input == null)
                    continue;

                weightedSum += input.Value * variable.Weight;
                totalWeight += variable.Weight;
            }

            if (totalWeight == 0)
                return 0;

            return Math.Round(weightedSum / totalWeight, 2);
        }

        private string GenerateRuleBasedFeedback(
            decimal digitalMaturity,
            decimal operationalEfficiency,
            decimal customerExperience,
            decimal globalScore)
        {
            if (operationalEfficiency >= 70 && customerExperience < 50)
            {
                return "La eficiencia operativa es alta, pero la experiencia del cliente es baja. Se recomienda reforzar prácticas de Design Thinking para centrar la transformación en las necesidades del usuario.";
            }

            if (digitalMaturity < 50)
            {
                return "La madurez digital es baja. Se recomienda iniciar con un diagnóstico integral de capacidades digitales antes de escalar iniciativas de transformación.";
            }

            if (operationalEfficiency < 50)
            {
                return "La eficiencia operativa es baja. Se recomienda revisar y optimizar procesos con enfoque BPM para reducir cuellos de botella y mejorar el desempeño.";
            }

            if (globalScore >= 75)
            {
                return "El escenario muestra un desempeño sólido. La organización presenta una base favorable para continuar iterando mejoras mediante Lean Startup.";
            }

            return "El escenario presenta avances parciales. Se recomienda equilibrar diagnóstico digital, optimización de procesos y enfoque en usuario para fortalecer la transformación.";
        }
    }
}