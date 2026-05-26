using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimuladorApi.DTOs.Methodologies;
using SimuladorApi.Services;

namespace SimuladorApi.Controllers
{
    [ApiController]
    [Route("api/methodologies")]
    public class MethodologiesController : ControllerBase
    {
        private readonly MethodologyCatalogService _methodologyCatalogService;

        public MethodologiesController(MethodologyCatalogService methodologyCatalogService)
        {
            _methodologyCatalogService = methodologyCatalogService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetMethodologies()
        {
            var methodologies = await _methodologyCatalogService.GetActiveMethodologiesAsync();

            var result = methodologies.Select(m => new MethodologyDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Description = m.Description,
                Phases = m.Phases
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PhaseOrder)
                    .Select(p => new MethodologyPhaseDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PhaseOrder = p.PhaseOrder,
                        Description = p.Description,
                        DefaultWeight = p.DefaultWeight,
                        DefaultMaxSelections = p.DefaultMaxSelections,
                        Criteria = p.Criteria
                            .Where(c => c.IsActive)
                            .Select(c => new MethodologyPhaseCriteriaDto
                            {
                                Id = c.Id,
                                Name = c.Name,
                                DefaultWeight = c.DefaultWeight,
                                EvaluationType = c.EvaluationType
                            })
                            .ToList()
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("{code}")]
        public async Task<IActionResult> GetMethodologyByCode(string code)
        {
            var methodology = await _methodologyCatalogService.GetByCodeAsync(code);

            if (methodology == null)
                return NotFound("Metodología no encontrada.");

            var result = new MethodologyDto
            {
                Id = methodology.Id,
                Code = methodology.Code,
                Name = methodology.Name,
                Description = methodology.Description,
                Phases = methodology.Phases
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.PhaseOrder)
                    .Select(p => new MethodologyPhaseDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        PhaseOrder = p.PhaseOrder,
                        Description = p.Description,
                        DefaultWeight = p.DefaultWeight,
                        DefaultMaxSelections = p.DefaultMaxSelections,
                        Criteria = p.Criteria
                            .Where(c => c.IsActive)
                            .Select(c => new MethodologyPhaseCriteriaDto
                            {
                                Id = c.Id,
                                Name = c.Name,
                                DefaultWeight = c.DefaultWeight,
                                EvaluationType = c.EvaluationType
                            })
                            .ToList()
                    })
                    .ToList()
            };

            return Ok(result);
        }
    }
}