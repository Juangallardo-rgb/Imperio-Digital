using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace SimuladorApi.Services.OpenRouter;

public sealed class OpenRouterClient : IOpenRouterClient
{
    private const string Endpoint = "https://openrouter.ai/api/v1/chat/completions";
    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public OpenRouterClient(
        HttpClient httpClient,
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenRouterResult<T>> GenerateJsonAsync<T>(
        OpenRouterJsonRequest request,
        CancellationToken cancellationToken = default)
    {
        var promptHash = ComputePromptHash(request.Messages);
        var response = await SendWithRetriesAsync(
            request.Operation,
            request.Model,
            request.Messages,
            request.Temperature,
            request.MaxTokens,
            new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = request.SchemaName,
                    strict = true,
                    schema = request.Schema
                }
            },
            "json_schema",
            promptHash,
            request.CorrelationId,
            request.TimeoutSeconds,
            cancellationToken);

        if (!response.Success &&
            response.JsonSchemaRejected &&
            _options.AllowJsonObjectFallback)
        {
            response = await SendWithRetriesAsync(
                request.Operation,
                request.Model,
                request.Messages,
                request.Temperature,
                request.MaxTokens,
                new { type = "json_object" },
                "json_object",
                promptHash,
                request.CorrelationId,
                request.TimeoutSeconds,
                cancellationToken);
        }

        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            return OpenRouterResult<T>.Failed(
                request.Model,
                response.RetryCount,
                response.StatusCode,
                response.ErrorCode ?? "openrouter_failed",
                promptHash,
                response.ResponseFormat,
                response.JsonSchemaRejected);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(CleanJson(response.Content), _jsonOptions);
            return parsed is null
                ? OpenRouterResult<T>.Failed(
                    request.Model,
                    response.RetryCount,
                    response.StatusCode,
                    "empty_json_result",
                    promptHash,
                    response.ResponseFormat)
                : new OpenRouterResult<T>(
                    true,
                    parsed,
                    request.Model,
                    response.EffectiveModel,
                    response.RetryCount,
                    response.StatusCode,
                    null,
                    promptHash,
                    response.ResponseFormat);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "OpenRouter returned invalid JSON for {Operation}. PromptHash={PromptHash}",
                request.Operation,
                promptHash);
            return OpenRouterResult<T>.Failed(
                request.Model,
                response.RetryCount,
                response.StatusCode,
                "invalid_json",
                promptHash,
                response.ResponseFormat);
        }
    }

    public async Task<OpenRouterResult<string>> GenerateTextAsync(
        OpenRouterTextRequest request,
        CancellationToken cancellationToken = default)
    {
        var promptHash = ComputePromptHash(request.Messages);
        var response = await SendWithRetriesAsync(
            request.Operation,
            request.Model,
            request.Messages,
            request.Temperature,
            request.MaxTokens,
            null,
            "none",
            promptHash,
            null,
            null,
            cancellationToken);

        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            return OpenRouterResult<string>.Failed(
                request.Model,
                response.RetryCount,
                response.StatusCode,
                response.ErrorCode ?? "openrouter_failed",
                promptHash,
                response.ResponseFormat);
        }

        return new OpenRouterResult<string>(
            true,
            response.Content.Trim(),
            request.Model,
            response.EffectiveModel,
            response.RetryCount,
            response.StatusCode,
            null,
            promptHash,
            response.ResponseFormat);
    }

    private async Task<RawOpenRouterResult> SendWithRetriesAsync(
        string operation,
        string model,
        IReadOnlyList<OpenRouterMessage> messages,
        double temperature,
        int maxTokens,
        object? responseFormat,
        string responseFormatName,
        string promptHash,
        Guid? correlationId,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "OpenRouter is not configured for {Operation}. PromptHash={PromptHash}",
                operation,
                promptHash);
            return RawOpenRouterResult.Failed(0, null, "not_configured", responseFormatName);
        }

        var maxRetries = Math.Clamp(_options.MaxRetries, 0, 2);
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds ?? _options.TimeoutSeconds)));
                using var requestMessage = CreateRequest(
                    model,
                    messages,
                    temperature,
                    maxTokens,
                    responseFormat);
                using var response = await _httpClient.SendAsync(requestMessage, timeoutSource.Token);
                var statusCode = (int)response.StatusCode;
                var responseBody = await response.Content.ReadAsStringAsync(timeoutSource.Token);

                if (response.IsSuccessStatusCode)
                {
                    var envelope = JsonSerializer.Deserialize<OpenRouterEnvelope>(responseBody, _jsonOptions);
                    var content = envelope?.Choices?.FirstOrDefault()?.Message?.Content;
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return RawOpenRouterResult.Failed(attempt, statusCode, "empty_response", responseFormatName);
                    }

                    _logger.LogInformation(
                        "OpenRouter completed. CorrelationId={CorrelationId} Operation={Operation} Model={Model} EffectiveModel={EffectiveModel} HttpStatus={HttpStatus} DurationMs={DurationMs} AttemptNumber={AttemptNumber} ResponseFormat={ResponseFormat} PromptHash={PromptHash}",
                        correlationId,
                        operation,
                        model,
                        envelope?.Model,
                        statusCode,
                        stopwatch.ElapsedMilliseconds,
                        attempt + 1,
                        responseFormatName,
                        promptHash);
                    return RawOpenRouterResult.Completed(
                        content,
                        envelope?.Model,
                        attempt,
                        statusCode,
                        responseFormatName);
                }

                var retryable = response.StatusCode == HttpStatusCode.TooManyRequests ||
                    statusCode >= 500;
                if (!retryable || attempt == maxRetries)
                {
                    var jsonSchemaRejected = IsExplicitJsonSchemaRejection(statusCode, responseBody);
                    _logger.LogWarning(
                        "OpenRouter failed. CorrelationId={CorrelationId} Operation={Operation} HttpStatus={HttpStatus} DurationMs={DurationMs} AttemptNumber={AttemptNumber} ResponseFormat={ResponseFormat} JsonSchemaRejected={JsonSchemaRejected} PromptHash={PromptHash}",
                        correlationId,
                        operation,
                        statusCode,
                        stopwatch.ElapsedMilliseconds,
                        attempt + 1,
                        responseFormatName,
                        jsonSchemaRejected,
                        promptHash);
                    return RawOpenRouterResult.Failed(attempt, statusCode, "http_error", responseFormatName, jsonSchemaRejected);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == maxRetries)
                {
                    return RawOpenRouterResult.Failed(attempt, null, "timeout", responseFormatName);
                }
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "OpenRouter network failure. CorrelationId={CorrelationId} Operation={Operation} AttemptNumber={AttemptNumber} ResponseFormat={ResponseFormat} PromptHash={PromptHash}",
                    correlationId,
                    operation,
                    attempt + 1,
                    responseFormatName,
                    promptHash);
                if (attempt == maxRetries)
                {
                    return RawOpenRouterResult.Failed(attempt, null, "network_error", responseFormatName);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken);
        }

        return RawOpenRouterResult.Failed(maxRetries, null, "openrouter_failed", responseFormatName);
    }

    private static bool IsExplicitJsonSchemaRejection(int statusCode, string responseBody)
    {
        if ((statusCode != (int)HttpStatusCode.BadRequest && statusCode != 422) ||
            string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        return responseBody.Contains("json_schema", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("response_format", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("structured output", StringComparison.OrdinalIgnoreCase);
    }

    private HttpRequestMessage CreateRequest(
        string model,
        IReadOnlyList<OpenRouterMessage> messages,
        double temperature,
        int maxTokens,
        object? responseFormat)
    {
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages.Select(message => new
            {
                role = message.Role,
                content = message.Content
            }),
            ["temperature"] = temperature,
            ["max_tokens"] = maxTokens
        };

        if (responseFormat is not null)
        {
            requestBody["response_format"] = responseFormat;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        if (!string.IsNullOrWhiteSpace(_options.SiteUrl))
        {
            request.Headers.TryAddWithoutValidation("HTTP-Referer", _options.SiteUrl);
        }
        if (!string.IsNullOrWhiteSpace(_options.SiteName))
        {
            request.Headers.TryAddWithoutValidation("X-OpenRouter-Title", _options.SiteName);
        }

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, _jsonOptions),
            Encoding.UTF8,
            "application/json");
        return request;
    }

    private static string CleanJson(string content)
    {
        var clean = content.Trim();
        if (!clean.StartsWith("```", StringComparison.Ordinal))
        {
            return clean;
        }

        var firstNewLine = clean.IndexOf('\n');
        var lastFence = clean.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine
            ? clean[(firstNewLine + 1)..lastFence].Trim()
            : clean;
    }

    private static string ComputePromptHash(IReadOnlyList<OpenRouterMessage> messages)
    {
        var prompt = string.Join("\n", messages.Select(message => $"{message.Role}:{message.Content}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(prompt))).ToLowerInvariant();
    }

    private sealed record RawOpenRouterResult(
        bool Success,
        string? Content,
        string? EffectiveModel,
        int RetryCount,
        int? StatusCode,
        string? ErrorCode,
        string ResponseFormat,
        bool JsonSchemaRejected)
    {
        public static RawOpenRouterResult Completed(
            string content,
            string? effectiveModel,
            int retryCount,
            int statusCode,
            string responseFormat) =>
            new(true, content, effectiveModel, retryCount, statusCode, null, responseFormat, false);

        public static RawOpenRouterResult Failed(
            int retryCount,
            int? statusCode,
            string errorCode,
            string responseFormat,
            bool jsonSchemaRejected = false) =>
            new(false, null, null, retryCount, statusCode, errorCode, responseFormat, jsonSchemaRejected);
    }

    private sealed record OpenRouterEnvelope(
        [property: JsonPropertyName("model")] string? Model,
        [property: JsonPropertyName("choices")] IReadOnlyList<OpenRouterChoice>? Choices);

    private sealed record OpenRouterChoice(
        [property: JsonPropertyName("message")] OpenRouterResponseMessage? Message);

    private sealed record OpenRouterResponseMessage(
        [property: JsonPropertyName("content")] string? Content);
}
