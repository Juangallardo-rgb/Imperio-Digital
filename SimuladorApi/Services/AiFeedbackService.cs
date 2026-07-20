using Microsoft.Extensions.Options;
using SimuladorApi.Services.OpenRouter;

namespace SimuladorApi.Services;

public sealed class AiFeedbackService
{
    private readonly IOpenRouterClient _client;
    private readonly OpenRouterOptions _options;

    public AiFeedbackService(
        IOpenRouterClient client,
        IOptions<OpenRouterOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> GeneratePhaseFeedbackAsync(
        string phaseName,
        decimal score,
        string methodologyCode = "DesignThinking")
    {
        var prompt = $"""
            Interpreta de forma narrativa un resultado académico ya calculado. No cambies ni
            recalcules el valor numérico. Metodología: {GetMethodologyName(methodologyCode)}.
            Fase: {phaseName}. Puntaje determinista: {score}/100. En máximo 90 palabras,
            indica una fortaleza y una mejora práctica, con tono respetuoso y formativo.
            """;
        var result = await _client.GenerateTextAsync(new OpenRouterTextRequest(
            "phase-feedback",
            _options.ResolveFeedbackModel(),
            [
                new OpenRouterMessage(
                    "system",
                    "Solo interpretas resultados ya calculados; nunca alteras puntajes, recursos ni KPI."),
                new OpenRouterMessage("user", prompt)
            ],
            Temperature: 0.4,
            MaxTokens: 180));
        return result.Success && !string.IsNullOrWhiteSpace(result.Value)
            ? result.Value
            : BuildLocalPhaseSummary(phaseName, score, methodologyCode);
    }

    public async Task<string> GenerateFinalFeedbackAsync(
        decimal finalScore,
        List<(string PhaseName, decimal Score)> phaseScores,
        string methodologyCode = "DesignThinking")
    {
        var strongest = phaseScores.OrderByDescending(phase => phase.Score).FirstOrDefault();
        var weakest = phaseScores.OrderBy(phase => phase.Score).FirstOrDefault();
        var results = string.Join(
            "; ",
            phaseScores.Select(phase => $"{phase.PhaseName}: {phase.Score}"));
        var prompt = $"""
            Redacta una interpretación final de máximo 140 palabras. Los números fueron calculados
            por el sistema y no debes modificarlos. Metodología: {GetMethodologyName(methodologyCode)}.
            Puntaje final: {finalScore}. Resultados por fase: {results}. Mejor fase:
            {strongest.PhaseName} ({strongest.Score}). Fase a reforzar: {weakest.PhaseName}
            ({weakest.Score}). Explica tendencias y recomienda dos acciones prácticas.
            """;
        var result = await _client.GenerateTextAsync(new OpenRouterTextRequest(
            "final-feedback",
            _options.ResolveFeedbackModel(),
            [
                new OpenRouterMessage(
                    "system",
                    "Solo interpretas resultados ya calculados; nunca alteras puntajes, recursos ni KPI."),
                new OpenRouterMessage("user", prompt)
            ],
            Temperature: 0.4,
            MaxTokens: 260));
        return result.Success && !string.IsNullOrWhiteSpace(result.Value)
            ? result.Value
            : BuildLocalFinalSummary(finalScore, strongest, weakest, methodologyCode);
    }

    private static string BuildLocalPhaseSummary(
        string phaseName,
        decimal score,
        string methodologyCode)
    {
        var assessment = score >= 85
            ? "El desempeño fue sólido"
            : score >= 70
                ? "El desempeño fue adecuado"
                : score >= 50
                    ? "El desempeño fue intermedio"
                    : "El desempeño requiere refuerzo";
        return $"Resumen automático local: {assessment} en {phaseName}. Revisa la relación entre evidencia, decisión y {GetMethodologyName(methodologyCode)} para fortalecer el siguiente intento.";
    }

    private static string BuildLocalFinalSummary(
        decimal finalScore,
        (string PhaseName, decimal Score) strongest,
        (string PhaseName, decimal Score) weakest,
        string methodologyCode) =>
        $"Resumen automático local: el puntaje final fue {finalScore} en {GetMethodologyName(methodologyCode)}. La fase más fuerte fue {strongest.PhaseName} ({strongest.Score}) y la fase a reforzar fue {weakest.PhaseName} ({weakest.Score}). Conviene revisar la evidencia y la coherencia entre decisiones antes de un nuevo intento.";

    private static string GetMethodologyName(string methodologyCode) =>
        methodologyCode switch
        {
            "BPM" => "Business Process Management",
            "DigitalMaturity" => "Madurez Digital",
            "LeanStartup" => "Lean Startup",
            _ => "Design Thinking"
        };
}
