using SimuladorApi.Models;
using SimuladorApi.Services;
using SimuladorApi.Services.Ai;

namespace SimuladorApi.Tests;

public sealed class ScoringAndEvaluationTests
{
    [Theory]
    [InlineData(100, 100, 100, 100, 100, 100)]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(80, 70, 60, 50, 40, 62)]
    [InlineData(120, 120, 120, 120, 120, 100)]
    public void TextScore_IsCalculatedByBackend(
        decimal relevance,
        decimal reasoning,
        decimal evidence,
        decimal coherence,
        decimal clarity,
        decimal expected)
    {
        var score = AiTextEvaluationService.CalculateScore(new AiTextEvaluationContent
        {
            RelevanceScore = relevance,
            ReasoningScore = reasoning,
            EvidenceScore = evidence,
            CoherenceScore = coherence,
            ClarityScore = clarity,
            Confidence = 1
        });

        Assert.Equal(expected, score);
    }

    [Fact]
    public void OffTopicText_IsCapped()
    {
        var score = AiTextEvaluationService.CalculateScore(new AiTextEvaluationContent
        {
            RelevanceScore = 100,
            ReasoningScore = 100,
            EvidenceScore = 100,
            CoherenceScore = 100,
            ClarityScore = 100,
            IsOffTopic = true
        });

        Assert.Equal(20, score);
    }

    [Fact]
    public void InappropriateText_ReceivesZero()
    {
        var score = AiTextEvaluationService.CalculateScore(new AiTextEvaluationContent
        {
            RelevanceScore = 100,
            ReasoningScore = 100,
            EvidenceScore = 100,
            CoherenceScore = 100,
            ClarityScore = 100,
            IsInappropriate = true
        });

        Assert.Equal(0, score);
    }

    [Fact]
    public void UnavailableText_RenormalizesToSelectionCriteria()
    {
        var service = new ScoringService();
        var phase = PhaseSetting();

        var score = service.CombinePhaseScore(80, null, phase, false);

        Assert.Equal(80, score);
    }

    [Fact]
    public void AvailableText_UsesConfiguredWeights()
    {
        var service = new ScoringService();

        var score = service.CombinePhaseScore(80, 60, PhaseSetting(), true);

        Assert.Equal(74, score);
    }

    [Theory]
    [InlineData(1, 0, 100)]
    [InlineData(1, 1, 90)]
    [InlineData(0, 1, 0)]
    public void SelectionScoring_RemainsDeterministic(
        int correctSelected,
        int distractorsSelected,
        decimal expected)
    {
        var correct = new ScenarioOption { Id = 1, IsCorrect = true };
        var distractor = new ScenarioOption { Id = 2, IsCorrect = false };
        var selected = new List<ScenarioOption>();
        if (correctSelected > 0) selected.Add(correct);
        if (distractorsSelected > 0) selected.Add(distractor);

        var score = new ScoringService().CalculateSelectionScore(
            selected,
            new List<ScenarioOption> { correct, distractor });

        Assert.Equal(expected, score);
    }

    private static ScenarioPhaseSetting PhaseSetting() => new()
    {
        Criteria = new List<PhaseCriteriaSetting>
        {
            new() { EvaluationType = "Selection", CriterionWeight = 70 },
            new() { EvaluationType = "AIText", CriterionWeight = 30 }
        }
    };
}
