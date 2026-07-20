using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using Microsoft.Extensions.Logging;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;
using SimuladorApi.Services.Ai;
using System.Diagnostics;

namespace SimuladorApi.Services
{
    public class ScenarioService
    {
        private readonly AppDbContext _context;
        private readonly AiScenarioContentService _aiScenarioContentService;
        private readonly ScenarioOptionTemplateService _scenarioOptionTemplateService;
        private readonly ScenarioPhaseMappingService _scenarioPhaseMappingService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly ILogger<ScenarioService> _logger;
        private readonly AiGenerationAuditService _aiGenerationAuditService;
        private readonly AiScenarioContentValidator _aiScenarioContentValidator;

        public ScenarioService(
            AppDbContext context,
            AiScenarioContentService aiScenarioContentService,
            ScenarioOptionTemplateService scenarioOptionTemplateService,
            ScenarioPhaseMappingService scenarioPhaseMappingService,
            IRealtimeNotificationService realtime,
            AiGenerationAuditService aiGenerationAuditService,
            AiScenarioContentValidator aiScenarioContentValidator,
            ILogger<ScenarioService> logger)
        {
            _context = context;
            _aiScenarioContentService = aiScenarioContentService;
            _scenarioOptionTemplateService = scenarioOptionTemplateService;
            _scenarioPhaseMappingService = scenarioPhaseMappingService;
            _realtime = realtime;
            _aiGenerationAuditService = aiGenerationAuditService;
            _aiScenarioContentValidator = aiScenarioContentValidator;
            _logger = logger;
        }

