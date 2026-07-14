using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SimuladorApi.Services
{
    public class AiScenarioContentService
    {
        private static readonly string[] BpmCanonicalPhaseNames =
        {
            "Identificar proceso",
            "Modelar proceso actual",
            "Analizar cuellos de botella",
            "Rediseñar proceso",
            "Monitorear indicadores"
        };

        private static readonly IReadOnlyDictionary<string, string[]> BpmOptionTypesByPhase =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Identificar proceso"] = new[] { "ProcessEvidence", "ProcessSelection" },
                ["Modelar proceso actual"] = new[] { "CurrentProcessStep", "CurrentProcess" },
                ["Analizar cuellos de botella"] = new[] { "Bottleneck" },
                ["Rediseñar proceso"] = new[] { "ProcessImprovement", "Redesign" },
                ["Monitorear indicadores"] = new[] { "Kpi", "KpiSelection" }
            };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiScenarioContentService> _logger;

        public AiScenarioContentService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AiScenarioContentService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        internal async Task<AiOptionsGenerationResult> GenerateOptionsWithDiagnosticsAsync(
            Scenario scenario)
        {
            var stopwatch = Stopwatch.StartNew();
            var expectedPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => phase.PhaseName)
                .ToList();
            var isBpm = string.Equals(
                scenario.Methodology,
                "BPM",
                StringComparison.OrdinalIgnoreCase
            );

            _logger.LogInformation(
                "[AI_OPTIONS] Starting generation. ScenarioId={ScenarioId}, Methodology={Methodology}, ExpectedPhases={ExpectedPhases}",
                scenario.Id,
                scenario.Methodology,
                string.Join(", ", expectedPhases));

            AiOptionsGenerationResult result;

            if (isBpm)
            {
                result = await GenerateBpmOptionsByPhaseAsync(scenario, expectedPhases, stopwatch);
            }
            else
            {
                result = await GenerateStandardOptionsAsync(scenario, expectedPhases, stopwatch);
            }

            _logger.LogInformation(
                "[AI_OPTIONS] Generation finished. ScenarioId={ScenarioId}, Methodology={Methodology}, Success={Success}, ErrorCode={ErrorCode}, Options={OptionsCount}, DurationMs={DurationMs}",
                scenario.Id,
                scenario.Methodology,
                result.Success,
                result.ErrorCode,
                result.Options.Count,
                result.Duration.TotalMilliseconds);

            return result;
        }

        private async Task<AiOptionsGenerationResult> GenerateStandardOptionsAsync(
            Scenario scenario,
            List<string> expectedPhases,
            Stopwatch stopwatch)
        {
            try
            {
                var options = await GenerateOptionsForScenarioAsync(scenario);

                return new AiOptionsGenerationResult
                {
                    Success = true,
                    Options = options,
                    ExpectedPhases = expectedPhases,
                    ReceivedPhases = options
                        .Select(option => option.PhaseName)
                        .Distinct(StringComparer.Ordinal)
                        .ToList(),
                    Duration = stopwatch.Elapsed,
                    OpenRouterResponded = true
                };
            }
            catch (Exception exception)
            {
                var errorCode = ClassifyLegacyGenerationException(exception);
                var result = CreateFailure(
                    errorCode,
                    exception.Message,
                    expectedPhases,
                    stopwatch.Elapsed
                );

                _logger.LogWarning(
                    exception,
                    "[AI_OPTIONS] Standard generation failed. ScenarioId={ScenarioId}, Methodology={Methodology}, ErrorCode={ErrorCode}, DurationMs={DurationMs}",
                    scenario.Id,
                    scenario.Methodology,
                    result.ErrorCode,
                    result.Duration.TotalMilliseconds);

                return result;
            }
        }

        private async Task<AiOptionsGenerationResult> GenerateBpmOptionsByPhaseAsync(
            Scenario scenario,
            List<string> expectedPhases,
            Stopwatch stopwatch)
        {
            var enabledPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .ToList();
            var invalidPhaseNames = enabledPhases
                .Select(phase => phase.PhaseName)
                .Where(phaseName => !BpmCanonicalPhaseNames.Contains(phaseName, StringComparer.Ordinal))
                .ToList();

            if (enabledPhases.Count != BpmCanonicalPhaseNames.Length || invalidPhaseNames.Any())
            {
                return CreateFailure(
                    AiOptionsGenerationErrorCodes.BpmInvalidPhaseNames,
                    $"BPM must use the five canonical phases. Invalid phases: {string.Join(", ", invalidPhaseNames)}.",
                    expectedPhases,
                    stopwatch.Elapsed
                );
            }

            var allOptions = new List<ScenarioOption>();
            var receivedPhases = new List<string>();
            var phaseResults = await Task.WhenAll(enabledPhases.Select(phase =>
                GenerateBpmPhaseWithRetryAsync(scenario, phase)));

            for (var index = 0; index < enabledPhases.Count; index++)
            {
                var phase = enabledPhases[index];
                var phaseResult = phaseResults[index];
                receivedPhases.AddRange(phaseResult.ReceivedPhases);

                if (!phaseResult.Success)
                {
                    var pendingPhases = enabledPhases
                        .SkipWhile(enabledPhase => enabledPhase.Id != phase.Id)
                        .Select(enabledPhase => enabledPhase.PhaseName)
                        .ToList();

                    _logger.LogWarning(
                        "[AI_OPTIONS] BPM generation rejected. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}, MissingPhases={MissingPhases}",
                        scenario.Id,
                        phase.PhaseName,
                        phaseResult.ErrorCode,
                        string.Join(", ", pendingPhases));

                    return new AiOptionsGenerationResult
                    {
                        Success = false,
                        UserMessage = phaseResult.UserMessage,
                        TechnicalReason = phaseResult.TechnicalReason,
                        ErrorCode = phaseResult.ErrorCode,
                        ExpectedPhases = expectedPhases,
                        ReceivedPhases = receivedPhases.Distinct(StringComparer.Ordinal).ToList(),
                        MissingPhases = pendingPhases,
                        Duration = stopwatch.Elapsed,
                        OpenRouterResponded = phaseResult.OpenRouterResponded,
                        OpenRouterStatusCode = phaseResult.OpenRouterStatusCode
                    };
                }

                allOptions.AddRange(phaseResult.Options);
            }

            var missingPhases = enabledPhases
                .Where(phase => allOptions.Count(option =>
                    option.MethodologyPhaseId == phase.MethodologyPhaseId) < 3 ||
                    !allOptions.Any(option =>
                        option.MethodologyPhaseId == phase.MethodologyPhaseId && option.IsCorrect))
                .Select(phase => phase.PhaseName)
                .ToList();

            if (missingPhases.Any())
            {
                return CreateFailure(
                    AiOptionsGenerationErrorCodes.BpmMissingPhases,
                    $"BPM coverage is incomplete. Missing or invalid phases: {string.Join(", ", missingPhases)}.",
                    expectedPhases,
                    stopwatch.Elapsed,
                    receivedPhases,
                    missingPhases
                );
            }

            return new AiOptionsGenerationResult
            {
                Success = true,
                Options = allOptions,
                ExpectedPhases = expectedPhases,
                ReceivedPhases = receivedPhases.Distinct(StringComparer.Ordinal).ToList(),
                Duration = stopwatch.Elapsed,
                OpenRouterResponded = true
            };
        }

        private async Task<AiOptionsGenerationResult> GenerateBpmPhaseWithRetryAsync(
            Scenario scenario,
            ScenarioPhaseSetting phase)
        {
            if (!phase.MethodologyPhaseId.HasValue)
            {
                return CreateFailure(
                    AiOptionsGenerationErrorCodes.BpmInvalidPhaseNames,
                    $"The BPM phase '{phase.PhaseName}' does not have a MethodologyPhaseId.",
                    new List<string> { phase.PhaseName },
                    TimeSpan.Zero
                );
            }

            var stopwatch = Stopwatch.StartNew();

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                var call = await RequestBpmPhaseAsync(scenario, phase, attempt);

                if (!call.Success)
                {
                    if (attempt == 1 && IsRetriable(call.ErrorCode))
                    {
                        _logger.LogWarning(
                            "[AI_OPTIONS] Retrying BPM phase. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}",
                            scenario.Id,
                            phase.PhaseName,
                            call.ErrorCode);
                        continue;
                    }

                    return CreateFailure(
                        call.ErrorCode,
                        call.TechnicalReason,
                        new List<string> { phase.PhaseName },
                        stopwatch.Elapsed,
                        openRouterResponded: call.OpenRouterResponded,
                        openRouterStatusCode: call.OpenRouterStatusCode
                    );
                }

                List<AiGeneratedScenarioOptionDto> generatedOptions;

                try
                {
                    generatedOptions = ParseGeneratedOptions(call.AssistantContent);
                }
                catch (JsonException exception)
                {
                    _logger.LogWarning(
                        exception,
                        "[AI_OPTIONS] Invalid AI JSON. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}, ResponseSnippet={ResponseSnippet}",
                        scenario.Id,
                        phase.PhaseName,
                        AiOptionsGenerationErrorCodes.AiInvalidJson,
                        SafeSnippet(call.AssistantContent));

                    if (attempt == 1)
                        continue;

                    return CreateFailure(
                        AiOptionsGenerationErrorCodes.AiInvalidJson,
                        exception.Message,
                        new List<string> { phase.PhaseName },
                        stopwatch.Elapsed,
                        openRouterResponded: true,
                        openRouterStatusCode: call.OpenRouterStatusCode
                    );
                }

                var receivedPhases = generatedOptions
                    .Select(option => option.PhaseName)
                    .Where(phaseName => !string.IsNullOrWhiteSpace(phaseName))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                _logger.LogInformation(
                    "[AI_OPTIONS] Parsed options. ScenarioId={ScenarioId}, Phase={PhaseName}, Attempt={Attempt}, Options={OptionsCount}, ReceivedPhases={ReceivedPhases}",
                    scenario.Id,
                    phase.PhaseName,
                    attempt,
                    generatedOptions.Count,
                    string.Join(", ", receivedPhases));

                var validation = ValidateBpmPhaseOptions(generatedOptions, phase);

                if (!validation.Success)
                {
                    _logger.LogWarning(
                        "[AI_OPTIONS] BPM phase validation failed. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}, Reason={Reason}, Options={OptionsCount}",
                        scenario.Id,
                        phase.PhaseName,
                        validation.ErrorCode,
                        validation.TechnicalReason,
                        generatedOptions.Count);

                    if (attempt == 1 &&
                        validation.ErrorCode == AiOptionsGenerationErrorCodes.BpmMissingPhases)
                    {
                        _logger.LogWarning(
                            "[AI_OPTIONS] Retrying incomplete BPM phase. ScenarioId={ScenarioId}, Phase={PhaseName}",
                            scenario.Id,
                            phase.PhaseName);
                        continue;
                    }

                    return new AiOptionsGenerationResult
                    {
                        Success = false,
                        UserMessage = GetUserMessage(validation.ErrorCode),
                        TechnicalReason = validation.TechnicalReason,
                        ErrorCode = validation.ErrorCode,
                        ExpectedPhases = new List<string> { phase.PhaseName },
                        ReceivedPhases = receivedPhases,
                        MissingPhases = new List<string> { phase.PhaseName },
                        Duration = stopwatch.Elapsed,
                        OpenRouterResponded = true,
                        OpenRouterStatusCode = call.OpenRouterStatusCode
                    };
                }

                var options = generatedOptions.Select((option, index) => new ScenarioOption
                {
                    ScenarioId = scenario.Id,
                    MethodologyPhaseId = phase.MethodologyPhaseId,
                    PhaseName = phase.PhaseName,
                    OptionType = option.OptionType.Trim(),
                    Text = option.Text.Trim(),
                    Score = option.IsCorrect.GetValueOrDefault() ? 100 : 0,
                    IsCorrect = option.IsCorrect.GetValueOrDefault(),
                    ImpactJson = SerializeImpact(option),
                    TagsJson = SerializeTags(option),
                    OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1,
                    Cost = option.Cost ?? 0,
                    TimeCost = option.TimeCost ?? 0,
                    RiskImpact = option.RiskImpact ?? 0,
                    MaxSelections = option.MaxSelections ?? 0,
                    ExpectedImpactLevel = option.ExpectedImpactLevel ?? string.Empty,
                    ExpectedEffortLevel = option.ExpectedEffortLevel ?? string.Empty,
                    ExpectedViabilityLevel = option.ExpectedViabilityLevel ?? string.Empty
                }).ToList();

                _logger.LogInformation(
                    "[AI_OPTIONS] BPM phase generated. ScenarioId={ScenarioId}, Phase={PhaseName}, Options={OptionsCount}, Correct={CorrectCount}, ReceivedPhases={ReceivedPhases}",
                    scenario.Id,
                    phase.PhaseName,
                    options.Count,
                    options.Count(option => option.IsCorrect),
                    string.Join(", ", receivedPhases));

                return new AiOptionsGenerationResult
                {
                    Success = true,
                    Options = options,
                    ExpectedPhases = new List<string> { phase.PhaseName },
                    ReceivedPhases = receivedPhases,
                    Duration = stopwatch.Elapsed,
                    OpenRouterResponded = true,
                    OpenRouterStatusCode = call.OpenRouterStatusCode
                };
            }

            return CreateFailure(
                AiOptionsGenerationErrorCodes.UnknownError,
                "The BPM phase generation loop finished unexpectedly.",
                new List<string> { phase.PhaseName },
                stopwatch.Elapsed
            );
        }

        public async Task<List<ScenarioOption>> GenerateOptionsForScenarioAsync(Scenario scenario)
        {
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"] ?? "openrouter/auto";
            var siteUrl = _configuration["OpenRouter:SiteUrl"] ?? "http://localhost:7160";
            var siteName = _configuration["OpenRouter:SiteName"] ?? "SimuladorApi";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "TU_API_KEY")
            {
                throw new Exception("OpenRouter API Key no configurada.");
            }

            var isDesignThinking = string.Equals(
                scenario.Methodology,
                "DesignThinking",
                StringComparison.OrdinalIgnoreCase
            );
            var isBpm = string.Equals(
                scenario.Methodology,
                "BPM",
                StringComparison.OrdinalIgnoreCase
            );
            var prompt = isDesignThinking
                ? BuildDesignThinkingV2Prompt(scenario)
                : isBpm
                    ? BuildBpmPrompt(scenario)
                    : BuildPrompt(scenario);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = isDesignThinking
                            ? "Eres un experto academico en Design Thinking y diseno de simuladores universitarios. Devuelves exclusivamente JSON valido, completo y evaluable."
                            : isBpm
                                ? "Eres un experto academico en Business Process Management y diseno de simuladores universitarios. Devuelves exclusivamente JSON valido, completo y evaluable."
                                : "Eres un experto académico en Design Thinking, transformación digital y diseño de simuladores educativos. Generas opciones coherentes, evaluables y contextualizadas para casos de estudio."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.4
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", siteUrl);
            request.Headers.Add("X-Title", siteName);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            _httpClient.Timeout = TimeSpan.FromSeconds(180);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error OpenRouter: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var aiText = ExtractAssistantContent(responseContent);

            var cleanJson = CleanJson(aiText);

            var aiOptions = JsonSerializer.Deserialize<List<AiGeneratedScenarioOptionDto>>(
                cleanJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (aiOptions == null || aiOptions.Count == 0)
            {
                throw new Exception("La IA no devolvió opciones válidas.");
            }

            if (isDesignThinking)
            {
                ValidateDesignThinkingV2Options(aiOptions, scenario);
            }

            return aiOptions.Select((option, index) => new ScenarioOption
            {
                ScenarioId = scenario.Id,
                PhaseName = option.PhaseName,
                OptionType = option.OptionType,
                Text = option.Text,
                Score = option.IsCorrect.GetValueOrDefault() ? 100 : 0,
                IsCorrect = option.IsCorrect.GetValueOrDefault(),
                ImpactJson = SerializeImpact(option),
                OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1,
                Cost = option.Cost ?? 0,
                TimeCost = option.TimeCost ?? 0,
                RiskImpact = option.RiskImpact ?? 0,
                TagsJson = SerializeTags(option),
                MaxSelections = option.MaxSelections ?? 0,
                ExpectedImpactLevel = option.ExpectedImpactLevel ?? string.Empty,
                ExpectedEffortLevel = option.ExpectedEffortLevel ?? string.Empty,
                ExpectedViabilityLevel = option.ExpectedViabilityLevel ?? string.Empty
            }).ToList();
        }

        private async Task<OpenRouterPhaseCallResult> RequestBpmPhaseAsync(
            Scenario scenario,
            ScenarioPhaseSetting phase,
            int attempt)
        {
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"] ?? "openrouter/auto";
            var siteUrl = _configuration["OpenRouter:SiteUrl"] ?? "http://localhost:7160";
            var siteName = _configuration["OpenRouter:SiteName"] ?? "SimuladorApi";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "TU_API_KEY")
            {
                return OpenRouterPhaseCallResult.Failure(
                    AiOptionsGenerationErrorCodes.OpenRouterHttpError,
                    "OpenRouter API key is not configured.",
                    false,
                    null
                );
            }

            var timeoutSeconds = GetBpmGenerationTimeoutSeconds();
            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Eres un experto academico en Business Process Management y diseno de simuladores universitarios. Devuelves exclusivamente JSON valido."
                    },
                    new
                    {
                        role = "user",
                        content = BuildBpmPhasePrompt(scenario, phase.PhaseName)
                    }
                },
                temperature = 0.3
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", siteUrl);
            request.Headers.Add("X-Title", siteName);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            using var cancellationSource = new CancellationTokenSource(
                TimeSpan.FromSeconds(timeoutSeconds));
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[AI_OPTIONS] OpenRouter request started. ScenarioId={ScenarioId}, Phase={PhaseName}, Attempt={Attempt}, TimeoutSeconds={TimeoutSeconds}",
                scenario.Id,
                phase.PhaseName,
                attempt,
                timeoutSeconds);

            try
            {
                using var response = await _httpClient.SendAsync(
                    request,
                    cancellationSource.Token);
                var responseContent = await response.Content.ReadAsStringAsync(
                    cancellationSource.Token);

                _logger.LogInformation(
                    "[AI_OPTIONS] OpenRouter completed. ScenarioId={ScenarioId}, Phase={PhaseName}, Attempt={Attempt}, StatusCode={StatusCode}, DurationMs={DurationMs}",
                    scenario.Id,
                    phase.PhaseName,
                    attempt,
                    (int)response.StatusCode,
                    stopwatch.Elapsed.TotalMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[AI_OPTIONS] OpenRouter HTTP error. ScenarioId={ScenarioId}, Phase={PhaseName}, StatusCode={StatusCode}, ResponseSnippet={ResponseSnippet}",
                        scenario.Id,
                        phase.PhaseName,
                        (int)response.StatusCode,
                        SafeSnippet(responseContent));

                    return OpenRouterPhaseCallResult.Failure(
                        AiOptionsGenerationErrorCodes.OpenRouterHttpError,
                        $"OpenRouter returned HTTP {(int)response.StatusCode}: {SafeSnippet(responseContent)}",
                        true,
                        (int)response.StatusCode
                    );
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    return OpenRouterPhaseCallResult.Failure(
                        AiOptionsGenerationErrorCodes.OpenRouterEmptyResponse,
                        "OpenRouter returned an empty HTTP response.",
                        true,
                        (int)response.StatusCode
                    );
                }

                string assistantContent;

                try
                {
                    assistantContent = ExtractAssistantContent(responseContent);
                }
                catch (Exception exception) when (
                    exception is JsonException ||
                    exception is KeyNotFoundException ||
                    exception is InvalidOperationException ||
                    exception is ArgumentOutOfRangeException)
                {
                    _logger.LogWarning(
                        exception,
                        "[AI_OPTIONS] Invalid OpenRouter response envelope. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}, ResponseSnippet={ResponseSnippet}",
                        scenario.Id,
                        phase.PhaseName,
                        AiOptionsGenerationErrorCodes.AiInvalidJson,
                        SafeSnippet(responseContent));

                    return OpenRouterPhaseCallResult.Failure(
                        AiOptionsGenerationErrorCodes.AiInvalidJson,
                        exception.Message,
                        true,
                        (int)response.StatusCode
                    );
                }

                if (string.IsNullOrWhiteSpace(assistantContent))
                {
                    return OpenRouterPhaseCallResult.Failure(
                        AiOptionsGenerationErrorCodes.OpenRouterEmptyResponse,
                        "OpenRouter returned an empty assistant message.",
                        true,
                        (int)response.StatusCode
                    );
                }

                return OpenRouterPhaseCallResult.Successful(
                    assistantContent,
                    (int)response.StatusCode);
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogWarning(
                    exception,
                    "[AI_OPTIONS] OpenRouter timeout after {TimeoutSeconds}s. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}",
                    timeoutSeconds,
                    scenario.Id,
                    phase.PhaseName,
                    AiOptionsGenerationErrorCodes.OpenRouterTimeout);

                return OpenRouterPhaseCallResult.Failure(
                    AiOptionsGenerationErrorCodes.OpenRouterTimeout,
                    $"OpenRouter timed out after {timeoutSeconds} seconds.",
                    false,
                    null
                );
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "[AI_OPTIONS] OpenRouter network error. ScenarioId={ScenarioId}, Phase={PhaseName}, ErrorCode={ErrorCode}",
                    scenario.Id,
                    phase.PhaseName,
                    AiOptionsGenerationErrorCodes.OpenRouterHttpError);

                return OpenRouterPhaseCallResult.Failure(
                    AiOptionsGenerationErrorCodes.OpenRouterHttpError,
                    exception.Message,
                    false,
                    null
                );
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "[AI_OPTIONS] Unexpected OpenRouter error. ScenarioId={ScenarioId}, Phase={PhaseName}",
                    scenario.Id,
                    phase.PhaseName);

                return OpenRouterPhaseCallResult.Failure(
                    AiOptionsGenerationErrorCodes.UnknownError,
                    exception.Message,
                    false,
                    null
                );
            }
        }

        private static List<AiGeneratedScenarioOptionDto> ParseGeneratedOptions(
            string assistantContent)
        {
            var cleanJson = CleanJson(assistantContent);
            var options = JsonSerializer.Deserialize<List<AiGeneratedScenarioOptionDto>>(
                cleanJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (options == null || !options.Any())
                throw new JsonException("The AI response does not contain any options.");

            return options;
        }

        private static (bool Success, string ErrorCode, string TechnicalReason)
            ValidateBpmPhaseOptions(
                List<AiGeneratedScenarioOptionDto> options,
                ScenarioPhaseSetting phase)
        {
            if (options.Count < 3)
            {
                return (
                    false,
                    AiOptionsGenerationErrorCodes.BpmMissingPhases,
                    $"The phase '{phase.PhaseName}' returned only {options.Count} options."
                );
            }

            if (!options.Any(option => option.IsCorrect == true))
            {
                return (
                    false,
                    AiOptionsGenerationErrorCodes.BpmMissingPhases,
                    $"The phase '{phase.PhaseName}' has no correct option."
                );
            }

            if (!BpmOptionTypesByPhase.TryGetValue(phase.PhaseName, out var allowedTypes))
            {
                return (
                    false,
                    AiOptionsGenerationErrorCodes.BpmInvalidPhaseNames,
                    $"The phase '{phase.PhaseName}' is not a canonical BPM phase."
                );
            }

            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];

                if (string.IsNullOrWhiteSpace(option.Text) ||
                    string.IsNullOrWhiteSpace(option.OptionType) ||
                    !option.IsCorrect.HasValue)
                {
                    return (
                        false,
                        AiOptionsGenerationErrorCodes.AiInvalidSchema,
                        $"Option {index + 1} in '{phase.PhaseName}' is missing text, option type, or isCorrect."
                    );
                }

                if (!allowedTypes.Contains(option.OptionType.Trim(), StringComparer.OrdinalIgnoreCase))
                {
                    return (
                        false,
                        AiOptionsGenerationErrorCodes.AiInvalidSchema,
                        $"Option {index + 1} in '{phase.PhaseName}' has invalid type '{option.OptionType}'."
                    );
                }
            }

            return (true, string.Empty, string.Empty);
        }

        private int GetBpmGenerationTimeoutSeconds()
        {
            return int.TryParse(
                _configuration["OpenRouter:OptionsGenerationTimeoutSeconds"],
                out var configuredSeconds)
                ? Math.Clamp(configuredSeconds, 30, 120)
                : 90;
        }

        private static bool IsRetriable(string errorCode)
        {
            return errorCode is
                AiOptionsGenerationErrorCodes.OpenRouterTimeout or
                AiOptionsGenerationErrorCodes.AiInvalidJson;
        }

        private static string ClassifyLegacyGenerationException(Exception exception)
        {
            if (exception is OperationCanceledException)
                return AiOptionsGenerationErrorCodes.OpenRouterTimeout;

            if (exception is JsonException)
                return AiOptionsGenerationErrorCodes.AiInvalidJson;

            if (exception.Message.Contains("contenido vacío", StringComparison.OrdinalIgnoreCase) ||
                exception.Message.Contains("opciones válidas", StringComparison.OrdinalIgnoreCase))
            {
                return AiOptionsGenerationErrorCodes.OpenRouterEmptyResponse;
            }

            if (exception.Message.Contains("OpenRouter", StringComparison.OrdinalIgnoreCase))
                return AiOptionsGenerationErrorCodes.OpenRouterHttpError;

            return AiOptionsGenerationErrorCodes.UnknownError;
        }

        private static AiOptionsGenerationResult CreateFailure(
            string errorCode,
            string technicalReason,
            List<string> expectedPhases,
            TimeSpan duration,
            IEnumerable<string>? receivedPhases = null,
            IEnumerable<string>? missingPhases = null,
            bool openRouterResponded = false,
            int? openRouterStatusCode = null)
        {
            return new AiOptionsGenerationResult
            {
                Success = false,
                UserMessage = GetUserMessage(errorCode),
                TechnicalReason = technicalReason,
                ErrorCode = errorCode,
                ExpectedPhases = expectedPhases,
                ReceivedPhases = receivedPhases?.Distinct(StringComparer.Ordinal).ToList() ?? new(),
                MissingPhases = missingPhases?.Distinct(StringComparer.Ordinal).ToList() ?? expectedPhases,
                Duration = duration,
                OpenRouterResponded = openRouterResponded,
                OpenRouterStatusCode = openRouterStatusCode
            };
        }

        private static string GetUserMessage(string errorCode)
        {
            return errorCode switch
            {
                AiOptionsGenerationErrorCodes.OpenRouterTimeout =>
                    "No se pudieron generar las opciones porque la IA tardó demasiado. Intenta nuevamente.",
                AiOptionsGenerationErrorCodes.AiInvalidJson =>
                    "La IA respondió con un formato inválido. Intenta regenerar nuevamente.",
                AiOptionsGenerationErrorCodes.OpenRouterEmptyResponse =>
                    "La IA no devolvió contenido utilizable. Intenta regenerar nuevamente.",
                AiOptionsGenerationErrorCodes.BpmMissingPhases =>
                    "La IA no generó opciones válidas para todas las fases BPM. Intenta regenerar nuevamente.",
                AiOptionsGenerationErrorCodes.BpmInvalidPhaseNames =>
                    "La configuración de fases BPM no es válida. Revisa las fases del escenario.",
                AiOptionsGenerationErrorCodes.AiInvalidSchema =>
                    "La IA respondió con opciones incompletas. Intenta regenerar nuevamente.",
                _ => "No se pudieron generar las opciones con IA. Revisa los logs del backend para más detalle."
            };
        }

        private static string SafeSnippet(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            return normalized.Length <= 500
                ? normalized
                : normalized[..500];
        }

        private static string BuildBpmPhasePrompt(Scenario scenario, string phaseName)
        {
            var optionTypes = string.Join(
                " o ",
                BpmOptionTypesByPhase[phaseName]);

            return $@"
Genera exactamente 3 opciones para una sola fase de una simulacion universitaria BPM.

CASO:
Titulo: {scenario.Title}
Empresa: {scenario.CompanyType}
Problema: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

FASE SOLICITADA: {phaseName}
TIPOS PERMITIDOS: {optionTypes}

Devuelve SOLO un arreglo JSON valido, sin markdown ni texto adicional.
Cada objeto debe tener exactamente:
phaseName, optionType, text, isCorrect, impactJson, tagsJson, orderIndex, cost, timeCost, riskImpact, maxSelections, expectedImpactLevel, expectedEffortLevel, expectedViabilityLevel.

REGLAS:
- Las 3 opciones deben pertenecer a la fase solicitada y al caso.
- Incluye al menos una opcion correcta y un distractor.
- optionType debe ser uno de los tipos permitidos.
- phaseName puede repetirse como '{phaseName}', pero el servidor lo normalizara.
- impactJson y tagsJson pueden ser string JSON, objeto, arreglo o null.
- text no puede estar vacio.
- isCorrect es obligatorio.
";
        }

        private sealed class OpenRouterPhaseCallResult
        {
            public bool Success { get; init; }

            public string AssistantContent { get; init; } = string.Empty;

            public string ErrorCode { get; init; } = string.Empty;

            public string TechnicalReason { get; init; } = string.Empty;

            public bool OpenRouterResponded { get; init; }

            public int? OpenRouterStatusCode { get; init; }

            public static OpenRouterPhaseCallResult Successful(
                string assistantContent,
                int statusCode)
            {
                return new OpenRouterPhaseCallResult
                {
                    Success = true,
                    AssistantContent = assistantContent,
                    OpenRouterResponded = true,
                    OpenRouterStatusCode = statusCode
                };
            }

            public static OpenRouterPhaseCallResult Failure(
                string errorCode,
                string technicalReason,
                bool openRouterResponded,
                int? statusCode)
            {
                return new OpenRouterPhaseCallResult
                {
                    ErrorCode = errorCode,
                    TechnicalReason = technicalReason,
                    OpenRouterResponded = openRouterResponded,
                    OpenRouterStatusCode = statusCode
                };
            }
        }

        private static string BuildDesignThinkingV2Prompt(Scenario scenario)
        {
            var configuredPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => phase.PhaseName)
                .ToList();

            if (!configuredPhases.Any())
            {
                configuredPhases = new List<string>
                {
                    "Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"
                };
            }

            return $@"
