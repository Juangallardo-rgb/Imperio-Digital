using Microsoft.Extensions.Options;
using SimuladorApi.Services.OpenRouter;

namespace SimuladorApi.Services;

public sealed class OpenRouterService
{
    private readonly IOpenRouterClient _client;
    private readonly OpenRouterOptions _options;

    public OpenRouterService(
        IOpenRouterClient client,
        IOptions<OpenRouterOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> GenerateFeedbackAsync(
        decimal digitalMaturity,
        decimal operationalEfficiency,
        decimal customerExperience,
        decimal globalScore,
        string feedbackRule)
    {
        var prompt = $"""
            Eres un asistente académico especializado en transformación digital.

            Con base en estos KPIs de una simulación:
            - Madurez digital: {digitalMaturity}
            - Eficiencia operativa: {operationalEfficiency}
            - Experiencia del cliente: {customerExperience}
            - Score global: {globalScore}

            Feedback base del sistema:
            {feedbackRule}

            Redacta un feedback breve, claro y profesional en español. Interpreta los
            resultados, relaciónalos con metodologías de transformación digital y
            recomienda acciones concretas. Máximo 120 palabras.
            """;

        var result = await _client.GenerateTextAsync(new OpenRouterTextRequest(
            "legacy-kpi-feedback",
            _options.ResolveFeedbackModel(),
            [
                new OpenRouterMessage(
                    "system",
                    "Eres un experto en transformación digital, BPM, Design Thinking, Lean Startup y madurez digital."),
                new OpenRouterMessage("user", prompt)
            ],
            Temperature: 0.7,
            MaxTokens: 220));

        return result.Success && !string.IsNullOrWhiteSpace(result.Value)
            ? result.Value
            : "Resumen automático no disponible.";
    }
}
