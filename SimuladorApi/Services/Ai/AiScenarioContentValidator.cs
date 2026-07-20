using System.Globalization;
using System.Text;
using System.Text.Json;
using SimuladorApi.Models;

namespace SimuladorApi.Services.Ai;

public sealed class AiScenarioContentValidator
{
    private static readonly IReadOnlySet<string> MasculineLevels =
        new HashSet<string>(new[] { "Bajo", "Medio", "Alto" }, StringComparer.Ordinal);
    private static readonly IReadOnlySet<string> FeminineLevels =
        new HashSet<string>(new[] { "Baja", "Media", "Alta" }, StringComparer.Ordinal);
    private const int MaximumTagsPerOption = 6;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string[]>> AllowedOptionTypes =
        new Dictionary<string, IReadOnlyDictionary<string, string[]>>(StringComparer.Ordinal)
        {
            ["DesignThinking"] = BuildPhaseTypes(
                ("Empatizar", new[] { "Evidence", "UserInsight" }),
                ("Definir", new[] { "ProblemStatement", "ProblemDefinition" }),
                ("Idear", new[] { "SolutionIdea", "Idea" }),
                ("Prototipar", new[] { "PrototypeComponent", "MvpComponent" }),
                ("Evaluar", new[] { "TestFinding", "IterationDecision" })),
            ["BPM"] = BuildPhaseTypes(
                ("Identificar proceso", new[] { "ProcessEvidence", "ProcessSelection" }),
                ("Modelar proceso actual", new[] { "CurrentProcessStep", "CurrentProcess" }),
                ("Analizar cuellos de botella", new[] { "Bottleneck", "ProcessIssue" }),
                ("Rediseñar proceso", new[] { "ProcessImprovement", "Redesign" }),
                ("Monitorear indicadores", new[] { "Kpi", "KpiSelection" })),
            ["DigitalMaturity"] = BuildPhaseTypes(
                ("Diagnóstico inicial", new[] { "MaturityEvidence", "CurrentState" }),
                ("Evaluar capacidades", new[] { "DigitalCapability", "CapabilityAssessment" }),
                ("Priorizar brechas", new[] { "MaturityGap", "GapPriority" }),
                ("Plan de transformación", new[] { "TransformationInitiative", "RoadmapAction" }),
                ("Seguimiento de madurez", new[] { "MaturityKpi", "KpiSelection" })),
            ["LeanStartup"] = BuildPhaseTypes(
                ("Hipótesis", new[] { "Hypothesis", "CriticalAssumption" }),
                ("MVP", new[] { "MvpComponent", "Experiment" }),
                ("Medición", new[] { "ActionableMetric", "Measurement" }),
                ("Aprendizaje", new[] { "ValidatedLearning", "EvidenceConclusion" }),
                ("Pivote o perseverancia", new[] { "StrategicDecision", "PivotDecision" }))
        };

    public IReadOnlyCollection<string> GetAllowedOptionTypes(string methodologyCode, string phaseName) =>
        AllowedOptionTypes.TryGetValue(methodologyCode, out var phases) &&
        phases.TryGetValue(phaseName, out var types)
            ? types
            : Array.Empty<string>();

    public AiValidationResult ValidateDraft(
        AiScenarioDraftContent draft,
        Methodology methodology)
    {
        var errors = new List<string>();
        ValidateText(errors, draft.Title, "title", 8, 160);
        ValidateText(errors, draft.Description, "description", 40, 1800);
        ValidateText(errors, draft.CompanyType, "companyType", 3, 160);
        ValidateText(errors, draft.Problem, "problem", 25, 1200);
        ValidateText(errors, draft.TargetUser, "targetUser", 8, 600);
        ValidateText(errors, draft.Constraints, "constraints", 8, 800);
        ValidateText(errors, draft.LearningObjective, "learningObjective", 15, 600);
        if (!new[] { "Baja", "Media", "Alta" }.Contains(draft.Difficulty, StringComparer.Ordinal))
        {
            errors.Add("difficulty debe ser Baja, Media o Alta.");
        }
        if (!string.Equals(draft.MethodologyCode, methodology.Code, StringComparison.Ordinal))
        {
            errors.Add($"methodologyCode debe ser exactamente {methodology.Code}.");
        }

        return errors.Count == 0 ? AiValidationResult.Valid : new(false, errors);
    }

