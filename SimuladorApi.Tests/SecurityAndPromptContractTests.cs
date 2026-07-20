using System.Text.Json;
using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;
using SimuladorApi.Services.Ai;

namespace SimuladorApi.Tests;

public sealed class SecurityAndPromptContractTests
{
    [Fact]
    public void StudentExecutionDto_DoesNotExposeCorrectness()
    {
        var properties = typeof(ScenarioExecutionOptionDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("IsCorrect", properties);
    }

    [Fact]
    public void StudentExecutionDto_DoesNotExposeScore()
    {
        var properties = typeof(ScenarioExecutionOptionDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Score", properties);
    }

    [Theory]
    [InlineData("DesignThinking", "Empatizar", "Evidence", "Evidencias")]
    [InlineData("BPM", "Identificar proceso", "ProcessEvidence", "Proceso crítico")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial", "MaturityEvidence", "Estrategia")]
    [InlineData("LeanStartup", "Hipótesis", "Hypothesis", "propuesta de valor")]
    public void MethodologyPrompt_UsesItsOwnPhaseGuidance(
        string methodology,
        string phaseName,
        string optionType,
        string expectedGuidance)
    {
        var builder = new AiScenarioPromptBuilder();
        var scenario = new Scenario
        {
            Methodology = methodology,
            CompanyType = "Empresa de prueba",
            Description = "Contexto empresarial suficiente para la prueba.",
            Problem = "Problema comprobable del caso.",
            TargetUser = "Usuario del proceso",
            Constraints = "Presupuesto y tiempo limitados"
        };
        var phase = new MethodologyPhase { Name = phaseName };

        var prompt = methodology switch
        {
            "DesignThinking" => builder.BuildDesignThinkingPhasePrompt(scenario, phase, new[] { optionType }),
            "BPM" => builder.BuildBpmPhasePrompt(scenario, phase, new[] { optionType }),
            "DigitalMaturity" => builder.BuildDigitalMaturityPhasePrompt(scenario, phase, new[] { optionType }),
            _ => builder.BuildLeanStartupPhasePrompt(scenario, phase, new[] { optionType })
        };

        Assert.Contains(expectedGuidance, prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"exactamente \"{phaseName}\"", prompt);
    }

    [Fact]
    public void InitialAndRepairPrompts_UseTheSameOptionContract()
    {
        var builder = new AiScenarioPromptBuilder();
        var scenario = Scenario("LeanStartup");
        var phase = new MethodologyPhase { Name = "Hipótesis" };
        var optionTypes = new[] { "Hypothesis", "CriticalAssumption" };
        var initial = builder.BuildLeanStartupPhasePrompt(scenario, phase, optionTypes);
        var repair = builder.BuildRepairPrompt(
            scenario,
            "LeanStartup",
            phase,
            optionTypes,
            new[] { "orderIndex debe comenzar en 1." });

        foreach (var field in OptionContractFields())
        {
            Assert.Contains($"\"{field}\"", initial);
            Assert.Contains($"\"{field}\"", repair);
        }
        Assert.Contains("validatedLearning", repair);
        Assert.DoesNotContain("plantilla", repair, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonSchemaAndDto_HaveEquivalentOptionFields()
    {
        var schema = AiScenarioJsonSchemas.BuildPhaseOptions(
            "LeanStartup",
            "Hipótesis",
            new[] { "Hypothesis" },
            new[] { "validatedLearning" });
        var schemaFields = schema
            .GetProperty("properties")
            .GetProperty("options")
            .GetProperty("items")
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var dtoFields = typeof(AiScenarioOptionContent)
            .GetProperties()
            .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(dtoFields.Order(), schemaFields.Order());
        Assert.Equal(
            "Hipótesis",
            schema.GetProperty("properties").GetProperty("phaseName").GetProperty("enum")[0].GetString());
    }

    [Fact]
    public void DraftSchema_UsesValidatorLimitsAndCanonicalMethodology()
    {
        var schema = AiScenarioJsonSchemas.BuildDraft("LeanStartup");
        var properties = schema.GetProperty("properties");

        Assert.Equal(8, properties.GetProperty("title").GetProperty("minLength").GetInt32());
        Assert.Equal(160, properties.GetProperty("title").GetProperty("maxLength").GetInt32());
        Assert.Equal(40, properties.GetProperty("description").GetProperty("minLength").GetInt32());
        Assert.Equal(
            "LeanStartup",
            properties.GetProperty("methodologyCode").GetProperty("enum")[0].GetString());
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void Schema_UsesExplicitLevelConventionsAndKpiKeys()
    {
        var schema = AiScenarioJsonSchemas.BuildPhaseOptions(
            "LeanStartup",
            "Hipótesis",
            new[] { "Hypothesis" },
            new[] { "validatedLearning" });
        var properties = schema.GetProperty("properties").GetProperty("options")
            .GetProperty("items").GetProperty("properties");

        Assert.Equal(
            new[] { "Alta", "Media", "Baja" },
            properties.GetProperty("expectedViabilityLevel").GetProperty("enum")
                .EnumerateArray().Select(value => value.GetString()));
        Assert.True(properties.GetProperty("impact").GetProperty("properties")
            .TryGetProperty("validatedLearning", out _));
        Assert.False(properties.GetProperty("impact").GetProperty("additionalProperties").GetBoolean());
    }

    [Theory]
    [InlineData("DesignThinking", "Empatizar|Definir|Idear|Prototipar|Evaluar")]
    [InlineData("BPM", "Identificar proceso|Modelar proceso actual|Analizar cuellos de botella|Rediseñar proceso|Monitorear indicadores")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial|Evaluar capacidades|Priorizar brechas|Plan de transformación|Seguimiento de madurez")]
    [InlineData("LeanStartup", "Hipótesis|MVP|Medición|Aprendizaje|Pivote o perseverancia")]
    public void EveryMethodology_HasExactlyItsFiveSupportedPhases(string methodology, string joinedPhases)
    {
        var validator = new AiScenarioContentValidator();
        var phases = joinedPhases.Split('|');

        Assert.Equal(5, phases.Length);
        Assert.All(phases, phase => Assert.NotEmpty(validator.GetAllowedOptionTypes(methodology, phase)));
    }

    private static Scenario Scenario(string methodology) => new()
    {
        Methodology = methodology,
        CompanyType = "Empresa de prueba",
        Description = "Contexto empresarial suficiente para la prueba.",
        Problem = "Problema comprobable del caso.",
        TargetUser = "Usuario del proceso",
        Constraints = "Presupuesto y tiempo limitados"
    };

    private static string[] OptionContractFields() =>
    [
        "phaseName", "options", "optionType", "text", "isBestOption", "rationale",
        "impact", "tags", "cost", "timeCost", "riskImpact", "maxSelections",
        "expectedImpactLevel", "expectedEffortLevel", "expectedViabilityLevel", "orderIndex"
    ];
}
