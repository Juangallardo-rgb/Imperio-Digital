using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SimuladorApi.Data;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class SimulationService
    {
        private readonly AppDbContext _context;
        private readonly ScoringService _scoringService;
        private readonly AiFeedbackService _aiFeedbackService;
        private readonly KpiSimulationService _kpiSimulationService;
        private readonly IRealtimeNotificationService _realtime;
        private readonly ILogger<SimulationService> _logger;

        public SimulationService(
            AppDbContext context,
            ScoringService scoringService,
            AiFeedbackService aiFeedbackService,
            KpiSimulationService kpiSimulationService,
            IRealtimeNotificationService realtime,
            ILogger<SimulationService> logger)
        {
            _context = context;
            _scoringService = scoringService;
            _aiFeedbackService = aiFeedbackService;
            _kpiSimulationService = kpiSimulationService;
            _realtime = realtime;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, int AttemptId)> StartSimulationAsync(
            int studentId,
            StartSimulationDto request)
        {
            var scenario = await _context.Scenarios
                .Include(s => s.PhaseSettings)
                .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.IsPublished);

            if (scenario == null)
                return (false, "Escenario publicado no encontrado.", 0);

            var now = DateTime.UtcNow;

            if (!scenario.AllowLateAttempts)
            {
                if (scenario.AvailableFrom.HasValue && now < scenario.AvailableFrom.Value)
                {
                    return (false, "Este escenario todavía no está disponible.", 0);
                }

                if (scenario.AvailableUntil.HasValue && now > scenario.AvailableUntil.Value)
                {
                    return (false, "Este escenario ya no está disponible.", 0);
                }
            }

            var previousAttemptsCount = await _context.SimulationAttempts
                .CountAsync(a =>
                    a.ScenarioId == request.ScenarioId &&
                    a.StudentId == studentId &&
                    a.CourseId == request.CourseId);

            var maxAttempts = scenario.MaxAttemptsPerStudent <= 0
                ? 1
                : scenario.MaxAttemptsPerStudent;

            if (previousAttemptsCount >= maxAttempts)
            {
                return (false, $"Ya alcanzaste el máximo de intentos permitidos para este escenario ({maxAttempts}).", 0);
            }

            if (request.CourseId.HasValue)
            {
                var isEnrolled = await _context.CourseEnrollments
                    .AnyAsync(e => e.CourseId == request.CourseId.Value && e.StudentId == studentId);

                if (!isEnrolled)
                    return (false, "No estás inscrito en este curso.", 0);

                var scenarioAssigned = await _context.CourseScenarios
                    .AnyAsync(cs => cs.CourseId == request.CourseId.Value && cs.ScenarioId == request.ScenarioId);

                if (!scenarioAssigned)
                    return (false, "Este escenario no está asignado al curso.", 0);
            }

            var initialKpis = _kpiSimulationService.GetDefaultInitialKpis(scenario.Methodology);
            var initialKpisJson = _kpiSimulationService.SerializeKpis(initialKpis);
            var firstPhase = scenario.PhaseSettings
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.PhaseOrder)
            .Select(p => p.PhaseName)
            .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstPhase))
                return (false, "El escenario no tiene fases configuradas.", 0);

            var attempt = new SimulationAttempt
            {
                ScenarioId = request.ScenarioId,
                StudentId = studentId,
                CourseId = request.CourseId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress",
                CurrentPhase = firstPhase,
                InitialBudget = 100,
                RemainingBudget = 100,
                InitialTimeWeeks = 8,
                RemainingTimeWeeks = 8,
                RiskLevel = 20,
                InitialKpisJson = initialKpisJson,
                CurrentKpisJson = initialKpisJson,
                DecisionTraceJson = "[]",
                TriggeredEventsJson = "[]"
            };

            _context.SimulationAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            await NotifyResultsChangedSafelyAsync(
                attempt.CourseId,
                studentId,
                attempt.Id
            );

            return (true, "Simulación iniciada correctamente.", attempt.Id);
        }

        public async Task<CurrentSimulationDto?> GetCurrentSimulationAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                .Include(a => a.PhaseResponses)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            var completedPhases = attempt.PhaseResponses
                .Select(r => r.PhaseName)
                .ToList();

            var currentPhase = attempt.CurrentPhase;

            var phaseOrder = GetScenarioPhaseOrder(attempt.Scenario);

            if (completedPhases.Count >= phaseOrder.Count || attempt.Status == "Finished")
                currentPhase = "Resultado";

            var currentPhaseOrder = currentPhase == "Resultado"
                ? phaseOrder.Count + 1
                : phaseOrder.IndexOf(currentPhase) + 1;

            var options = attempt.Scenario.Options
    .Where(o => NormalizePhaseName(o.PhaseName) == NormalizePhaseName(currentPhase))
    .OrderBy(o => o.OrderIndex)
    .Select(o => new ScenarioOptionDetailDto
    {
        Id = o.Id,
        PhaseName = o.PhaseName,
        OptionType = o.OptionType,
        Text = o.Text,
        Score = 0,
        IsCorrect = false,
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
    .ToList();

            return new CurrentSimulationDto
            {
                AttemptId = attempt.Id,
                ScenarioId = attempt.ScenarioId,
                ScenarioTitle = attempt.Scenario.Title,
                ScenarioDescription = attempt.Scenario.Description,
                ScenarioProblem = attempt.Scenario.Problem,
                ScenarioCompanyType = attempt.Scenario.CompanyType,
                ScenarioTargetUser = attempt.Scenario.TargetUser,
                ScenarioConstraints = attempt.Scenario.Constraints,
                MethodologyCode = attempt.Scenario.Methodology,
                MethodologyName = GetMethodologyName(attempt.Scenario.Methodology),
                PhaseOrder = attempt.Scenario.PhaseSettings
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.PhaseOrder)
                .Select(p => new SimulationPhaseNavigationDto
                {
                    PhaseName = p.PhaseName,
                    PhaseOrder = p.PhaseOrder,
                    PhaseWeight = p.PhaseWeight
                })
                .ToList(),
                Status = attempt.Status,
                CurrentPhaseName = currentPhase,
                CurrentPhaseOrder = currentPhaseOrder,
                CompletedPhases = completedPhases,
                CurrentPhaseOptions = options,
                InitialBudget = attempt.InitialBudget,
                RemainingBudget = attempt.RemainingBudget,
                InitialTimeWeeks = attempt.InitialTimeWeeks,
                RemainingTimeWeeks = attempt.RemainingTimeWeeks,
                RiskLevel = attempt.RiskLevel,
                CurrentKpisJson = attempt.CurrentKpisJson,
                DecisionTraceJson = attempt.DecisionTraceJson,
                TriggeredEventsJson = attempt.TriggeredEventsJson
            };
        }
        private static string NormalizePhaseName(string value)
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

        public async Task<(bool Success, string Message, SubmitPhaseResultDto? Result)> SubmitPhaseAsync(
            int attemptId,
            int studentId,
            string phaseName,
            SubmitPhaseDto request)
        {

            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                        .ThenInclude(p => p.Criteria)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.PhaseResponses)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return (false, "Simulación no encontrada.", null);

            if (attempt.Status != "InProgress")
                return (false, "La simulación ya fue finalizada.", null);

            var phaseOrder = GetScenarioPhaseOrder(attempt.Scenario);

            if (!phaseOrder.Contains(phaseName))
                return (false, "Fase inválida para la metodología de este escenario.", null);

            if (attempt.CurrentPhase != phaseName)
                return (false, $"La fase actual esperada es {attempt.CurrentPhase}.", null);

            var alreadySubmitted = attempt.PhaseResponses.Any(r => r.PhaseName == phaseName);

            if (alreadySubmitted)
                return (false, "Esta fase ya fue enviada.", null);

            var phaseSetting = attempt.Scenario.PhaseSettings
                .FirstOrDefault(p => p.PhaseName == phaseName);

            if (phaseSetting == null)
                return (false, "Configuración de fase no encontrada.", null);

            var allOptionsForPhase = attempt.Scenario.Options
                .Where(o => o.PhaseName == phaseName)
                .ToList();

            var selectedOptions = allOptionsForPhase
                .Where(o => request.SelectedOptionIds.Contains(o.Id))
                .ToList();

            var maxSelectionsValidation = ValidateMaxSelections(phaseName, selectedOptions, allOptionsForPhase);

            if (!maxSelectionsValidation.Success)
                return (false, maxSelectionsValidation.Message, null);

            var totalCost = selectedOptions.Sum(o => o.Cost);
            var totalTime = selectedOptions.Sum(o => o.TimeCost);

            if (totalCost > attempt.RemainingBudget)
                return (false, $"Presupuesto insuficiente. Necesitas {totalCost}, pero tienes {attempt.RemainingBudget}.", null);

            if (totalTime > attempt.RemainingTimeWeeks)
                return (false, $"Tiempo insuficiente. Necesitas {totalTime} semanas, pero tienes {attempt.RemainingTimeWeeks}.", null);

            var selectionScore = _scoringService.CalculateSelectionScore(
                selectedOptions,
                allOptionsForPhase
            );

            var textEvaluation = await _aiFeedbackService.EvaluateTextAnswerAsync(
                phaseName,
                request.TextAnswer,
                attempt.Scenario.Methodology
            );

            var phaseScore = _scoringService.CombinePhaseScore(
                selectionScore,
                textEvaluation.Score,
                phaseSetting
            );

            var coherencePenalty = CalculateBasicCoherencePenalty(attempt, phaseName, selectedOptions);

            phaseScore -= coherencePenalty;

            if (phaseScore < 0)
                phaseScore = 0;

            var feedback = await _aiFeedbackService.GeneratePhaseFeedbackAsync(
                phaseName,
                phaseScore,
                attempt.Scenario.Methodology
            );

            attempt.RemainingBudget -= totalCost;
            attempt.RemainingTimeWeeks -= totalTime;
            attempt.RiskLevel += selectedOptions.Sum(o => o.RiskImpact);
            attempt.RiskLevel = Math.Clamp(attempt.RiskLevel, 0, 100);

            var currentKpis = _kpiSimulationService.DeserializeKpis(
                attempt.CurrentKpisJson,
                attempt.Scenario.Methodology
            );

            var updatedKpis = _kpiSimulationService.ApplyOptionImpacts(
                currentKpis,
                selectedOptions,
                attempt.Scenario.Methodology
            );
            attempt.CurrentKpisJson = _kpiSimulationService.SerializeKpis(updatedKpis);

            var triggeredEventJson = ApplySimpleEventIfNeeded(attempt, phaseName, selectedOptions);

            AppendDecisionTrace(attempt, phaseName, selectedOptions, phaseScore, coherencePenalty, totalCost, totalTime);

            var nextPhase = GetNextPhase(phaseName, phaseOrder);
            attempt.CurrentPhase = nextPhase ?? "Resultado";

            var phaseResponse = new SimulationPhaseResponse
            {
                SimulationAttemptId = attempt.Id,
                PhaseName = phaseName,
                Score = phaseScore,
                Feedback = feedback,
                SubmittedAt = DateTime.UtcNow,
                Answers = new List<SimulationAnswer>
                {
                    new()
                    {
                        QuestionType = "Selection",
                        SelectedOptionIdsJson = JsonSerializer.Serialize(request.SelectedOptionIds),
                        TextAnswer = string.Empty,
                        Score = selectionScore,
                        Feedback = $"Puntaje de selección: {selectionScore}. Penalización de coherencia: {coherencePenalty}."
                    },
                    new()
                    {
                        QuestionType = "Text",
                        SelectedOptionIdsJson = string.Empty,
                        TextAnswer = request.TextAnswer,
                        Score = textEvaluation.Score,
                        Feedback = textEvaluation.Feedback
                    }
                }
            };

            _context.SimulationPhaseResponses.Add(phaseResponse);
            await _context.SaveChangesAsync();

            await NotifyResultsChangedSafelyAsync(
                attempt.CourseId,
                studentId,
                attempt.Id
            );

            var result = new SubmitPhaseResultDto
            {
                AttemptId = attempt.Id,
                PhaseName = phaseName,
                Score = phaseScore,
                Feedback = feedback,
                NextPhaseName = nextPhase ?? "Resultado",
                IsLastPhase = nextPhase == null,
                RemainingBudget = attempt.RemainingBudget,
                RemainingTimeWeeks = attempt.RemainingTimeWeeks,
                RiskLevel = attempt.RiskLevel,
                CurrentKpisJson = attempt.CurrentKpisJson,
                TriggeredEventJson = triggeredEventJson
            };

            return (true, "Fase enviada correctamente.", result);
        }

        public async Task<(bool Success, string Message)> FinishSimulationAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                .Include(a => a.PhaseResponses)
                    .ThenInclude(r => r.Answers)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return (false, "Simulación no encontrada.");

            var phaseOrder = GetScenarioPhaseOrder(attempt.Scenario);

            if (attempt.PhaseResponses.Count < phaseOrder.Count)
                return (false, $"Debe completar las {phaseOrder.Count} fases antes de finalizar.");

            if (attempt.Status == "Finished")
                return (true, "La simulación ya estaba finalizada.");

            var finalScore = _scoringService.CalculateFinalScore(
                attempt.PhaseResponses,
                attempt.Scenario.PhaseSettings
            );

            if (attempt.RiskLevel >= 80)
                finalScore -= 5;

            if (attempt.RemainingBudget <= 5)
                finalScore -= 3;

            if (attempt.RemainingTimeWeeks <= 0)
                finalScore -= 3;

            finalScore = Math.Clamp(finalScore, 0, 100);

            var phaseScores = attempt.PhaseResponses
                .Select(r => (r.PhaseName, r.Score))
                .ToList();

            var finalFeedback = await _aiFeedbackService.GenerateFinalFeedbackAsync(
                finalScore,
                phaseScores,
                attempt.Scenario.Methodology
            );

            var kpiResults = _kpiSimulationService.BuildKpiResults(
                attempt.Id,
                attempt.InitialKpisJson,
                attempt.CurrentKpisJson,
                attempt.Scenario.Methodology
            );

            if (attempt.KpiResults.Any())
            {
                _context.SimulationKpiResults.RemoveRange(attempt.KpiResults);
            }

            _context.SimulationKpiResults.AddRange(kpiResults);

            attempt.FinalScore = finalScore;
            attempt.FinalFeedback = finalFeedback;
            attempt.FinishedAt = DateTime.UtcNow;
            attempt.Status = "Finished";
            attempt.CurrentPhase = "Resultado";

            await _context.SaveChangesAsync();

            await NotifyResultsChangedSafelyAsync(
                attempt.CourseId,
                studentId,
                attempt.Id
            );

            return (true, "Simulación finalizada correctamente.");
        }

        public async Task<SimulationResultsDto?> GetResultsAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.PhaseResponses)
                    .ThenInclude(r => r.Answers)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a =>
                    a.Id == attemptId &&
                    a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            if (attempt.Status != "Finished")
                return null;

            var phaseOrder = GetScenarioPhaseOrder(attempt.Scenario);

            var phaseReviews = attempt.PhaseResponses
                .OrderBy(response => phaseOrder.IndexOf(response.PhaseName))
                .Select(response =>
                {
                    var selectionAnswer = response.Answers
                        .FirstOrDefault(answer =>
                            answer.QuestionType == "Selection");

                    var textAnswer = response.Answers
                        .FirstOrDefault(answer =>
                            answer.QuestionType == "Text");

                    var selectedOptionIds = DeserializeSelectedOptionIds(
                        selectionAnswer?.SelectedOptionIdsJson
                    );

                    var optionsToReview = attempt.Scenario.Options
                        .Where(option =>
                            NormalizePhaseName(option.PhaseName) ==
                            NormalizePhaseName(response.PhaseName))
                        .Where(option =>
                            selectedOptionIds.Contains(option.Id) ||
                            option.IsCorrect)
                        .OrderBy(option => option.OrderIndex)
                        .Select(option => new OptionAnswerReviewDto
                        {
                            OptionId = option.Id,
                            OptionType = option.OptionType,
                            Text = option.Text,
                            Score = option.Score,
                            WasSelected = selectedOptionIds.Contains(option.Id),
                            IsCorrect = option.IsCorrect,
                            ImpactJson = option.ImpactJson,
                            TagsJson = option.TagsJson,
                            Cost = option.Cost,
                            TimeCost = option.TimeCost,
                            RiskImpact = option.RiskImpact,
                            ExpectedImpactLevel = option.ExpectedImpactLevel,
                            ExpectedEffortLevel = option.ExpectedEffortLevel,
                            ExpectedViabilityLevel = option.ExpectedViabilityLevel
                        })
                        .ToList();

                    return new PhaseAnswerReviewDto
                    {
                        PhaseName = response.PhaseName,
                        SelectionScore = selectionAnswer?.Score ?? 0,
                        SelectionFeedback = selectionAnswer?.Feedback ?? string.Empty,
                        TextAnswer = textAnswer?.TextAnswer ?? string.Empty,
                        TextAnswerScore = textAnswer?.Score ?? 0,
                        TextAnswerFeedback = textAnswer?.Feedback ?? string.Empty,
                        Options = optionsToReview
                    };
                })
                .ToList();

            return new SimulationResultsDto
            {
                AttemptId = attempt.Id,
                ScenarioTitle = attempt.Scenario.Title,
                MethodologyCode = attempt.Scenario.Methodology,
                MethodologyName = GetMethodologyName(
                    attempt.Scenario.Methodology
                ),
                Status = attempt.Status,
                FinalScore = attempt.FinalScore,
                FinalFeedback = attempt.FinalFeedback,

                PhaseScores = attempt.PhaseResponses
                    .OrderBy(response =>
                        phaseOrder.IndexOf(response.PhaseName))
                    .Select(response => new PhaseScoreDto
                    {
                        PhaseName = response.PhaseName,
                        Score = response.Score,
                        Feedback = response.Feedback
                    })
                    .ToList(),

                PhaseReviews = phaseReviews,

                KpiResults = attempt.KpiResults
                    .Select(kpi => new KpiResultDto
                    {
                        KpiName = kpi.KpiName,
                        InitialValue = kpi.InitialValue,
                        FinalValue = kpi.FinalValue,
                        Unit = kpi.Unit
                    })
                    .ToList()
            };
        }


        public async Task<List<SimulationHistoryItemDto>> GetMyHistoryAsync(int studentId)
        {
            return await _context.SimulationAttempts
                .Include(a => a.Scenario)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.StartedAt)
                .Select(a => new SimulationHistoryItemDto
                {
                    AttemptId = a.Id,
                    ScenarioId = a.ScenarioId,
                    ScenarioTitle = a.Scenario != null ? a.Scenario.Title : "",
                    StartedAt = a.StartedAt,
                    FinishedAt = a.FinishedAt,
                    FinalScore = a.FinalScore,
                    Status = a.Status
                })
                .ToListAsync();
        }

        private async Task NotifyResultsChangedSafelyAsync(
            int? courseId,
            int studentId,
            int attemptId)
        {
            try
            {
                await _realtime.NotifyResultsChangedAsync(
                    courseId,
                    studentId,
                    attemptId
                );
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "La simulación se guardó, pero no se pudo enviar la notificación en tiempo real para el intento {AttemptId}.",
                    attemptId
                );
            }
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
        private List<string> GetScenarioPhaseOrder(Scenario scenario)
        {
            return scenario.PhaseSettings
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.PhaseOrder)
                .Select(p => p.PhaseName)
                .ToList();
        }

        private (bool Success, string Message) ValidateMaxSelections(
            string phaseName,
            List<ScenarioOption> selectedOptions,
            List<ScenarioOption> allOptionsForPhase)
        {
            var configuredMax = allOptionsForPhase
                .Where(o => o.MaxSelections > 0)
                .Select(o => o.MaxSelections)
                .DefaultIfEmpty(5)
                .Max();

            if (selectedOptions.Count > configuredMax)
                return (false, $"En la fase {phaseName} solo puedes seleccionar máximo {configuredMax} opciones.");

            return (true, "");
        }

        private string? GetNextPhase(string currentPhase, List<string> phaseOrder)
        {
            var index = phaseOrder.IndexOf(currentPhase);

            if (index < 0)
                return null;

            if (index + 1 >= phaseOrder.Count)
                return null;

            return phaseOrder[index + 1];
        }

        private decimal CalculateBasicCoherencePenalty(
            SimulationAttempt attempt,
            string phaseName,
            List<ScenarioOption> selectedOptions)
        {
            if (phaseName == "Empatizar")
                return 0;

            var previousTags = ExtractTagsFromDecisionTrace(attempt.DecisionTraceJson);
            var currentTags = selectedOptions
                .SelectMany(o => DeserializeStringList(o.TagsJson))
                .Distinct()
                .ToList();

            if (!previousTags.Any() || !currentTags.Any())
                return 0;

            var matches = currentTags.Count(t => previousTags.Contains(t));

            if (matches == 0)
                return 10;

            return 0;
        }

        private List<string> ExtractTagsFromDecisionTrace(string decisionTraceJson)
        {
            if (string.IsNullOrWhiteSpace(decisionTraceJson))
                return new List<string>();

            try
            {
                var trace = JsonSerializer.Deserialize<List<DecisionTraceItem>>(decisionTraceJson)
                            ?? new List<DecisionTraceItem>();

                return trace
                    .SelectMany(t => t.Tags)
                    .Distinct()
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private void AppendDecisionTrace(
            SimulationAttempt attempt,
            string phaseName,
            List<ScenarioOption> selectedOptions,
            decimal phaseScore,
            decimal coherencePenalty,
            decimal totalCost,
            decimal totalTime)
        {
            var trace = new List<DecisionTraceItem>();

            if (!string.IsNullOrWhiteSpace(attempt.DecisionTraceJson))
            {
                try
                {
                    trace = JsonSerializer.Deserialize<List<DecisionTraceItem>>(attempt.DecisionTraceJson)
                            ?? new List<DecisionTraceItem>();
                }
                catch
                {
                    trace = new List<DecisionTraceItem>();
                }
            }

            trace.Add(new DecisionTraceItem
            {
                PhaseName = phaseName,
                SelectedOptionIds = selectedOptions.Select(o => o.Id).ToList(),
                SelectedTexts = selectedOptions.Select(o => o.Text).ToList(),
                Tags = selectedOptions
                    .SelectMany(o => DeserializeStringList(o.TagsJson))
                    .Distinct()
                    .ToList(),
                Score = phaseScore,
                CoherencePenalty = coherencePenalty,
                BudgetUsed = totalCost,
                TimeUsed = totalTime
            });

            attempt.DecisionTraceJson = JsonSerializer.Serialize(trace);
        }

        private string ApplySimpleEventIfNeeded(
            SimulationAttempt attempt,
            string phaseName,
            List<ScenarioOption> selectedOptions)
        {
            var events = new List<SimulationTriggeredEvent>();

            if (!string.IsNullOrWhiteSpace(attempt.TriggeredEventsJson))
            {
                try
                {
                    events = JsonSerializer.Deserialize<List<SimulationTriggeredEvent>>(attempt.TriggeredEventsJson)
                             ?? new List<SimulationTriggeredEvent>();
                }
                catch
                {
                    events = new List<SimulationTriggeredEvent>();
                }
            }

            if (events.Any(e => e.TriggerPhase == phaseName))
                return string.Empty;

            SimulationTriggeredEvent? newEvent = null;

            if (phaseName == "Empatizar")
            {
                var selectedTags = selectedOptions
                    .SelectMany(o => DeserializeStringList(o.TagsJson))
                    .ToList();

                newEvent = new SimulationTriggeredEvent
                {
                    TriggerPhase = "Empatizar",
                    Title = "Nuevo hallazgo de usuarios",
                    Description = selectedTags.Contains("mobile")
                        ? "Nuevo hallazgo: la mayoría de usuarios afectados usa dispositivos móviles. Tu selección previa ya consideró este factor, por lo que reduces el riesgo."
                        : "Nuevo hallazgo: el 65% de los usuarios afectados usa dispositivos móviles. Será importante considerar la experiencia móvil en las próximas fases.",
                    BudgetDelta = 0,
                    TimeDelta = 0,
                    RiskDelta = selectedTags.Contains("mobile") ? -5 : 5
                };
            }

            if (phaseName == "Idear")
            {
                newEvent = new SimulationTriggeredEvent
                {
                    TriggerPhase = "Idear",
                    Title = "Cambio de restricción del proyecto",
                    Description = "La gerencia redujo el presupuesto restante en 10 puntos por ajustes internos.",
                    BudgetDelta = -10,
                    TimeDelta = 0,
                    RiskDelta = 5
                };
            }

            if (newEvent == null)
                return string.Empty;

            attempt.RemainingBudget += newEvent.BudgetDelta;
            attempt.RemainingTimeWeeks += newEvent.TimeDelta;
            attempt.RiskLevel += newEvent.RiskDelta;

            attempt.RemainingBudget = Math.Max(0, attempt.RemainingBudget);
            attempt.RemainingTimeWeeks = Math.Max(0, attempt.RemainingTimeWeeks);
            attempt.RiskLevel = Math.Clamp(attempt.RiskLevel, 0, 100);

            events.Add(newEvent);
            attempt.TriggeredEventsJson = JsonSerializer.Serialize(events);

            return JsonSerializer.Serialize(newEvent);
        }

        private static List<int> DeserializeSelectedOptionIds(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(json)
                       ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static List<string> DeserializeStringList(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private class DecisionTraceItem
        {
            public string PhaseName { get; set; } = string.Empty;

            public List<int> SelectedOptionIds { get; set; } = new();

            public List<string> SelectedTexts { get; set; } = new();

            public List<string> Tags { get; set; } = new();

            public decimal Score { get; set; }

            public decimal CoherencePenalty { get; set; }

            public decimal BudgetUsed { get; set; }

            public decimal TimeUsed { get; set; }
        }

        private class SimulationTriggeredEvent
        {
            public string TriggerPhase { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public decimal BudgetDelta { get; set; }

            public decimal TimeDelta { get; set; }

            public decimal RiskDelta { get; set; }
        }
    }
}
