using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.Data;
using SimuladorApi.DTOs;
using SimuladorApi.Models;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScenarioVariableController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ScenarioVariableController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Docente")]
        [HttpPost]
        public IActionResult CreateVariable(CreateScenarioVariableDto request)
        {
            var scenario = _context.Scenarios.FirstOrDefault(s => s.Id == request.ScenarioId);

            if (scenario == null)
                return NotFound("El escenario no existe");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("El nombre de la variable es obligatorio");

            if (string.IsNullOrWhiteSpace(request.Methodology))
                return BadRequest("La metodología es obligatoria");

            if (string.IsNullOrWhiteSpace(request.Phase))
                return BadRequest("La fase es obligatoria");

            if (string.IsNullOrWhiteSpace(request.TargetKpi))
                return BadRequest("El KPI objetivo es obligatorio");

            if (request.MinValue > request.MaxValue)
                return BadRequest("El valor mínimo no puede ser mayor al máximo");

            if (request.BaseValue < request.MinValue || request.BaseValue > request.MaxValue)
                return BadRequest("El valor base debe estar entre el mínimo y el máximo");

            if (request.Weight < 0 || request.Weight > 1)
                return BadRequest("El peso debe estar entre 0 y 1");

            var variable = new ScenarioVariable
            {
                ScenarioId = request.ScenarioId,
                Name = request.Name,
                Methodology = request.Methodology,
                Phase = request.Phase,
                TargetKpi = request.TargetKpi,
                Weight = request.Weight,
                BaseValue = request.BaseValue,
                MinValue = request.MinValue,
                MaxValue = request.MaxValue
            };

            _context.ScenarioVariables.Add(variable);
            _context.SaveChanges();

            return Ok("Variable metodológica creada correctamente");
        }

        [Authorize]
        [HttpGet("by-scenario/{scenarioId}")]
        public IActionResult GetVariablesByScenario(int scenarioId)
        {
            var scenarioExists = _context.Scenarios.Any(s => s.Id == scenarioId);

            if (!scenarioExists)
                return NotFound("El escenario no existe");

            var variables = _context.ScenarioVariables
                .Where(v => v.ScenarioId == scenarioId)
                .OrderBy(v => v.Methodology)
                .ThenBy(v => v.Phase)
                .ThenBy(v => v.Name)
                .ToList();

            return Ok(variables);
        }
    }
}