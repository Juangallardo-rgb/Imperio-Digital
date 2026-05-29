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
        private readonly ScenarioOptionTemplateService _scenarioOptionTemplateService;

        public ScenarioService(
            AppDbContext context,
            AiScenarioContentService aiScenarioContentService,
            ScenarioOptionTemplateService scenarioOptionTemplateService)
        {
            _context = context;
            _aiScenarioContentService = aiScenarioContentService;
            _scenarioOptionTemplateService = scenarioOptionTemplateService;
        }

        public async Task<ScenarioDetailDto> CreateDesignThinkingScenarioAsync(
            CreateDesignThinkingScenarioDto request,
            int teacherId)
        {
            var methodologyCode = string.IsNullOrWhiteSpace(request.MethodologyCode)
                ? "DesignThinking"
                : request.MethodologyCode.Trim();

            var methodology = await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .FirstOrDefaultAsync(m => m.Code == methodologyCode && m.IsActive);

            if (methodology == null)
                throw new Exception($"La metodología '{methodologyCode}' no existe o no está activa.");

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
                AvailableFrom = request.AvailableFrom,
                AvailableUntil = request.AvailableUntil,
                MaxAttemptsPerStudent = request.MaxAttemptsPerStudent <= 0 ? 1 : request.MaxAttemptsPerStudent,
                AllowLateAttempts = request.AllowLateAttempts,
                IsPublished = false,
                CreatedByUserId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scenarios.Add(scenario);
            await _context.SaveChangesAsync();

            await AddPhaseSettingsFromMethodologyAsync(scenario.Id, methodology.Id);
            await _context.SaveChangesAsync();

            await AddScenarioOptionsAsync(scenario.Id, methodology.Code);

            scenario.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetScenarioDetailAsync(scenario.Id, teacherId, true)
                   ?? throw new Exception("No se pudo recuperar el escenario creado.");
        }

        public async Task<List<ScenarioSummaryDto>> GetMyScenariosAsync(int teacherId)
        {
            return await _context.Scenarios
                .Where(s => s.CreatedByUserId == teacherId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ScenarioSummaryDto
                {
                    Id = s.Id,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? s.Name : s.Title,
                    Description = s.Description,
                    CompanyType = s.CompanyType,
                    Problem = s.Problem,
                    TargetUser = s.TargetUser,
                    Methodology = s.Methodology,
                    MethodologyName = GetMethodologyName(s.Methodology),
                    Difficulty = s.Difficulty,
                    IsPublished = s.IsPublished,
                    AvailableFrom = s.AvailableFrom,
                    AvailableUntil = s.AvailableUntil,
                    MaxAttemptsPerStudent = s.MaxAttemptsPerStudent,
                    AllowLateAttempts = s.AllowLateAttempts,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<ScenarioSummaryDto>> GetPublishedScenariosAsync()
        {
            return await _context.Scenarios
                .Where(s => s.IsPublished)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ScenarioSummaryDto
                {
                    Id = s.Id,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? s.Name : s.Title,
                    Description = s.Description,
                    CompanyType = s.CompanyType,
                    Problem = s.Problem,
                    TargetUser = s.TargetUser,
                    Methodology = s.Methodology,
                    MethodologyName = GetMethodologyName(s.Methodology),
                    Difficulty = s.Difficulty,
                    IsPublished = s.IsPublished,
                    AvailableFrom = s.AvailableFrom,
                    AvailableUntil = s.AvailableUntil,
                    MaxAttemptsPerStudent = s.MaxAttemptsPerStudent,
                    AllowLateAttempts = s.AllowLateAttempts,
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

            query = allowTeacherOwnerOnly
                ? query.Where(s => s.CreatedByUserId == userId)
                : query.Where(s => s.IsPublished || s.CreatedByUserId == userId);

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
            scenario.AvailableFrom = request.AvailableFrom;
            scenario.AvailableUntil = request.AvailableUntil;
            scenario.MaxAttemptsPerStudent = request.MaxAttemptsPerStudent <= 0 ? 1 : request.MaxAttemptsPerStudent;
            scenario.AllowLateAttempts = request.AllowLateAttempts;
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
            var scenario = await _context.Scenarios
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            var phaseExists = scenario.PhaseSettings.Any(p => p.PhaseName == request.PhaseName);

            if (!phaseExists)
                return (false, "La fase no pertenece a la metodología de este escenario.");

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

            scenario.UpdatedAt = DateTime.UtcNow;
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

            var enabledPhases = scenario.PhaseSettings
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.PhaseOrder)
                .ToList();

            if (!enabledPhases.Any())
                return (false, "El escenario debe tener fases configuradas.");

            var totalWeight = enabledPhases.Sum(p => p.PhaseWeight);

            if (totalWeight != 100)
                return (false, $"La suma de pesos debe ser 100. Actualmente es {totalWeight}.");

            if (!scenario.Options.Any())
                return (false, "El escenario debe tener opciones configuradas antes de publicarse.");

            var phasesWithoutOptions = enabledPhases
                .Where(p => !scenario.Options.Any(o => o.PhaseName == p.PhaseName))
                .Select(p => p.PhaseName)
                .ToList();

            if (phasesWithoutOptions.Any())
                return (false, $"Faltan opciones para estas fases: {string.Join(", ", phasesWithoutOptions)}.");

            if (scenario.AvailableFrom.HasValue &&
                scenario.AvailableUntil.HasValue &&
                scenario.AvailableUntil.Value <= scenario.AvailableFrom.Value)
            {
                return (false, "La fecha de cierre debe ser posterior a la fecha de inicio.");
            }

            if (scenario.MaxAttemptsPerStudent <= 0)
                scenario.MaxAttemptsPerStudent = 1;

            scenario.IsPublished = true;
            scenario.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (true, "Escenario publicado correctamente.");
        }

        public async Task<(bool Success, string Message)> RegenerateBaseOptionsAsync(int scenarioId, int teacherId)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.Options)
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            _context.ScenarioOptions.RemoveRange(scenario.Options);
            await _context.SaveChangesAsync();

            await AddScenarioOptionsAsync(scenario.Id, scenario.Methodology);

            scenario.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Opciones regeneradas correctamente según la metodología del escenario.");
        }

        private async Task AddScenarioOptionsAsync(int scenarioId, string methodologyCode)
        {
            var phaseNames = await _context.ScenarioPhaseSettings
                .Where(p => p.ScenarioId == scenarioId && p.IsEnabled)
                .Select(p => p.PhaseName)
                .ToListAsync();

            List<ScenarioOption> options;

            try
            {
                var scenario = await _context.Scenarios
                    .FirstAsync(s => s.Id == scenarioId);

                var aiOptions = await _aiScenarioContentService.GenerateOptionsForScenarioAsync(scenario);

                options = AreOptionsValidForScenario(aiOptions, phaseNames)
                    ? aiOptions
                    : _scenarioOptionTemplateService.GenerateBaseOptions(scenarioId, methodologyCode);
            }
            catch
            {
                options = _scenarioOptionTemplateService.GenerateBaseOptions(scenarioId, methodologyCode);
            }

            foreach (var option in options)
            {
                option.ScenarioId = scenarioId;
            }

            _context.ScenarioOptions.AddRange(options);
        }

        private static bool AreOptionsValidForScenario(
            List<ScenarioOption> options,
            List<string> phaseNames)
        {
            if (options == null || options.Count == 0)
                return false;

            if (!options.All(o => phaseNames.Contains(o.PhaseName)))
                return false;

            foreach (var phaseName in phaseNames)
            {
                var hasOptions = options.Any(o => o.PhaseName == phaseName);
                var hasCorrect = options.Any(o => o.PhaseName == phaseName && o.IsCorrect);

                if (!hasOptions || !hasCorrect)
                    return false;
            }

            return true;
        }

        private async Task AddPhaseSettingsFromMethodologyAsync(int scenarioId, int methodologyId)
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
                MethodologyName = GetMethodologyName(scenario.Methodology),
                Difficulty = scenario.Difficulty,
                IsPublished = scenario.IsPublished,
                AvailableFrom = scenario.AvailableFrom,
                AvailableUntil = scenario.AvailableUntil,
                MaxAttemptsPerStudent = scenario.MaxAttemptsPerStudent,
                AllowLateAttempts = scenario.AllowLateAttempts,
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
                    .OrderBy(o => GetPhaseOrder(scenario, o.PhaseName))
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

        private static int GetPhaseOrder(Scenario scenario, string phaseName)
        {
            return scenario.PhaseSettings
                .FirstOrDefault(p => p.PhaseName == phaseName)
                ?.PhaseOrder ?? 999;
        }

        private static string GetMethodologyName(string methodologyCode)
        {
            return methodologyCode switch
            {
                "BPM" => "Business Process Management",
                "DigitalMaturity" => "Madurez Digital",
                "LeanStartup" => "Lean Startup",
                _ => "Design Thinking"
            };
        }
        public async Task<GeneratedScenarioDraftDto> GenerateScenarioDraftAsync(string methodology)
        {
            return await _aiScenarioContentService.GenerateScenarioDraftAsync(methodology);
        }
    }
}