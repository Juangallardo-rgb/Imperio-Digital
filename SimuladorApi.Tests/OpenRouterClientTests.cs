using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimuladorApi.Services.OpenRouter;

namespace SimuladorApi.Tests;

public sealed class OpenRouterClientTests
{
    private static readonly JsonElement Schema = JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new { value = new { type = "string" } },
        required = new[] { "value" }
    });

    [Fact]
    public void ScenarioDefaults_UseStableFastModelAndAllFivePhases()
    {
        var options = new OpenRouterOptions();

        Assert.Equal("google/gemini-2.5-flash-lite", options.ResolveScenarioModel());
        Assert.Equal("openai/gpt-4.1-mini", options.ResolveScenarioOptionsModel());
        Assert.Equal("google/gemini-2.5-flash-lite", options.ResolveScenarioModelForAttempt(1));
        Assert.Equal(2, options.MaxConcurrentScenarioPhaseRequests);
        Assert.False(options.OptimizeScenarioRequestsForSpeed);
        Assert.Equal("throughput", options.ScenarioProviderSort);
        Assert.False(options.UseScenarioResponseHealing);
        Assert.Empty(options.ScenarioReasoningEffort);
    }

    [Fact]
    public async Task GenerateJson_UsesSchemaAndReturnsEffectiveModel()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"value\":\"ok\"}", "effective/model"));
        var client = CreateClient(handler);

        var result = await client.GenerateJsonAsync<Sample>(Request());

        Assert.True(result.Success);
        Assert.Equal("ok", result.Value?.Value);
        Assert.Equal("effective/model", result.EffectiveModel);
        Assert.Contains("json_schema", handler.RequestBodies.Single());
    }

    [Fact]
    public async Task GenerateJson_Retries429ThenSucceeds()
    {
        var handler = new FakeHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests),
            _ => JsonResponse("{\"value\":\"ok\"}"));
        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.True(result.Success);
        Assert.Equal(1, result.RetryCount);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GenerateJson_StopsAfterTwoRetriesOnServerErrors()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal(2, result.RetryCount);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GenerateJson_DoesNotRetryOrdinaryBadRequest()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GenerateJson_CanUseConfiguredJsonObjectFallback()
    {
        var handler = new FakeHttpMessageHandler(
            _ => ErrorResponse(HttpStatusCode.BadRequest, "json_schema is not supported by this provider"),
            _ => JsonResponse("{\"value\":\"fallback\"}"));
        var result = await CreateClient(handler, allowFallback: true)
            .GenerateJsonAsync<Sample>(Request());

        Assert.True(result.Success);
        Assert.Contains("json_object", handler.RequestBodies.Last());
        Assert.Equal("json_object", result.ResponseFormat);
    }

    [Fact]
    public async Task GenerateJson_DoesNotFallbackForUnrelatedBadRequest()
    {
        var handler = new FakeHttpMessageHandler(
            _ => ErrorResponse(HttpStatusCode.BadRequest, "invalid model"),
            _ => JsonResponse("{\"value\":\"must-not-run\"}"));

        var result = await CreateClient(handler, allowFallback: true)
            .GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal("json_schema", result.ResponseFormat);
    }

    [Fact]
    public async Task GenerateJson_RejectsInvalidJson()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("not-json"));
        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal("invalid_json", result.ErrorCode);
    }

    [Fact]
    public async Task GenerateJson_ExtractsObjectFromMixedModelText()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            JsonResponse("Resultado generado:\n{\"value\":\"ok\"}\nFin."));

        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.True(result.Success);
        Assert.Equal("ok", result.Value?.Value);
    }

    [Fact]
    public async Task GenerateJson_RejectsTruncatedResponseBeforeParsing()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            JsonResponse("{\"value\":\"incompleto", finishReason: "length"));

        var result = await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal("truncated_response", result.ErrorCode);
    }

    [Fact]
    public async Task GenerateText_ReturnsPlainContent()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("texto narrativo"));
        var result = await CreateClient(handler).GenerateTextAsync(new OpenRouterTextRequest(
            "feedback", "requested/model", [new OpenRouterMessage("user", "prompt")]));

        Assert.True(result.Success);
        Assert.Equal("texto narrativo", result.Value);
    }

    [Fact]
    public async Task MissingApiKey_FailsWithoutNetworkCall()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("unused"));
        var result = await CreateClient(handler, apiKey: string.Empty)
            .GenerateJsonAsync<Sample>(Request());

        Assert.False(result.Success);
        Assert.Equal("not_configured", result.ErrorCode);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Request_UsesBearerAndOpenRouterAttributionHeaders()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-key", request.Headers.Authorization?.Parameter);
            Assert.True(request.Headers.Contains("HTTP-Referer"));
            Assert.True(request.Headers.Contains("X-OpenRouter-Title"));
            return JsonResponse("{\"value\":\"ok\"}");
        });

        await CreateClient(handler).GenerateJsonAsync<Sample>(Request());
    }

    [Fact]
    public async Task OptimizedJsonRequest_UsesFastStructuredProviderAndResponseHealing()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"value\":\"ok\"}"));
        var request = Request() with
        {
            OptimizeForSpeed = true,
            ReasoningEffort = null
        };

        await CreateClient(handler).GenerateJsonAsync<Sample>(request);

        using var body = JsonDocument.Parse(handler.RequestBodies.Single());
        var root = body.RootElement;
        Assert.Equal("throughput", root.GetProperty("provider").GetProperty("sort").GetString());
        Assert.True(root.GetProperty("provider").GetProperty("require_parameters").GetBoolean());
        Assert.False(root.TryGetProperty("reasoning", out _));
        Assert.True(root.GetProperty("usage").GetProperty("include").GetBoolean());
        Assert.Equal(
            "response-healing",
            root.GetProperty("plugins")[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task OrdinaryJsonRequest_DoesNotChangeRoutingOrReasoning()
    {
        var handler = new FakeHttpMessageHandler(_ => JsonResponse("{\"value\":\"ok\"}"));

        await CreateClient(handler).GenerateJsonAsync<Sample>(Request());

        using var body = JsonDocument.Parse(handler.RequestBodies.Single());
        Assert.False(body.RootElement.TryGetProperty("provider", out _));
        Assert.False(body.RootElement.TryGetProperty("reasoning", out _));
        Assert.False(body.RootElement.TryGetProperty("usage", out _));
    }

    private static OpenRouterJsonRequest Request() => new(
        "test", "requested/model", [new OpenRouterMessage("user", "prompt")],
        "sample", Schema);

    private static OpenRouterClient CreateClient(
        FakeHttpMessageHandler handler,
        bool allowFallback = false,
        string apiKey = "test-key") =>
        new(
            new HttpClient(handler),
            Options.Create(new OpenRouterOptions
            {
                ApiKey = apiKey,
                SiteUrl = "https://example.test",
                SiteName = "tests",
                MaxRetries = 2,
                TimeoutSeconds = 2,
                AllowJsonObjectFallback = allowFallback,
                RequireScenarioParameters = true,
                UseScenarioResponseHealing = true
            }),
            NullLogger<OpenRouterClient>.Instance);

    private static HttpResponseMessage JsonResponse(
        string content,
        string model = "effective/model",
        string? finishReason = null) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model,
                    choices = new[] { new { finish_reason = finishReason, message = new { content } } }
                }),
                Encoding.UTF8,
                "application/json")
        };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode statusCode, string message) =>
        new(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { error = new { message } }),
                Encoding.UTF8,
                "application/json")
        };

    private sealed class Sample
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responses;
        public int CallCount { get; private set; }
        public List<string> RequestBodies { get; } = new();

        public FakeHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpRequestMessage, HttpResponseMessage>>(responses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestBodies.Add(request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken));
            var response = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
            return response(request);
        }
    }
}
