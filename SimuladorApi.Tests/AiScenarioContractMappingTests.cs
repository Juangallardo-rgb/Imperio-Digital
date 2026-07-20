using System.Text.Json;
using SimuladorApi.Models;
using SimuladorApi.Services;
using SimuladorApi.Services.Ai;
using SimuladorApi.Services.OpenRouter;

namespace SimuladorApi.Tests;

public sealed class AiScenarioContractMappingTests
{
    [Fact]
    public void ImpactAndTags_AreSerializedIntoEntityJsonFields()
    {
        var generated = ValidOption();
        var mapped = AiScenarioContentService.MapOption(
            12,
            "LeanStartup",
            new MethodologyPhase { Id = 8, Name = "Hipótesis" },
            generated,
            0);

        var impact = JsonSerializer.Deserialize<Dictionary<string, decimal>>(mapped.ImpactJson);
        var tags = JsonSerializer.Deserialize<List<string>>(mapped.TagsJson);
        Assert.Equal(5, impact?["validatedLearning"]);
        Assert.Equal(new[] { "estrategia", "aprendizaje" }, tags);
        Assert.Equal("Hipótesis", mapped.PhaseName);
    }

    [Theory]
    [InlineData(0, true, 100)]
    [InlineData(2, false, 0)]
    public void MethodologyPolicy_MapsCorrectnessAndDeterministicScore(
        int optionIndex,
        bool expectedCorrect,
        decimal expectedScore)
    {
        var generated = ValidOption();
        generated.IsBestOption = !expectedCorrect;

        var mapped = AiScenarioContentService.MapOption(
            12,
            "LeanStartup",
            new MethodologyPhase { Id = 8, Name = "Hipótesis" },
            generated,
            optionIndex);

        Assert.Equal(expectedCorrect, mapped.IsCorrect);
        Assert.Equal(expectedScore, mapped.Score);
        Assert.Equal(4, mapped.MaxSelections);
        Assert.Equal(0, mapped.Cost);
        Assert.Equal(0, mapped.TimeCost);
    }

    [Fact]
    public void GeneratedControls_AreNormalizedFromPolicyBeforeValidation()
    {
        var phase = new MethodologyPhase { Id = 1, Name = "Empatizar" };
        var content = new AiPhaseOptionsContent
        {
            PhaseName = "fase incorrecta",
            Options = Enumerable.Range(1, 4)
                .Select(index => new AiScenarioOptionContent
                {
                    OptionType = "Evidence",
                    Text = $"Evidencia concreta y diferente obtenida con la investigación número {index}.",
                    IsBestOption = false,
                    Rationale = $"Explicación válida y diferente para sustentar la decisión número {index}.",
                    Impact = new() { ["satisfaction"] = index },
                    Tags = new() { $"evidencia-{index}" },
                    Cost = 99,
                    TimeCost = 8,
                    RiskImpact = 20,
                    MaxSelections = 99,
                    ExpectedImpactLevel = "Alto",
                    ExpectedEffortLevel = "Medio",
                    ExpectedViabilityLevel = "Alta",
                    OrderIndex = 1
                })
                .ToList()
        };

        AiScenarioContentService.NormalizeGeneratedPhaseControls(
            "DesignThinking",
            phase,
            content);

        var validation = new AiScenarioContentValidator()
            .ValidatePhaseOptions("DesignThinking", phase, content);
        var policy = AiScenarioGenerationPolicy.GetRequired("DesignThinking", phase.Name);

        Assert.True(validation.IsValid, string.Join(" | ", validation.Errors));
        Assert.Equal("Empatizar", content.PhaseName);
        Assert.Equal(new[] { 1, 2, 3, 4 }, content.Options.Select(option => option.OrderIndex));
        Assert.Equal(policy.ExpectedCorrectCount, content.Options.Count(option => option.IsBestOption));
        Assert.All(content.Options, option => Assert.Equal(policy.MaxSelections, option.MaxSelections));
    }

