using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class ScenarioService
    {
        private readonly AppDbContext _context;
        private readonly AiScenarioContentService _aiScenarioContentService;

        public ScenarioService(
            AppDbContext context,
            AiScenarioContentService aiScenarioContentService)
        {
            _context = context;
            _aiScenarioContentService = aiScenarioContentService;
        }

        public async Task<ScenarioDetailDto> CreateDesignThinkingScenarioAsync(
            CreateDesignThinkingScenarioDto request,
            int teacherId)
        {
            var methodology = await EnsureDesignThinkingMethodologyAsync();

            var scenario = new Scenario
            {
                Title = request.Title,
                Name = request.Title,
                Description = request.Description,
                CompanyType = request.CompanyType,
                Problem = request.Problem,
                TargetUser = request.TargetUser,
                Constraints = request.Constraints,
                Methodology = methodology.Code,
                MethodologyId = methodology.Id,
                Difficulty = request.Difficulty,
                IsPublished = false,
                CreatedByUserId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scenarios.Add(scenario);
            await _context.SaveChangesAsync();

            await AddDefaultPhaseSettingsFromMethodologyAsync(scenario.Id, methodology.Id);

            try
            {
                var aiOptions = await _aiScenarioContentService.GenerateOptionsForScenarioAsync(scenario);

                _context.ScenarioOptions.AddRange(aiOptions);
            }
            catch
            {
                AddBaseScenarioOptions(scenario.Id);
            }

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
                OrderIndex = request.OrderIndex,
                Cost = 0,
                TimeCost = 0,
                RiskImpact = request.IsCorrect ? 0 : 5,
                TagsJson = "[]",
                MaxSelections = 0,
                ExpectedImpactLevel = "",
                ExpectedEffortLevel = "",
                ExpectedViabilityLevel = ""
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

            try
            {
                var aiOptions = await _aiScenarioContentService.GenerateOptionsForScenarioAsync(scenario);

                _context.ScenarioOptions.AddRange(aiOptions);

                scenario.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return (true, "Opciones personalizadas generadas correctamente con IA.");
            }
            catch (Exception ex)
            {
                AddBaseScenarioOptions(scenarioId);

                scenario.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return (true, $"No se pudo generar con IA. Se cargaron opciones base. Detalle: {ex.Message}");
            }
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
        // =========================
        // EMPATIZAR - EVIDENCIAS
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "Evidence",
            Text = "Los usuarios abandonan el proceso cuando aparecen costos adicionales al final.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 1,
            TagsJson = "[\"hidden-costs\",\"trust\",\"checkout\",\"conversion\"]",
            MaxSelections = 3,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "Evidence",
            Text = "Los usuarios indican que el proceso digital es lento y poco claro.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 2,
            TagsJson = "[\"ux\",\"checkout\",\"purchase-time\",\"friction\"]",
            MaxSelections = 3,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "Evidence",
            Text = "Varios usuarios reportan dificultades al completar el proceso desde dispositivos móviles.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 3,
            TagsJson = "[\"mobile\",\"ux\",\"checkout\",\"friction\"]",
            MaxSelections = 3,
            RiskImpact = -3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "Evidence",
            Text = "El logo de la empresa no utiliza colores modernos.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 4,
            TagsJson = "[\"branding\"]",
            MaxSelections = 3,
            RiskImpact = 5
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "Evidence",
            Text = "El equipo de marketing quiere publicar más contenido institucional.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 5,
            TagsJson = "[\"social-media\",\"marketing\"]",
            MaxSelections = 3,
            RiskImpact = 5
        },

        // =========================
        // EMPATIZAR - DOLORES
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "PainPoint",
            Text = "Falta de transparencia en costos y tiempos del servicio.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 6,
            TagsJson = "[\"hidden-costs\",\"trust\",\"delivery-time\"]",
            MaxSelections = 2,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "PainPoint",
            Text = "Confusión durante el flujo digital para completar la acción principal.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 7,
            TagsJson = "[\"ux\",\"checkout\",\"friction\"]",
            MaxSelections = 2,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Empatizar",
            OptionType = "PainPoint",
            Text = "Necesidad de cambiar la paleta de colores de la marca.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 8,
            TagsJson = "[\"branding\"]",
            MaxSelections = 2,
            RiskImpact = 5
        },

        // =========================
        // DEFINIR
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Definir",
            OptionType = "ProblemStatement",
            Text = "Los usuarios digitales necesitan un proceso claro y transparente porque la falta de información reduce la confianza y aumenta el abandono.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 1,
            TagsJson = "[\"hidden-costs\",\"trust\",\"checkout\",\"conversion\"]",
            MaxSelections = 1,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Definir",
            OptionType = "ProblemStatement",
            Text = "Los usuarios móviles necesitan una experiencia más simple porque el proceso actual genera fricción y abandono.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 2,
            TagsJson = "[\"mobile\",\"ux\",\"checkout\",\"friction\"]",
            MaxSelections = 1,
            RiskImpact = -2
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Definir",
            OptionType = "ProblemStatement",
            Text = "La empresa necesita cambiar su imagen porque el diseño visual no parece moderno.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 3,
            TagsJson = "[\"branding\"]",
            MaxSelections = 1,
            RiskImpact = 8
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Definir",
            OptionType = "ProblemStatement",
            Text = "La empresa necesita publicar más en redes sociales para mejorar su presencia digital.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 4,
            TagsJson = "[\"social-media\",\"marketing\"]",
            MaxSelections = 1,
            RiskImpact = 8
        },

        // =========================
        // IDEAR - SOLUCIONES
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Mostrar costos completos desde el inicio del proceso.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-5,\"conversionRate\":0.8,\"satisfaction\":7,\"purchaseTime\":-0.5,\"digitalAdoption\":4}",
            OrderIndex = 1,
            Cost = 25,
            TimeCost = 2,
            RiskImpact = 5,
            TagsJson = "[\"hidden-costs\",\"trust\",\"checkout\",\"conversion\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Alto",
            ExpectedEffortLevel = "Bajo",
            ExpectedViabilityLevel = "Alta"
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Simplificar el flujo de compra o solicitud digital.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-8,\"conversionRate\":1.2,\"satisfaction\":10,\"purchaseTime\":-1.5,\"digitalAdoption\":7}",
            OrderIndex = 2,
            Cost = 45,
            TimeCost = 4,
            RiskImpact = 12,
            TagsJson = "[\"ux\",\"checkout\",\"friction\",\"conversion\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Alto",
            ExpectedEffortLevel = "Medio",
            ExpectedViabilityLevel = "Media"
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Optimizar la experiencia móvil del proceso principal.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-6,\"conversionRate\":1.0,\"satisfaction\":8,\"purchaseTime\":-1.0,\"digitalAdoption\":8}",
            OrderIndex = 3,
            Cost = 40,
            TimeCost = 3,
            RiskImpact = 10,
            TagsJson = "[\"mobile\",\"ux\",\"checkout\",\"digital-adoption\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Alto",
            ExpectedEffortLevel = "Medio",
            ExpectedViabilityLevel = "Media"
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Implementar chatbot de soporte para dudas frecuentes.",
            IsCorrect = true,
            Score = 80,
            ImpactJson = "{\"cartAbandonment\":-3,\"conversionRate\":0.4,\"satisfaction\":5,\"purchaseTime\":-0.2,\"digitalAdoption\":3}",
            OrderIndex = 4,
            Cost = 35,
            TimeCost = 3,
            RiskImpact = 8,
            TagsJson = "[\"support\",\"automation\",\"trust\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Medio",
            ExpectedEffortLevel = "Medio",
            ExpectedViabilityLevel = "Alta"
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Desarrollar una aplicación móvil completamente nueva.",
            IsCorrect = false,
            Score = 30,
            ImpactJson = "{\"cartAbandonment\":-2,\"conversionRate\":0.2,\"satisfaction\":3,\"purchaseTime\":-0.2,\"digitalAdoption\":5}",
            OrderIndex = 5,
            Cost = 90,
            TimeCost = 12,
            RiskImpact = 25,
            TagsJson = "[\"mobile\",\"high-cost\",\"high-risk\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Alto",
            ExpectedEffortLevel = "Alto",
            ExpectedViabilityLevel = "Baja"
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Idear",
            OptionType = "Solution",
            Text = "Realizar únicamente un rediseño visual de colores y banners.",
            IsCorrect = false,
            Score = 10,
            ImpactJson = "{\"cartAbandonment\":0,\"conversionRate\":0,\"satisfaction\":1,\"purchaseTime\":0,\"digitalAdoption\":0}",
            OrderIndex = 6,
            Cost = 60,
            TimeCost = 5,
            RiskImpact = 12,
            TagsJson = "[\"branding\"]",
            MaxSelections = 3,
            ExpectedImpactLevel = "Bajo",
            ExpectedEffortLevel = "Medio",
            ExpectedViabilityLevel = "Media"
        },

        // =========================
        // PROTOTIPAR
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "PrototypeFeature",
            Text = "Pantalla con resumen claro de costos, tiempos y acción principal visible.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-3,\"conversionRate\":0.5,\"satisfaction\":5,\"purchaseTime\":-0.3,\"digitalAdoption\":3}",
            OrderIndex = 1,
            Cost = 15,
            TimeCost = 1,
            RiskImpact = 4,
            TagsJson = "[\"hidden-costs\",\"trust\",\"checkout\"]",
            MaxSelections = 4
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "PrototypeFeature",
            Text = "Formulario reducido con solo los datos necesarios para completar la acción.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-3,\"conversionRate\":0.5,\"satisfaction\":4,\"purchaseTime\":-0.7,\"digitalAdoption\":4}",
            OrderIndex = 2,
            Cost = 20,
            TimeCost = 2,
            RiskImpact = 6,
            TagsJson = "[\"ux\",\"checkout\",\"friction\"]",
            MaxSelections = 4
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "PrototypeFeature",
            Text = "Vista optimizada para dispositivos móviles.",
            IsCorrect = true,
            Score = 100,
            ImpactJson = "{\"cartAbandonment\":-4,\"conversionRate\":0.7,\"satisfaction\":6,\"purchaseTime\":-0.8,\"digitalAdoption\":6}",
            OrderIndex = 3,
            Cost = 25,
            TimeCost = 2,
            RiskImpact = 7,
            TagsJson = "[\"mobile\",\"ux\",\"digital-adoption\"]",
            MaxSelections = 4
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "PrototypeFeature",
            Text = "Cambio completo de colores, tipografías y banners promocionales.",
            IsCorrect = false,
            Score = 10,
            ImpactJson = "{\"cartAbandonment\":0,\"conversionRate\":0,\"satisfaction\":1,\"purchaseTime\":0,\"digitalAdoption\":0}",
            OrderIndex = 4,
            Cost = 30,
            TimeCost = 2,
            RiskImpact = 8,
            TagsJson = "[\"branding\"]",
            MaxSelections = 4
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "UserFlowStep",
            Text = "Usuario agrega producto → ve costos completos → confirma datos → realiza pago → recibe confirmación.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 5,
            Cost = 0,
            TimeCost = 0,
            RiskImpact = -2,
            TagsJson = "[\"checkout\",\"trust\",\"ux\"]",
            MaxSelections = 4
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Prototipar",
            OptionType = "UserFlowStep",
            Text = "Usuario ve banners → lee noticias corporativas → cambia colores → revisa redes sociales.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 6,
            Cost = 0,
            TimeCost = 0,
            RiskImpact = 5,
            TagsJson = "[\"branding\",\"social-media\"]",
            MaxSelections = 4
        },

        // =========================
        // EVALUAR
        // =========================
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Tasa de abandono del proceso.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 1,
            TagsJson = "[\"cartAbandonment\",\"checkout\",\"conversion\"]",
            MaxSelections = 3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Tasa de conversión.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 2,
            TagsJson = "[\"conversion\",\"checkout\"]",
            MaxSelections = 3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Satisfacción del usuario.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 3,
            TagsJson = "[\"satisfaction\",\"ux\",\"trust\"]",
            MaxSelections = 3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Tiempo promedio para completar la acción digital.",
            IsCorrect = true,
            Score = 100,
            OrderIndex = 4,
            TagsJson = "[\"purchaseTime\",\"ux\",\"friction\"]",
            MaxSelections = 3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Cantidad de colores nuevos en el sitio.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 5,
            TagsJson = "[\"branding\"]",
            MaxSelections = 3
        },
        new()
        {
            ScenarioId = scenarioId,
            PhaseName = "Evaluar",
            OptionType = "KPI",
            Text = "Cantidad de publicaciones institucionales en redes.",
            IsCorrect = false,
            Score = 0,
            OrderIndex = 6,
            TagsJson = "[\"social-media\"]",
            MaxSelections = 3
        }
    };

            _context.ScenarioOptions.AddRange(options);
        }

        private async Task<Methodology> EnsureDesignThinkingMethodologyAsync()
        {
            var methodology = await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .FirstOrDefaultAsync(m => m.Code == "DesignThinking");

            if (methodology != null)
                return methodology;

            methodology = new Methodology
            {
                Code = "DesignThinking",
                Name = "Design Thinking",
                Description = "Metodología centrada en el usuario para resolver problemas mediante empatía, definición, ideación, prototipado y evaluación.",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Phases = new List<MethodologyPhase>
        {
            new()
            {
                Name = "Empatizar",
                PhaseOrder = 1,
                Description = "Comprender necesidades, dolores y evidencias del usuario.",
                DefaultWeight = 25,
                ActivityType = "SelectionAndText",
                DefaultMaxSelections = 5,
                Criteria = new List<MethodologyPhaseCriteria>
                {
                    new() { Name = "Selección de evidencias relevantes", DefaultWeight = 40, EvaluationType = "Selection" },
                    new() { Name = "Identificación de dolores del usuario", DefaultWeight = 30, EvaluationType = "Selection" },
                    new() { Name = "Justificación escrita", DefaultWeight = 30, EvaluationType = "AIText" }
                }
            },
            new()
            {
                Name = "Definir",
                PhaseOrder = 2,
                Description = "Formular el problema correcto a partir de la evidencia del usuario.",
                DefaultWeight = 20,
                ActivityType = "SelectionAndText",
                DefaultMaxSelections = 2,
                Criteria = new List<MethodologyPhaseCriteria>
                {
                    new() { Name = "Claridad del problema", DefaultWeight = 40, EvaluationType = "Selection" },
                    new() { Name = "Enfoque en usuario", DefaultWeight = 30, EvaluationType = "Selection" },
                    new() { Name = "Relación con evidencia", DefaultWeight = 30, EvaluationType = "AIText" }
                }
            },
            new()
            {
                Name = "Idear",
                PhaseOrder = 3,
                Description = "Generar y seleccionar soluciones digitales viables.",
                DefaultWeight = 20,
                ActivityType = "SelectionAndText",
                DefaultMaxSelections = 3,
                Criteria = new List<MethodologyPhaseCriteria>
                {
                    new() { Name = "Creatividad", DefaultWeight = 25, EvaluationType = "Selection" },
                    new() { Name = "Viabilidad", DefaultWeight = 25, EvaluationType = "Selection" },
                    new() { Name = "Impacto esperado", DefaultWeight = 30, EvaluationType = "Selection" },
                    new() { Name = "Alineación digital", DefaultWeight = 20, EvaluationType = "AIText" }
                }
            },
            new()
            {
                Name = "Prototipar",
                PhaseOrder = 4,
                Description = "Construir una propuesta mínima y coherente para validar la solución.",
                DefaultWeight = 20,
                ActivityType = "SelectionAndText",
                DefaultMaxSelections = 4,
                Criteria = new List<MethodologyPhaseCriteria>
                {
                    new() { Name = "Coherencia con solución", DefaultWeight = 35, EvaluationType = "Selection" },
                    new() { Name = "Funcionalidades mínimas", DefaultWeight = 35, EvaluationType = "Selection" },
                    new() { Name = "Claridad del flujo", DefaultWeight = 30, EvaluationType = "AIText" }
                }
            },
            new()
            {
                Name = "Evaluar",
                PhaseOrder = 5,
                Description = "Medir resultados, validar KPIs y proponer mejoras.",
                DefaultWeight = 15,
                ActivityType = "SelectionAndText",
                DefaultMaxSelections = 3,
                Criteria = new List<MethodologyPhaseCriteria>
                {
                    new() { Name = "Selección de KPIs", DefaultWeight = 40, EvaluationType = "Selection" },
                    new() { Name = "Interpretación de resultados", DefaultWeight = 35, EvaluationType = "AIText" },
                    new() { Name = "Propuesta de mejora", DefaultWeight = 25, EvaluationType = "AIText" }
                }
            }
        }
            };

            _context.Methodologies.Add(methodology);
            await _context.SaveChangesAsync();

            return methodology;
        }

        private async Task AddDefaultPhaseSettingsFromMethodologyAsync(int scenarioId, int methodologyId)
        {
            var phases = await _context.MethodologyPhases
                .Include(p => p.Criteria)
                .Where(p => p.MethodologyId == methodologyId && p.IsActive)
                .OrderBy(p => p.PhaseOrder)
                .ToListAsync();

            var scenarioPhases = phases.Select(phase => new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                MethodologyPhaseId = phase.Id,
                PhaseName = phase.Name,
                CustomName = phase.Name,
                PhaseOrder = phase.PhaseOrder,
                PhaseWeight = phase.DefaultWeight,
                IsEnabled = true,
                Criteria = phase.Criteria
                    .Where(c => c.IsActive)
                    .Select(c => new PhaseCriteriaSetting
                    {
                        MethodologyPhaseCriteriaId = c.Id,
                        CriterionName = c.Name,
                        CriterionWeight = c.DefaultWeight,
                        EvaluationType = c.EvaluationType
                    })
                    .ToList()
            }).ToList();

            _context.ScenarioPhaseSettings.AddRange(scenarioPhases);
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
                        OrderIndex = o.OrderIndex,
                        Cost = o.Cost,
                        TimeCost = o.TimeCost,
                        RiskImpact = o.RiskImpact,
                        TagsJson = o.TagsJson,
                        MaxSelections = o.MaxSelections,
                        ExpectedImpactLevel = o.ExpectedImpactLevel,
                        ExpectedEffortLevel = o.ExpectedEffortLevel,
                        ExpectedViabilityLevel = o.ExpectedViabilityLevel
                    })
                    .ToList()
            };
        }
    }
}