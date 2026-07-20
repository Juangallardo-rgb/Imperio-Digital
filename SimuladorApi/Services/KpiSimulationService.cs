using System.Text.Json;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class KpiSimulationService
    {
        public const decimal InitialBudget = 100;
        public const decimal InitialTimeWeeks = 8;
        public const decimal MinimumOptionCost = 0;
        public const decimal MaximumOptionCost = InitialBudget;
        public const decimal MinimumOptionTimeCost = 0;
        public const decimal MaximumOptionTimeCost = InitialTimeWeeks;
        public const decimal MinimumOptionRiskImpact = -20;
        public const decimal MaximumOptionRiskImpact = 20;
        public const decimal MinimumKpiImpact = -25;
        public const decimal MaximumKpiImpact = 25;

        public static IReadOnlySet<string> GetAllowedKpiKeys(string methodologyCode)
        {
            var keys = methodologyCode switch
            {
                "BPM" => new[] { "processEfficiency", "cycleTime", "errorRate", "satisfaction", "digitalAdoption" },
                "DigitalMaturity" => new[] { "digitalMaturity", "processEfficiency", "dataUsage", "satisfaction", "digitalAdoption" },
                "LeanStartup" => new[] { "validatedLearning", "conversionRate", "satisfaction", "experimentVelocity", "digitalAdoption" },
                _ => new[] { "cartAbandonment", "conversionRate", "satisfaction", "purchaseTime", "digitalAdoption" }
            };

            return new HashSet<string>(keys, StringComparer.Ordinal);
        }

        public Dictionary<string, decimal> GetDefaultInitialKpis(string methodologyCode = "DesignThinking")
        {
            return methodologyCode switch
            {
                "BPM" => new Dictionary<string, decimal>
                {
                    { "processEfficiency", 55 },
                    { "cycleTime", 8 },
                    { "errorRate", 18 },
                    { "satisfaction", 60 },
                    { "digitalAdoption", 45 }
                },

                "DigitalMaturity" => new Dictionary<string, decimal>
                {
                    { "digitalMaturity", 35 },
                    { "processEfficiency", 50 },
                    { "dataUsage", 30 },
                    { "satisfaction", 60 },
                    { "digitalAdoption", 40 }
                },

                "LeanStartup" => new Dictionary<string, decimal>
                {
                    { "validatedLearning", 20 },
                    { "conversionRate", 3 },
                    { "satisfaction", 60 },
                    { "experimentVelocity", 40 },
                    { "digitalAdoption", 45 }
                },

                _ => new Dictionary<string, decimal>
                {
                    { "cartAbandonment", 35 },
                    { "conversionRate", 3 },
                    { "satisfaction", 60 },
                    { "purchaseTime", 6 },
                    { "digitalAdoption", 45 }
                }
            };
        }

        public string SerializeKpis(Dictionary<string, decimal> kpis)
        {
            return JsonSerializer.Serialize(kpis);
        }

        public Dictionary<string, decimal> DeserializeKpis(string json, string methodologyCode = "DesignThinking")
        {
            if (string.IsNullOrWhiteSpace(json))
                return GetDefaultInitialKpis(methodologyCode);

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json)
                       ?? GetDefaultInitialKpis(methodologyCode);
            }
            catch
            {
                return GetDefaultInitialKpis(methodologyCode);
            }
        }

        public Dictionary<string, decimal> ApplyOptionImpacts(
            Dictionary<string, decimal> currentKpis,
            List<ScenarioOption> selectedOptions,
            string methodologyCode = "DesignThinking")
        {
            var updated = new Dictionary<string, decimal>(currentKpis);

            foreach (var option in selectedOptions)
            {
                if (string.IsNullOrWhiteSpace(option.ImpactJson))
                    continue;

                try
                {
                    var impact = JsonSerializer.Deserialize<Dictionary<string, decimal>>(option.ImpactJson);

                    if (impact == null)
                        continue;

                    foreach (var item in impact)
                    {
                        if (item.Key == "budgetCost" || item.Key == "timeCost" || item.Key == "risk")
                            continue;

                        if (!updated.ContainsKey(item.Key))
                            updated[item.Key] = 0;

                        updated[item.Key] += item.Value;
                    }
                }
                catch
                {
                    continue;
                }
            }

            return ClampKpis(updated, methodologyCode);
        }

        public Dictionary<string, decimal> ClampKpis(
            Dictionary<string, decimal> kpis,
            string methodologyCode = "DesignThinking")
        {
            ClampIfExists(kpis, "satisfaction", 0, 100);
            ClampIfExists(kpis, "digitalAdoption", 0, 100);

            ClampIfExists(kpis, "cartAbandonment", 0, 100);
            ClampIfExists(kpis, "conversionRate", 0, 100);
            ClampIfExists(kpis, "purchaseTime", 1, 999);

            ClampIfExists(kpis, "processEfficiency", 0, 100);
            ClampIfExists(kpis, "cycleTime", 1, 999);
            ClampIfExists(kpis, "errorRate", 0, 100);

            ClampIfExists(kpis, "digitalMaturity", 0, 100);
            ClampIfExists(kpis, "dataUsage", 0, 100);

            ClampIfExists(kpis, "validatedLearning", 0, 100);
            ClampIfExists(kpis, "experimentVelocity", 0, 100);

            return kpis;
        }

        public List<SimulationKpiResult> BuildKpiResults(
            int attemptId,
            string initialKpisJson,
            string currentKpisJson,
            string methodologyCode = "DesignThinking")
        {
            var initial = DeserializeKpis(initialKpisJson, methodologyCode);
            var current = DeserializeKpis(currentKpisJson, methodologyCode);

            return methodologyCode switch
            {
                "BPM" => new List<SimulationKpiResult>
                {
                    CreateResult(attemptId, "Eficiencia del proceso", initial, current, "processEfficiency", "/100"),
                    CreateResult(attemptId, "Tiempo de ciclo", initial, current, "cycleTime", "días"),
                    CreateResult(attemptId, "Tasa de errores", initial, current, "errorRate", "%"),
                    CreateResult(attemptId, "Satisfacción", initial, current, "satisfaction", "/100"),
                    CreateResult(attemptId, "Adopción digital", initial, current, "digitalAdoption", "/100")
                },

                "DigitalMaturity" => new List<SimulationKpiResult>
                {
                    CreateResult(attemptId, "Madurez digital", initial, current, "digitalMaturity", "/100"),
                    CreateResult(attemptId, "Eficiencia operativa", initial, current, "processEfficiency", "/100"),
                    CreateResult(attemptId, "Uso de datos", initial, current, "dataUsage", "/100"),
                    CreateResult(attemptId, "Satisfacción", initial, current, "satisfaction", "/100"),
                    CreateResult(attemptId, "Adopción digital", initial, current, "digitalAdoption", "/100")
                },

                "LeanStartup" => new List<SimulationKpiResult>
                {
                    CreateResult(attemptId, "Aprendizaje validado", initial, current, "validatedLearning", "/100"),
                    CreateResult(attemptId, "Conversión", initial, current, "conversionRate", "%"),
                    CreateResult(attemptId, "Satisfacción", initial, current, "satisfaction", "/100"),
                    CreateResult(attemptId, "Velocidad experimental", initial, current, "experimentVelocity", "/100"),
                    CreateResult(attemptId, "Adopción digital", initial, current, "digitalAdoption", "/100")
                },

                _ => new List<SimulationKpiResult>
                {
                    CreateResult(attemptId, "Abandono de carrito", initial, current, "cartAbandonment", "%"),
                    CreateResult(attemptId, "Conversión", initial, current, "conversionRate", "%"),
                    CreateResult(attemptId, "Satisfacción del usuario", initial, current, "satisfaction", "/100"),
                    CreateResult(attemptId, "Tiempo promedio de compra", initial, current, "purchaseTime", "min"),
                    CreateResult(attemptId, "Adopción digital", initial, current, "digitalAdoption", "/100")
                }
            };
        }

        private static SimulationKpiResult CreateResult(
            int attemptId,
            string label,
            Dictionary<string, decimal> initial,
            Dictionary<string, decimal> current,
            string key,
            string unit)
        {
            return new SimulationKpiResult
            {
                SimulationAttemptId = attemptId,
                KpiName = label,
                InitialValue = GetValue(initial, key),
                FinalValue = GetValue(current, key),
                Unit = unit
            };
        }

        private static decimal GetValue(Dictionary<string, decimal> kpis, string key)
        {
            return kpis.ContainsKey(key) ? Math.Round(kpis[key], 2) : 0;
        }

        private static void ClampIfExists(
            Dictionary<string, decimal> kpis,
            string key,
            decimal min,
            decimal max)
        {
            if (kpis.ContainsKey(key))
            {
                kpis[key] = Math.Clamp(kpis[key], min, max);
            }
        }
    }
}
