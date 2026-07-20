namespace SimuladorApi.Services.Ai;

public sealed record AiOptionResourcePolicy(
    bool IsCorrect,
    decimal Cost,
    decimal TimeCost,
    decimal RiskImpact);

public sealed record AiPhaseGenerationPolicy(
    int MaxSelections,
    IReadOnlyList<AiOptionResourcePolicy> Options)
{
    public int ExpectedOptionCount => Options.Count;

    public int ExpectedCorrectCount => Options.Count(option => option.IsCorrect);

    public AiOptionResourcePolicy GetOption(int orderIndex)
    {
        if (orderIndex < 1 || orderIndex > Options.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(orderIndex));
        }

        return Options[orderIndex - 1];
    }
}

public static class AiScenarioGenerationPolicy
{
    private static AiOptionResourcePolicy Correct(
        decimal cost,
        decimal timeCost,
        decimal riskImpact) =>
        new(true, cost, timeCost, riskImpact);

    private static AiOptionResourcePolicy Distractor(
        decimal cost,
        decimal timeCost,
        decimal riskImpact) =>
        new(false, cost, timeCost, riskImpact);

    private static AiPhaseGenerationPolicy Phase(
        int maxSelections,
        params AiOptionResourcePolicy[] options) =>
        new(maxSelections, options);

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, AiPhaseGenerationPolicy>> Policies =
        new Dictionary<string, IReadOnlyDictionary<string, AiPhaseGenerationPolicy>>(StringComparer.Ordinal)
        {
            ["DesignThinking"] = new Dictionary<string, AiPhaseGenerationPolicy>(StringComparer.Ordinal)
            {
                ["Empatizar"] = Phase(4,
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Correct(0, 0, -1),
                    Distractor(0, 0, 5)),
                ["Definir"] = Phase(3,
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Distractor(0, 0, 6)),
                ["Idear"] = Phase(3,
                    Correct(18, 1, -4),
                    Correct(12, 1, -3),
                    Distractor(35, 2, 12)),
                ["Prototipar"] = Phase(3,
                    Correct(25, 2, -4),
                    Correct(15, 1, -3),
                    Distractor(45, 3, 14)),
                ["Evaluar"] = Phase(3,
                    Correct(6, 1, -3),
                    Correct(5, 1, -2),
                    Distractor(5, 1, 6))
            },
            ["BPM"] = new Dictionary<string, AiPhaseGenerationPolicy>(StringComparer.Ordinal)
            {
                ["Identificar proceso"] = Phase(4,
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Distractor(0, 0, 5)),
                ["Modelar proceso actual"] = Phase(4,
                    Correct(5, 1, -2),
                    Correct(5, 1, -2),
                    Distractor(30, 2, 10)),
                ["Analizar cuellos de botella"] = Phase(4,
                    Correct(6, 1, -3),
                    Correct(6, 1, -3),
                    Distractor(8, 1, 7)),
                ["Rediseñar proceso"] = Phase(4,
                    Correct(22, 2, -5),
                    Correct(12, 1, -4),
                    Distractor(10, 2, 12)),
                ["Monitorear indicadores"] = Phase(4,
                    Correct(8, 1, -3),
                    Correct(6, 1, -2),
                    Distractor(4, 1, 6))
            },
            ["DigitalMaturity"] = new Dictionary<string, AiPhaseGenerationPolicy>(StringComparer.Ordinal)
            {
                ["Diagnóstico inicial"] = Phase(4,
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Distractor(0, 0, 6)),
                ["Evaluar capacidades"] = Phase(4,
                    Correct(8, 1, -3),
                    Correct(8, 1, -3),
                    Distractor(6, 1, 6)),
                ["Priorizar brechas"] = Phase(4,
                    Correct(8, 1, -3),
                    Correct(7, 1, -2),
                    Distractor(10, 1, 7)),
                ["Plan de transformación"] = Phase(4,
                    Correct(25, 2, -4),
                    Correct(22, 2, -4),
                    Distractor(45, 3, 15)),
                ["Seguimiento de madurez"] = Phase(4,
                    Correct(8, 1, -3),
                    Correct(6, 1, -2),
                    Distractor(4, 1, 6))
            },
            ["LeanStartup"] = new Dictionary<string, AiPhaseGenerationPolicy>(StringComparer.Ordinal)
            {
                ["Hipótesis"] = Phase(4,
                    Correct(0, 0, -2),
                    Correct(0, 0, -2),
                    Distractor(0, 0, 5)),
                ["MVP"] = Phase(4,
                    Correct(20, 2, -4),
                    Correct(14, 1, -3),
                    Distractor(50, 4, 15)),
                ["Medición"] = Phase(4,
                    Correct(7, 1, -3),
                    Correct(6, 1, -2),
                    Distractor(4, 1, 6)),
                ["Aprendizaje"] = Phase(4,
                    Correct(6, 1, -3),
                    Correct(6, 1, -2),
                    Distractor(8, 1, 10)),
                ["Pivote o perseverancia"] = Phase(4,
                    Correct(8, 1, -3),
                    Correct(8, 1, -3),
                    Distractor(18, 2, 12))
            }
        };

    public static AiPhaseGenerationPolicy GetRequired(string methodologyCode, string phaseName)
    {
        if (Policies.TryGetValue(methodologyCode, out var phases) &&
            phases.TryGetValue(phaseName, out var policy))
        {
            return policy;
        }

        throw new ArgumentException(
            $"No existe una política de simulación para {methodologyCode}/{phaseName}.");
    }

    public static bool TryGet(
        string methodologyCode,
        string phaseName,
        out AiPhaseGenerationPolicy? policy)
    {
        policy = null;
        if (!Policies.TryGetValue(methodologyCode, out var phases) ||
            !phases.TryGetValue(phaseName, out var configured))
        {
            return false;
        }

        policy = configured;
        return true;
    }
}
