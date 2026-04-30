using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class ScenarioService
    {
        private readonly AppDbContext _context;

        public ScenarioService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ScenarioDetailDto> CreateDesignThinkingScenarioAsync(
            CreateDesignThinkingScenarioDto request,
            int teacherId)
        {
            var scenario = new Scenario
            {
                Title = request.Title,
                Name = request.Title, // compatibilidad con el flujo anterior
                Description = request.Description,
                CompanyType = request.CompanyType,
                Problem = request.Problem,
                TargetUser = request.TargetUser,
                Constraints = request.Constraints,
                Methodology = "DesignThinking",
                Difficulty = request.Difficulty,
                IsPublished = false,
                CreatedByUserId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scenarios.Add(scenario);
            await _context.SaveChangesAsync();

            AddDefaultPhaseSettings(scenario.Id);
            AddBaseScenarioOptions(scenario.Id);

            await _context.SaveChangesAsync();

            return await GetScenarioDetailAsync(scenario.Id, teacherId, true)
                   ?? throw new Exception("No se pudo recuperar el escenario creado.");
        }

        public async Task<List<ScenarioSummaryDto>> GetMyScenariosAsync(int teacherId)
        {
            return await _context.Scenarios
                .Where(s => s.CreatedByUserId == teacherId && s.Methodology == "DesignThinking")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ScenarioSummaryDto
                {
                    Id = s.Id,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? s.Name : s.Title,
                    Description = s.Description,
                    CompanyType = s.CompanyType,
                    Problem = s.Problem,
                    TargetUser = s.TargetUser,
                    Difficulty = s.Difficulty,
                    IsPublished = s.IsPublished,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<ScenarioSummaryDto>> GetPublishedScenariosAsync()
        {
            return await _context.Scenarios
                .Where(s => s.IsPublished && s.Methodology == "DesignThinking")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ScenarioSummaryDto
                {
                    Id = s.Id,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? s.Name : s.Title,
                    Description = s.Description,
                    CompanyType = s.CompanyType,
                    Problem = s.Problem,
                    TargetUser = s.TargetUser,
                    Difficulty = s.Difficulty,
                    IsPublished = s.IsPublished,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<ScenarioDetailDto?> GetScenarioDetailAsync(
            int scenarioId,
            int userId,
            bool allowTeacherOwnerOnly)
        {
            var query = _context.Scenarios
                .Include(s => s.PhaseSettings)
                    .ThenInclude(p => p.Criteria)
                .Include(s => s.Options)
                .AsQueryable();

            if (allowTeacherOwnerOnly)
            {
                query = query.Where(s => s.CreatedByUserId == userId);
            }
            else
            {
                query = query.Where(s => s.IsPublished || s.CreatedByUserId == userId);
            }

            var scenario = await query.FirstOrDefaultAsync(s => s.Id == scenarioId);

            if (scenario == null)
                return null;

            return MapToDetailDto(scenario);
        }

        public async Task<ScenarioDetailDto?> UpdateScenarioAsync(
            int scenarioId,
            int teacherId,
            UpdateDesignThinkingScenarioDto request)
        {
            var scenario = await _context.Scenarios
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return null;

            scenario.Title = request.Title;
            scenario.Name = request.Title;
            scenario.Description = request.Description;
            scenario.CompanyType = request.CompanyType;
            scenario.Problem = request.Problem;
            scenario.TargetUser = request.TargetUser;
            scenario.Constraints = request.Constraints;
            scenario.Difficulty = request.Difficulty;
            scenario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetScenarioDetailAsync(scenarioId, teacherId, true);
        }

        public async Task<(bool Success, string Message)> UpdatePhaseSettingsAsync(
            int scenarioId,
            int teacherId,
            UpdatePhaseSettingsDto request)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            if (request.Phases == null || request.Phases.Count == 0)
                return (false, "Debe enviar al menos una fase.");

            var totalWeight = request.Phases.Sum(p => p.PhaseWeight);

            if (totalWeight != 100)
                return (false, $"La suma de pesos debe ser 100. Actualmente es {totalWeight}.");

            foreach (var phaseRequest in request.Phases)
            {
                var phase = scenario.PhaseSettings
                    .FirstOrDefault(p => p.PhaseName == phaseRequest.PhaseName);

                if (phase == null)
                    return (false, $"La fase {phaseRequest.PhaseName} no existe.");

                phase.PhaseWeight = phaseRequest.PhaseWeight;
            }

            scenario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Pesos de fases actualizados correctamente.");
        }

        public async Task<(bool Success, string Message)> AddScenarioOptionAsync(
            int scenarioId,
            int teacherId,
            CreateScenarioOptionDto request)
        {
            var scenarioExists = await _context.Scenarios
                .AnyAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (!scenarioExists)
                return (false, "Escenario no encontrado.");

            var option = new ScenarioOption
            {
                ScenarioId = scenarioId,
                PhaseName = request.PhaseName,
                OptionType = request.OptionType,
                Text = request.Text,
                Score = request.Score,
                IsCorrect = request.IsCorrect,
                ImpactJson = request.ImpactJson,
                OrderIndex = request.OrderIndex
            };

            _context.ScenarioOptions.Add(option);
            await _context.SaveChangesAsync();

            return (true, "Opción agregada correctamente.");
        }

        public async Task<(bool Success, string Message)> PublishScenarioAsync(int scenarioId, int teacherId)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.PhaseSettings)
                .Include(s => s.Options)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            if (scenario.PhaseSettings.Count != 5)
                return (false, "El escenario debe tener las 5 fases de Design Thinking.");

            var totalWeight = scenario.PhaseSettings.Sum(p => p.PhaseWeight);

            if (totalWeight != 100)
                return (false, $"La suma de pesos debe ser 100. Actualmente es {totalWeight}.");

            if (!scenario.Options.Any())
                return (false, "El escenario debe tener opciones configuradas antes de publicarse.");

            scenario.IsPublished = true;
            scenario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Escenario publicado correctamente.");
        }

        public async Task<(bool Success, string Message)> RegenerateBaseOptionsAsync(int scenarioId, int teacherId)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.Options)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            _context.ScenarioOptions.RemoveRange(scenario.Options);

            AddBaseScenarioOptions(scenarioId);

            scenario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Opciones base regeneradas correctamente.");
        }

        private void AddDefaultPhaseSettings(int scenarioId)
        {
            var empathize = new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                PhaseName = "Empatizar",
                PhaseOrder = 1,
                PhaseWeight = 25,
                Criteria = new List<PhaseCriteriaSetting>
                {
                    new() { CriterionName = "Selección de evidencias relevantes", CriterionWeight = 40, EvaluationType = "Selection" },
                    new() { CriterionName = "Identificación de dolores del usuario", CriterionWeight = 30, EvaluationType = "Selection" },
                    new() { CriterionName = "Justificación escrita", CriterionWeight = 30, EvaluationType = "AIText" }
                }
            };

            var define = new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                PhaseName = "Definir",
                PhaseOrder = 2,
                PhaseWeight = 20,
                Criteria = new List<PhaseCriteriaSetting>
                {
                    new() { CriterionName = "Claridad del problema", CriterionWeight = 40, EvaluationType = "Selection" },
                    new() { CriterionName = "Enfoque en usuario", CriterionWeight = 30, EvaluationType = "Selection" },
                    new() { CriterionName = "Relación con evidencia", CriterionWeight = 30, EvaluationType = "AIText" }
                }
            };

            var ideate = new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                PhaseName = "Idear",
                PhaseOrder = 3,
                PhaseWeight = 20,
                Criteria = new List<PhaseCriteriaSetting>
                {
                    new() { CriterionName = "Creatividad", CriterionWeight = 25, EvaluationType = "Selection" },
                    new() { CriterionName = "Viabilidad", CriterionWeight = 25, EvaluationType = "Selection" },
                    new() { CriterionName = "Impacto esperado", CriterionWeight = 30, EvaluationType = "Selection" },
                    new() { CriterionName = "Alineación digital", CriterionWeight = 20, EvaluationType = "AIText" }
                }
            };

            var prototype = new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                PhaseName = "Prototipar",
                PhaseOrder = 4,
                PhaseWeight = 20,
                Criteria = new List<PhaseCriteriaSetting>
                {
                    new() { CriterionName = "Coherencia con solución", CriterionWeight = 35, EvaluationType = "Selection" },
                    new() { CriterionName = "Funcionalidades mínimas", CriterionWeight = 35, EvaluationType = "Selection" },
                    new() { CriterionName = "Claridad del flujo", CriterionWeight = 30, EvaluationType = "AIText" }
                }
            };

            var test = new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                PhaseName = "Evaluar",
                PhaseOrder = 5,
                PhaseWeight = 15,
                Criteria = new List<PhaseCriteriaSetting>
                {
                    new() { CriterionName = "Selección de KPIs", CriterionWeight = 40, EvaluationType = "Selection" },
                    new() { CriterionName = "Interpretación de resultados", CriterionWeight = 35, EvaluationType = "AIText" },
                    new() { CriterionName = "Propuesta de mejora", CriterionWeight = 25, EvaluationType = "AIText" }
                }
            };

            _context.ScenarioPhaseSettings.AddRange(empathize, define, ideate, prototype, test);
        }

        private void AddBaseScenarioOptions(int scenarioId)
        {
            var options = new List<ScenarioOption>
            {
                // EMPATIZAR - Evidencias
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "Evidence",
                    Text = "Los usuarios abandonan el proceso cuando aparecen costos adicionales al final.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 1
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "Evidence",
                    Text = "Los usuarios indican que el proceso digital es lento y poco claro.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 2
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "Evidence",
                    Text = "El logo de la empresa no utiliza colores modernos.",
                    IsCorrect = false,
                    Score = 0,
                    OrderIndex = 3
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "PainPoint",
                    Text = "Falta de transparencia en costos y tiempos del servicio.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 4
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "PainPoint",
                    Text = "Baja claridad del flujo digital para completar la acción principal.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 5
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Empatizar",
                    OptionType = "PainPoint",
                    Text = "Necesidad de publicar más contenido en redes sociales.",
                    IsCorrect = false,
                    Score = 0,
                    OrderIndex = 6
                },

                // DEFINIR
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Definir",
                    OptionType = "ProblemStatement",
                    Text = "Los usuarios digitales necesitan un proceso claro y transparente porque la falta de información reduce la confianza y aumenta el abandono.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 1
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Definir",
                    OptionType = "ProblemStatement",
                    Text = "La empresa necesita cambiar su imagen porque el diseño visual no parece moderno.",
                    IsCorrect = false,
                    Score = 0,
                    OrderIndex = 2
                },

                // IDEAR - Soluciones
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Idear",
                    OptionType = "Solution",
                    Text = "Mostrar costos completos desde el inicio del proceso.",
                    IsCorrect = true,
                    Score = 100,
                    ImpactJson = "{\"cartAbandonment\":-5,\"conversionRate\":0.8,\"satisfaction\":7,\"purchaseTime\":-0.5}",
                    OrderIndex = 1
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Idear",
                    OptionType = "Solution",
                    Text = "Simplificar el flujo de compra o solicitud digital.",
                    IsCorrect = true,
                    Score = 100,
                    ImpactJson = "{\"cartAbandonment\":-8,\"conversionRate\":1.2,\"satisfaction\":10,\"purchaseTime\":-1.5}",
                    OrderIndex = 2
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Idear",
                    OptionType = "Solution",
                    Text = "Crear una campaña únicamente centrada en rediseñar el logo.",
                    IsCorrect = false,
                    Score = 0,
                    ImpactJson = "{\"cartAbandonment\":0,\"conversionRate\":0,\"satisfaction\":1,\"purchaseTime\":0}",
                    OrderIndex = 3
                },

                // PROTOTIPAR
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Prototipar",
                    OptionType = "PrototypeFeature",
                    Text = "Pantalla con resumen de costos, tiempos y acción principal visible.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 1
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Prototipar",
                    OptionType = "PrototypeFeature",
                    Text = "Formulario reducido con solo los datos necesarios para completar la acción.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 2
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Prototipar",
                    OptionType = "UserFlowStep",
                    Text = "Usuario revisa información → confirma datos → visualiza costos → completa acción → recibe confirmación.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 3
                },

                // EVALUAR
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Evaluar",
                    OptionType = "KPI",
                    Text = "Tasa de abandono del proceso.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 1
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Evaluar",
                    OptionType = "KPI",
                    Text = "Tasa de conversión.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 2
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Evaluar",
                    OptionType = "KPI",
                    Text = "Satisfacción del usuario.",
                    IsCorrect = true,
                    Score = 100,
                    OrderIndex = 3
                },
                new()
                {
                    ScenarioId = scenarioId,
                    PhaseName = "Evaluar",
                    OptionType = "KPI",
                    Text = "Cantidad de colores usados en la marca.",
                    IsCorrect = false,
                    Score = 0,
                    OrderIndex = 4
                }
            };

            _context.ScenarioOptions.AddRange(options);
        }

        private static ScenarioDetailDto MapToDetailDto(Scenario scenario)
        {
            return new ScenarioDetailDto
            {
                Id = scenario.Id,
                Title = string.IsNullOrWhiteSpace(scenario.Title) ? scenario.Name : scenario.Title,
                Name = scenario.Name,
                Description = scenario.Description,
                CompanyType = scenario.CompanyType,
                Problem = scenario.Problem,
                TargetUser = scenario.TargetUser,
                Constraints = scenario.Constraints,
                Methodology = scenario.Methodology,
                Difficulty = scenario.Difficulty,
                IsPublished = scenario.IsPublished,
                CreatedAt = scenario.CreatedAt,
                UpdatedAt = scenario.UpdatedAt,
                PhaseSettings = scenario.PhaseSettings
                    .OrderBy(p => p.PhaseOrder)
                    .Select(p => new PhaseSettingDetailDto
                    {
                        Id = p.Id,
                        PhaseName = p.PhaseName,
                        PhaseOrder = p.PhaseOrder,
                        PhaseWeight = p.PhaseWeight,
                        Criteria = p.Criteria
                            .Select(c => new PhaseCriteriaDetailDto
                            {
                                Id = c.Id,
                                CriterionName = c.CriterionName,
                                CriterionWeight = c.CriterionWeight,
                                EvaluationType = c.EvaluationType
                            })
                            .ToList()
                    })
                    .ToList(),
                Options = scenario.Options
                    .OrderBy(o => o.PhaseName)
                    .ThenBy(o => o.OptionType)
                    .ThenBy(o => o.OrderIndex)
                    .Select(o => new ScenarioOptionDetailDto
                    {
                        Id = o.Id,
                        PhaseName = o.PhaseName,
                        OptionType = o.OptionType,
                        Text = o.Text,
                        Score = o.Score,
                        IsCorrect = o.IsCorrect,
                        ImpactJson = o.ImpactJson,
                        OrderIndex = o.OrderIndex
                    })
                    .ToList()
            };
        }
    }
}