    public AiValidationResult ValidatePhaseOptions(
        string methodologyCode,
        MethodologyPhase phase,
        AiPhaseOptionsContent content)
    {
        var errors = new List<string>();
        if (!string.Equals(content.PhaseName, phase.Name, StringComparison.Ordinal))
        {
            errors.Add($"phaseName debe ser exactamente {phase.Name}.");
        }

        ValidatePhaseOptions(methodologyCode, phase, content.Options ?? new(), errors);
        return errors.Count == 0 ? AiValidationResult.Valid : new(false, errors);
    }

    public AiValidationResult ValidatePhaseOptions(
        string methodologyCode,
        MethodologyPhase phase,
        IReadOnlyList<AiScenarioOptionContent> options)
    {
        var errors = new List<string>();
        ValidatePhaseOptions(methodologyCode, phase, options, errors);
        return errors.Count == 0 ? AiValidationResult.Valid : new(false, errors);
    }

    private void ValidatePhaseOptions(
        string methodologyCode,
        MethodologyPhase phase,
        IReadOnlyList<AiScenarioOptionContent> options,
        ICollection<string> errors)
    {
        if (!AllowedOptionTypes.ContainsKey(methodologyCode))
        {
            errors.Add($"Metodología no válida: {methodologyCode}.");
            return;
        }

        var allowedTypes = GetAllowedOptionTypes(methodologyCode, phase.Name);
        if (allowedTypes.Count == 0)
        {
            errors.Add($"Fase no válida para {methodologyCode}: {phase.Name}.");
            return;
        }
        if (!AiScenarioGenerationPolicy.TryGet(methodologyCode, phase.Name, out var policy) ||
            policy is null)
        {
            errors.Add($"No existe una política de simulación para {methodologyCode}/{phase.Name}.");
            return;
        }
        if (options.Count != policy.ExpectedOptionCount)
        {
            errors.Add($"La fase debe contener exactamente {policy.ExpectedOptionCount} opciones.");
        }
        if (options.Count(option => option.IsBestOption) != policy.ExpectedCorrectCount)
        {
            errors.Add($"La fase debe contener exactamente {policy.ExpectedCorrectCount} opciones adecuadas.");
        }

        var normalizedTexts = new HashSet<string>(StringComparer.Ordinal);
        var orderIndexes = new HashSet<int>();
        var allowedKpis = KpiSimulationService.GetAllowedKpiKeys(methodologyCode);
        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var label = $"Opción {index + 1}";
            ValidateText(errors, option.Text, $"{label} text", 15, 500);
            ValidateText(errors, option.Rationale, $"{label} rationale", 10, 600);
            if (!normalizedTexts.Add(Normalize(option.Text)))
            {
                errors.Add($"{label}: el texto está duplicado.");
            }
            if (!allowedTypes.Contains(option.OptionType, StringComparer.Ordinal))
            {
                errors.Add($"{label}: optionType no permitido para la fase.");
            }
            if (option.OrderIndex < 1 || !orderIndexes.Add(option.OrderIndex))
            {
                errors.Add($"{label}: orderIndex debe ser positivo y único.");
            }
            if (option.MaxSelections != policy.MaxSelections)
            {
                errors.Add($"{label}: maxSelections debe ser exactamente {policy.MaxSelections}.");
            }
            if (option.OrderIndex >= 1 && option.OrderIndex <= policy.ExpectedOptionCount)
            {
                var optionPolicy = policy.GetOption(option.OrderIndex);
                if (option.IsBestOption != optionPolicy.IsCorrect)
                {
                    errors.Add(
                        $"{label}: isBestOption no coincide con la regla del orderIndex {option.OrderIndex}.");
                }
            }
            ValidateRange(errors, option.Cost, KpiSimulationService.MinimumOptionCost, KpiSimulationService.MaximumOptionCost, $"{label} cost");
            ValidateRange(errors, option.TimeCost, KpiSimulationService.MinimumOptionTimeCost, KpiSimulationService.MaximumOptionTimeCost, $"{label} timeCost");
            ValidateRange(errors, option.RiskImpact, KpiSimulationService.MinimumOptionRiskImpact, KpiSimulationService.MaximumOptionRiskImpact, $"{label} riskImpact");
            ValidateMasculineLevel(errors, option.ExpectedImpactLevel, $"{label} expectedImpactLevel");
            ValidateMasculineLevel(errors, option.ExpectedEffortLevel, $"{label} expectedEffortLevel");
            ValidateFeminineLevel(errors, option.ExpectedViabilityLevel, $"{label} expectedViabilityLevel");
            if (option.Tags is null || option.Tags.Count == 0 || option.Tags.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"{label}: tags debe contener etiquetas válidas.");
            }
            else
            {
                if (option.Tags.Count > MaximumTagsPerOption)
                {
                    errors.Add($"{label}: tags no puede contener más de {MaximumTagsPerOption} etiquetas.");
                }
                if (option.Tags.Select(Normalize).Distinct(StringComparer.Ordinal).Count() != option.Tags.Count)
                {
                    errors.Add($"{label}: tags no debe contener duplicados.");
                }
            }
            if (option.Impact is null || option.Impact.Count == 0)
            {
                errors.Add($"{label}: impact debe contener al menos un KPI permitido.");
            }
            foreach (var impact in option.Impact ?? new Dictionary<string, decimal>())
            {
                if (!allowedKpis.Contains(impact.Key))
                {
                    errors.Add($"{label}: KPI desconocido {impact.Key}.");
                }
                ValidateRange(errors, impact.Value, KpiSimulationService.MinimumKpiImpact, KpiSimulationService.MaximumKpiImpact, $"{label} impacto {impact.Key}");
            }