    [Fact]
    public void GeneratedContent_DeduplicatesEquivalentTextsAndTagsBeforeValidation()
    {
        var phase = new MethodologyPhase { Id = 3, Name = "Idear" };
        var content = new AiPhaseOptionsContent
        {
            PhaseName = phase.Name,
            Options = Enumerable.Range(1, 3)
                .Select(index => new AiScenarioOptionContent
                {
                    OptionType = "SolutionIdea",
                    Text = index <= 2
                        ? "Crear una aplicación accesible para orientar a los usuarios rurales."
                        : $"Proponer una solución digital diferente y comprobable número {index}.",
                    Rationale = $"Este enfoque aporta una justificación específica para la alternativa {index}.",
                    Impact = new() { ["satisfaction"] = index },
                    Tags = new() { "Innovación", " innovacion ", "Accesibilidad" },
                    ExpectedImpactLevel = "alto",
                    ExpectedEffortLevel = "media",
                    ExpectedViabilityLevel = "alta",
                    OrderIndex = index
                })
                .ToList()
        };

        AiScenarioContentService.NormalizeGeneratedPhaseControls(
            "DesignThinking",
            phase,
            content);

        var validation = new AiScenarioContentValidator()
            .ValidatePhaseOptions("DesignThinking", phase, content);

        Assert.True(validation.IsValid, string.Join(" | ", validation.Errors));
        Assert.Equal(2, content.Options[0].Tags.Count);
        Assert.Contains("Enfoque 2:", content.Options[1].Text);
        Assert.All(content.Options, option => Assert.Equal("Alto", option.ExpectedImpactLevel));
        Assert.All(content.Options, option => Assert.Equal("Medio", option.ExpectedEffortLevel));
        Assert.All(content.Options, option => Assert.Equal("Alta", option.ExpectedViabilityLevel));
    }

    [Theory]
    [InlineData("DesignThinking", "Empatizar")]
    [InlineData("DesignThinking", "Definir")]
    [InlineData("DesignThinking", "Idear")]
    [InlineData("DesignThinking", "Prototipar")]
    [InlineData("DesignThinking", "Evaluar")]
    [InlineData("BPM", "Identificar proceso")]
    [InlineData("BPM", "Modelar proceso actual")]
    [InlineData("BPM", "Analizar cuellos de botella")]
    [InlineData("BPM", "Rediseñar proceso")]
    [InlineData("BPM", "Monitorear indicadores")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial")]
    [InlineData("DigitalMaturity", "Evaluar capacidades")]
    [InlineData("DigitalMaturity", "Priorizar brechas")]
    [InlineData("DigitalMaturity", "Plan de transformación")]
    [InlineData("DigitalMaturity", "Seguimiento de madurez")]
    [InlineData("LeanStartup", "Hipótesis")]
    [InlineData("LeanStartup", "MVP")]
    [InlineData("LeanStartup", "Medición")]
    [InlineData("LeanStartup", "Aprendizaje")]
    [InlineData("LeanStartup", "Pivote o perseverancia")]
    public void GeneratedNormalization_IsValidForEveryMethodologyPhase(
        string methodologyCode,
        string phaseName)
    {
        var validator = new AiScenarioContentValidator();
        var phase = new MethodologyPhase { Id = 1, Name = phaseName };
        var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phaseName);
        var optionType = validator.GetAllowedOptionTypes(methodologyCode, phaseName).First();
        var kpi = KpiSimulationService.GetAllowedKpiKeys(methodologyCode).First();
        var content = new AiPhaseOptionsContent
        {
            PhaseName = "nombre generado incorrecto",
            Options = policy.Options.Select((_, index) => new AiScenarioOptionContent
            {
                OptionType = $" {optionType} ",
                Text = "Aplicar una alternativa empresarial contextualizada para resolver el problema.",
                Rationale = $"Justificación contextual y evaluable para la alternativa {index + 1}.",
                Impact = new() { [kpi] = index + 1 },
                Tags = new() { "Evidencia", " evidencia ", $"fase-{index + 1}" },
                Cost = 99,
                TimeCost = 8,
                RiskImpact = 20,
                MaxSelections = 99,
                ExpectedImpactLevel = "alto",
                ExpectedEffortLevel = "media",
                ExpectedViabilityLevel = "alta",
                OrderIndex = 1
            }).ToList()
        };

        AiScenarioContentService.NormalizeGeneratedPhaseControls(
            methodologyCode,
            phase,
            content);

        var validation = validator.ValidatePhaseOptions(methodologyCode, phase, content);

        Assert.True(validation.IsValid, string.Join(" | ", validation.Errors));
        Assert.Equal(policy.ExpectedOptionCount, content.Options.Count);
        Assert.Equal(
            policy.ExpectedOptionCount,
            content.Options.Select(option => option.Text).Distinct(StringComparer.Ordinal).Count());
        Assert.All(content.Options, option => Assert.Equal(2, option.Tags.Count));

        var allowedKpis = KpiSimulationService.GetAllowedKpiKeys(methodologyCode);
        var schema = AiScenarioJsonSchemas.BuildPhaseOptions(
            methodologyCode,
            phaseName,
            validator.GetAllowedOptionTypes(methodologyCode, phaseName),
            allowedKpis);
        var schemaText = schema.GetRawText();
        Assert.DoesNotContain("\"minProperties\"", schemaText);
        Assert.DoesNotContain("\"maxProperties\"", schemaText);
        Assert.DoesNotContain("\"uniqueItems\"", schemaText);
        var impactSchema = schema
            .GetProperty("properties")
            .GetProperty("options")
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("impact");
        var requiredKpis = impactSchema
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();
        Assert.Equal(allowedKpis, requiredKpis);
    }

