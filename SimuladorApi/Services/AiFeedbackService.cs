namespace SimuladorApi.Services
{
    public class AiFeedbackService
    {
        public Task<(decimal Score, string Feedback)> EvaluateTextAnswerAsync(
            string phaseName,
            string studentAnswer,
            string methodologyCode = "DesignThinking")
        {
            var methodologyName = GetMethodologyName(methodologyCode);

            if (string.IsNullOrWhiteSpace(studentAnswer))
            {
                return Task.FromResult((
                    40m,
                    $"En la fase {phaseName}, la respuesta escrita fue limitada. Se recomienda justificar mejor la decisión usando evidencia del caso y el enfoque de {methodologyName}."
                ));
            }

            if (studentAnswer.Length < 80)
            {
                return Task.FromResult((
                    65m,
                    $"En la fase {phaseName}, la respuesta presenta una idea inicial, pero necesita mayor profundidad, relación con el problema y justificación metodológica."
                ));
            }

            return Task.FromResult((
                85m,
                $"En la fase {phaseName}, la respuesta demuestra comprensión adecuada del caso, conecta la decisión con la metodología {methodologyName} y presenta una justificación coherente."
            ));
        }

        public Task<string> GeneratePhaseFeedbackAsync(
            string phaseName,
            decimal score,
            string methodologyCode = "DesignThinking")
        {
            var methodologyName = GetMethodologyName(methodologyCode);

            string feedback;

            if (score >= 85)
            {
                feedback = $"Excelente desempeño en la fase {phaseName}. La decisión está bien alineada con {methodologyName} y demuestra análisis estratégico.";
            }
            else if (score >= 70)
            {
                feedback = $"Buen desempeño en la fase {phaseName}. Hay una base correcta, aunque se puede profundizar más la relación entre evidencia, decisión y metodología.";
            }
            else if (score >= 50)
            {
                feedback = $"Desempeño medio en la fase {phaseName}. Es necesario mejorar la selección de elementos clave y justificar mejor las decisiones.";
            }
            else
            {
                feedback = $"Desempeño bajo en la fase {phaseName}. Se recomienda revisar el propósito de esta fase dentro de {methodologyName} antes de continuar.";
            }

            return Task.FromResult(feedback);
        }

        public Task<string> GenerateFinalFeedbackAsync(
            decimal finalScore,
            List<(string PhaseName, decimal Score)> phaseScores,
            string methodologyCode = "DesignThinking")
        {
            var methodologyName = GetMethodologyName(methodologyCode);

            var strongest = phaseScores.OrderByDescending(p => p.Score).FirstOrDefault();
            var weakest = phaseScores.OrderBy(p => p.Score).FirstOrDefault();

            var feedback =
                $"El estudiante obtuvo un score final de {finalScore} aplicando {methodologyName}. " +
                $"Su fase más fuerte fue {strongest.PhaseName} con {strongest.Score}, " +
                $"mientras que la fase que requiere mayor refuerzo fue {weakest.PhaseName} con {weakest.Score}. " +
                $"Como recomendación, debe fortalecer la coherencia entre diagnóstico, decisiones, evidencia y resultados esperados.";

            return Task.FromResult(feedback);
        }

        private static string GetMethodologyName(string methodologyCode)
        {
            return methodologyCode switch
            {
                "BPM" => "Business Process Management",
                "DigitalMaturity" => "Madurez Digital",
                "LeanStartup" => "Lean Startup",
                _ => "Design Thinking"
            };
        }
    }
}