using SimuladorApi.Models;
using SimuladorApi.Services.Ai;

namespace SimuladorApi.Tests;

public sealed class AiScenarioContentValidatorTests
{
    private readonly AiScenarioContentValidator _validator = new();

    [Theory]
    [InlineData("DesignThinking", "Empatizar", "Evidence")]
    [InlineData("DesignThinking", "Definir", "ProblemStatement")]
    [InlineData("DesignThinking", "Idear", "SolutionIdea")]
    [InlineData("DesignThinking", "Prototipar", "PrototypeComponent")]
    [InlineData("DesignThinking", "Evaluar", "TestFinding")]
    [InlineData("BPM", "Identificar proceso", "ProcessEvidence")]
    [InlineData("BPM", "Modelar proceso actual", "CurrentProcessStep")]
    [InlineData("BPM", "Analizar cuellos de botella", "Bottleneck")]
    [InlineData("BPM", "Rediseñar proceso", "ProcessImprovement")]
    [InlineData("BPM", "Monitorear indicadores", "Kpi")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial", "MaturityEvidence")]
    [InlineData("DigitalMaturity", "Evaluar capacidades", "DigitalCapability")]
    [InlineData("DigitalMaturity", "Priorizar brechas", "MaturityGap")]
    [InlineData("DigitalMaturity", "Plan de transformación", "TransformationInitiative")]
    [InlineData("DigitalMaturity", "Seguimiento de madurez", "MaturityKpi")]
    [InlineData("LeanStartup", "Hipótesis", "Hypothesis")]
    [InlineData("LeanStartup", "MVP", "MvpComponent")]
    [InlineData("LeanStartup", "Medición", "ActionableMetric")]
    [InlineData("LeanStartup", "Aprendizaje", "ValidatedLearning")]
    [InlineData("LeanStartup", "Pivote o perseverancia", "StrategicDecision")]
    public void Catalog_ContainsExpectedPhaseOptionType(
        string methodology,
        string phase,
        string optionType)
    {
        Assert.Contains(optionType, _validator.GetAllowedOptionTypes(methodology, phase));
    }

    [Theory]
    [InlineData("DesignThinking", "Empatizar", "Evidence", "satisfaction")]
    [InlineData("BPM", "Identificar proceso", "ProcessEvidence", "processEfficiency")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial", "MaturityEvidence", "digitalMaturity")]
    [InlineData("LeanStartup", "Hipótesis", "Hypothesis", "validatedLearning")]
    public void ValidPhaseOptions_AreAccepted(
        string methodology,
        string phaseName,
        string optionType,
        string kpi)
    {
        var result = _validator.ValidatePhaseOptions(
            methodology,
            Phase(phaseName),
            ValidContent(methodology, phaseName, optionType, kpi));

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }

    [Theory]
    [InlineData("too_few")]
    [InlineData("no_best")]
    [InlineData("no_bad")]
    [InlineData("duplicate")]
    [InlineData("wrong_phase")]
    [InlineData("wrong_type")]
    [InlineData("bad_order")]
    [InlineData("bad_max")]
    [InlineData("bad_cost")]
    [InlineData("bad_time")]
    [InlineData("bad_risk")]
    [InlineData("bad_level")]
    [InlineData("unknown_kpi")]
    [InlineData("extreme_kpi")]
    [InlineData("empty_impact")]
    [InlineData("duplicate_tags")]
    [InlineData("too_many_tags")]
    [InlineData("bad_viability")]
    [InlineData("non_consecutive_order")]
    public void InvalidPhaseOptions_AreRejected(string mutation)
    {
        var content = ValidContent("DesignThinking", "Empatizar", "Evidence", "satisfaction");
        var options = content.Options;
        switch (mutation)
        {
            case "too_few": options.RemoveAt(2); break;
            case "no_best": options.ForEach(option => option.IsBestOption = false); break;
            case "no_bad": options.ForEach(option => option.IsBestOption = true); break;
            case "duplicate": options[1].Text = options[0].Text.ToUpperInvariant(); break;
            case "wrong_phase": content.PhaseName = "MVP"; break;
            case "wrong_type": options[0].OptionType = "Unknown"; break;
            case "bad_order": options[1].OrderIndex = options[0].OrderIndex; break;
            case "bad_max": options[0].MaxSelections = 99; break;
            case "bad_cost": options[0].Cost = 101; break;
            case "bad_time": options[0].TimeCost = 9; break;
            case "bad_risk": options[0].RiskImpact = 21; break;
            case "bad_level": options[0].ExpectedImpactLevel = "Extremo"; break;
            case "unknown_kpi": options[0].Impact["inventedKpi"] = 1; break;
            case "extreme_kpi": options[0].Impact["satisfaction"] = 26; break;
            case "empty_impact": options[0].Impact.Clear(); break;
            case "duplicate_tags": options[0].Tags = new() { "estrategia", "ESTRATEGIA" }; break;
            case "too_many_tags": options[0].Tags = Enumerable.Range(1, 7).Select(index => $"tag-{index}").ToList(); break;
            case "bad_viability": options[0].ExpectedViabilityLevel = "Extrema"; break;
            case "non_consecutive_order": options[2].OrderIndex = 4; break;
        }

        var result = _validator.ValidatePhaseOptions(
            "DesignThinking",
            Phase("Empatizar"),
            content);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("bajo", "Bajo")]
    [InlineData("BAJA", "Bajo")]
    [InlineData("medio", "Medio")]
    [InlineData("Media", "Medio")]
    [InlineData("ALTO", "Alto")]
    public void LevelNormalization_IsStable(string input, string expected)
    {
        Assert.Equal(expected, AiScenarioContentValidator.NormalizeLevel(input));
    }

    [Theory]
    [InlineData("alto", "Alta")]
    [InlineData("MEDIA", "Media")]
    [InlineData("bajo", "Baja")]
    public void ViabilityLevelNormalization_UsesFeminineConvention(string input, string expected)
    {
        Assert.Equal(expected, AiScenarioContentValidator.NormalizeFeminineLevel(input));
    }

    private static MethodologyPhase Phase(string name) => new() { Id = 10, Name = name };

    private static AiPhaseOptionsContent ValidContent(
        string methodology,
        string phase,
        string type,
        string kpi)
    {
        var policy = AiScenarioGenerationPolicy.GetRequired(methodology, phase);
        return new AiPhaseOptionsContent
        {
            PhaseName = phase,
            Options = policy.Options.Select((resource, index) => new AiScenarioOptionContent
            {
                OptionType = type,
                Text = $"Alternativa empresarial válida número {index + 1} con contexto suficiente.",
                IsBestOption = resource.IsCorrect,
                Rationale = "Justificación académica suficiente para evaluar la decisión.",
                Impact = new Dictionary<string, decimal> { [kpi] = resource.IsCorrect ? 5 : -2 },
                Tags = new List<string> { "strategy", $"option-{index + 1}" },
                Cost = resource.Cost,
                TimeCost = resource.TimeCost,
                RiskImpact = resource.RiskImpact,
                MaxSelections = policy.MaxSelections,
                ExpectedImpactLevel = "Medio",
                ExpectedEffortLevel = "Medio",
                ExpectedViabilityLevel = "Alta",
                OrderIndex = index + 1
            }).ToList()
        };
    }
}