    [Theory]
    [InlineData("invalid_json", 200, true)]
    [InlineData("truncated_response", 200, true)]
    [InlineData("http_error", 429, true)]
    [InlineData("http_error", 503, true)]
    [InlineData("http_error", 402, false)]
    [InlineData("not_configured", null, false)]
    public void PhaseRetryPolicy_OnlyRetriesRecoverableFailures(
        string errorCode,
        int? statusCode,
        bool expected)
    {
        var result = OpenRouterResult<AiPhaseOptionsContent>.Failed(
            "model",
            0,
            statusCode,
            errorCode,
            "hash",
            "json_schema");

        Assert.Equal(expected, AiScenarioContentService.CanRetryPhaseGeneration(result));
    }

    [Fact]
    public void DraftPolicy_RequiresTeacherMethodologyValidityAndUnusedStatus()
    {
        var now = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var record = ValidDraft(correlationId, now);

        Assert.True(AiGenerationAuditService.IsUsableDraft(record, correlationId, 7, "LeanStartup", now));
        Assert.False(AiGenerationAuditService.IsUsableDraft(record, correlationId, 8, "LeanStartup", now));
        Assert.False(AiGenerationAuditService.IsUsableDraft(record, correlationId, 7, "BPM", now));

        record.Status = "Superseded";
        Assert.False(AiGenerationAuditService.IsUsableDraft(record, correlationId, 7, "LeanStartup", now));
    }

    [Fact]
    public void DraftPolicy_RejectsExpiredOrConsumedGenerationId()
    {
        var now = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();
        var record = ValidDraft(correlationId, now);
        record.ExpiresAt = now.AddSeconds(-1);
        Assert.False(AiGenerationAuditService.IsUsableDraft(record, correlationId, 7, "LeanStartup", now));

        record.ExpiresAt = now.AddMinutes(5);
        record.ConsumedAt = now;
        Assert.False(AiGenerationAuditService.IsUsableDraft(record, correlationId, 7, "LeanStartup", now));
    }

    private static AiScenarioOptionContent ValidOption() => new()
    {
        OptionType = "Hypothesis",
        Text = "Validar el problema con entrevistas estructuradas.",
        IsBestOption = true,
        Rationale = "Obtiene evidencia antes de invertir en el producto.",
        Impact = new() { ["validatedLearning"] = 5 },
        Tags = new() { "estrategia", "aprendizaje", "ESTRATEGIA" },
        Cost = 10,
        TimeCost = 2,
        RiskImpact = -3,
        MaxSelections = 1,
        ExpectedImpactLevel = "Alto",
        ExpectedEffortLevel = "Medio",
        ExpectedViabilityLevel = "Alta",
        OrderIndex = 1
    };

    private static AiGenerationRecord ValidDraft(Guid correlationId, DateTime now) => new()
    {
        CorrelationId = correlationId,
        RequestedByUserId = 7,
        MethodologyCode = "LeanStartup",
        OperationType = "ScenarioDraft",
        Status = "Succeeded",
        ExpiresAt = now.AddMinutes(5)
    };
}