        public async Task<ScenarioDetailDto> CreateDesignThinkingScenarioAsync(
            CreateDesignThinkingScenarioDto request,
            int teacherId)
        {
            var methodologyCode = NormalizeMethodologyCode(request.MethodologyCode);
            var creationMode = NormalizeCreationMode(request.CreationMode);

            var methodology = await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .FirstOrDefaultAsync(m => m.Code == methodologyCode && m.IsActive);

            if (methodology == null)
                throw new ArgumentException($"La metodología '{methodologyCode}' no existe o no está activa.");

            var phaseSettingsByPhaseId = ValidateCreatePhaseSettings(
                request.PhaseSettings,
                methodology
            );

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
                CreationMode = creationMode,
                GeneratedByAi = creationMode == "AiAssisted",
                CreatedByUserId = teacherId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var phaseSettings = BuildPhaseSettingsFromMethodology(
                methodology,
                phaseSettingsByPhaseId);
            scenario.PhaseSettings = phaseSettings;

            AiGenerationRecord? draftRecord = null;
            AiGenerationRecord? optionsRecord = null;
            if (creationMode == "AiAssisted")
            {
                if (!request.AiDraftGenerationId.HasValue)
                {
                    throw new ArgumentException("Debe generar y revisar un borrador con IA antes de crear el escenario.");
                }
                draftRecord = await _aiGenerationAuditService.FindSuccessfulDraftAsync(
                    request.AiDraftGenerationId.Value,
                    teacherId,
                    methodology.Code);
                if (draftRecord is null)
                {
                    throw new ArgumentException("El identificador del borrador IA no es válido, ya fue utilizado o no pertenece al docente autenticado.");
                }

                optionsRecord = await _aiGenerationAuditService.StartAsync(
                    teacherId,
                    "ScenarioOptions",
                    draftRecord.RequestedModel,
                    draftRecord.PromptVersion,
                    methodologyCode: methodology.Code);
                var generationResult = await _aiScenarioContentService
                    .GenerateOptionsWithDiagnosticsAsync(scenario, optionsRecord.CorrelationId);
                if (!generationResult.Success)
                {
                    await _aiGenerationAuditService.CompleteAsync(
                        optionsRecord,
                        false,
                        generationResult.EffectiveModel,
                        generationResult.RetryCount,
                        generationResult.PromptHash,
                        errorCode: generationResult.ErrorCode,
                        errorMessage: generationResult.UserMessage,
                        responseFormat: generationResult.ResponseFormat);
                    throw new AiContentGenerationException(
                        generationResult.FailedPhaseName is null
                            ? generationResult.UserMessage
                            : $"No fue posible generar la fase {generationResult.FailedPhaseName}.",
                        generationResult.ErrorCode,
                        generationResult.OpenRouterStatusCode,
                        generationResult.FailedPhaseName,
                        generationResult.MethodologyCode,
                        generationResult.CorrelationId,
                        generationResult.ValidationErrors);
                }

                scenario.Options = generationResult.Options;
                scenario.AiProvider = "OpenRouter";
                scenario.AiModel = generationResult.EffectiveModel ?? generationResult.RequestedModel;
                scenario.AiPromptVersion = generationResult.PromptVersion;
                scenario.AiGeneratedAt = DateTime.UtcNow;
                await _aiGenerationAuditService.CompleteAsync(
                    optionsRecord,
                    true,
                    generationResult.EffectiveModel,
                    generationResult.RetryCount,
                    generationResult.PromptHash,
                    responseFormat: generationResult.ResponseFormat);
            }
            else if (creationMode == "Template")
            {
                var templateOptions = _scenarioOptionTemplateService
                    .GenerateBaseOptions(0, methodology.Code)
                    .ToList();
                var allOptionsMapped = templateOptions.All(option =>
                    _scenarioPhaseMappingService.TryMapOptionToEnabledPhase(option, phaseSettings));
                if (!allOptionsMapped ||
                    !_scenarioPhaseMappingService.AreOptionsValidForEnabledPhases(templateOptions, phaseSettings))
                {
                    throw new ArgumentException("La plantilla explícita no es compatible con la metodología seleccionada.");
                }
                scenario.Options = templateOptions;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Scenarios.Add(scenario);
                await _context.SaveChangesAsync();

                foreach (var option in scenario.Options)
                {
                    option.ScenarioId = scenario.Id;
                }
                if (draftRecord is not null)
                {
                    draftRecord.ScenarioId = scenario.Id;
                    draftRecord.ConsumedAt = DateTime.UtcNow;
                    draftRecord.Status = "Consumed";
                }
                if (optionsRecord is not null)
                {
                    optionsRecord.ScenarioId = scenario.Id;
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                if (optionsRecord is not null)
                {
                    _context.ChangeTracker.Clear();
                    var persistedAudit = await _context.AiGenerationRecords
                        .FirstOrDefaultAsync(record => record.Id == optionsRecord.Id);
                    if (persistedAudit is not null)
                    {
                        persistedAudit.Status = "Failed";
                        persistedAudit.CompletedAt = DateTime.UtcNow;
                        persistedAudit.ErrorCode = AiOptionsGenerationErrorCodes.DbSaveError;
                        persistedAudit.ErrorMessage = "Las opciones fueron válidas, pero no se pudo guardar el escenario.";
                        await _context.SaveChangesAsync();
                    }
                }
                _logger.LogError(
                    exception,
                    "Scenario creation transaction failed. TeacherId={TeacherId} Methodology={Methodology} CreationMode={CreationMode}",
                    teacherId,
                    methodology.Code,
                    creationMode);
                throw;
            }

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

        public async Task<ScenarioDeletionResult> DeleteScenarioAsync(
            int scenarioId,
            int teacherId)
        {
            var scenario = await _context.Scenarios
                .FirstOrDefaultAsync(s => s.Id == scenarioId);

            if (scenario == null)
            {
                return new ScenarioDeletionResult
                {
                    Status = ScenarioDeletionStatus.NotFound,
                    Message = "Escenario no encontrado."
                };
            }

            if (scenario.CreatedByUserId != teacherId)
            {
                _logger.LogWarning(
                    "Scenario deletion forbidden. ScenarioId={ScenarioId}, TeacherId={TeacherId}, OwnerId={OwnerId}",
                    scenarioId,
                    teacherId,
                    scenario.CreatedByUserId);

                return new ScenarioDeletionResult
                {
                    Status = ScenarioDeletionStatus.Forbidden,
                    Message = "No tienes permiso para eliminar este escenario."
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var courseScenarios = await _context.CourseScenarios
                    .Where(courseScenario => courseScenario.ScenarioId == scenarioId)
                    .ToListAsync();

                var simulationAttempts = await _context.SimulationAttempts
                    .Where(attempt => attempt.ScenarioId == scenarioId)
                    .ToListAsync();
                var attemptIds = simulationAttempts.Select(attempt => attempt.Id).ToList();

                var phaseResponses = await _context.SimulationPhaseResponses
                    .Where(response => attemptIds.Contains(response.SimulationAttemptId))
                    .ToListAsync();
                var phaseResponseIds = phaseResponses.Select(response => response.Id).ToList();

                var simulationAnswers = await _context.SimulationAnswers
                    .Where(answer => phaseResponseIds.Contains(answer.SimulationPhaseResponseId))
                    .ToListAsync();

                var simulationKpiResults = await _context.SimulationKpiResults
                    .Where(result => attemptIds.Contains(result.SimulationAttemptId))
                    .ToListAsync();

                var legacySimulations = await _context.Simulations
                    .Where(simulation => simulation.ScenarioId == scenarioId)
                    .ToListAsync();
                var legacySimulationIds = legacySimulations.Select(simulation => simulation.Id).ToList();

                var scenarioVariables = await _context.ScenarioVariables
                    .Where(variable => variable.ScenarioId == scenarioId)
                    .ToListAsync();
                var scenarioVariableIds = scenarioVariables.Select(variable => variable.Id).ToList();

                var simulationVariableValues = await _context.SimulationVariableValues
                    .Where(value =>
                        legacySimulationIds.Contains(value.SimulationId) ||
                        scenarioVariableIds.Contains(value.ScenarioVariableId))
                    .ToListAsync();

                var scenarioOptions = await _context.ScenarioOptions
                    .Where(option => option.ScenarioId == scenarioId)
                    .ToListAsync();

                var phaseSettings = await _context.ScenarioPhaseSettings
                    .Where(setting => setting.ScenarioId == scenarioId)
                    .ToListAsync();
                var phaseSettingIds = phaseSettings.Select(setting => setting.Id).ToList();

                var phaseCriteria = await _context.PhaseCriteriaSettings
                    .Where(criteria => phaseSettingIds.Contains(criteria.ScenarioPhaseSettingId))
                    .ToListAsync();

                _context.SimulationAnswers.RemoveRange(simulationAnswers);
                _context.SimulationPhaseResponses.RemoveRange(phaseResponses);
                _context.SimulationKpiResults.RemoveRange(simulationKpiResults);
                _context.SimulationAttempts.RemoveRange(simulationAttempts);

                _context.SimulationVariableValues.RemoveRange(simulationVariableValues);
                _context.Simulations.RemoveRange(legacySimulations);

                _context.CourseScenarios.RemoveRange(courseScenarios);
                _context.ScenarioOptions.RemoveRange(scenarioOptions);
                _context.PhaseCriteriaSettings.RemoveRange(phaseCriteria);
                _context.ScenarioPhaseSettings.RemoveRange(phaseSettings);
                _context.ScenarioVariables.RemoveRange(scenarioVariables);
                _context.Scenarios.Remove(scenario);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Scenario deleted. ScenarioId={ScenarioId}, TeacherId={TeacherId}, Options={Options}, CourseAssignments={CourseAssignments}, Attempts={Attempts}, PhaseResponses={PhaseResponses}, Answers={Answers}, KpiResults={KpiResults}, LegacySimulations={LegacySimulations}, VariableValues={VariableValues}, PhaseSettings={PhaseSettings}, PhaseCriteria={PhaseCriteria}, Variables={Variables}",
                    scenarioId,
                    teacherId,
                    scenarioOptions.Count,
                    courseScenarios.Count,
                    simulationAttempts.Count,
                    phaseResponses.Count,
                    simulationAnswers.Count,
                    simulationKpiResults.Count,
                    legacySimulations.Count,
                    simulationVariableValues.Count,
                    phaseSettings.Count,
                    phaseCriteria.Count,
                    scenarioVariables.Count);

                try
                {
                    await Task.WhenAll(courseScenarios.Select(courseScenario =>
                        _realtime.NotifyCourseScenariosChangedAsync(
                            courseScenario.CourseId,
                            scenarioId)));
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Scenario deletion completed but realtime notifications failed. ScenarioId={ScenarioId}",
                        scenarioId);
                }

                return new ScenarioDeletionResult
                {
                    Status = ScenarioDeletionStatus.Deleted,
                    Message = "Escenario eliminado correctamente."
                };
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    exception,
                    "Scenario deletion failed and was rolled back. ScenarioId={ScenarioId}, TeacherId={TeacherId}",
                    scenarioId,
                    teacherId);

                return new ScenarioDeletionResult
                {
                    Status = ScenarioDeletionStatus.Failed,
                    Message = "No se pudo eliminar el escenario. Intenta nuevamente."
                };
            }
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

            await _scenarioPhaseMappingService.RepairScenarioOptionPhaseMappingsAsync(scenario);

            var enabledPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .ToList();
            var phase = _scenarioPhaseMappingService.ResolveEnabledPhase(
                request.PhaseName,
                enabledPhases
            );

            if (phase == null)
                return (false, "La fase no pertenece a la metodología de este escenario.");

            var option = new ScenarioOption
            {
                ScenarioId = scenarioId,
                PhaseName = phase.PhaseName,
                MethodologyPhaseId = phase.MethodologyPhaseId,
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
                .Include(s => s.MethodologyCatalog)
                    .ThenInclude(methodology => methodology!.Phases)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.");

            await _scenarioPhaseMappingService.RepairScenarioOptionPhaseMappingsAsync(scenario);

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

            var coverage = _scenarioPhaseMappingService.GetOptionCoverage(
                scenario.Options,
                enabledPhases);
            var phasesWithoutOptions = coverage.MissingPhases;

            if (phasesWithoutOptions.Any())
                return (false, $"Faltan opciones para estas fases: {string.Join(", ", phasesWithoutOptions)}.");

            var isAiAssisted = scenario.GeneratedByAi && string.Equals(
                scenario.CreationMode,
                "AiAssisted",
                StringComparison.Ordinal);
            if (isAiAssisted)
            {
                var methodology = scenario.MethodologyCatalog;
                if (methodology is null)
                {
                    return (false, "No se pudo validar la metodología del escenario asistido por IA.");
                }

                var simulationCoverage = _aiScenarioContentValidator.ValidateCoverage(
                    methodology,
                    scenario.Options);
                if (!simulationCoverage.IsValid)
                {
                    _logger.LogWarning(
                        "AI scenario publication rejected. ScenarioId={ScenarioId} Methodology={Methodology} ValidationErrors={ValidationErrors}",
                        scenario.Id,
                        scenario.Methodology,
                        string.Join(" | ", simulationCoverage.Errors.Take(12)));
                    return (
                        false,
                        "Las opciones generadas no respetan los límites de selección, presupuesto o tiempo de la metodología. Regenera las opciones antes de publicar.");
                }
            }

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

        public async Task<(bool Success, string Message, int StatusCode)> RegenerateBaseOptionsAsync(int scenarioId, int teacherId)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.Options)
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == scenarioId && s.CreatedByUserId == teacherId);