Genera contenido estructurado para una simulacion universitaria de Design Thinking.

CASO:
Titulo: {scenario.Title}
Descripcion: {scenario.Description}
Empresa: {scenario.CompanyType}
Problema: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

Devuelve SOLO un arreglo JSON valido. No uses markdown ni texto adicional.
Cada objeto debe tener exactamente estas propiedades:
phaseName, optionType, text, isCorrect, impact, tags, orderIndex, cost, timeCost, riskImpact, maxSelections, expectedImpactLevel, expectedEffortLevel, expectedViabilityLevel.

REGLAS GENERALES:
- Genera contenido exclusivamente para estas fases activas: {string.Join(", ", configuredPhases)}.
- Aplica las instrucciones detalladas de una fase solo si esa fase esta activa.
- Incluye opciones correctas y distractores evaluables, relacionadas con el caso.
- tags es un arreglo JSON de palabras clave minusculas y utiles para continuidad.
- impact es un objeto JSON con impactos KPI; usa {{}} cuando no aplique.
- cost, timeCost y riskImpact son numeros reales del escenario.
- expectedImpactLevel, expectedEffortLevel y expectedViabilityLevel usan Alto, Medio o Bajo cuando apliquen; en otra fase usa cadena vacia.
- Cada fase debe tener al menos 3 opciones y una correcta.

