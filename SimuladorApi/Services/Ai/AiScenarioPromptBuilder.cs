using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;

namespace SimuladorApi.Services.Ai;

public sealed class AiScenarioPromptBuilder
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> PhaseGuidance =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            ["DesignThinking"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Empatizar"] = "Evidencias, necesidades, comportamientos, dolores y hallazgos del usuario.",
                ["Definir"] = "Formulaciones del problema, síntomas frente a causas, necesidad e insight principal.",
                ["Idear"] = "Ideas de solución con impacto, esfuerzo y viabilidad.",
                ["Prototipar"] = "Componentes o módulos de un MVP, alcance mínimo y elementos prioritarios.",
                ["Evaluar"] = "Hallazgos de prueba, decisiones de mantener, iterar o descartar y aprendizajes."
            },
            ["BPM"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Identificar proceso"] = "Proceso crítico, actores, entradas, salidas y evidencias del problema.",
                ["Modelar proceso actual"] = "Actividades, secuencia, responsables, transferencias y estado actual.",
                ["Analizar cuellos de botella"] = "Retrasos, duplicidades, esperas, reprocesos y puntos críticos.",
                ["Rediseñar proceso"] = "Mejoras, automatización pertinente, actividades sin valor, nuevo flujo y responsabilidades.",
                ["Monitorear indicadores"] = "Tiempo, calidad, eficiencia, trazabilidad, satisfacción e indicadores verificables."
            },
            ["DigitalMaturity"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Diagnóstico inicial"] = "Estrategia, cultura, procesos, tecnología y cliente.",
                ["Evaluar capacidades"] = "Automatización, integración, analítica, experiencia de usuario, talento y gestión.",
                ["Priorizar brechas"] = "Brechas, impacto, urgencia, dependencias y riesgo.",
                ["Plan de transformación"] = "Iniciativas, prioridades, horizontes, responsables y recursos.",
                ["Seguimiento de madurez"] = "Indicadores, eficiencia, datos, adopción, satisfacción y evolución de capacidades."
            },
            ["LeanStartup"] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Hipótesis"] = "Cliente, problema, propuesta de valor y supuestos verificables.",
                ["MVP"] = "Componentes mínimos, experimento, alcance y aprendizaje que se necesita validar.",
                ["Medición"] = "Métricas accionables, evidencia, comportamiento, conversión y uso.",
                ["Aprendizaje"] = "Conclusiones basadas en evidencia, hipótesis confirmadas o rechazadas y aprendizajes.",
                ["Pivote o perseverancia"] = "Continuar, ajustar, cambiar segmento o propuesta y replantear el modelo."
            }
        };

    public string BuildDraftPrompt(
        GenerateScenarioDraftDto request,
        Methodology methodology)
    {
        var phaseList = string.Join(
            "; ",
            methodology.Phases
                .Where(phase => phase.IsActive)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => $"{phase.Name}: {phase.Description}"));

        return $"""
            Diseña un caso empresarial original para estudiantes universitarios de Negocios Digitales.
            Metodología obligatoria: {methodology.Code} ({methodology.Name}).
            Fases reales: {phaseList}.
            Tema sugerido: {ValueOrNotSpecified(request.Topic)}.
            Tipo de empresa sugerido: {ValueOrNotSpecified(request.CompanyType)}.
            Dificultad sugerida: {ValueOrNotSpecified(request.Difficulty)}.
            Instrucciones adicionales: {ValueOrNotSpecified(request.AdditionalInstructions)}.

            El problema debe poder resolverse mediante las fases indicadas y desarrollar habilidades
            prácticas. Varía el sector y el problema; evita reciclar casos prediseñados de restaurante,
            clínica, belleza o comercio electrónico. No generes opciones ni respuestas. Devuelve
            methodologyCode exactamente como "{methodology.Code}" y contenido concreto en español.
            Usa exactamente las claves title, description, companyType, problem, targetUser, constraints,
            difficulty, learningObjective y methodologyCode. Respeta los límites del esquema JSON.
            """;
    }

    public string BuildDraftRepairPrompt(
        GenerateScenarioDraftDto request,
        Methodology methodology,
        IReadOnlyCollection<string> errors) =>
        $"""
        Regenera el objeto completo del borrador. La respuesta anterior fue rechazada por estas reglas:
        {string.Join(" | ", errors.Take(12))}

        No omitas propiedades, no agregues propiedades y no traduzcas los nombres de las claves.
        {BuildDraftPrompt(request, methodology)}
        """;

    public string BuildDesignThinkingPhasePrompt(
        Scenario scenario,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes) =>
        BuildPhasePrompt(scenario, phase, optionTypes, "DesignThinking");

    public string BuildBpmPhasePrompt(
        Scenario scenario,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes) =>
        BuildPhasePrompt(scenario, phase, optionTypes, "BPM");

    public string BuildDigitalMaturityPhasePrompt(
        Scenario scenario,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes) =>
        BuildPhasePrompt(scenario, phase, optionTypes, "DigitalMaturity");

    public string BuildLeanStartupPhasePrompt(
        Scenario scenario,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes) =>
        BuildPhasePrompt(scenario, phase, optionTypes, "LeanStartup");

    public string BuildRepairPrompt(
        Scenario scenario,
        string methodologyCode,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes,
        IReadOnlyCollection<string> errors)
    {
        var guidance = PhaseGuidance[methodologyCode][phase.Name];
        return $$"""
            Regenera únicamente el contenido de la fase canónica "{{phase.Name}}" de la metodología
            {{methodologyCode}}. La respuesta anterior fue rechazada por estas causas:
            - {{string.Join("\n- ", errors)}}

            Enfoque pedagógico: {{guidance}}
            Empresa: {{scenario.CompanyType}}.
            Descripción: {{scenario.Description}}.
            Problema: {{scenario.Problem}}.
            Usuario objetivo: {{scenario.TargetUser}}.
            Restricciones: {{scenario.Constraints}}.

            {{BuildPhaseContract(methodologyCode, phase, optionTypes)}}
            Corrige todas las causas indicadas y devuelve únicamente el objeto JSON, sin markdown ni texto adicional.
            """;
    }

    private static string BuildPhasePrompt(
        Scenario scenario,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes,
        string methodologyCode)
    {
        var guidance = PhaseGuidance[methodologyCode][phase.Name];
        return $$"""
            Genera opciones evaluables para una simulación académica de {{methodologyCode}}.
            Fase canónica obligatoria: {{phase.Name}}.
            Enfoque de la fase: {{guidance}}
            Empresa: {{scenario.CompanyType}}.
            Descripción: {{scenario.Description}}.
            Problema: {{scenario.Problem}}.
            Usuario objetivo: {{scenario.TargetUser}}.
            Restricciones: {{scenario.Constraints}}.

            Genera al menos tres alternativas creíbles: una claramente adecuada, una parcialmente
            adecuada y una inadecuada pero plausible. No uses distractores absurdos.

            {{BuildPhaseContract(methodologyCode, phase, optionTypes)}}
            Devuelve únicamente el objeto JSON, sin markdown ni texto adicional.
            """;
    }

    public static string BuildPhaseContract(
        string methodologyCode,
        MethodologyPhase phase,
        IReadOnlyCollection<string> optionTypes)
    {
        var kpis = string.Join(", ", KpiSimulationService.GetAllowedKpiKeys(methodologyCode));
        var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phase.Name);
        var resourceContract = string.Join(
            Environment.NewLine,
            policy.Options.Select((option, index) =>
                $"- orderIndex {index + 1}: isBestOption={option.IsCorrect.ToString().ToLowerInvariant()}, " +
                $"cost={option.Cost}, timeCost={option.TimeCost}, riskImpact={option.RiskImpact}, " +
                $"maxSelections={policy.MaxSelections}."));
        var firstOption = policy.GetOption(1);
        return $$"""
            Contrato único obligatorio:
            {
              "phaseName": "{{phase.Name}}",
              "options": [
                {
                  "optionType": "uno de: {{string.Join(", ", optionTypes)}}",
                  "text": "decisión concreta y única",
                  "isBestOption": true,
                  "rationale": "explicación breve",
                  "impact": { "kpiPermitido": 5 },
                  "tags": ["etiqueta"],
                  "cost": {{firstOption.Cost}},
                  "timeCost": {{firstOption.TimeCost}},
                  "riskImpact": {{firstOption.RiskImpact}},
                  "maxSelections": {{policy.MaxSelections}},
                  "expectedImpactLevel": "Alto|Medio|Bajo",
                  "expectedEffortLevel": "Alto|Medio|Bajo",
                  "expectedViabilityLevel": "Alta|Media|Baja",
                  "orderIndex": 1
                }
              ]
            }
            Genera exactamente {{policy.ExpectedOptionCount}} opciones y usa todos los orderIndex de 1 a
            {{policy.ExpectedOptionCount}}. Debe haber exactamente {{policy.ExpectedCorrectCount}} opciones adecuadas.
            Los valores de control son reglas del simulador y deben coincidir exactamente:
            {{resourceContract}}
            phaseName debe ser exactamente "{{phase.Name}}". Solo se permiten optionType: {{string.Join(", ", optionTypes)}}.
            Solo se permiten estas claves dentro de impact: {{kpis}}. Incluye al menos un impacto por opción.
            Cada impacto debe estar entre -25 y 25. Usa de 1 a 6 tags no vacíos y sin duplicados.
            orderIndex debe ser consecutivo y comenzar en 1. No incluyas phaseName dentro
            de cada opción, score, isCorrect, ImpactJson, TagsJson ni identificadores de base de datos.
            """;
    }

    private static string ValueOrNotSpecified(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "No especificado" : value.Trim();
}