            if (scenario == null)
                return (false, "Escenario no encontrado.", StatusCodes.Status404NotFound);

            if (scenario.IsPublished)
            {
                return (false, "Solo se pueden regenerar opciones de escenarios en borrador.", StatusCodes.Status409Conflict);
            }

            if (await _context.SimulationAttempts.AnyAsync(attempt => attempt.ScenarioId == scenarioId))
            {
                return (
                    false,
                    "No se pueden regenerar las opciones porque el escenario ya tiene intentos registrados. Duplique el escenario para crear una nueva versión.",
                    StatusCodes.Status409Conflict);
            }

            await _scenarioPhaseMappingService.RepairScenarioOptionPhaseMappingsAsync(scenario);

            var enabledPhases = scenario.PhaseSettings
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.PhaseOrder)
                .ToList();

            if (!enabledPhases.Any())
                return (false, "El escenario no tiene fases activas.", StatusCodes.Status400BadRequest);

            var previousOptionsCount = scenario.Options.Count;
            var regenerationStopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[AI_OPTIONS] Starting regeneration. ScenarioId={ScenarioId}, Methodology={Methodology}, MethodologyName={MethodologyName}, PreviousOptions={PreviousOptions}",
                scenarioId,
                scenario.Methodology,
                GetMethodologyName(scenario.Methodology),
                previousOptionsCount);

            var generationResult = await _aiScenarioContentService
                .GenerateOptionsWithDiagnosticsAsync(scenario);

            if (!generationResult.Success)
            {
                _logger.LogWarning(
                    "[AI_OPTIONS] Generation failed and previous options were preserved. ScenarioId={ScenarioId}, Methodology={Methodology}, ErrorCode={ErrorCode}, TechnicalReason={TechnicalReason}, ExpectedPhases={ExpectedPhases}, ReceivedPhases={ReceivedPhases}, MissingPhases={MissingPhases}, PreviousOptions={PreviousOptions}, DurationMs={DurationMs}, OpenRouterResponded={OpenRouterResponded}, OpenRouterStatusCode={OpenRouterStatusCode}",
                    scenarioId,
                    scenario.Methodology,
                    generationResult.ErrorCode,
                    generationResult.TechnicalReason,
                    string.Join(", ", generationResult.ExpectedPhases),
                    string.Join(", ", generationResult.ReceivedPhases),
                    string.Join(", ", generationResult.MissingPhases),
                    previousOptionsCount,
                    generationResult.Duration.TotalMilliseconds,
                    generationResult.OpenRouterResponded,
                    generationResult.OpenRouterStatusCode);

                return (false, generationResult.UserMessage, StatusCodes.Status502BadGateway);
            }

            var aiOptions = generationResult.Options;

            var allOptionsMapped = aiOptions.All(option =>
                _scenarioPhaseMappingService.TryMapOptionToEnabledPhase(option, enabledPhases));
            var minimumOptionsPerPhase = string.Equals(
                scenario.Methodology,
                "BPM",
                StringComparison.OrdinalIgnoreCase)
                ? 3
                : 1;
            var coverage = _scenarioPhaseMappingService.GetOptionCoverage(
                aiOptions,
                enabledPhases,
                minimumOptionsPerPhase,
                requireCorrectOption: true);

            if (!allOptionsMapped ||
                !_scenarioPhaseMappingService.AreOptionsValidForEnabledPhases(aiOptions, enabledPhases) ||
                coverage.MissingPhases.Any() ||
                coverage.InvalidOptionCount > 0)
            {
                _logger.LogWarning(
                    "[AI_OPTIONS] Rejecting generation. ScenarioId={ScenarioId}, ErrorCode={ErrorCode}, GeneratedOptions={GeneratedOptions}, ValidOptions={ValidOptions}, ExpectedPhases={ExpectedPhases}, ReceivedPhases={ReceivedPhases}, MissingPhases={MissingPhases}, InvalidOptions={InvalidOptions}, PreviousOptions={PreviousOptions}",
                    scenarioId,
                    string.Equals(scenario.Methodology, "BPM", StringComparison.OrdinalIgnoreCase)
                        ? AiOptionsGenerationErrorCodes.BpmMissingPhases
                        : AiOptionsGenerationErrorCodes.AiInvalidSchema,
                    aiOptions.Count,
                    aiOptions.Count - coverage.InvalidOptionCount,
                    string.Join(", ", coverage.ExpectedPhases),
                    string.Join(", ", generationResult.ReceivedPhases),
                    string.Join(", ", coverage.MissingPhases),
                    coverage.InvalidOptionCount,
                    previousOptionsCount);
                return (
                    false,
                    string.Equals(scenario.Methodology, "BPM", StringComparison.OrdinalIgnoreCase)
                        ? "La IA no generó opciones válidas para todas las fases BPM. Intenta regenerar nuevamente."
                        : "La IA respondió con opciones incompletas. Intenta regenerar nuevamente.",
                    StatusCodes.Status502BadGateway
                );
            }

            foreach (var option in aiOptions)
            {
                option.Id = 0;
                option.ScenarioId = scenario.Id;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.ScenarioOptions.RemoveRange(scenario.Options);
                await _context.SaveChangesAsync();

                _context.ScenarioOptions.AddRange(aiOptions);

                scenario.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync();
                _logger.LogError(
                    exception,
                    "[AI_OPTIONS] Database save failed. ScenarioId={ScenarioId}, ErrorCode={ErrorCode}, PreviousOptions={PreviousOptions}, GeneratedOptions={GeneratedOptions}",
                    scenarioId,
                    AiOptionsGenerationErrorCodes.DbSaveError,
                    previousOptionsCount,
                    aiOptions.Count);
                return (false, "No se pudieron guardar las opciones generadas. Las opciones anteriores se conservaron.", StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation(
                "[AI_OPTIONS] Regeneration committed. ScenarioId={ScenarioId}, PreviousOptions={PreviousOptions}, GeneratedOptions={GeneratedOptions}, Correct={CorrectOptions}, Distractors={DistractorOptions}, DurationMs={DurationMs}",
                scenarioId,
                previousOptionsCount,
                aiOptions.Count,
                aiOptions.Count(option => option.IsCorrect),
                aiOptions.Count(option => !option.IsCorrect),
                regenerationStopwatch.Elapsed.TotalMilliseconds);

            return (true, $"Opciones regeneradas con IA correctamente para {GetMethodologyName(scenario.Methodology)}.", StatusCodes.Status200OK);
        }

        private async Task AddScenarioOptionsAsync(int scenarioId, string methodologyCode)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == scenarioId);

            if (scenario == null)
                throw new Exception("Escenario no encontrado para generar opciones.");

            var enabledPhases = scenario.PhaseSettings
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.PhaseOrder)
                .ToList();

            if (!enabledPhases.Any())
                throw new Exception("El escenario no tiene fases activas para generar opciones.");

            var baseOptions = _scenarioOptionTemplateService
                .GenerateBaseOptions(scenario.Id, methodologyCode)
                .ToList();

            var allOptionsMapped = baseOptions.All(option =>
                _scenarioPhaseMappingService.TryMapOptionToEnabledPhase(option, enabledPhases));

            if (!allOptionsMapped ||
                !_scenarioPhaseMappingService.AreOptionsValidForEnabledPhases(baseOptions, enabledPhases))
            {
                throw new Exception("No se pudieron preparar las opciones iniciales del escenario.");
            }

            foreach (var option in baseOptions)
            {
                option.Id = 0;
                option.ScenarioId = scenario.Id;
            }

            _context.ScenarioOptions.AddRange(baseOptions);
        }
        private static void NormalizeAiOptionPhaseNames(
    List<ScenarioOption> options,
    List<string> enabledPhaseNames)
        {
            if (options == null || options.Count == 0 || enabledPhaseNames.Count == 0)
                return;

            var phaseMap = enabledPhaseNames.ToDictionary(
                p => NormalizeText(p),
                p => p
            );

            foreach (var option in options)
            {
                var normalized = NormalizeText(option.PhaseName);

                if (phaseMap.ContainsKey(normalized))
                {
                    option.PhaseName = phaseMap[normalized];
                    continue;
                }

                var repairedPhase = TryMatchPhaseByAlias(option.PhaseName, enabledPhaseNames);

                if (!string.IsNullOrWhiteSpace(repairedPhase))
                {
                    option.PhaseName = repairedPhase;
                    continue;
                }

                repairedPhase = TryMatchPhaseByOptionType(option.OptionType, enabledPhaseNames);

                if (!string.IsNullOrWhiteSpace(repairedPhase))
                {
                    option.PhaseName = repairedPhase;
                }
            }

            var invalidOptions = options
                .Where(o => !enabledPhaseNames.Any(p => NormalizeText(p) == NormalizeText(o.PhaseName)))
                .ToList();

            if (!invalidOptions.Any())
                return;

            var incomingGroups = options
                .GroupBy(o => NormalizeText(o.PhaseName))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToList();

            if (incomingGroups.Count == enabledPhaseNames.Count)
            {
                for (int i = 0; i < incomingGroups.Count; i++)
                {
                    foreach (var option in incomingGroups[i])
                    {
                        option.PhaseName = enabledPhaseNames[i];
                    }
                }
            }
        }

        private static string TryMatchPhaseByAlias(string phaseName, List<string> enabledPhaseNames)
        {
            var value = NormalizeText(phaseName);

            var aliases = new Dictionary<string, string[]>
            {
                ["Empatizar"] = new[] { "empathize", "empatizar", "empathy", "comprender usuario" },
                ["Definir"] = new[] { "define", "definir", "problem", "problema" },
                ["Idear"] = new[] { "ideate", "idear", "idea", "ideacion" },
                ["Prototipar"] = new[] { "prototype", "prototipar", "prototipo" },
                ["Evaluar"] = new[] { "test", "evaluar", "evaluacion", "validar" },

                ["Identificar proceso"] = new[] { "identify process", "identificar proceso", "proceso critico" },
                ["Modelar proceso actual"] = new[] { "model process", "modelar proceso", "current process", "proceso actual" },
                ["Analizar cuellos de botella"] = new[] { "bottleneck", "cuello de botella", "analizar cuellos" },
                ["Rediseñar proceso"] = new[] { "redesign", "rediseno", "redisenar proceso", "mejora proceso" },
                ["Monitorear indicadores"] = new[] { "monitor", "kpi", "indicadores", "monitorear indicadores" },

                ["Diagnóstico inicial"] = new[] { "diagnostic", "diagnostico", "estado actual", "current state" },
                ["Evaluar capacidades"] = new[] { "capability", "capacidades", "evaluar capacidades" },
                ["Priorizar brechas"] = new[] { "gap", "brecha", "brechas", "priorizar brechas" },
                ["Plan de transformación"] = new[] { "transformation plan", "plan transformacion", "iniciativa" },
                ["Seguimiento de madurez"] = new[] { "maturity tracking", "seguimiento", "madurez" },

                ["Hipótesis"] = new[] { "hypothesis", "hipotesis", "supuesto" },
                ["MVP"] = new[] { "mvp", "minimum viable product", "producto minimo viable" },
                ["Medición"] = new[] { "measure", "medicion", "metric", "metrica" },
                ["Aprendizaje"] = new[] { "learn", "learning", "aprendizaje" },
                ["Pivote o perseverancia"] = new[] { "pivot", "persevere", "pivote", "perseverancia" }
            };

            foreach (var enabledPhase in enabledPhaseNames)
            {
                var normalizedEnabled = NormalizeText(enabledPhase);

                if (normalizedEnabled == value)
                    return enabledPhase;

                if (aliases.TryGetValue(enabledPhase, out var phaseAliases))
                {
                    if (phaseAliases.Any(alias => value.Contains(NormalizeText(alias))))
                        return enabledPhase;
                }
            }

            return string.Empty;
        }

        private static string TryMatchPhaseByOptionType(string optionType, List<string> enabledPhaseNames)
        {
            var value = NormalizeText(optionType);

            var optionTypeToPhase = new Dictionary<string, string>
            {
                ["evidence"] = "Empatizar",
                ["painpoint"] = "Empatizar",
                ["problemstatement"] = "Definir",
                ["solution"] = "Idear",
                ["prototypefeature"] = "Prototipar",
                ["userflowstep"] = "Prototipar",
                ["test"] = "Evaluar",

                ["processevidence"] = "Identificar proceso",
                ["processselection"] = "Identificar proceso",
                ["currentprocessstep"] = "Modelar proceso actual",
                ["currentprocess"] = "Modelar proceso actual",
                ["bottleneck"] = "Analizar cuellos de botella",
                ["processimprovement"] = "Rediseñar proceso",
                ["redesign"] = "Rediseñar proceso",
                ["kpi"] = "Monitorear indicadores",
                ["kpiselection"] = "Monitorear indicadores",

                ["currentstate"] = "Diagnóstico inicial",
                ["capability"] = "Evaluar capacidades",
                ["gap"] = "Priorizar brechas",
                ["transformationinitiative"] = "Plan de transformación",
                ["maturitykpi"] = "Seguimiento de madurez",

                ["hypothesis"] = "Hipótesis",
                ["mvpfeature"] = "MVP",
                ["metric"] = "Medición",
                ["learning"] = "Aprendizaje",
                ["pivotdecision"] = "Pivote o perseverancia",
                ["decision"] = "Pivote o perseverancia"
            };

            if (!optionTypeToPhase.TryGetValue(value, out var expectedPhase))
                return string.Empty;

            return enabledPhaseNames
                .FirstOrDefault(p => NormalizeText(p) == NormalizeText(expectedPhase))
                ?? string.Empty;
        }
        private void AddBaseScenarioOptionsByMethodology(int scenarioId, string methodologyCode)
        {
            var normalizedMethodology = NormalizeMethodologyCode(methodologyCode);

            switch (normalizedMethodology)
            {
                case "BPM":
                    AddBaseBpmScenarioOptions(scenarioId);
                    break;

                case "DigitalMaturity":
                    AddBaseDigitalMaturityScenarioOptions(scenarioId);
                    break;

                case "LeanStartup":
                    AddBaseLeanStartupScenarioOptions(scenarioId);
                    break;

                case "DesignThinking":
                default:
                    AddBaseScenarioOptions(scenarioId);
                    break;
            }
        }

        private static string NormalizeMethodologyCode(string? methodologyCode)
        {
            if (string.IsNullOrWhiteSpace(methodologyCode))
                return "DesignThinking";

            var value = methodologyCode.Trim();

            return value switch
            {
                "Design Thinking" => "DesignThinking",
                "DesignThinking" => "DesignThinking",
                "design-thinking" => "DesignThinking",

                "BPM" => "BPM",
                "Business Process Management" => "BPM",
                "BusinessProcessManagement" => "BPM",
                "business-process-management" => "BPM",

                "DigitalMaturity" => "DigitalMaturity",
                "Madurez Digital" => "DigitalMaturity",
                "MadurezDigital" => "DigitalMaturity",
                "digital-maturity" => "DigitalMaturity",

                "LeanStartup" => "LeanStartup",
                "Lean Startup" => "LeanStartup",
                "lean-startup" => "LeanStartup",

                _ => value
            };
        }

        private static Dictionary<int, CreateScenarioPhaseSettingDto> ValidateCreatePhaseSettings(
            List<CreateScenarioPhaseSettingDto> requestedPhaseSettings,
            Methodology methodology)
        {
            var activePhases = methodology.Phases
                .Where(p => p.IsActive)
                .OrderBy(p => p.PhaseOrder)
                .ToList();

            if (!activePhases.Any())
                throw new ArgumentException("La metodología seleccionada no tiene fases activas.");

            if (requestedPhaseSettings == null || requestedPhaseSettings.Count == 0)
                throw new ArgumentException("Debe enviar la configuración de pesos de las fases.");

            var phaseSettingsByPhaseId = new Dictionary<int, CreateScenarioPhaseSettingDto>();

            foreach (var requestedPhase in requestedPhaseSettings)
            {
                if (requestedPhase.PhaseWeight < 0 || requestedPhase.PhaseWeight > 100)
                    throw new ArgumentException("Los pesos de las fases deben estar entre 0 y 100.");

                if (!requestedPhase.IsEnabled)
                    throw new ArgumentException("Todas las fases de la metodología seleccionada deben estar habilitadas al crear el escenario.");

                MethodologyPhase? catalogPhase = null;

                if (requestedPhase.MethodologyPhaseId.HasValue)
                {
                    catalogPhase = activePhases.FirstOrDefault(p =>
                        p.Id == requestedPhase.MethodologyPhaseId.Value);
                }

                if (catalogPhase == null && !string.IsNullOrWhiteSpace(requestedPhase.PhaseName))
                {
                    catalogPhase = activePhases.FirstOrDefault(p =>
                        NormalizeText(p.Name) == NormalizeText(requestedPhase.PhaseName));
                }

                if (catalogPhase == null)
                    throw new ArgumentException("La fase enviada no pertenece a la metodología seleccionada.");

                if (requestedPhase.PhaseOrder > 0 &&
                    requestedPhase.PhaseOrder != catalogPhase.PhaseOrder)
                {
                    throw new ArgumentException("El orden de fase enviado no corresponde a la metodología seleccionada.");
                }

                if (phaseSettingsByPhaseId.ContainsKey(catalogPhase.Id))
                    throw new ArgumentException("No se permiten fases duplicadas en la configuración de pesos.");

                phaseSettingsByPhaseId[catalogPhase.Id] = requestedPhase;
            }

            if (phaseSettingsByPhaseId.Count != activePhases.Count)
                throw new ArgumentException("Debe enviar todas las fases de la metodología seleccionada.");

            var totalWeight = phaseSettingsByPhaseId.Values.Sum(p => p.PhaseWeight);

            if (totalWeight != 100)
            {
                throw new ArgumentException(
                    $"La suma de los pesos de las fases debe ser exactamente 100%. Total recibido: {totalWeight}%."
                );
            }

            return phaseSettingsByPhaseId;
        }

        private void AddPhaseSettingsFromMethodology(
            int scenarioId,
            Methodology methodology,
            Dictionary<int, CreateScenarioPhaseSettingDto> requestedPhaseSettings)
        {
            var phases = methodology.Phases
                .Where(p => p.IsActive)
                .OrderBy(p => p.PhaseOrder)
                .ToList();

            var scenarioPhases = phases.Select(phase => new ScenarioPhaseSetting
            {
                ScenarioId = scenarioId,
                MethodologyPhaseId = phase.Id,
                PhaseName = phase.Name,
                CustomName = phase.Name,
                PhaseOrder = phase.PhaseOrder,
                PhaseWeight = requestedPhaseSettings[phase.Id].PhaseWeight,
                IsEnabled = requestedPhaseSettings[phase.Id].IsEnabled,
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

        private static List<ScenarioPhaseSetting> BuildPhaseSettingsFromMethodology(
            Methodology methodology,
            Dictionary<int, CreateScenarioPhaseSettingDto> requestedPhaseSettings)
        {
            return methodology.Phases
                .Where(phase => phase.IsActive)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => new ScenarioPhaseSetting
                {
                    MethodologyPhaseId = phase.Id,
                    PhaseName = phase.Name,
                    CustomName = phase.Name,
                    PhaseOrder = phase.PhaseOrder,
                    PhaseWeight = requestedPhaseSettings[phase.Id].PhaseWeight,
                    IsEnabled = requestedPhaseSettings[phase.Id].IsEnabled,
                    Criteria = phase.Criteria
                        .Where(criteria => criteria.IsActive)
                        .Select(criteria => new PhaseCriteriaSetting
                        {
                            MethodologyPhaseCriteriaId = criteria.Id,
                            CriterionName = criteria.Name,
                            CriterionWeight = criteria.DefaultWeight,
                            EvaluationType = criteria.EvaluationType
                        })
                        .ToList()
                })
                .ToList();
        }

        private static string NormalizeCreationMode(string? creationMode)
        {
            var value = string.IsNullOrWhiteSpace(creationMode)
                ? "Manual"
                : creationMode.Trim();
            return value switch
            {
                "Manual" => "Manual",
                "AiAssisted" => "AiAssisted",
                "Template" => "Template",
                "Legacy" => "Legacy",
                _ => throw new ArgumentException("El modo de creación debe ser Manual, AiAssisted, Template o Legacy.")
            };
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
                CreationMode = scenario.CreationMode,
                GeneratedByAi = scenario.GeneratedByAi,
                AiProvider = scenario.AiProvider,
                AiModel = scenario.AiModel,
                AiPromptVersion = scenario.AiPromptVersion,
                AiGeneratedAt = scenario.AiGeneratedAt,

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
                .FirstOrDefault(p => NormalizeText(p.PhaseName) == NormalizeText(phaseName))
                ?.PhaseOrder ?? 999;
        }

        private static string GetMethodologyName(string methodologyCode)
        {
            return NormalizeMethodologyCode(methodologyCode) switch
            {
                "BPM" => "Business Process Management",
                "DigitalMaturity" => "Madurez Digital",
                "LeanStartup" => "Lean Startup",
                _ => "Design Thinking"
            };
        }
        private static bool AreOptionsValidForScenario(
    List<ScenarioOption> options,
    List<string> phaseNames)
        {
            if (options == null || options.Count == 0)
                return false;

            var normalizedPhaseNames = phaseNames
                .Select(NormalizeText)
                .ToList();

            if (!options.All(o => normalizedPhaseNames.Contains(NormalizeText(o.PhaseName))))
                return false;

            foreach (var phaseName in phaseNames)
            {
                var normalizedPhase = NormalizeText(phaseName);

                var phaseOptions = options
                    .Where(o => NormalizeText(o.PhaseName) == normalizedPhase)
                    .ToList();

                if (!phaseOptions.Any())
                    return false;

                if (!phaseOptions.Any(o => o.IsCorrect))
                    return false;
            }

            return true;
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Trim()
                .ToLower()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ñ", "n");
        }
        public async Task<GeneratedScenarioDraftDto> GenerateScenarioDraftAsync(
            GenerateScenarioDraftDto request,
            int requestedByUserId,
            CancellationToken cancellationToken = default)
        {
            return await _aiScenarioContentService.GenerateScenarioDraftAsync(
                request,
                requestedByUserId,
                cancellationToken);
        }

        private void AddBaseScenarioOptions(int scenarioId)
        {
            _context.ScenarioOptions.AddRange(new List<ScenarioOption>
            {
                Option(scenarioId, "Empatizar", "Evidence", "Entrevista a usuarios que reportan frustración, dudas y abandono durante el proceso digital.", true, 100, 1, 0, 0, -2, "[\"user-research\",\"friction\",\"ux\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Empatizar", "Evidence", "Análisis de registros muestra que muchos usuarios abandonan antes de completar la acción principal.", true, 95, 2, 0, 0, -2, "[\"analytics\",\"abandonment\",\"conversion\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Empatizar", "Evidence", "Observación del flujo revela pasos confusos, mensajes poco claros y exceso de campos.", true, 90, 3, 0, 0, -1, "[\"user-flow\",\"friction\",\"ux\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Empatizar", "Evidence", "El principal problema es que el color del logotipo no parece moderno.", false, 20, 4, 0, 0, 5, "[\"branding\"]", 4, "Bajo", "Bajo", "Media"),

                Option(scenarioId, "Definir", "ProblemStatement", "Los usuarios abandonan porque no comprenden claramente los costos, tiempos o pasos necesarios para finalizar.", true, 100, 1, 0, 0, -2, "[\"problem-definition\",\"friction\",\"user-need\"]", 3, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Definir", "ProblemStatement", "La baja confianza y la falta de información clara reducen la conversión en el canal digital.", true, 95, 2, 0, 0, -2, "[\"trust\",\"conversion\",\"clarity\"]", 3, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Definir", "ProblemStatement", "La empresa necesita publicar más contenido en redes sociales sin cambiar el flujo digital.", false, 25, 3, 0, 0, 6, "[\"social-media\"]", 3, "Bajo", "Medio", "Media"),

                Option(scenarioId, "Idear", "Solution", "Simplificar el flujo digital reduciendo pasos, campos innecesarios y mensajes ambiguos.", true, 100, 1, 18, 1, -4, "[\"simplification\",\"ux\",\"conversion\"]", 3, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Idear", "Solution", "Agregar información clara sobre costos, tiempos, beneficios y seguridad antes de la acción final.", true, 95, 2, 12, 1, -3, "[\"clarity\",\"trust\",\"conversion\"]", 3, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Idear", "Solution", "Invertir todo el presupuesto en una campaña de influencers sin corregir el proceso digital.", false, 20, 3, 35, 2, 12, "[\"marketing\",\"social-media\"]", 3, "Medio", "Alto", "Baja"),

                Option(scenarioId, "Prototipar", "PrototypeFeature", "Crear un prototipo navegable del flujo simplificado con mensajes de ayuda y confirmaciones claras.", true, 100, 1, 25, 2, -4, "[\"prototype\",\"ux\",\"validation\"]", 3, "Alto", "Alto", "Alta"),
                Option(scenarioId, "Prototipar", "PrototypeFeature", "Probar una versión mínima con usuarios reales antes de desarrollar la solución completa.", true, 95, 2, 15, 1, -3, "[\"mvp\",\"user-test\",\"learning\"]", 3, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Prototipar", "PrototypeFeature", "Construir directamente una plataforma completa sin validar con usuarios.", false, 25, 3, 45, 3, 14, "[\"overbuilding\",\"risk\"]", 3, "Medio", "Alto", "Baja"),

                Option(scenarioId, "Evaluar", "KPI", "Medir tasa de conversión, abandono, tiempo de finalización y satisfacción del usuario.", true, 100, 1, 6, 1, -3, "[\"kpi\",\"conversion\",\"satisfaction\"]", 3, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Evaluar", "KPI", "Comparar el desempeño antes y después del rediseño para validar mejora real.", true, 95, 2, 5, 1, -2, "[\"measurement\",\"baseline\",\"impact\"]", 3, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Evaluar", "KPI", "Evaluar únicamente el número de seguidores en redes sociales.", false, 20, 3, 5, 1, 6, "[\"vanity-metric\",\"social-media\"]", 3, "Bajo", "Bajo", "Baja")
            });
        }

        private void AddBaseBpmScenarioOptions(int scenarioId)
        {
            _context.ScenarioOptions.AddRange(new List<ScenarioOption>
            {
                Option(scenarioId, "Identificar proceso", "ProcessEvidence", "El proceso presenta demoras recurrentes entre la solicitud inicial y la respuesta final al cliente.", true, 100, 1, 0, 0, -2, "[\"process-delay\",\"customer-response\",\"bpm\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Identificar proceso", "ProcessEvidence", "Existen múltiples responsables interviniendo sin claridad sobre quién debe aprobar cada paso.", true, 95, 2, 0, 0, -2, "[\"roles\",\"handoff\",\"approval\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Identificar proceso", "ProcessEvidence", "La falta de trazabilidad impide saber en qué etapa se encuentra cada solicitud o pedido.", true, 90, 3, 0, 0, -2, "[\"traceability\",\"visibility\",\"process\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Identificar proceso", "ProcessEvidence", "La empresa debe priorizar el cambio de colores del logotipo como proceso crítico.", false, 15, 4, 0, 0, 5, "[\"branding\"]", 4, "Bajo", "Bajo", "Media"),

                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep", "Solicitud recibida → revisión manual → aprobación interna → respuesta al cliente → registro final.", true, 100, 1, 5, 1, -2, "[\"as-is\",\"manual-review\",\"approval\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep", "Identificar responsables, tiempos de espera, puntos de retrabajo y transferencias entre áreas.", true, 95, 2, 5, 1, -2, "[\"handoff\",\"waiting-time\",\"rework\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep", "Saltar el modelado actual y comprar directamente un software.", false, 25, 3, 30, 2, 10, "[\"software-first\",\"risk\"]", 4, "Medio", "Alto", "Baja"),

                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck", "La aprobación manual concentra retrasos porque depende de una sola persona o área.", true, 100, 1, 6, 1, -3, "[\"bottleneck\",\"approval\",\"delay\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck", "La falta de trazabilidad impide saber qué etapa genera más demoras y errores.", true, 95, 2, 6, 1, -3, "[\"traceability\",\"metrics\",\"errors\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck", "El problema principal es que el sitio web no tiene suficientes imágenes.", false, 20, 3, 8, 1, 7, "[\"visual-design\"]", 4, "Bajo", "Bajo", "Baja"),

                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement", "Automatizar estados y notificaciones para reducir consultas manuales y mejorar trazabilidad.", true, 100, 1, 22, 2, -5, "[\"automation\",\"status\",\"notifications\"]", 4, "Alto", "Alto", "Alta"),
                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement", "Eliminar pasos duplicados y definir responsables claros por etapa.", true, 95, 2, 12, 1, -4, "[\"simplification\",\"roles\",\"efficiency\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement", "Agregar más aprobaciones manuales para controlar cada solicitud.", false, 20, 3, 10, 2, 12, "[\"bureaucracy\",\"delay\"]", 4, "Bajo", "Medio", "Baja"),

                Option(scenarioId, "Monitorear indicadores", "KPI", "Medir tiempo de ciclo del proceso, tasa de errores, retrabajo y satisfacción del cliente.", true, 100, 1, 8, 1, -3, "[\"cycleTime\",\"errorRate\",\"satisfaction\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Monitorear indicadores", "KPI", "Comparar indicadores antes y después del rediseño para validar mejora operativa.", true, 95, 2, 6, 1, -2, "[\"baseline\",\"improvement\",\"processEfficiency\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Monitorear indicadores", "KPI", "Medir únicamente la cantidad de publicaciones en redes sociales.", false, 15, 3, 4, 1, 6, "[\"vanity-metric\"]", 4, "Bajo", "Bajo", "Baja")
            });
        }

        private void AddBaseDigitalMaturityScenarioOptions(int scenarioId)
        {
            _context.ScenarioOptions.AddRange(new List<ScenarioOption>
            {
                Option(scenarioId, "Diagnóstico inicial", "CurrentState", "La organización usa herramientas digitales aisladas sin integración entre áreas.", true, 100, 1, 0, 0, -2, "[\"digitalMaturity\",\"silos\",\"tools\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Diagnóstico inicial", "CurrentState", "Los datos se registran manualmente y no se usan para tomar decisiones.", true, 95, 2, 0, 0, -2, "[\"dataUsage\",\"manual\",\"decision-making\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Diagnóstico inicial", "CurrentState", "El principal indicador de madurez digital es tener muchos seguidores en redes.", false, 20, 3, 0, 0, 6, "[\"social-media\",\"vanity-metric\"]", 4, "Bajo", "Bajo", "Baja"),

                Option(scenarioId, "Evaluar capacidades", "Capability", "Evaluar procesos, datos, tecnología, cultura digital y experiencia del cliente.", true, 100, 1, 8, 1, -3, "[\"capability\",\"culture\",\"technology\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Evaluar capacidades", "Capability", "Identificar el nivel de automatización, integración de sistemas y calidad de datos.", true, 95, 2, 8, 1, -3, "[\"automation\",\"integration\",\"data-quality\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Evaluar capacidades", "Capability", "Evaluar únicamente si la marca tiene una imagen visual moderna.", false, 25, 3, 6, 1, 6, "[\"branding\"]", 4, "Bajo", "Bajo", "Media"),

                Option(scenarioId, "Priorizar brechas", "Gap", "Priorizar brechas que afectan eficiencia operativa, experiencia del cliente y uso de datos.", true, 100, 1, 8, 1, -3, "[\"gap\",\"processEfficiency\",\"satisfaction\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Priorizar brechas", "Gap", "Elegir brechas críticas según impacto, urgencia, costo y viabilidad de implementación.", true, 95, 2, 7, 1, -2, "[\"impact\",\"priority\",\"viability\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "Priorizar brechas", "Gap", "Priorizar primero el cambio completo del logotipo corporativo.", false, 20, 3, 10, 1, 7, "[\"branding\"]", 4, "Bajo", "Bajo", "Baja"),

                Option(scenarioId, "Plan de transformación", "TransformationInitiative", "Implementar integración de datos y tableros de indicadores para decisiones gerenciales.", true, 100, 1, 25, 2, -4, "[\"dataUsage\",\"dashboard\",\"analytics\"]", 4, "Alto", "Alto", "Alta"),
                Option(scenarioId, "Plan de transformación", "TransformationInitiative", "Automatizar procesos repetitivos de alto impacto antes de invertir en soluciones complejas.", true, 95, 2, 22, 2, -4, "[\"automation\",\"processEfficiency\",\"quick-win\"]", 4, "Alto", "Alto", "Alta"),
                Option(scenarioId, "Plan de transformación", "TransformationInitiative", "Comprar varias plataformas sin priorizar brechas ni capacitar usuarios.", false, 20, 3, 45, 3, 15, "[\"overbuilding\",\"risk\"]", 4, "Medio", "Alto", "Baja"),

                Option(scenarioId, "Seguimiento de madurez", "MaturityKpi", "Medir madurez digital, adopción de herramientas, uso de datos y eficiencia operativa.", true, 100, 1, 8, 1, -3, "[\"digitalMaturity\",\"digitalAdoption\",\"dataUsage\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Seguimiento de madurez", "MaturityKpi", "Definir revisiones periódicas para comparar avances contra la línea base.", true, 95, 2, 6, 1, -2, "[\"baseline\",\"monitoring\",\"continuous-improvement\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Seguimiento de madurez", "MaturityKpi", "Medir solamente número de publicaciones en redes sociales.", false, 20, 3, 4, 1, 6, "[\"vanity-metric\"]", 4, "Bajo", "Bajo", "Baja")
            });
        }

        private void AddBaseLeanStartupScenarioOptions(int scenarioId)
        {
            _context.ScenarioOptions.AddRange(new List<ScenarioOption>
            {
                Option(scenarioId, "Hipótesis", "Hypothesis", "Los usuarios abandonan porque no perciben suficiente valor antes de completar la acción principal.", true, 100, 1, 0, 0, -2, "[\"hypothesis\",\"value-proposition\",\"conversion\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Hipótesis", "Hypothesis", "Si se reduce la fricción inicial, aumentará la conversión de usuarios interesados.", true, 95, 2, 0, 0, -2, "[\"friction\",\"conversionRate\",\"experiment\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Hipótesis", "Hypothesis", "El principal problema es que el logo no tiene colores modernos.", false, 20, 3, 0, 0, 5, "[\"branding\"]", 4, "Bajo", "Bajo", "Media"),

                Option(scenarioId, "MVP", "MvpFeature", "Crear una versión mínima que permita validar la propuesta de valor con usuarios reales.", true, 100, 1, 20, 2, -4, "[\"mvp\",\"validatedLearning\",\"user-test\"]", 4, "Alto", "Medio", "Alta"),
                Option(scenarioId, "MVP", "MvpFeature", "Lanzar una landing page o prototipo funcional para medir interés antes de construir todo.", true, 95, 2, 14, 1, -3, "[\"landing-page\",\"experimentVelocity\",\"conversionRate\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "MVP", "MvpFeature", "Desarrollar el producto completo con todas las funcionalidades desde el inicio.", false, 20, 3, 50, 4, 15, "[\"overbuilding\",\"risk\"]", 4, "Medio", "Alto", "Baja"),

                Option(scenarioId, "Medición", "Metric", "Medir conversión, intención de uso, satisfacción y aprendizaje validado.", true, 100, 1, 7, 1, -3, "[\"conversionRate\",\"satisfaction\",\"validatedLearning\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Medición", "Metric", "Usar métricas accionables que permitan decidir si la hipótesis se valida o no.", true, 95, 2, 6, 1, -2, "[\"actionable-metric\",\"learning\",\"experiment\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Medición", "Metric", "Medir únicamente likes y seguidores sin relacionarlos con adopción real.", false, 20, 3, 4, 1, 6, "[\"vanity-metric\",\"social-media\"]", 4, "Bajo", "Bajo", "Baja"),

                Option(scenarioId, "Aprendizaje", "Learning", "Comparar resultados del experimento con la hipótesis inicial y documentar aprendizajes.", true, 100, 1, 6, 1, -3, "[\"learning\",\"validatedLearning\",\"evidence\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Aprendizaje", "Learning", "Identificar qué comportamiento real del usuario confirma o contradice la propuesta de valor.", true, 95, 2, 6, 1, -2, "[\"user-behavior\",\"evidence\",\"value-proposition\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Aprendizaje", "Learning", "Ignorar los datos negativos y continuar con el plan original.", false, 15, 3, 8, 1, 10, "[\"confirmation-bias\",\"risk\"]", 4, "Bajo", "Bajo", "Baja"),

                Option(scenarioId, "Pivote o perseverancia", "PivotDecision", "Perseverar si la evidencia valida la hipótesis y existen señales reales de adopción.", true, 100, 1, 8, 1, -3, "[\"persevere\",\"evidence\",\"digitalAdoption\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Pivote o perseverancia", "PivotDecision", "Pivotar si los datos muestran bajo interés o un problema distinto al supuesto inicial.", true, 95, 2, 8, 1, -3, "[\"pivot\",\"learning\",\"evidence\"]", 4, "Alto", "Bajo", "Alta"),
                Option(scenarioId, "Pivote o perseverancia", "PivotDecision", "Seguir construyendo sin revisar métricas porque ya se invirtió tiempo.", false, 15, 3, 18, 2, 12, "[\"sunk-cost\",\"risk\"]", 4, "Bajo", "Medio", "Baja")
            });
        }

        private static ScenarioOption Option(
            int scenarioId,
            string phaseName,
            string optionType,
            string text,
            bool isCorrect,
            decimal score,
            int orderIndex,
            decimal cost,
            decimal timeCost,
            decimal riskImpact,
            string tagsJson,
            int maxSelections,
            string expectedImpactLevel,
            string expectedEffortLevel,
            string expectedViabilityLevel)
        {
            return new ScenarioOption
            {
                ScenarioId = scenarioId,
                PhaseName = phaseName,
                OptionType = optionType,
                Text = text,
                IsCorrect = isCorrect,
                Score = score,
                ImpactJson = "{}",
                OrderIndex = orderIndex,
                Cost = cost,
                TimeCost = timeCost,
                RiskImpact = riskImpact,
                TagsJson = tagsJson,
                MaxSelections = maxSelections,
                ExpectedImpactLevel = expectedImpactLevel,
                ExpectedEffortLevel = expectedEffortLevel,
                ExpectedViabilityLevel = expectedViabilityLevel
            };
        }
    }
}