EMPATIZAR:
- optionType Evidence o PainPoint.
- Genera entrevistas, observaciones, metricas, quejas, comportamientos, dolores y necesidades mediante texto y tags.
- maxSelections entre 2 y 5.

DEFINIR:
- optionType ProblemStatement.
- Incluye formulaciones centradas en usuario, necesidad e insight, junto con sintomas y soluciones anticipadas como distractores.
- maxSelections entre 1 y 2.

IDEAR:
- optionType Solution.
- Incluye impacto, esfuerzo, viabilidad, costo, tiempo, riesgo y tags para cada idea.
- impact debe usar solo claves KPI del caso de Design Thinking: cartAbandonment, conversionRate, satisfaction, purchaseTime, digitalAdoption.
- maxSelections entre 1 y 3.

PROTOTIPAR:
- optionType PrototypeFeature o UserFlowStep.
- Incluye funcionalidades minimas, dependencias en tags, costo, tiempo, riesgo y prioridad mediante expectedImpactLevel y expectedViabilityLevel.
- maxSelections entre 2 y 4.

EVALUAR:
- optionType KPI o Test.
- Incluye metricas de prueba, feedback, problemas, hallazgos y acciones de siguiente iteracion en el texto y tags.
- maxSelections entre 1 y 3.

EJEMPLO DE FORMA:
[
  {{
    ""phaseName"": ""Idear"",
    ""optionType"": ""Solution"",
    ""text"": ""..."",
    ""isCorrect"": true,
    ""impact"": {{""cartAbandonment"": -5, ""conversionRate"": 0.8, ""satisfaction"": 6, ""purchaseTime"": -0.4, ""digitalAdoption"": 3}},
    ""tags"": [""trust"", ""clarity""],
    ""orderIndex"": 1,
    ""cost"": 20,
    ""timeCost"": 2,
    ""riskImpact"": 3,
    ""maxSelections"": 3,
    ""expectedImpactLevel"": ""Alto"",
    ""expectedEffortLevel"": ""Bajo"",
    ""expectedViabilityLevel"": ""Alta""
  }}
]";
        }

        private static string BuildBpmPrompt(Scenario scenario)
        {
            return $@"
Genera opciones estructuradas para una simulacion universitaria de Business Process Management (BPM).

CASO:
Titulo: {scenario.Title}
Descripcion: {scenario.Description}
Empresa: {scenario.CompanyType}
Problema: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

Devuelve SOLO un arreglo JSON valido. No uses markdown ni texto adicional.
Cada objeto debe tener exactamente estas propiedades:
phaseName, optionType, text, isCorrect, impactJson, tagsJson, orderIndex, cost, timeCost, riskImpact, maxSelections, expectedImpactLevel, expectedEffortLevel, expectedViabilityLevel.

Usa exactamente estas cinco fases BPM y no inventes nombres alternativos:
1. Identificar proceso
2. Modelar proceso actual
3. Analizar cuellos de botella
4. Rediseñar proceso
5. Monitorear indicadores

REGLAS:
- Genera al menos 3 opciones por cada fase, con al menos una correcta y un distractor evaluable.
- Todas las opciones deben describir decisiones concretas y coherentes con el caso.
- impactJson debe ser una cadena JSON valida. Usa ""{{}}"" cuando no aplique.
- tagsJson debe ser una cadena JSON valida con palabras clave utiles. Usa ""[]"" cuando no aplique.
- cost, timeCost y riskImpact deben ser numeros realistas.
- No devuelvas fases de Design Thinking ni nombres en ingles para phaseName.

TIPOS PERMITIDOS POR FASE:
- Identificar proceso: ProcessEvidence o ProcessSelection.
- Modelar proceso actual: CurrentProcessStep o CurrentProcess.
- Analizar cuellos de botella: Bottleneck.
- Rediseñar proceso: ProcessImprovement o Redesign.
- Monitorear indicadores: Kpi o KpiSelection.

EJEMPLO DE FORMA:
[
  {{
    ""phaseName"": ""Analizar cuellos de botella"",
    ""optionType"": ""Bottleneck"",
    ""text"": ""..."",
    ""isCorrect"": true,
    ""impactJson"": ""{{}}"",
    ""tagsJson"": ""[]"",
    ""orderIndex"": 1,
    ""cost"": 5,
    ""timeCost"": 1,
    ""riskImpact"": 2,
    ""maxSelections"": 2,
    ""expectedImpactLevel"": ""Alto"",
    ""expectedEffortLevel"": ""Medio"",
    ""expectedViabilityLevel"": ""Alta""
  }}
]";
        }

        private static void ValidateDesignThinkingV2Options(
            List<AiGeneratedScenarioOptionDto> options,
            Scenario scenario)
        {
            var expectedPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .Select(phase => phase.PhaseName)
                .ToList();

            if (!expectedPhases.Any())
            {
                expectedPhases = new List<string>
                {
                    "Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"
                };
            }

            if (options.Any(option =>
                string.IsNullOrWhiteSpace(option.PhaseName) ||
                string.IsNullOrWhiteSpace(option.OptionType) ||
                string.IsNullOrWhiteSpace(option.Text) ||
                option.Tags == null || !option.Tags.Any()))
            {
                throw new Exception("La IA devolvio opciones incompletas para la experiencia V2.");
            }

            foreach (var phaseName in expectedPhases)
            {
                var phaseOptions = options
                    .Where(option => NormalizePhase(option.PhaseName) == NormalizePhase(phaseName))
                    .ToList();

                if (phaseOptions.Count < 3 || !phaseOptions.Any(option => option.IsCorrect == true))
                {
                    throw new Exception($"La IA no genero contenido suficiente para la fase {phaseName}.");
                }

                ValidatePhaseMetadata(phaseName, phaseOptions);
            }
        }

        private static void ValidatePhaseMetadata(
            string phaseName,
            List<AiGeneratedScenarioOptionDto> options)
        {
            var phase = NormalizePhase(phaseName);
            var types = options
                .Select(option => option.OptionType.Trim().ToLowerInvariant())
                .ToList();

            if (phase == "empatizar" && !types.Any(type => type is "evidence" or "painpoint"))
                throw new Exception("Empatizar requiere evidencias o dolores de usuario.");

            if (phase == "definir" && !types.Contains("problemstatement"))
                throw new Exception("Definir requiere formulaciones de problema.");

            if (phase == "idear")
            {
                if (!types.Contains("solution") || options.Any(option =>
                    string.IsNullOrWhiteSpace(option.ExpectedImpactLevel) ||
                    string.IsNullOrWhiteSpace(option.ExpectedEffortLevel) ||
                    string.IsNullOrWhiteSpace(option.ExpectedViabilityLevel) ||
                    option.Cost == null || option.TimeCost == null || option.RiskImpact == null ||
                    option.Impact == null || option.Impact.Value.ValueKind != JsonValueKind.Object))
                {
                    throw new Exception("Idear requiere soluciones con metadata de priorizacion completa.");
                }
            }

            if (phase == "prototipar" && !types.Any(type => type is "prototypefeature" or "userflowstep"))
                throw new Exception("Prototipar requiere modulos o pasos de flujo.");

            if (phase == "evaluar" && !types.Any(type => type is "kpi" or "test"))
                throw new Exception("Evaluar requiere KPIs o pruebas.");
        }

        private static string SerializeImpact(AiGeneratedScenarioOptionDto option)
        {
            var impact = NormalizeJsonField(option.Impact);

            return string.IsNullOrWhiteSpace(impact)
                ? NormalizeJsonField(option.ImpactJson)
                : impact;
        }

        private static string SerializeTags(AiGeneratedScenarioOptionDto option)
        {
            if (option.Tags?.Any() == true)
                return JsonSerializer.Serialize(option.Tags);

            return NormalizeJsonField(option.TagsJson);
        }

        private static string NormalizeJsonField(JsonElement? element)
        {
            if (!element.HasValue ||
                element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            return element.Value.ValueKind == JsonValueKind.String
                ? element.Value.GetString() ?? string.Empty
                : element.Value.GetRawText();
        }

        private static string NormalizePhase(string value)
        {
            return value
                .Trim()
                .ToLowerInvariant()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");
        }

        private static string BuildPrompt(Scenario scenario)
        {
            return $@"
Genera opciones personalizadas para un simulador educativo basado en Design Thinking.

CASO DE ESTUDIO:
Título: {scenario.Title}
Descripción: {scenario.Description}
Tipo de empresa: {scenario.CompanyType}
Problema principal: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

FASES:
1. Empatizar
2. Definir
3. Idear
4. Prototipar
5. Evaluar

Necesito opciones para que un estudiante resuelva el caso fase por fase.

REGLAS:
- Las opciones deben estar totalmente relacionadas con el caso.
- Incluye opciones correctas y distractores.
- No uses opciones genéricas.
- Las opciones deben servir para evaluar al estudiante.
- En Idear, las soluciones correctas deben tener ImpactJson.
- En Evaluar, los KPIs deben estar relacionados con el problema.
- impactJson debe ser una cadena JSON, como ""{{}}"" cuando no aplique.
- tagsJson debe ser una cadena JSON, como ""[]"" cuando no aplique.
- Devuelve SOLO JSON válido.
- No uses markdown.
- No escribas explicaciones fuera del JSON.

FORMATO EXACTO:
[
  {{
    ""phaseName"": ""Empatizar"",
    ""optionType"": ""Evidence"",
    ""text"": ""..."",
    ""isCorrect"": true,
    ""impactJson"": ""{{}}"",
    ""tagsJson"": ""[]"",
    ""orderIndex"": 1
  }}
]

CANTIDAD REQUERIDA:
Empatizar:
- 3 Evidence correctas
- 2 Evidence distractoras
- 3 PainPoint correctos
- 2 PainPoint distractores

Definir:
- 2 ProblemStatement correctos
- 2 ProblemStatement distractores

Idear:
- 4 Solution correctas con ImpactJson
- 2 Solution distractoras con ImpactJson neutro

Prototipar:
- 3 PrototypeFeature correctas
- 2 PrototypeFeature distractoras
- 2 UserFlowStep correctos
- 1 UserFlowStep distractor

Evaluar:
- 4 KPI correctos
- 2 KPI distractores

ImpactJson para soluciones:
Debe contener un objeto JSON con cartAbandonment, conversionRate, satisfaction y purchaseTime.

Si el caso no es e-commerce, adapta los impactos conceptualmente pero conserva esas mismas claves para que el motor de KPIs funcione.
";
        }

        private static string ExtractAssistantContent(string openRouterResponse)
        {
            using var document = JsonDocument.Parse(openRouterResponse);

            var root = document.RootElement;

            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }

        private static string CleanJson(string text)
        {
            var clean = text.Trim();

            if (clean.StartsWith("```json"))
            {
                clean = clean.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (clean.StartsWith("```"))
            {
                clean = clean.Replace("```", "").Trim();
            }

            var firstBracket = clean.IndexOf('[');
            var lastBracket = clean.LastIndexOf(']');

            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                clean = clean.Substring(firstBracket, lastBracket - firstBracket + 1);
            }

            return clean;
        }

        private class AiGeneratedScenarioOptionDto
        {
            public string PhaseName { get; set; } = string.Empty;

            public string OptionType { get; set; } = string.Empty;

            public string Text { get; set; } = string.Empty;

            public bool? IsCorrect { get; set; }

            public JsonElement? ImpactJson { get; set; }

            public JsonElement? Impact { get; set; }

            public List<string> Tags { get; set; } = new();

            public JsonElement? TagsJson { get; set; }

            public decimal? Cost { get; set; }

            public decimal? TimeCost { get; set; }

            public decimal? RiskImpact { get; set; }

            public int? MaxSelections { get; set; }

            public string ExpectedImpactLevel { get; set; } = string.Empty;

            public string ExpectedEffortLevel { get; set; } = string.Empty;

            public string ExpectedViabilityLevel { get; set; } = string.Empty;

            public int OrderIndex { get; set; }
        }
        public Task<GeneratedScenarioDraftDto> GenerateScenarioDraftAsync(string methodology)
        {
            var normalizedMethodology = methodology?.Trim() ?? "DesignThinking";

            GeneratedScenarioDraftDto draft = normalizedMethodology switch
            {
                "BPM" => new GeneratedScenarioDraftDto
                {
                    Title = "Optimización del proceso de atención de pedidos en restaurante",
                    Description = "Un restaurante familiar recibe pedidos por WhatsApp, llamadas telefónicas y atención presencial. En horas pico, los pedidos se duplican, se confunden comandas, se retrasan entregas y el personal no cuenta con una vista clara del estado de cada orden. La gerencia busca rediseñar el proceso operativo mediante herramientas digitales que permitan mejorar tiempos, trazabilidad y satisfacción del cliente.",
                    CompanyType = "Restaurante familiar",
                    Problem = "El proceso de recepción, preparación y entrega de pedidos depende de pasos manuales, comunicación informal y poca trazabilidad, generando retrasos, errores en comandas y baja eficiencia operativa.",
                    TargetUser = "Personal de cocina, cajeros, repartidores internos y clientes que realizan pedidos en horas pico.",
                    Constraints = "Presupuesto limitado, equipo pequeño, resistencia inicial del personal, operación continua sin posibilidad de detener el restaurante y necesidad de implementar mejoras en menos de 6 semanas.",
                    Difficulty = "Alta"
                },

                "DigitalMaturity" => new GeneratedScenarioDraftDto
                {
                    Title = "Diagnóstico de madurez digital en una clínica privada",
                    Description = "Una clínica privada atiende pacientes por llamadas, mensajes y recepción presencial. Aunque utiliza algunas herramientas digitales, la información se encuentra dispersa entre agendas físicas, hojas de cálculo y sistemas no integrados. La dirección desea conocer su nivel de madurez digital para priorizar inversiones tecnológicas que mejoren la atención, la eficiencia administrativa y el uso de datos para la toma de decisiones.",
                    CompanyType = "Clínica privada",
                    Problem = "La organización utiliza herramientas digitales aisladas, no cuenta con integración de datos ni indicadores consolidados, lo que dificulta medir desempeño, reducir tiempos administrativos y mejorar la experiencia del paciente.",
                    TargetUser = "Personal administrativo, médicos, pacientes recurrentes y responsables de gestión de la clínica.",
                    Constraints = "Presupuesto moderado, datos dispersos, personal con diferentes niveles de habilidad digital, procesos sensibles por información médica y necesidad de priorizar iniciativas de alto impacto.",
                    Difficulty = "Media"
                },

                "LeanStartup" => new GeneratedScenarioDraftDto
                {
                    Title = "Validación de una plataforma digital para reservas de servicios de belleza",
                    Description = "Un emprendimiento de servicios de belleza quiere lanzar una plataforma que permita reservar citas, pagar anticipos y recibir recordatorios automáticos. El equipo aún no sabe si las clientas realmente usarían la solución ni qué funcionalidades son prioritarias. Antes de invertir en el desarrollo completo, necesita validar hipótesis mediante un MVP y medir señales reales de adopción.",
                    CompanyType = "Emprendimiento de servicios de belleza",
                    Problem = "El negocio quiere digitalizar la reserva de citas, pero no ha validado si sus clientas usarían una plataforma digital, qué problema les duele más ni qué propuesta de valor generaría adopción real.",
                    TargetUser = "Mujeres entre 25 y 40 años que reservan servicios de belleza, maquillaje, uñas o tratamientos estéticos.",
                    Constraints = "Presupuesto inicial bajo, tiempo de validación de 4 semanas, equipo pequeño, necesidad de evitar construir funcionalidades innecesarias y dependencia de feedback rápido de usuarias reales.",
                    Difficulty = "Media"
                },

                _ => new GeneratedScenarioDraftDto
                {
                    Title = "Rediseño de la experiencia digital en una tienda online",
                    Description = "Una tienda online de productos de consumo recibe tráfico constante, pero muchos usuarios abandonan el proceso de compra antes de finalizar el pago. Los clientes reportan dudas sobre costos de envío, tiempos de entrega y seguridad del pago. La empresa necesita comprender mejor la experiencia del usuario y proponer soluciones digitales centradas en sus necesidades reales.",
                    CompanyType = "E-commerce",
                    Problem = "La tienda presenta una alta tasa de abandono durante el checkout debido a falta de claridad, fricción en el proceso de compra y baja confianza del usuario al momento de pagar.",
                    TargetUser = "Clientes digitales que compran productos en línea desde dispositivos móviles.",
                    Constraints = "Presupuesto limitado, equipo de desarrollo pequeño, plazo máximo de 4 semanas, necesidad de mejorar la experiencia sin reconstruir toda la plataforma.",
                    Difficulty = "Media"
                }
            };

            return Task.FromResult(draft);
        }
    }
}
