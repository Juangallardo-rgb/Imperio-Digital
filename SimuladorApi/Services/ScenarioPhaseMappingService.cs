using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class ScenarioPhaseMappingService
    {
        private static readonly HashSet<string> MinorWords = new(StringComparer.Ordinal)
        {
            "el", "la", "los", "las", "un", "una", "de", "del", "the", "a", "an"
        };

        private static readonly IReadOnlyDictionary<string, string[]> PhaseAliases =
            new Dictionary<string, string[]>
            {
                ["empatizar"] = new[] { "empathize", "empathy", "comprender usuario" },
                ["definir"] = new[] { "define", "problem", "problema" },
                ["idear"] = new[] { "ideate", "idea", "ideacion" },
                ["prototipar"] = new[] { "prototype", "prototipo" },
                ["evaluar"] = new[] { "test", "evaluacion", "validar" },

                ["identificar proceso"] = new[]
                {
                    "identificacion proceso", "process identification", "identify process",
                    "identify the process"
                },
                ["modelar proceso actual"] = new[]
                {
                    "modelado proceso actual", "proceso actual", "as is process",
                    "current process model", "model current process"
                },
                ["analizar cuellos botella"] = new[]
                {
                    "analisis cuellos botella", "cuellos botella", "bottleneck analysis",
                    "analyze bottlenecks", "bottlenecks"
                },
                ["redisenar proceso"] = new[]
                {
                    "rediseno proceso", "proceso mejorado", "to be process",
                    "redesign process", "process redesign"
                },
                ["monitorear indicadores"] = new[]
                {
                    "monitoreo indicadores", "seguimiento indicadores", "medir indicadores",
                    "monitor kpis", "kpi monitoring", "process monitoring"
                },

                ["diagnostico inicial"] = new[] { "diagnostic", "estado actual", "current state" },
                ["evaluar capacidades"] = new[] { "capability", "capacidades" },
                ["priorizar brechas"] = new[] { "gap", "brecha", "brechas" },
                ["plan transformacion"] = new[] { "transformation plan", "iniciativa" },
                ["seguimiento madurez"] = new[] { "maturity tracking", "seguimiento", "madurez" },

                ["hipotesis"] = new[] { "hypothesis", "supuesto" },
                ["mvp"] = new[] { "minimum viable product", "producto minimo viable" },
                ["medicion"] = new[] { "measure", "metric", "metrica" },
                ["aprendizaje"] = new[] { "learn", "learning" },
                ["pivote o perseverancia"] = new[] { "pivot", "persevere" }
            };

        private static readonly IReadOnlyDictionary<string, string> OptionTypeToPhase =
            new Dictionary<string, string>
            {
                ["evidence"] = "Empatizar",
                ["painpoint"] = "Empatizar",
                ["problemstatement"] = "Definir",
                ["solution"] = "Idear",
                ["prototypefeature"] = "Prototipar",
                ["userflowstep"] = "Prototipar",
                ["test"] = "Evaluar",

                ["processevidence"] = "Identificar proceso",
                ["processselection"] = "Identificar proceso",
                ["currentprocessstep"] = "Modelar proceso actual",
                ["currentprocess"] = "Modelar proceso actual",
                ["bottleneck"] = "Analizar cuellos de botella",
                ["processimprovement"] = "Rediseñar proceso",
                ["redesign"] = "Rediseñar proceso",
                ["kpi"] = "Monitorear indicadores",
                ["kpiselection"] = "Monitorear indicadores",

                ["currentstate"] = "Diagnóstico inicial",
                ["capability"] = "Evaluar capacidades",
                ["gap"] = "Priorizar brechas",
                ["transformationinitiative"] = "Plan de transformación",
                ["maturitykpi"] = "Seguimiento de madurez",

                ["hypothesis"] = "Hipótesis",
                ["mvpfeature"] = "MVP",
                ["metric"] = "Medición",
                ["learning"] = "Aprendizaje",
                ["pivotdecision"] = "Pivote o perseverancia",
                ["decision"] = "Pivote o perseverancia"
            };

        private readonly AppDbContext _context;

        public ScenarioPhaseMappingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RepairScenarioOptionPhaseMappingsAsync(Scenario scenario)
        {
            var methodology = await GetMethodologyAsync(scenario);

            if (methodology == null)
                return;

            var catalogPhases = methodology.Phases
                .Where(phase => phase.IsActive)
                .OrderBy(phase => phase.PhaseOrder)
                .ToList();

            if (!catalogPhases.Any())
                return;

            var changed = scenario.MethodologyId != methodology.Id ||
                !string.Equals(scenario.Methodology, methodology.Code, StringComparison.Ordinal);

            scenario.MethodologyId = methodology.Id;
            scenario.Methodology = methodology.Code;

            foreach (var phaseSetting in scenario.PhaseSettings)
            {
                var catalogPhase = ResolveCatalogPhase(phaseSetting, catalogPhases);

                if (catalogPhase == null)
                    continue;

                if (phaseSetting.MethodologyPhaseId != catalogPhase.Id ||
                    !string.Equals(phaseSetting.PhaseName, catalogPhase.Name, StringComparison.Ordinal))
                {
                    phaseSetting.MethodologyPhaseId = catalogPhase.Id;
                    phaseSetting.PhaseName = catalogPhase.Name;
                    changed = true;
                }
            }

            var enabledPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .ToList();

            if (enabledPhases.Any())
            {
                var options = await _context.ScenarioOptions
                    .Where(option => option.ScenarioId == scenario.Id)
                    .ToListAsync();

                foreach (var option in options)
                {
                    var originalPhaseName = option.PhaseName;
                    var originalPhaseId = option.MethodologyPhaseId;

                    if (TryMapOptionToEnabledPhase(option, enabledPhases) &&
                        (originalPhaseId != option.MethodologyPhaseId ||
                         !string.Equals(originalPhaseName, option.PhaseName, StringComparison.Ordinal)))
                    {
                        changed = true;
                    }
                }
            }

            if (!changed)
                return;

            scenario.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public bool TryMapOptionToEnabledPhase(
            ScenarioOption option,
            IReadOnlyCollection<ScenarioPhaseSetting> enabledPhases)
        {
            var phase = ResolveScenarioPhase(option, enabledPhases);

            if (phase == null)
                return false;

            option.PhaseName = phase.PhaseName;
            option.MethodologyPhaseId = phase.MethodologyPhaseId;
            return true;
        }

        public ScenarioPhaseSetting? ResolveEnabledPhase(
            string? phaseName,
            IReadOnlyCollection<ScenarioPhaseSetting> enabledPhases)
        {
            if (!TryResolveCanonicalPhaseName(
                    phaseName,
                    enabledPhases.Select(phase => phase.PhaseName),
                    out var canonicalName))
            {
                return null;
            }

            return enabledPhases.First(phase =>
                string.Equals(phase.PhaseName, canonicalName, StringComparison.Ordinal));
        }

        public bool AreOptionsValidForEnabledPhases(
            IReadOnlyCollection<ScenarioOption> options,
            IReadOnlyCollection<ScenarioPhaseSetting> enabledPhases)
        {
            if (!options.Any() || !enabledPhases.Any())
                return false;

            if (options.Any(option => !enabledPhases.Any(phase => IsOptionMappedToPhase(option, phase))))
                return false;

            return enabledPhases.All(phase =>
            {
                var phaseOptions = options
                    .Where(option => IsOptionMappedToPhase(option, phase))
                    .ToList();

                return phaseOptions.Any() && phaseOptions.Any(option => option.IsCorrect);
            });
        }

        public bool IsOptionMappedToPhase(ScenarioOption option, ScenarioPhaseSetting phase)
        {
            if (phase.MethodologyPhaseId.HasValue &&
                option.MethodologyPhaseId == phase.MethodologyPhaseId)
            {
                return true;
            }

            return TryResolveCanonicalPhaseName(
                option.PhaseName,
                new[] { phase.PhaseName },
                out _
            );
        }

        private async Task<Methodology?> GetMethodologyAsync(Scenario scenario)
        {
            var methodologyCode = NormalizeMethodologyCode(scenario.Methodology);

            return await _context.Methodologies
                .Include(methodology => methodology.Phases)
                .FirstOrDefaultAsync(methodology =>
                    methodology.IsActive &&
                    ((scenario.MethodologyId.HasValue && methodology.Id == scenario.MethodologyId.Value) ||
                     methodology.Code == methodologyCode));
        }

        private static MethodologyPhase? ResolveCatalogPhase(
            ScenarioPhaseSetting phaseSetting,
            IReadOnlyCollection<MethodologyPhase> catalogPhases)
        {
            if (phaseSetting.MethodologyPhaseId.HasValue)
            {
                var byId = catalogPhases.FirstOrDefault(phase =>
                    phase.Id == phaseSetting.MethodologyPhaseId.Value);

                if (byId != null)
                    return byId;
            }

            if (!TryResolveCanonicalPhaseName(
                    phaseSetting.PhaseName,
                    catalogPhases.Select(phase => phase.Name),
                    out var canonicalName))
            {
                return null;
            }

            return catalogPhases.FirstOrDefault(phase =>
                string.Equals(phase.Name, canonicalName, StringComparison.Ordinal));
        }

        private static ScenarioPhaseSetting? ResolveScenarioPhase(
            ScenarioOption option,
            IReadOnlyCollection<ScenarioPhaseSetting> enabledPhases)
        {
            if (option.MethodologyPhaseId.HasValue)
            {
                var byId = enabledPhases.FirstOrDefault(phase =>
                    phase.MethodologyPhaseId == option.MethodologyPhaseId.Value);

                if (byId != null)
                    return byId;
            }

            if (TryResolveCanonicalPhaseName(
                    option.PhaseName,
                    enabledPhases.Select(phase => phase.PhaseName),
                    out var canonicalName))
            {
                return enabledPhases.First(phase =>
                    string.Equals(phase.PhaseName, canonicalName, StringComparison.Ordinal));
            }

            if (!OptionTypeToPhase.TryGetValue(NormalizeText(option.OptionType), out var phaseFromOptionType) ||
                !TryResolveCanonicalPhaseName(
                    phaseFromOptionType,
                    enabledPhases.Select(phase => phase.PhaseName),
                    out canonicalName))
            {
                return null;
            }

            return enabledPhases.First(phase =>
                string.Equals(phase.PhaseName, canonicalName, StringComparison.Ordinal));
        }

        private static bool TryResolveCanonicalPhaseName(
            string? value,
            IEnumerable<string> canonicalPhaseNames,
            out string canonicalName)
        {
            canonicalName = string.Empty;
            var normalizedValue = NormalizeText(value);

            if (string.IsNullOrWhiteSpace(normalizedValue))
                return false;

            var phases = canonicalPhaseNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var exactPhase = phases.FirstOrDefault(phase =>
                NormalizeText(phase) == normalizedValue);

            if (!string.IsNullOrWhiteSpace(exactPhase))
            {
                canonicalName = exactPhase;
                return true;
            }

            foreach (var phase in phases)
            {
                var normalizedPhase = NormalizeText(phase);

                if (!PhaseAliases.TryGetValue(normalizedPhase, out var aliases))
                    continue;

                if (aliases.Select(NormalizeText).Any(alias =>
                    normalizedValue == alias ||
                    normalizedValue.Contains(alias, StringComparison.Ordinal) ||
                    alias.Contains(normalizedValue, StringComparison.Ordinal)))
                {
                    canonicalName = phase;
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeMethodologyCode(string? methodologyCode)
        {
            return methodologyCode?.Trim() switch
            {
                "BPM" or "Business Process Management" or "BusinessProcessManagement" or "business-process-management" => "BPM",
                "DigitalMaturity" or "Madurez Digital" or "MadurezDigital" or "digital-maturity" => "DigitalMaturity",
                "LeanStartup" or "Lean Startup" or "lean-startup" => "LeanStartup",
                _ => "DesignThinking"
            };
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder();

            foreach (var character in value.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                    continue;

                builder.Append(char.IsLetterOrDigit(character)
                    ? char.ToLowerInvariant(character)
                    : ' ');
            }

            return string.Join(
                ' ',
                builder.ToString()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => !MinorWords.Contains(token))
            );
        }
    }
}
