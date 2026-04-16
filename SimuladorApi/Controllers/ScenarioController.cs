using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.Data;
using SimuladorApi.DTOs;
using SimuladorApi.Models;
using System.Security.Claims;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScenarioController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ScenarioController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Docente")]
        [HttpPost]
        public IActionResult CreateScenario(CreateScenarioDto request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var scenario = new Scenario
            {
                Name = request.Name,
                Description = request.Description,
                CreatedByUserId = userId
            };

            _context.Scenarios.Add(scenario);
            _context.SaveChanges();

            return Ok("Escenario creado correctamente");
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetScenarios()
        {
            var scenarios = _context.Scenarios.ToList();
            return Ok(scenarios);
        }
    }
}