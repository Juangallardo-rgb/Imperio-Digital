using System.Text.Json;

namespace SimuladorApi.Services.OpenRouter;

public sealed record OpenRouterMessage(string Role, string Content);

public sealed record OpenRouterJsonRequest(
    string Operation,
    string Model,
    IReadOnlyList<OpenRouterMessage> Messages,
    string SchemaName,
    JsonElement Schema,
    double Temperature = 0.2,
    int MaxTokens = 1200,
    Guid? CorrelationId = null,
    int? TimeoutSeconds = null);

public sealed record OpenRouterTextRequest(
    string Operation,
    string Model,
    IReadOnlyList<OpenRouterMessage> Messages,
    double Temperature = 0.5,
    int MaxTokens = 500);

public sealed record OpenRouterResult<T>(
    bool Success,
    T? Value,
    string RequestedModel,
    string? EffectiveModel,
    int RetryCount,
    int? StatusCode,
    string? ErrorCode,
    string PromptHash,
    string ResponseFormat = "none",
    bool JsonSchemaRejected = false)
{
    public static OpenRouterResult<T> Failed(
        string requestedModel,
        int retryCount,
        int? statusCode,
        string errorCode,
        string promptHash,
        string responseFormat = "none",
        bool jsonSchemaRejected = false) =>
        new(false, default, requestedModel, null, retryCount, statusCode, errorCode, promptHash, responseFormat, jsonSchemaRejected);
}

public interface IOpenRouterClient
{
    Task<OpenRouterResult<T>> GenerateJsonAsync<T>(
        OpenRouterJsonRequest request,
        CancellationToken cancellationToken = default);

    Task<OpenRouterResult<string>> GenerateTextAsync(
        OpenRouterTextRequest request,
        CancellationToken cancellationToken = default);
}
