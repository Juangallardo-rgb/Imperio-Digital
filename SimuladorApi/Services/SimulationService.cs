using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

        private readonly List<string> _phaseOrder = new()
        {
            "Empatizar",
            "Definir",
            "Idear",
            "Prototipar",
            "Evaluar"
        };

        public SimulationService(
            AppDbContext context,
            ScoringService scoringService,
            AiFeedbackService aiFeedbackService,
            KpiSimulationService kpiSimulationService)
        {
            _context = context;
            _scoringService = scoringService;
            _aiFeedbackService = aiFeedbackService;
            _kpiSimulationService = kpiSimulationService;
        }

        public async Task<(bool Success, string Message, int AttemptId)> StartSimulationAsync(
            int studentId,
            StartSimulationDto request)
        {
            var scenario = await _context.Scenarios
                .FirstOrDefaultAsync(s => s.Id == request.ScenarioId && s.IsPublished);

            if (scenario == null)
                return (false, "Escenario publicado no encontrado.", 0);

            var attempt = new SimulationAttempt
            {
                ScenarioId = request.ScenarioId,
                StudentId = studentId,
                StartedAt = DateTime.UtcNow,
                Status = "InProgress"
            };

            _context.SimulationAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return (true, "Simulación iniciada correctamente.", attempt.Id);
        }

        public async Task<CurrentSimulationDto?> GetCurrentSimulationAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.PhaseResponses)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            var completedPhases = attempt.PhaseResponses
                .Select(r => r.PhaseName)
                .ToList();

            var currentPhase = _phaseOrder
                .FirstOrDefault(p => !completedPhases.Contains(p));

            if (currentPhase == null)
                currentPhase = "Resultado";

            var currentPhaseOrder = currentPhase == "Resultado"
                ? 6
                : _phaseOrder.IndexOf(currentPhase) + 1;

            var options = attempt.Scenario.Options
                .Where(o => o.PhaseName == currentPhase)
                .OrderBy(o => o.OptionType)
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
                .ToList();

            return new CurrentSimulationDto
            {
                AttemptId = attempt.Id,
                ScenarioId = attempt.ScenarioId,
                ScenarioTitle = attempt.Scenario.Title,
                Status = attempt.Status,
                CurrentPhaseName = currentPhase,
                CurrentPhaseOrder = currentPhaseOrder,
                CompletedPhases = completedPhases,
                CurrentPhaseOptions = options
            };
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

            if (!_phaseOrder.Contains(phaseName))
                return (false, "Fase inválida.", null);

            var alreadySubmitted = attempt.PhaseResponses.Any(r => r.PhaseName == phaseName);

            if (alreadySubmitted)
                return (false, "Esta fase ya fue enviada.", null);

            var expectedPhase = _phaseOrder
                .FirstOrDefault(p => !attempt.PhaseResponses.Select(r => r.PhaseName).Contains(p));

            if (expectedPhase != phaseName)
                return (false, $"La fase actual esperada es {expectedPhase}.", null);

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

            var selectionScore = _scoringService.CalculateSelectionScore(
                selectedOptions,
                allOptionsForPhase
            );

            var textEvaluation = await _aiFeedbackService.EvaluateTextAnswerAsync(
                phaseName,
                request.TextAnswer
            );

            var phaseScore = _scoringService.CombinePhaseScore(
                selectionScore,
                textEvaluation.Score,
                phaseSetting
            );

            var feedback = await _aiFeedbackService.GeneratePhaseFeedbackAsync(
                phaseName,
                phaseScore
            );

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
                        Feedback = $"Puntaje de selección: {selectionScore}"
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

            var nextPhase = _phaseOrder
                .FirstOrDefault(p => !attempt.PhaseResponses.Select(r => r.PhaseName).Append(phaseName).Contains(p));

            var result = new SubmitPhaseResultDto
            {
                AttemptId = attempt.Id,
                PhaseName = phaseName,
                Score = phaseScore,
                Feedback = feedback,
                NextPhaseName = nextPhase ?? "Resultado",
                IsLastPhase = nextPhase == null
            };

            return (true, "Fase enviada correctamente.", result);
        }

        public async Task<(bool Success, string Message)> FinishSimulationAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.PhaseSettings)
                .Include(a => a.Scenario)
                    .ThenInclude(s => s!.Options)
                .Include(a => a.PhaseResponses)
                    .ThenInclude(r => r.Answers)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return (false, "Simulación no encontrada.");

            if (attempt.PhaseResponses.Count < 5)
                return (false, "Debe completar las 5 fases antes de finalizar.");

            if (attempt.Status == "Finished")
                return (true, "La simulación ya estaba finalizada.");

            var finalScore = _scoringService.CalculateFinalScore(
                attempt.PhaseResponses,
                attempt.Scenario.PhaseSettings
            );

            var phaseScores = attempt.PhaseResponses
                .Select(r => (r.PhaseName, r.Score))
                .ToList();

            var finalFeedback = await _aiFeedbackService.GenerateFinalFeedbackAsync(
                finalScore,
                phaseScores
            );

            var selectedSolutionIds = attempt.PhaseResponses
                .SelectMany(r => r.Answers)
                .Where(a => a.QuestionType == "Selection")
                .SelectMany(a =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<List<int>>(a.SelectedOptionIdsJson) ?? new List<int>();
                    }
                    catch
                    {
                        return new List<int>();
                    }
                })
                .ToList();

            var selectedSolutions = attempt.Scenario.Options
                .Where(o => selectedSolutionIds.Contains(o.Id) && o.OptionType == "Solution")
                .ToList();

            var kpiResults = _kpiSimulationService.CalculateKpis(attempt.Id, selectedSolutions);

            if (attempt.KpiResults.Any())
            {
                _context.SimulationKpiResults.RemoveRange(attempt.KpiResults);
            }

            _context.SimulationKpiResults.AddRange(kpiResults);

            attempt.FinalScore = finalScore;
            attempt.FinalFeedback = finalFeedback;
            attempt.FinishedAt = DateTime.UtcNow;
            attempt.Status = "Finished";

            await _context.SaveChangesAsync();

            return (true, "Simulación finalizada correctamente.");
        }

        public async Task<SimulationResultsDto?> GetResultsAsync(int attemptId, int studentId)
        {
            var attempt = await _context.SimulationAttempts
                .Include(a => a.Scenario)
                .Include(a => a.PhaseResponses)
                .Include(a => a.KpiResults)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId);

            if (attempt == null || attempt.Scenario == null)
                return null;

            return new SimulationResultsDto
            {
                AttemptId = attempt.Id,
                ScenarioTitle = attempt.Scenario.Title,
                Status = attempt.Status,
                FinalScore = attempt.FinalScore,
                FinalFeedback = attempt.FinalFeedback,
                PhaseScores = attempt.PhaseResponses
                    .OrderBy(p => _phaseOrder.IndexOf(p.PhaseName))
                    .Select(p => new PhaseScoreDto
                    {
                        PhaseName = p.PhaseName,
                        Score = p.Score,
                        Feedback = p.Feedback
                    })
                    .ToList(),
                KpiResults = attempt.KpiResults
                    .Select(k => new KpiResultDto
                    {
                        KpiName = k.KpiName,
                        InitialValue = k.InitialValue,
                        FinalValue = k.FinalValue,
                        Unit = k.Unit
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
    }
}