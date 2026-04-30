using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class ScoringService
    {
        public decimal CalculateSelectionScore(
            List<ScenarioOption> selectedOptions,
            List<ScenarioOption> allOptionsForQuestion)
        {
            var correctTotal = allOptionsForQuestion.Count(o => o.IsCorrect);

            if (correctTotal == 0)
                return 0;

            var correctSelected = selectedOptions.Count(o => o.IsCorrect);
            var distractorsSelected = selectedOptions.Count(o => !o.IsCorrect);

            var score = ((decimal)correctSelected / correctTotal) * 100 - (distractorsSelected * 10);

            if (score < 0)
                score = 0;

            if (score > 100)
                score = 100;

            return Math.Round(score, 2);
        }

        public decimal CombinePhaseScore(
            decimal selectionScore,
            decimal textScore,
            ScenarioPhaseSetting phaseSetting)
        {
            var selectionCriteriaWeight = phaseSetting.Criteria
                .Where(c => c.EvaluationType == "Selection")
                .Sum(c => c.CriterionWeight);

            var textCriteriaWeight = phaseSetting.Criteria
                .Where(c => c.EvaluationType == "AIText")
                .Sum(c => c.CriterionWeight);

            var score =
                (selectionScore * (selectionCriteriaWeight / 100)) +
                (textScore * (textCriteriaWeight / 100));

            if (score < 0)
                score = 0;

            if (score > 100)
                score = 100;

            return Math.Round(score, 2);
        }

        public decimal CalculateFinalScore(
            List<SimulationPhaseResponse> phaseResponses,
            List<ScenarioPhaseSetting> phaseSettings)
        {
            decimal finalScore = 0;

            foreach (var phase in phaseSettings)
            {
                var response = phaseResponses.FirstOrDefault(r => r.PhaseName == phase.PhaseName);

                if (response == null)
                    continue;

                finalScore += response.Score * (phase.PhaseWeight / 100);
            }

            if (finalScore < 0)
                finalScore = 0;

            if (finalScore > 100)
                finalScore = 100;

            return Math.Round(finalScore, 2);
        }
    }
}