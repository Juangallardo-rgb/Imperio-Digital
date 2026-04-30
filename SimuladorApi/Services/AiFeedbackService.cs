namespace SimuladorApi.Services
{
    public class AiFeedbackService
    {
        public Task<(decimal Score, string Feedback)> EvaluateTextAnswerAsync(
            string phaseName,
            string studentAnswer)
        {
            if (string.IsNullOrWhiteSpace(studentAnswer))
            {
                return Task.FromResult((
                    40m,
                    $"En la fase {phaseName}, la respuesta escrita fue limitada. Se recomienda justificar mejor la decisión usando evidencia del caso."
                ));
            }

            if (studentAnswer.Length < 80)
            {
                return Task.FromResult((
                    65m,
                    $"En la fase {phaseName}, la respuesta presenta una idea inicial, pero necesita mayor profundidad, relación con el usuario y justificación."
                ));
            }

            return Task.FromResult((
                85m,
                $"En la fase {phaseName}, la respuesta demuestra una comprensión adecuada del problema, relaciona la decisión con el usuario y propone una justificación coherente."
            ));
        }

        public Task<string> GeneratePhaseFeedbackAsync(string phaseName, decimal score)
        {
            string feedback;

            if (score >= 85)
            {
                feedback = $"Excelente desempeño en la fase {phaseName}. La decisión tomada está bien alineada con Design Thinking y demuestra comprensión del usuario.";
            }
            else if (score >= 70)
            {
                feedback = $"Buen desempeño en la fase {phaseName}. Hay una base correcta, aunque se puede profundizar más en la relación entre evidencia, usuario y solución.";
            }
            else if (score >= 50)
            {
                feedback = $"Desempeño medio en la fase {phaseName}. Es necesario mejorar la selección de elementos clave y justificar mejor las decisiones.";
            }
            else
            {
                feedback = $"Desempeño bajo en la fase {phaseName}. Se recomienda revisar el propósito de esta fase dentro de Design Thinking antes de continuar.";
            }

            return Task.FromResult(feedback);
        }

        public Task<string> GenerateFinalFeedbackAsync(
            decimal finalScore,
            List<(string PhaseName, decimal Score)> phaseScores)
        {
            var strongest = phaseScores.OrderByDescending(p => p.Score).FirstOrDefault();
            var weakest = phaseScores.OrderBy(p => p.Score).FirstOrDefault();

            var feedback =
                $"El estudiante obtuvo un score final de {finalScore}. " +
                $"Su fase más fuerte fue {strongest.PhaseName} con {strongest.Score}, " +
                $"mientras que la fase que requiere mayor refuerzo fue {weakest.PhaseName} con {weakest.Score}. " +
                $"Como recomendación, debe fortalecer la conexión entre la evidencia del usuario, la definición del problema y la solución digital propuesta.";

            return Task.FromResult(feedback);
        }
    }
}