            try
            {
                JsonSerializer.Serialize(option.Impact);
                JsonSerializer.Serialize(option.Tags);
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                errors.Add($"{label}: impacto o etiquetas no serializables.");
            }
        }

        if (orderIndexes.Count == options.Count &&
            !Enumerable.Range(1, options.Count).All(orderIndexes.Contains))
        {
            errors.Add("orderIndex debe ser consecutivo y comenzar en 1.");
        }
    }

    public AiValidationResult ValidateCoverage(
        Methodology methodology,
        IReadOnlyList<ScenarioOption> options)
    {
        var errors = new List<string>();
        var activePhases = methodology.Phases
            .Where(item => item.IsActive)
            .OrderBy(item => item.PhaseOrder)
            .ToList();
        foreach (var phase in activePhases)
        {
            var phaseOptions = options
                .Where(option => option.MethodologyPhaseId == phase.Id)
                .OrderBy(option => option.OrderIndex)
                .ToList();
            if (!AiScenarioGenerationPolicy.TryGet(methodology.Code, phase.Name, out var policy) ||
                policy is null)
            {
                errors.Add($"La fase {phase.Name} no tiene una política de simulación.");
                continue;
            }
            if (phaseOptions.Count != policy.ExpectedOptionCount)
            {
                errors.Add(
                    $"La fase {phase.Name} debe contener exactamente {policy.ExpectedOptionCount} opciones válidas.");
            }
            if (phaseOptions.Any(option => !string.Equals(option.PhaseName, phase.Name, StringComparison.Ordinal)))
            {
                errors.Add($"La fase {phase.Name} contiene una asignación de fase inconsistente.");
            }
            if (phaseOptions.Any(option => option.MaxSelections != policy.MaxSelections))
            {
                errors.Add($"La fase {phase.Name} contiene un límite de selecciones inconsistente.");
            }
            foreach (var option in phaseOptions)
            {
                if (option.OrderIndex < 1 || option.OrderIndex > policy.ExpectedOptionCount)
                {
                    errors.Add($"La fase {phase.Name} contiene un orden de opción inválido.");
                    continue;
                }

                var optionPolicy = policy.GetOption(option.OrderIndex);
                if (option.IsCorrect != optionPolicy.IsCorrect ||
                    option.Cost != optionPolicy.Cost ||
                    option.TimeCost != optionPolicy.TimeCost ||
                    option.RiskImpact != optionPolicy.RiskImpact)
                {
                    errors.Add(
                        $"La fase {phase.Name} contiene recursos o corrección incompatibles con la simulación.");
                }
            }
        }

        ValidateFeasibleCorrectPath(activePhases, options, errors);

        return errors.Count == 0 ? AiValidationResult.Valid : new(false, errors);
    }

    private static void ValidateFeasibleCorrectPath(
        IReadOnlyList<MethodologyPhase> phases,
        IReadOnlyList<ScenarioOption> options,
        ICollection<string> errors)
    {
        var feasibleStates = new List<(decimal Cost, decimal Time)> { (0, 0) };
        foreach (var phase in phases)
        {
            var correctOptions = options
                .Where(option => option.MethodologyPhaseId == phase.Id && option.IsCorrect)
                .ToList();
            if (correctOptions.Count == 0)
            {
                errors.Add($"La fase {phase.Name} no contiene una decisión correcta.");
                return;
            }

            feasibleStates = feasibleStates
                .SelectMany(state => correctOptions.Select(option =>
                    (Cost: state.Cost + option.Cost, Time: state.Time + option.TimeCost)))
                .Where(state =>
                    state.Cost <= KpiSimulationService.InitialBudget &&
                    state.Time <= KpiSimulationService.InitialTimeWeeks)
                .Distinct()
                .ToList();
            if (feasibleStates.Count == 0)
            {
                errors.Add(
                    "No existe una ruta de decisiones correctas que complete la simulación con el presupuesto y tiempo disponibles.");
                return;
            }
        }
    }

    public static string NormalizeMasculineLevel(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "bajo" or "baja" => "Bajo",
            "medio" or "media" => "Medio",
            "alto" or "alta" => "Alto",
            _ => value.Trim()
        };
    }

    public static string NormalizeFeminineLevel(string value)
    {
        var normalized = Normalize(value);
        return normalized switch
        {
            "bajo" or "baja" => "Baja",
            "medio" or "media" => "Media",
            "alto" or "alta" => "Alta",
            _ => value.Trim()
        };
    }

    public static string NormalizeLevel(string value) => NormalizeMasculineLevel(value);

    private static IReadOnlyDictionary<string, string[]> BuildPhaseTypes(
        params (string Phase, string[] Types)[] entries) =>
        entries.ToDictionary(entry => entry.Phase, entry => entry.Types, StringComparer.Ordinal);

    private static void ValidateText(
        ICollection<string> errors,
        string? value,
        string property,
        int minimum,
        int maximum)
    {
        var length = value?.Trim().Length ?? 0;
        if (length < minimum || length > maximum)
        {
            errors.Add($"{property} debe tener entre {minimum} y {maximum} caracteres.");
        }
    }

    private static void ValidateRange(
        ICollection<string> errors,
        decimal value,
        decimal minimum,
        decimal maximum,
        string property)
    {
        if (value < minimum || value > maximum)
        {
            errors.Add($"{property} debe estar entre {minimum} y {maximum}.");
        }
    }

    private static void ValidateMasculineLevel(ICollection<string> errors, string value, string property)
    {
        if (!MasculineLevels.Contains(NormalizeMasculineLevel(value)))
        {
            errors.Add($"{property} debe ser Bajo, Medio o Alto.");
        }
    }

    private static void ValidateFeminineLevel(ICollection<string> errors, string value, string property)
    {
        if (!FeminineLevels.Contains(NormalizeFeminineLevel(value)))
        {
            errors.Add($"{property} debe ser Baja, Media o Alta.");
        }
    }

    private static string Normalize(string? value)
    {
        var decomposed = (value ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark &&
                !char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
