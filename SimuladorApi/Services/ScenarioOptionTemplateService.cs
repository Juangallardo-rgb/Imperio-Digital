using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class ScenarioOptionTemplateService
    {
        public List<ScenarioOption> GenerateBaseOptions(int scenarioId, string methodologyCode)
        {
            var options = methodologyCode switch
            {
                "BPM" => GenerateBpmOptions(scenarioId),
                "DigitalMaturity" => GenerateDigitalMaturityOptions(scenarioId),
                "LeanStartup" => GenerateLeanStartupOptions(scenarioId),
                _ => GenerateDesignThinkingOptions(scenarioId)
            };

            ApplySimulationPolicy(options, methodologyCode);
            return options;
        }

        private static void ApplySimulationPolicy(
            IReadOnlyCollection<ScenarioOption> options,
            string methodologyCode)
        {
            foreach (var phaseGroup in options.GroupBy(option => option.PhaseName, StringComparer.Ordinal))
            {
                var policy = Ai.AiScenarioGenerationPolicy.GetRequired(methodologyCode, phaseGroup.Key);
                var phaseOptions = phaseGroup.OrderBy(option => option.OrderIndex).ToList();
                if (phaseOptions.Count != policy.ExpectedOptionCount)
                {
                    throw new InvalidOperationException(
                        $"La plantilla {methodologyCode}/{phaseGroup.Key} no coincide con la política de simulación.");
                }

                foreach (var option in phaseOptions)
                {
                    var optionPolicy = policy.GetOption(option.OrderIndex);
                    option.IsCorrect = optionPolicy.IsCorrect;
                    option.Score = optionPolicy.IsCorrect ? 100 : 0;
                    option.Cost = optionPolicy.Cost;
                    option.TimeCost = optionPolicy.TimeCost;
                    option.RiskImpact = optionPolicy.RiskImpact;
                    option.MaxSelections = policy.MaxSelections;
                }
            }
        }

        private static ScenarioOption Option(
            int scenarioId,
            string phaseName,
            string optionType,
            string text,
            bool isCorrect,
            int orderIndex,
            string tagsJson,
            int maxSelections,
            decimal score = 100,
            string impactJson = "",
            decimal cost = 0,
            decimal timeCost = 0,
            decimal riskImpact = 0,
            string impactLevel = "",
            string effortLevel = "",
            string viabilityLevel = "")
        {
            return new ScenarioOption
            {
                ScenarioId = scenarioId,
                PhaseName = phaseName,
                OptionType = optionType,
                Text = text,
                IsCorrect = isCorrect,
                Score = isCorrect ? score : 0,
                ImpactJson = impactJson,
                OrderIndex = orderIndex,
                TagsJson = tagsJson,
                MaxSelections = maxSelections,
                Cost = cost,
                TimeCost = timeCost,
                RiskImpact = riskImpact,
                ExpectedImpactLevel = impactLevel,
                ExpectedEffortLevel = effortLevel,
                ExpectedViabilityLevel = viabilityLevel
            };
        }

        private static List<ScenarioOption> GenerateDesignThinkingOptions(int scenarioId)
        {
            return new List<ScenarioOption>
            {
                Option(scenarioId, "Empatizar", "Evidence",
                    "Los usuarios reportan fricción, falta de claridad y pérdida de confianza durante el proceso digital.",
                    true, 1, "[\"ux\",\"trust\",\"friction\",\"user\"]", 4, riskImpact: -2),

                Option(scenarioId, "Empatizar", "Evidence",
                    "Existen señales de abandono cuando el usuario no comprende costos, tiempos o pasos requeridos.",
                    true, 2, "[\"abandonment\",\"clarity\",\"conversion\"]", 4, riskImpact: -2),

                Option(scenarioId, "Empatizar", "Evidence",
                    "La observación del flujo revela pasos confusos, mensajes ambiguos y campos innecesarios.",
                    true, 3, "[\"user-flow\",\"friction\",\"ux\"]", 4, riskImpact: -1),

                Option(scenarioId, "Empatizar", "Evidence",
                    "El problema principal es que la marca necesita colores más modernos.",
                    false, 4, "[\"branding\"]", 4, riskImpact: 5),

                Option(scenarioId, "Definir", "ProblemStatement",
                    "El usuario necesita un proceso digital claro, simple y confiable porque la fricción actual reduce la conversión y satisfacción.",
                    true, 1, "[\"ux\",\"trust\",\"conversion\"]", 2, riskImpact: -2),

                Option(scenarioId, "Definir", "ProblemStatement",
                    "El usuario necesita comprender mejor los pasos del proceso porque la falta de información genera abandono.",
                    true, 2, "[\"clarity\",\"friction\",\"user\"]", 2, riskImpact: -2),

                Option(scenarioId, "Definir", "ProblemStatement",
                    "La empresa necesita publicar más contenido institucional en redes sociales.",
                    false, 3, "[\"social-media\"]", 2, riskImpact: 6),

                Option(scenarioId, "Idear", "Solution",
                    "Simplificar el flujo principal, reducir pasos innecesarios y mostrar información crítica desde el inicio.",
                    true, 1, "[\"ux\",\"friction\",\"conversion\"]", 3, 100,
                    "{\"cartAbandonment\":-8,\"conversionRate\":1.2,\"satisfaction\":8,\"purchaseTime\":-1,\"digitalAdoption\":5}",
                    35, 3, 8, "Alto", "Medio", "Alta"),

                Option(scenarioId, "Idear", "Solution",
                    "Agregar confirmaciones, resumen de información y mensajes de confianza en los puntos críticos.",
                    true, 2, "[\"trust\",\"clarity\",\"satisfaction\"]", 3, 100,
                    "{\"cartAbandonment\":-5,\"conversionRate\":0.7,\"satisfaction\":7,\"purchaseTime\":-0.4,\"digitalAdoption\":4}",
                    25, 2, 5, "Medio", "Bajo", "Alta"),

                Option(scenarioId, "Idear", "Solution",
                    "Construir una aplicación móvil completa antes de validar el problema.",
                    false, 3, "[\"high-cost\",\"overbuilding\"]", 3, 0,
                    "{\"cartAbandonment\":-1,\"conversionRate\":0.1,\"satisfaction\":1,\"purchaseTime\":0,\"digitalAdoption\":2}",
                    90, 10, 25, "Bajo", "Alto", "Baja"),

                Option(scenarioId, "Prototipar", "PrototypeFeature",
                    "Pantalla mínima con resumen, pasos claros, acción principal visible y confirmación final.",
                    true, 1, "[\"prototype\",\"ux\",\"clarity\"]", 4, 100,
                    "{\"cartAbandonment\":-4,\"conversionRate\":0.6,\"satisfaction\":5,\"purchaseTime\":-0.5,\"digitalAdoption\":4}",
                    20, 2, 5),

                Option(scenarioId, "Prototipar", "PrototypeFeature",
                    "Formulario reducido con solo los datos necesarios para completar la acción principal.",
                    true, 2, "[\"form\",\"friction\",\"ux\"]", 4, 100,
                    "{\"cartAbandonment\":-3,\"conversionRate\":0.5,\"satisfaction\":4,\"purchaseTime\":-0.8,\"digitalAdoption\":3}",
                    20, 2, 5),

                Option(scenarioId, "Prototipar", "PrototypeFeature",
                    "Rediseño visual completo sin validar el flujo del usuario.",
                    false, 3, "[\"branding\"]", 4, 0,
                    "{\"cartAbandonment\":0,\"conversionRate\":0,\"satisfaction\":1,\"purchaseTime\":0,\"digitalAdoption\":0}",
                    35, 3, 8),

                Option(scenarioId, "Evaluar", "KPI",
                    "Tasa de abandono del proceso digital.",
                    true, 1, "[\"cartAbandonment\",\"conversion\"]", 3),

                Option(scenarioId, "Evaluar", "KPI",
                    "Satisfacción del usuario y tiempo para completar la acción.",
                    true, 2, "[\"satisfaction\",\"purchaseTime\"]", 3),

                Option(scenarioId, "Evaluar", "KPI",
                    "Cantidad de colores nuevos agregados al sitio.",
                    false, 3, "[\"branding\"]", 3)
            };
        }

        private static List<ScenarioOption> GenerateBpmOptions(int scenarioId)
        {
            return new List<ScenarioOption>
            {
                Option(scenarioId, "Identificar proceso", "ProcessEvidence",
                    "El proceso presenta demoras recurrentes entre la solicitud inicial y la respuesta final.",
                    true, 1, "[\"process-delay\",\"cycleTime\",\"bpm\"]", 4, riskImpact: -2),

                Option(scenarioId, "Identificar proceso", "ProcessEvidence",
                    "Existen responsables múltiples sin claridad de aprobación ni trazabilidad.",
                    true, 2, "[\"roles\",\"approval\",\"traceability\"]", 4, riskImpact: -2),

                Option(scenarioId, "Identificar proceso", "ProcessEvidence",
                    "Las transferencias entre áreas generan esperas, retrabajo y pérdida de información.",
                    true, 3, "[\"handoff\",\"rework\",\"waiting-time\"]", 4, riskImpact: -2),

                Option(scenarioId, "Identificar proceso", "ProcessEvidence",
                    "La marca necesita una paleta de colores más moderna.",
                    false, 4, "[\"branding\"]", 4, riskImpact: 5),

                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep",
                    "Solicitud recibida → revisión manual → aprobación interna → respuesta → registro final.",
                    true, 1, "[\"as-is\",\"manual-review\",\"approval\"]", 4, riskImpact: -2),

                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep",
                    "El flujo actual depende de validaciones manuales, correos y pasos no estandarizados.",
                    true, 2, "[\"manual-process\",\"handoff\",\"as-is\"]", 4, riskImpact: -2),

                Option(scenarioId, "Modelar proceso actual", "CurrentProcessStep",
                    "Cliente ve publicidad → revisa redes sociales → cambia colores de interfaz.",
                    false, 3, "[\"marketing\"]", 4, riskImpact: 6),

                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck",
                    "La aprobación manual concentra retrasos porque depende de una sola persona.",
                    true, 1, "[\"bottleneck\",\"approval\",\"delay\"]", 4, riskImpact: -3),

                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck",
                    "La falta de trazabilidad impide saber en qué etapa se encuentra cada solicitud.",
                    true, 2, "[\"traceability\",\"visibility\",\"process-control\"]", 4, riskImpact: -3),

                Option(scenarioId, "Analizar cuellos de botella", "Bottleneck",
                    "El problema principal es que el sitio web no tiene suficientes imágenes.",
                    false, 3, "[\"visual-design\"]", 4, riskImpact: 6),

                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement",
                    "Automatizar estados y notificaciones para reducir consultas manuales.",
                    true, 1, "[\"automation\",\"traceability\",\"efficiency\"]", 4, 100,
                    "{\"processEfficiency\":12,\"cycleTime\":-2,\"errorRate\":-4,\"satisfaction\":6,\"digitalAdoption\":5}",
                    35, 3, 8, "Alto", "Medio", "Alta"),

                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement",
                    "Eliminar pasos duplicados y definir responsables por etapa.",
                    true, 2, "[\"simplification\",\"roles\",\"efficiency\"]", 4, 100,
                    "{\"processEfficiency\":15,\"cycleTime\":-3,\"errorRate\":-3,\"satisfaction\":5,\"digitalAdoption\":3}",
                    25, 2, 6, "Alto", "Bajo", "Alta"),

                Option(scenarioId, "Rediseñar proceso", "ProcessImprovement",
                    "Agregar una campaña publicitaria sin cambiar el proceso interno.",
                    false, 3, "[\"marketing\"]", 4, 0,
                    "{\"processEfficiency\":0,\"cycleTime\":0,\"errorRate\":0,\"satisfaction\":1,\"digitalAdoption\":0}",
                    30, 2, 10, "Bajo", "Medio", "Baja"),

                Option(scenarioId, "Monitorear indicadores", "KPI",
                    "Tiempo de ciclo del proceso.",
                    true, 1, "[\"cycleTime\",\"process-efficiency\"]", 3),

                Option(scenarioId, "Monitorear indicadores", "KPI",
                    "Tasa de errores o reprocesos.",
                    true, 2, "[\"errorRate\",\"quality\"]", 3),

                Option(scenarioId, "Monitorear indicadores", "KPI",
                    "Cantidad de publicaciones en redes sociales.",
                    false, 3, "[\"social-media\"]", 3)
            };
        }

        private static List<ScenarioOption> GenerateDigitalMaturityOptions(int scenarioId)
        {
            return new List<ScenarioOption>
            {
                Option(scenarioId, "Diagnóstico inicial", "CurrentState",
                    "La organización usa herramientas digitales aisladas sin integración entre áreas.",
                    true, 1, "[\"digital-tools\",\"integration\",\"maturity\"]", 4, riskImpact: -2),

                Option(scenarioId, "Diagnóstico inicial", "CurrentState",
                    "Los datos se registran manualmente y no se usan para tomar decisiones.",
                    true, 2, "[\"data\",\"manual-process\",\"decision-making\"]", 4, riskImpact: -2),

                Option(scenarioId, "Diagnóstico inicial", "CurrentState",
                    "La empresa tiene pocos seguidores en redes sociales.",
                    false, 3, "[\"social-media\"]", 4, riskImpact: 5),

                Option(scenarioId, "Evaluar capacidades", "Capability",
                    "Procesos: bajo nivel de automatización y alta dependencia de tareas manuales.",
                    true, 1, "[\"processes\",\"automation\",\"manual-work\"]", 5, riskImpact: -2),

                Option(scenarioId, "Evaluar capacidades", "Capability",
                    "Datos: ausencia de indicadores consolidados para medir desempeño.",
                    true, 2, "[\"data\",\"analytics\",\"kpi\"]", 5, riskImpact: -2),

                Option(scenarioId, "Evaluar capacidades", "Capability",
                    "Imagen: necesidad de modernizar la identidad visual.",
                    false, 3, "[\"branding\"]", 5, riskImpact: 5),

                Option(scenarioId, "Priorizar brechas", "Gap",
                    "Brecha crítica: falta de integración entre sistemas y procesos clave.",
                    true, 1, "[\"integration-gap\",\"systems\",\"processes\"]", 4, riskImpact: -3),

                Option(scenarioId, "Priorizar brechas", "Gap",
                    "Brecha crítica: baja cultura de uso de datos para la toma de decisiones.",
                    true, 2, "[\"data-culture\",\"analytics\",\"decision-making\"]", 4, riskImpact: -3),

                Option(scenarioId, "Priorizar brechas", "Gap",
                    "Brecha prioritaria: cambiar el logo corporativo.",
                    false, 3, "[\"branding\"]", 4, riskImpact: 6),

                Option(scenarioId, "Plan de transformación", "TransformationInitiative",
                    "Implementar tablero de indicadores para seguimiento de procesos críticos.",
                    true, 1, "[\"dashboard\",\"data\",\"kpi\"]", 4, 100,
                    "{\"digitalMaturity\":10,\"processEfficiency\":6,\"dataUsage\":12,\"satisfaction\":4,\"digitalAdoption\":8}",
                    35, 3, 8, "Alto", "Medio", "Alta"),

                Option(scenarioId, "Plan de transformación", "TransformationInitiative",
                    "Automatizar un proceso operativo prioritario con alto volumen de tareas repetitivas.",
                    true, 2, "[\"automation\",\"processes\",\"digital-adoption\"]", 4, 100,
                    "{\"digitalMaturity\":12,\"processEfficiency\":10,\"dataUsage\":4,\"satisfaction\":5,\"digitalAdoption\":7}",
                    45, 4, 10, "Alto", "Medio", "Media"),

                Option(scenarioId, "Plan de transformación", "TransformationInitiative",
                    "Invertir todo el presupuesto en publicidad digital sin cambiar capacidades internas.",
                    false, 3, "[\"marketing\"]", 4, 0,
                    "{\"digitalMaturity\":1,\"processEfficiency\":0,\"dataUsage\":0,\"satisfaction\":1,\"digitalAdoption\":1}",
                    50, 3, 12, "Bajo", "Medio", "Baja"),

                Option(scenarioId, "Seguimiento de madurez", "KPI",
                    "Nivel de madurez digital.",
                    true, 1, "[\"digitalMaturity\"]", 3),

                Option(scenarioId, "Seguimiento de madurez", "KPI",
                    "Porcentaje de procesos digitalizados y uso de datos.",
                    true, 2, "[\"digitalAdoption\",\"dataUsage\",\"processes\"]", 3),

                Option(scenarioId, "Seguimiento de madurez", "KPI",
                    "Cantidad de publicaciones en redes sociales.",
                    false, 3, "[\"social-media\"]", 3)
            };
        }

        private static List<ScenarioOption> GenerateLeanStartupOptions(int scenarioId)
        {
            return new List<ScenarioOption>
            {
                Option(scenarioId, "Hipótesis", "Hypothesis",
                    "Los usuarios abandonan porque no perciben suficiente valor antes de completar la acción principal.",
                    true, 1, "[\"value-proposition\",\"user-problem\",\"hypothesis\"]", 4, riskImpact: -2),

                Option(scenarioId, "Hipótesis", "Hypothesis",
                    "Si se reduce la fricción inicial, aumentará la conversión de usuarios interesados.",
                    true, 2, "[\"conversion\",\"friction\",\"experiment\"]", 4, riskImpact: -2),

                Option(scenarioId, "Hipótesis", "Hypothesis",
                    "El principal problema es que el logo no tiene colores modernos.",
                    false, 3, "[\"branding\"]", 4, riskImpact: 5),

                Option(scenarioId, "MVP", "MvpFeature",
                    "Crear una versión mínima que valide si el usuario completa la acción principal con menos pasos.",
                    true, 1, "[\"mvp\",\"conversion\",\"experiment\"]", 4, 100,
                    "{\"validatedLearning\":8,\"conversionRate\":0.8,\"satisfaction\":5,\"experimentVelocity\":6,\"digitalAdoption\":4}",
                    30, 2, 6, "Alto", "Bajo", "Alta"),

                Option(scenarioId, "MVP", "MvpFeature",
                    "Lanzar una prueba simple con usuarios reales para medir interés antes de construir todo.",
                    true, 2, "[\"experiment\",\"validatedLearning\",\"mvp\"]", 4, 100,
                    "{\"validatedLearning\":10,\"conversionRate\":0.6,\"satisfaction\":4,\"experimentVelocity\":8,\"digitalAdoption\":3}",
                    20, 2, 5, "Alto", "Bajo", "Alta"),

                Option(scenarioId, "MVP", "MvpFeature",
                    "Desarrollar la plataforma completa antes de validar si el usuario la necesita.",
                    false, 3, "[\"overbuilding\",\"high-risk\"]", 4, 0,
                    "{\"validatedLearning\":1,\"conversionRate\":0.1,\"satisfaction\":1,\"experimentVelocity\":-4,\"digitalAdoption\":2}",
                    90, 10, 25, "Bajo", "Alto", "Baja"),

                Option(scenarioId, "Medición", "Metric",
                    "Tasa de conversión del experimento.",
                    true, 1, "[\"conversionRate\",\"experiment\"]", 3),

                Option(scenarioId, "Medición", "Metric",
                    "Porcentaje de usuarios que completan la acción clave.",
                    true, 2, "[\"activation\",\"user-action\"]", 3),

                Option(scenarioId, "Medición", "Metric",
                    "Número de seguidores nuevos en redes sociales.",
                    false, 3, "[\"social-media\"]", 3),

                Option(scenarioId, "Aprendizaje", "Learning",
                    "La evidencia muestra si la hipótesis de valor fue validada o debe ajustarse.",
                    true, 1, "[\"validatedLearning\",\"hypothesis\",\"evidence\"]", 3, riskImpact: -2),

                Option(scenarioId, "Aprendizaje", "Learning",
                    "Los datos deben compararse con la hipótesis inicial para decidir el siguiente paso.",
                    true, 2, "[\"data\",\"experiment\",\"learning\"]", 3, riskImpact: -2),

                Option(scenarioId, "Aprendizaje", "Learning",
                    "La decisión debe tomarse únicamente por opinión del equipo interno.",
                    false, 3, "[\"opinion\"]", 3, riskImpact: 6),

                Option(scenarioId, "Pivote o perseverancia", "Decision",
                    "Perseverar si las métricas accionables demuestran adopción y valor real.",
                    true, 1, "[\"persevere\",\"metrics\",\"validatedLearning\"]", 2),

                Option(scenarioId, "Pivote o perseverancia", "Decision",
                    "Pivotar si la evidencia muestra que el problema o solución no generan suficiente valor.",
                    true, 2, "[\"pivot\",\"evidence\",\"learning\"]", 2),

                Option(scenarioId, "Pivote o perseverancia", "Decision",
                    "Ignorar los datos y continuar porque ya se invirtió tiempo en la idea.",
                    false, 3, "[\"bias\",\"opinion\"]", 2, riskImpact: 8)
            };
        }
    }
}
