using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;
using SimuladorApi.Services.Ai;
using SimuladorApi.Services.OpenRouter;

namespace SimuladorApi.Services;

public sealed class AiScenarioContentService
{
    private const int MaximumDraftRepairAttempts = 2;
    private const int MaximumRepairAttemptsPerPhase = 2;
    private readonly IOpenRouterClient _openRouterClient;
    private readonly OpenRouterOptions _options;
    private readonly MethodologyCatalogService _methodologyCatalogService;
    private readonly AiScenarioPromptBuilder _promptBuilder;
    private readonly AiScenarioContentValidator _validator;
    private readonly AiGenerationAuditService _auditService;
    private readonly ILogger<AiScenarioContentService> _logger;

    public AiScenarioContentService(
        IOpenRouterClient openRouterClient,
        IOptions<OpenRouterOptions> options,
        MethodologyCatalogService methodologyCatalogService,
        AiScenarioPromptBuilder promptBuilder,
        AiScenarioContentValidator validator,
        AiGenerationAuditService auditService,
        ILogger<AiScenarioContentService> logger)
    {
        _openRouterClient = openRouterClient;
        _options = options.Value;
        _methodologyCatalogService = methodologyCatalogService;
        _promptBuilder = promptBuilder;
        _validator = validator;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<GeneratedScenarioDraftDto> GenerateScenarioDraftAsync(
        GenerateScenarioDraftDto request,
        int requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        var methodologyCode = request.ResolveMethodologyCode();
        if (string.IsNullOrWhiteSpace(methodologyCode))
        {
            throw new ArgumentException("La metodología es obligatoria.");
        }

        var methodology = await _methodologyCatalogService.GetByCodeAsync(methodologyCode)
            ?? throw new ArgumentException("La metodología seleccionada no existe o está inactiva.");
        var prompt = _promptBuilder.BuildDraftPrompt(request, methodology);
        var auditRecord = await _auditService.StartAsync(
            requestedByUserId,
            "ScenarioDraft",
            _options.ResolveScenarioModel(),
            _options.PromptVersion,
            cancellationToken: cancellationToken,
            methodologyCode: methodology.Code,
            expiresAt: DateTime.UtcNow.AddMinutes(Math.Max(5, _options.DraftValidityMinutes)));
        var result = await _openRouterClient.GenerateJsonAsync<AiScenarioDraftContent>(
            new OpenRouterJsonRequest(
                "scenario-draft",
                _options.ResolveScenarioModel(),
                [
                    new OpenRouterMessage(
                        "system",
                        "Eres un diseñador académico de simulaciones empresariales. Responde en español y cumple estrictamente el esquema JSON."),
                    new OpenRouterMessage("user", prompt)
                ],
                "scenario_draft",
                AiScenarioJsonSchemas.BuildDraft(methodology.Code),
                Temperature: 0.6,
                MaxTokens: 1400,
                CorrelationId: auditRecord.CorrelationId,
                OptimizeForSpeed: _options.OptimizeScenarioRequestsForSpeed,
                ReasoningEffort: _options.ScenarioReasoningEffort),
            cancellationToken);

        if (!result.Success || result.Value is null)
        {
            await _auditService.CompleteAsync(
                auditRecord,
                false,
                result.EffectiveModel,
                result.RetryCount,
                result.PromptHash,
                errorCode: result.ErrorCode,
                errorMessage: "OpenRouter no completó la generación del borrador.",
                responseFormat: result.ResponseFormat,
                cancellationToken: cancellationToken);
            throw new AiContentGenerationException(
                "No se pudo generar el borrador con OpenRouter. Intenta nuevamente.",
                result.ErrorCode ?? "openrouter_failed",
                result.StatusCode);
        }

        var totalRetryCount = result.RetryCount;
        var validation = _validator.ValidateDraft(result.Value, methodology);
        for (var repairAttempt = 1;
             !validation.IsValid && repairAttempt <= MaximumDraftRepairAttempts;
             repairAttempt++)
        {
            _logger.LogWarning(
                "AI draft rejected. Methodology={Methodology} AttemptNumber={AttemptNumber} ValidationErrors={ValidationErrors} PromptHash={PromptHash}",
                methodology.Code,
                repairAttempt,
                string.Join(" | ", validation.Errors.Take(12)),
                result.PromptHash);

            var repairResult = await _openRouterClient.GenerateJsonAsync<AiScenarioDraftContent>(
                new OpenRouterJsonRequest(
                    $"scenario-draft:repair-{repairAttempt}",
                    _options.ResolveScenarioModel(),
                    [
                        new OpenRouterMessage(
                            "system",
                            "Corrige un borrador académico y devuelve exclusivamente un objeto que cumpla el esquema JSON."),
                        new OpenRouterMessage(
                            "user",
                            _promptBuilder.BuildDraftRepairPrompt(request, methodology, validation.Errors))
                    ],
                    "scenario_draft",
                    AiScenarioJsonSchemas.BuildDraft(methodology.Code),
                    Temperature: 0.2,
                    MaxTokens: 1600,
                    CorrelationId: auditRecord.CorrelationId,
                    OptimizeForSpeed: _options.OptimizeScenarioRequestsForSpeed,
                    ReasoningEffort: _options.ScenarioReasoningEffort),
                cancellationToken);

            totalRetryCount += 1 + repairResult.RetryCount;
            result = repairResult;
            if (!result.Success || result.Value is null)
            {
                await _auditService.CompleteAsync(
                    auditRecord,
                    false,
                    result.EffectiveModel,
                    totalRetryCount,
                    result.PromptHash,
                    errorCode: result.ErrorCode,
                    errorMessage: "OpenRouter no completó la reparación del borrador.",
                    responseFormat: result.ResponseFormat,
                    cancellationToken: cancellationToken);
                throw new AiContentGenerationException(
                    "No se pudo generar un borrador válido con OpenRouter. Intenta nuevamente.",
                    result.ErrorCode ?? "openrouter_failed",
                    result.StatusCode);
            }

            validation = _validator.ValidateDraft(result.Value, methodology);
        }

        if (!validation.IsValid)
        {
            _logger.LogWarning(
                "AI draft repair exhausted. Methodology={Methodology} Errors={ErrorCount} ValidationErrors={ValidationErrors} PromptHash={PromptHash}",
                methodology.Code,
                validation.Errors.Count,
                string.Join(" | ", validation.Errors.Take(12)),
                result.PromptHash);
            await _auditService.CompleteAsync(
                auditRecord,
                false,
                result.EffectiveModel,
                totalRetryCount,
                result.PromptHash,
                errorCode: "invalid_draft",
                errorMessage: "El borrador no superó la validación estructural.",
                responseFormat: result.ResponseFormat,
                cancellationToken: cancellationToken);
            throw new AiContentGenerationException(
                "OpenRouter devolvió un borrador incompleto o inválido. Intenta nuevamente.",
                "invalid_draft",
                result.StatusCode);
        }

        var generatedAt = DateTime.UtcNow;
        await _auditService.CompleteDraftSuccessAsync(
            auditRecord,
            result.EffectiveModel,
            totalRetryCount,
            result.PromptHash,
            responseHash: null,
            responseFormat: result.ResponseFormat,
            cancellationToken: cancellationToken);
        return new GeneratedScenarioDraftDto
        {
            Title = result.Value.Title.Trim(),
            Description = result.Value.Description.Trim(),
            CompanyType = result.Value.CompanyType.Trim(),
            Problem = result.Value.Problem.Trim(),
            TargetUser = result.Value.TargetUser.Trim(),
            Constraints = result.Value.Constraints.Trim(),
            Difficulty = result.Value.Difficulty,
            LearningObjective = result.Value.LearningObjective.Trim(),
            MethodologyCode = methodology.Code,
            GenerationId = auditRecord.CorrelationId,
            GeneratedByAi = true,
            Provider = "OpenRouter",
            RequestedModel = result.RequestedModel,
            EffectiveModel = result.EffectiveModel,
            PromptVersion = _options.PromptVersion,
            GeneratedAt = generatedAt
        };
    }

    internal async Task<AiOptionsGenerationResult> GenerateOptionsWithDiagnosticsAsync(
        Scenario scenario,
        Guid? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var effectiveCorrelationId = correlationId ?? Guid.NewGuid();
        var methodologyCode = NormalizeMethodologyCode(scenario.Methodology);
        var methodology = await _methodologyCatalogService.GetByCodeAsync(methodologyCode);
        if (methodology is null)
        {
            return Failure(
                "INVALID_METHODOLOGY",
                "La metodología del escenario no existe o está inactiva.",
                Array.Empty<string>(),
                stopwatch.Elapsed,
                methodologyCode: methodologyCode,
                correlationId: effectiveCorrelationId);
        }

        var enabledSettings = scenario.PhaseSettings
            .Where(setting => setting.IsEnabled)
            .OrderBy(setting => setting.PhaseOrder)
            .ToList();
        var catalogPhases = methodology.Phases
            .Where(phase => phase.IsActive && enabledSettings.Any(setting =>
                setting.MethodologyPhaseId == phase.Id ||
                string.Equals(setting.PhaseName, phase.Name, StringComparison.Ordinal)))
            .OrderBy(phase => phase.PhaseOrder)
            .ToList();
        var expectedPhases = enabledSettings.Select(setting => setting.PhaseName).ToList();

        if (enabledSettings.Count == 0 || catalogPhases.Count != enabledSettings.Count)
        {
            return Failure(
                "INVALID_PHASE_CONFIGURATION",
                "Las fases habilitadas no coinciden con el catálogo de la metodología.",
                expectedPhases,
                stopwatch.Elapsed,
                methodologyCode: methodologyCode,
                correlationId: effectiveCorrelationId);
        }

        var maximumConcurrency = Math.Clamp(
            _options.MaxConcurrentScenarioPhaseRequests,
            1,
            catalogPhases.Count);
        using var semaphore = new SemaphoreSlim(maximumConcurrency);
        var phaseTasks = catalogPhases.Select(async phase =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GeneratePhaseWithRepairAsync(
                    scenario,
                    methodologyCode,
                    phase,
                    effectiveCorrelationId,
                    cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });
        var phaseResults = await Task.WhenAll(phaseTasks);
        var failed = phaseResults.FirstOrDefault(result => !result.Success);
        if (failed is not null)
        {
            return Failure(
                failed.ErrorCode,
                failed.UserMessage,
                expectedPhases,
                stopwatch.Elapsed,
                failed.StatusCode,
                failed.RequestedModel,
                failed.EffectiveModel,
                failed.RetryCount,
                failed.PromptHash,
                methodologyCode,
                failed.PhaseName,
                effectiveCorrelationId,
                failed.ValidationErrors,
                failed.ResponseFormat);
        }

        var options = phaseResults.SelectMany(result => result.Options).ToList();
        var coverage = _validator.ValidateCoverage(methodology, options);
        if (!coverage.IsValid)
        {
            return Failure(
                AiOptionsGenerationErrorCodes.AiInvalidSchema,
                "OpenRouter no generó una configuración completa para todas las fases.",
                expectedPhases,
                stopwatch.Elapsed,
                methodologyCode: methodologyCode,
                correlationId: effectiveCorrelationId,
                validationErrors: coverage.Errors);
        }

        var last = phaseResults.Last();
        _logger.LogInformation(
            "AI options generated. ScenarioId={ScenarioId} Methodology={Methodology} Phases={PhaseCount} Options={OptionCount} DurationMs={DurationMs}",
            scenario.Id,
            methodologyCode,
            catalogPhases.Count,
            options.Count,
            stopwatch.ElapsedMilliseconds);
        return new AiOptionsGenerationResult
        {
            Success = true,
            Options = options,
            ExpectedPhases = expectedPhases,
            ReceivedPhases = phaseResults.Select(result => result.PhaseName).ToList(),
            Duration = stopwatch.Elapsed,
            OpenRouterResponded = true,
            OpenRouterStatusCode = last.StatusCode,
            RequestedModel = last.RequestedModel,
            EffectiveModel = last.EffectiveModel,
            PromptVersion = _options.PromptVersion,
            RetryCount = phaseResults.Sum(result => result.RetryCount),
            PromptHash = last.PromptHash,
            MethodologyCode = methodologyCode,
            CorrelationId = effectiveCorrelationId,
            ResponseFormat = phaseResults.Any(result => result.ResponseFormat == "json_object")
                ? "json_object"
                : "json_schema"
        };
    }

    public async Task<List<ScenarioOption>> GenerateOptionsForScenarioAsync(
        Scenario scenario,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateOptionsWithDiagnosticsAsync(scenario, cancellationToken: cancellationToken);
        if (!result.Success)
        {
            throw new AiContentGenerationException(
                result.UserMessage,
                result.ErrorCode,
                result.OpenRouterStatusCode,
                result.FailedPhaseName,
                result.MethodologyCode,
                result.CorrelationId,
                result.ValidationErrors);
        }
        return result.Options;
    }

    private async Task<PhaseGenerationResult> GeneratePhaseWithRepairAsync(
        Scenario scenario,
        string methodologyCode,
        MethodologyPhase phase,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var allowedTypes = _validator.GetAllowedOptionTypes(methodologyCode, phase.Name);
        IReadOnlyList<string> previousErrors = Array.Empty<string>();
        var totalRetryCount = 0;
        for (var repairAttempt = 0; repairAttempt <= MaximumRepairAttemptsPerPhase; repairAttempt++)
        {
            var prompt = repairAttempt == 0
                ? BuildPhasePrompt(scenario, methodologyCode, phase, allowedTypes)
                : previousErrors.Count > 0
                    ? _promptBuilder.BuildRepairPrompt(scenario, methodologyCode, phase, allowedTypes, previousErrors)
                    : BuildPhasePrompt(scenario, methodologyCode, phase, allowedTypes);
            var requestedModel = _options.ResolveScenarioOptionsModel();
            var result = await _openRouterClient.GenerateJsonAsync<AiPhaseOptionsContent>(
                new OpenRouterJsonRequest(
                    $"scenario-options:{methodologyCode}:{phase.Name}:repair-{repairAttempt}",
                    requestedModel,
                    [
                        new OpenRouterMessage(
                            "system",
                            "Genera decisiones académicas evaluables y cumple estrictamente el esquema JSON. No incluyas texto fuera del objeto."),
                        new OpenRouterMessage("user", prompt)
                    ],
                    "scenario_phase_options",
                    AiScenarioJsonSchemas.BuildPhaseOptions(
                        methodologyCode,
                        phase.Name,
                        allowedTypes,
                        KpiSimulationService.GetAllowedKpiKeys(methodologyCode)),
                    Temperature: repairAttempt == 0 ? 0.45 : 0.2,
                    MaxTokens: 3600,
                    CorrelationId: correlationId,
                    TimeoutSeconds: _options.OptionsGenerationTimeoutSeconds,
                    OptimizeForSpeed: _options.OptimizeScenarioRequestsForSpeed,
                    ReasoningEffort: _options.ScenarioReasoningEffort),
                cancellationToken);
            totalRetryCount += result.RetryCount + (repairAttempt > 0 ? 1 : 0);

            if (!result.Success || result.Value is null)
            {
                if (repairAttempt < MaximumRepairAttemptsPerPhase && CanRetryPhaseGeneration(result))
                {
                    _logger.LogWarning(
                        "Retrying failed AI phase with fallback model. CorrelationId={CorrelationId} MethodologyCode={MethodologyCode} PhaseName={PhaseName} RequestedModel={RequestedModel} HttpStatus={HttpStatus} ErrorCode={ErrorCode} AttemptNumber={AttemptNumber} PromptHash={PromptHash}",
                        correlationId,
                        methodologyCode,
                        phase.Name,
                        result.RequestedModel,
                        result.StatusCode,
                        result.ErrorCode,
                        repairAttempt + 1,
                        result.PromptHash);
                    continue;
                }

                return PhaseGenerationResult.Failed(
                    phase.Name,
                    "No se pudieron generar opciones válidas con OpenRouter. Las opciones anteriores se conservaron.",
                    result.ErrorCode ?? "openrouter_failed",
                    result.StatusCode,
                    result.RequestedModel,
                    result.EffectiveModel,
                    totalRetryCount,
                    result.PromptHash,
                    previousErrors,
                    result.ResponseFormat);
            }

            NormalizeGeneratedPhaseControls(methodologyCode, phase, result.Value);
            var validation = _validator.ValidatePhaseOptions(methodologyCode, phase, result.Value);
            if (validation.IsValid)
            {
                var options = result.Value.Options
                    .OrderBy(option => option.OrderIndex)
                    .Select((option, index) => MapOption(
                        scenario.Id,
                        methodologyCode,
                        phase,
                        option,
                        index))
                    .ToList();
                return PhaseGenerationResult.Completed(
                    phase.Name,
                    options,
                    result.StatusCode,
                    result.RequestedModel,
                    result.EffectiveModel,
                    totalRetryCount,
                    result.PromptHash,
                    result.ResponseFormat);
            }

            previousErrors = validation.Errors;
            _logger.LogWarning(
                "AI phase options rejected. CorrelationId={CorrelationId} MethodologyCode={MethodologyCode} PhaseName={PhaseName} RequestedModel={RequestedModel} EffectiveModel={EffectiveModel} HttpStatus={HttpStatus} AttemptNumber={AttemptNumber} ValidationErrors={ValidationErrors} GenerationStatus={GenerationStatus} ResponseFormat={ResponseFormat} PromptHash={PromptHash}",
                correlationId,
                methodologyCode,
                phase.Name,
                result.RequestedModel,
                result.EffectiveModel,
                result.StatusCode,
                repairAttempt + 1,
                string.Join(" | ", validation.Errors.Take(12)),
                "Rejected",
                result.ResponseFormat,
                result.PromptHash);
        }

        return PhaseGenerationResult.Failed(
            phase.Name,
            "OpenRouter no pudo reparar las opciones de la fase.",
            AiOptionsGenerationErrorCodes.AiInvalidSchema,
            null,
            _options.ResolveScenarioOptionsModel(),
            null,
            totalRetryCount,
            string.Empty,
            previousErrors,
            "json_schema");
    }

    internal static bool CanRetryPhaseGeneration(
        OpenRouterResult<AiPhaseOptionsContent> result)
    {
        if (result.ErrorCode is "invalid_json" or "empty_json_result" or
            "empty_response" or "truncated_response" or "timeout" or
            "network_error" or "openrouter_failed")
        {
            return true;
        }

        return result.StatusCode == StatusCodes.Status408RequestTimeout ||
            result.StatusCode == StatusCodes.Status429TooManyRequests ||
            result.StatusCode >= StatusCodes.Status500InternalServerError;
    }

    private string BuildPhasePrompt(
        Scenario scenario,
        string methodologyCode,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes) =>
        methodologyCode switch
        {
            "DesignThinking" => _promptBuilder.BuildDesignThinkingPhasePrompt(scenario, phase, optionTypes),
            "BPM" => _promptBuilder.BuildBpmPhasePrompt(scenario, phase, optionTypes),
            "DigitalMaturity" => _promptBuilder.BuildDigitalMaturityPhasePrompt(scenario, phase, optionTypes),
            "LeanStartup" => _promptBuilder.BuildLeanStartupPhasePrompt(scenario, phase, optionTypes),
            _ => throw new ArgumentException("Metodología no soportada.")
        };

    internal static ScenarioOption MapOption(
        int scenarioId,
        string methodologyCode,
        MethodologyPhase phase,
        AiScenarioOptionContent generated,
        int index)
    {
        var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phase.Name);
        var resourcePolicy = policy.GetOption(index + 1);
        return new ScenarioOption
        {
            ScenarioId = scenarioId,
            MethodologyPhaseId = phase.Id,
            PhaseName = phase.Name,
            OptionType = generated.OptionType.Trim(),
            Text = generated.Text.Trim(),
            IsCorrect = resourcePolicy.IsCorrect,
            Score = resourcePolicy.IsCorrect ? 100 : 0,
            ImpactJson = JsonSerializer.Serialize(generated.Impact),
            TagsJson = JsonSerializer.Serialize(generated.Tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)),
            Cost = resourcePolicy.Cost,
            TimeCost = resourcePolicy.TimeCost,
            RiskImpact = resourcePolicy.RiskImpact,
            MaxSelections = policy.MaxSelections,
            ExpectedImpactLevel = AiScenarioContentValidator.NormalizeMasculineLevel(generated.ExpectedImpactLevel),
            ExpectedEffortLevel = AiScenarioContentValidator.NormalizeMasculineLevel(generated.ExpectedEffortLevel),
            ExpectedViabilityLevel = AiScenarioContentValidator.NormalizeFeminineLevel(generated.ExpectedViabilityLevel),
            OrderIndex = index + 1
        };
    }

    internal static void NormalizeGeneratedPhaseControls(
        string methodologyCode,
        MethodologyPhase phase,
        AiPhaseOptionsContent content)
    {
        var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phase.Name);
        content.PhaseName = phase.Name;
        content.Options = (content.Options ?? new List<AiScenarioOptionContent>())
            .Select((option, originalIndex) => new { Option = option, OriginalIndex = originalIndex })
            .OrderBy(item => item.Option.OrderIndex is >= 1 and <= 100
                ? item.Option.OrderIndex
                : int.MaxValue)
            .ThenBy(item => item.OriginalIndex)
            .Select(item => item.Option)
            .ToList();

        for (var index = 0; index < content.Options.Count && index < policy.ExpectedOptionCount; index++)
        {
            var option = content.Options[index];
            var resourcePolicy = policy.GetOption(index + 1);
            option.OptionType = option.OptionType.Trim();
            option.Text = option.Text.Trim();
            option.Rationale = option.Rationale.Trim();
            option.Tags = NormalizeGeneratedTags(option.Tags, phase.Name);
            option.ExpectedImpactLevel = AiScenarioContentValidator.NormalizeMasculineLevel(option.ExpectedImpactLevel);
            option.ExpectedEffortLevel = AiScenarioContentValidator.NormalizeMasculineLevel(option.ExpectedEffortLevel);
            option.ExpectedViabilityLevel = AiScenarioContentValidator.NormalizeFeminineLevel(option.ExpectedViabilityLevel);
            option.OrderIndex = index + 1;
            option.IsBestOption = resourcePolicy.IsCorrect;
            option.Cost = resourcePolicy.Cost;
            option.TimeCost = resourcePolicy.TimeCost;
            option.RiskImpact = resourcePolicy.RiskImpact;
            option.MaxSelections = policy.MaxSelections;
        }

        EnsureGeneratedOptionTextsAreDistinct(content.Options);
    }

    private static List<string> NormalizeGeneratedTags(
        IEnumerable<string>? tags,
        string phaseName)
    {
        var normalizedTags = (tags ?? Array.Empty<string>())
            .Select(tag => tag?.Trim() ?? string.Empty)
            .Where(tag => tag.Length > 0)
            .DistinctBy(AiScenarioContentValidator.NormalizeComparableText, StringComparer.Ordinal)
            .Take(6)
            .ToList();

        if (normalizedTags.Count == 0)
        {
            normalizedTags.Add(phaseName.Trim());
        }

        return normalizedTags;
    }

    private static void EnsureGeneratedOptionTextsAreDistinct(
        IReadOnlyList<AiScenarioOptionContent> options)
    {
        const int maximumTextLength = 500;
        const int maximumRationaleExcerptLength = 120;
        var normalizedTexts = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            if (normalizedTexts.Add(AiScenarioContentValidator.NormalizeComparableText(option.Text)))
            {
                continue;
            }

            var rationaleExcerpt = option.Rationale.Length <= maximumRationaleExcerptLength
                ? option.Rationale
                : option.Rationale[..maximumRationaleExcerptLength].TrimEnd();
            var suffix = $" Enfoque {index + 1}: {rationaleExcerpt}";
            var prefixLength = Math.Max(0, maximumTextLength - suffix.Length);
            var prefix = option.Text.Length <= prefixLength
                ? option.Text
                : option.Text[..prefixLength].TrimEnd();
            option.Text = $"{prefix}{suffix}".Trim();
            normalizedTexts.Add(AiScenarioContentValidator.NormalizeComparableText(option.Text));
        }
    }

    private static string NormalizeMethodologyCode(string methodology) =>
        methodology.Trim() switch
        {
            "Madurez Digital" or "MadurezDigital" or "digital-maturity" => "DigitalMaturity",
            "Design Thinking" or "design-thinking" => "DesignThinking",
            "Lean Startup" or "lean-startup" => "LeanStartup",
            var value => value
        };

    private AiOptionsGenerationResult Failure(
        string errorCode,
        string message,
        IReadOnlyCollection<string> expectedPhases,
        TimeSpan duration,
        int? statusCode = null,
        string requestedModel = "",
        string? effectiveModel = null,
        int retryCount = 0,
        string promptHash = "",
        string methodologyCode = "",
        string? failedPhaseName = null,
        Guid correlationId = default,
        IReadOnlyCollection<string>? validationErrors = null,
        string responseFormat = "none") =>
        new()
        {
            Success = false,
            UserMessage = message,
            TechnicalReason = errorCode,
            ErrorCode = errorCode,
            ExpectedPhases = expectedPhases.ToList(),
            MissingPhases = expectedPhases.ToList(),
            Duration = duration,
            OpenRouterResponded = statusCode.HasValue,
            OpenRouterStatusCode = statusCode,
            RequestedModel = string.IsNullOrWhiteSpace(requestedModel)
                ? _options.ResolveScenarioModel()
                : requestedModel,
            EffectiveModel = effectiveModel,
            PromptVersion = _options.PromptVersion,
            RetryCount = retryCount,
            PromptHash = promptHash,
            MethodologyCode = methodologyCode,
            FailedPhaseName = failedPhaseName,
            CorrelationId = correlationId,
            ValidationErrors = validationErrors?.Take(12).ToList() ?? new(),
            ResponseFormat = responseFormat
        };

    private sealed record PhaseGenerationResult(
        bool Success,
        string PhaseName,
        List<ScenarioOption> Options,
        string UserMessage,
        string ErrorCode,
        int? StatusCode,
        string RequestedModel,
        string? EffectiveModel,
        int RetryCount,
        string PromptHash,
        IReadOnlyList<string> ValidationErrors,
        string ResponseFormat)
    {
        public static PhaseGenerationResult Completed(
            string phaseName,
            List<ScenarioOption> options,
            int? statusCode,
            string requestedModel,
            string? effectiveModel,
            int retryCount,
            string promptHash,
            string responseFormat) =>
            new(true, phaseName, options, string.Empty, string.Empty, statusCode, requestedModel, effectiveModel, retryCount, promptHash, Array.Empty<string>(), responseFormat);

        public static PhaseGenerationResult Failed(
            string phaseName,
            string message,
            string errorCode,
            int? statusCode,
            string requestedModel,
            string? effectiveModel,
            int retryCount,
            string promptHash,
            IReadOnlyList<string> validationErrors,
            string responseFormat) =>
            new(false, phaseName, new(), message, errorCode, statusCode, requestedModel, effectiveModel, retryCount, promptHash, validationErrors, responseFormat);
    }
}

public sealed class AiContentGenerationException : Exception
{
    public AiContentGenerationException(
        string message,
        string errorCode,
        int? statusCode = null,
        string? phaseName = null,
        string? methodologyCode = null,
        Guid? correlationId = null,
        IReadOnlyCollection<string>? validationErrors = null)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        PhaseName = phaseName;
        MethodologyCode = methodologyCode;
        CorrelationId = correlationId;
        ValidationErrors = validationErrors?.Take(12).ToArray() ?? Array.Empty<string>();
    }

    public string ErrorCode { get; }
    public int? StatusCode { get; }
    public string? PhaseName { get; }
    public string? MethodologyCode { get; }
    public Guid? CorrelationId { get; }
    public IReadOnlyList<string> ValidationErrors { get; }
}
