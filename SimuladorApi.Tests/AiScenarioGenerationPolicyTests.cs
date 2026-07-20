using SimuladorApi.Models;
using SimuladorApi.Services;
using SimuladorApi.Services.Ai;

namespace SimuladorApi.Tests;

public sealed class AiScenarioGenerationPolicyTests
{
    [Theory]
    [InlineData("DesignThinking", "Empatizar", 4, 4, 3)]
    [InlineData("DesignThinking", "Definir", 3, 3, 2)]
    [InlineData("DesignThinking", "Idear", 3, 3, 2)]
    [InlineData("DesignThinking", "Prototipar", 3, 3, 2)]
    [InlineData("DesignThinking", "Evaluar", 3, 3, 2)]
    [InlineData("BPM", "Identificar proceso", 4, 4, 3)]
    [InlineData("BPM", "Modelar proceso actual", 3, 4, 2)]
    [InlineData("BPM", "Analizar cuellos de botella", 3, 4, 2)]
    [InlineData("BPM", "Rediseñar proceso", 3, 4, 2)]
    [InlineData("BPM", "Monitorear indicadores", 3, 4, 2)]
    [InlineData("DigitalMaturity", "Diagnóstico inicial", 3, 4, 2)]
    [InlineData("DigitalMaturity", "Evaluar capacidades", 3, 4, 2)]
    [InlineData("DigitalMaturity", "Priorizar brechas", 3, 4, 2)]
    [InlineData("DigitalMaturity", "Plan de transformación", 3, 4, 2)]
    [InlineData("DigitalMaturity", "Seguimiento de madurez", 3, 4, 2)]
    [InlineData("LeanStartup", "Hipótesis", 3, 4, 2)]
    [InlineData("LeanStartup", "MVP", 3, 4, 2)]
    [InlineData("LeanStartup", "Medición", 3, 4, 2)]
    [InlineData("LeanStartup", "Aprendizaje", 3, 4, 2)]
    [InlineData("LeanStartup", "Pivote o perseverancia", 3, 4, 2)]
    public void Policies_RestoreWorkingSimulationLimits(
        string methodology,
        string phase,
        int optionCount,
        int maxSelections,
        int correctCount)
    {
        var policy = AiScenarioGenerationPolicy.GetRequired(methodology, phase);

        Assert.Equal(optionCount, policy.ExpectedOptionCount);
        Assert.Equal(maxSelections, policy.MaxSelections);
        Assert.Equal(correctCount, policy.ExpectedCorrectCount);
    }

    [Theory]
    [InlineData("DesignThinking", "Empatizar|Definir|Idear|Prototipar|Evaluar")]
    [InlineData("BPM", "Identificar proceso|Modelar proceso actual|Analizar cuellos de botella|Rediseñar proceso|Monitorear indicadores")]
    [InlineData("DigitalMaturity", "Diagnóstico inicial|Evaluar capacidades|Priorizar brechas|Plan de transformación|Seguimiento de madurez")]
    [InlineData("LeanStartup", "Hipótesis|MVP|Medición|Aprendizaje|Pivote o perseverancia")]
    public void CanonicalOptions_AlwaysHaveAFeasibleCorrectPath(
        string methodologyCode,
        string joinedPhases)
    {
        var methodology = new Methodology
        {
            Code = methodologyCode,
            Phases = joinedPhases.Split('|').Select((phaseName, index) => new MethodologyPhase
            {
                Id = index + 1,
                Name = phaseName,
                PhaseOrder = index + 1,
                IsActive = true
            }).ToList()
        };
        var options = methodology.Phases.SelectMany(phase =>
        {
            var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phase.Name);
            return policy.Options.Select((resource, index) => new ScenarioOption
            {
                MethodologyPhaseId = phase.Id,
                PhaseName = phase.Name,
                OrderIndex = index + 1,
                IsCorrect = resource.IsCorrect,
                Score = resource.IsCorrect ? 100 : 0,
                Cost = resource.Cost,
                TimeCost = resource.TimeCost,
                RiskImpact = resource.RiskImpact,
                MaxSelections = policy.MaxSelections
            });
        }).ToList();

        var result = new AiScenarioContentValidator().ValidateCoverage(methodology, options);

        Assert.True(result.IsValid, string.Join(" | ", result.Errors));
    }

    [Theory]
    [InlineData("DesignThinking")]
    [InlineData("BPM")]
    [InlineData("DigitalMaturity")]
    [InlineData("LeanStartup")]
    public void ExplicitTemplates_UseTheSameSimulationPolicy(string methodologyCode)
    {
        var options = new ScenarioOptionTemplateService()
            .GenerateBaseOptions(0, methodologyCode);

        foreach (var phaseGroup in options.GroupBy(option => option.PhaseName))
        {
            var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phaseGroup.Key);
            var phaseOptions = phaseGroup.OrderBy(option => option.OrderIndex).ToList();
            Assert.Equal(policy.ExpectedOptionCount, phaseOptions.Count);
            Assert.All(phaseOptions, option => Assert.Equal(policy.MaxSelections, option.MaxSelections));
            Assert.Equal(policy.ExpectedCorrectCount, phaseOptions.Count(option => option.IsCorrect));
            Assert.All(phaseOptions, option =>
            {
                var expected = policy.GetOption(option.OrderIndex);
                Assert.Equal(expected.Cost, option.Cost);
                Assert.Equal(expected.TimeCost, option.TimeCost);
                Assert.Equal(expected.RiskImpact, option.RiskImpact);
            });
        }
    }
}
