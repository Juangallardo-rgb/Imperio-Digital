using System.Text.Json;
using SimuladorApi.Models;
using SimuladorApi.Services;
using SimuladorApi.Services.Ai;

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
