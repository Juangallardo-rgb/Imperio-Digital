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
            decimal? textScore,
            ScenarioPhaseSetting phaseSetting,
            bool isTextEvaluationAvailable = true)
        {
            var selectionCriteriaWeight = phaseSetting.Criteria
                .Where(c => c.EvaluationType == "Selection")
                .Sum(c => c.CriterionWeight);

            var textCriteriaWeight = phaseSetting.Criteria
                .Where(c => c.EvaluationType == "AIText")
                .Sum(c => c.CriterionWeight);

            // Si OpenRouter no está disponible, la respuesta se conserva y la fase se
            // calcula únicamente con los criterios realmente evaluados, renormalizados.
            var availableTextWeight = isTextEvaluationAvailable && textScore.HasValue
                ? textCriteriaWeight
                : 0;
            var availableWeight = selectionCriteriaWeight + availableTextWeight;
            if (availableWeight <= 0)
                return 0;

            var score =
                (selectionScore * (selectionCriteriaWeight / availableWeight)) +
                ((textScore ?? 0) * (availableTextWeight / availableWeight));

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
