using System.Text.Json;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class KpiSimulationService
    {
        public Dictionary<string, decimal> GetDefaultInitialKpis()
        {
            return new Dictionary<string, decimal>
            {
                { "cartAbandonment", 35 },
                { "conversionRate", 3 },
                { "satisfaction", 60 },
                { "purchaseTime", 6 },
                { "digitalAdoption", 45 }
            };
        }

        public string SerializeKpis(Dictionary<string, decimal> kpis)
        {
            return JsonSerializer.Serialize(kpis);
        }

        public Dictionary<string, decimal> DeserializeKpis(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return GetDefaultInitialKpis();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json)
                       ?? GetDefaultInitialKpis();
            }
            catch
            {
                return GetDefaultInitialKpis();
            }
        }

        public Dictionary<string, decimal> ApplyOptionImpacts(
            Dictionary<string, decimal> currentKpis,
            List<ScenarioOption> selectedOptions)
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

            return ClampKpis(updated);
        }

        public Dictionary<string, decimal> ClampKpis(Dictionary<string, decimal> kpis)
        {
            if (kpis.ContainsKey("satisfaction"))
            {
                kpis["satisfaction"] = Math.Clamp(kpis["satisfaction"], 0, 100);
            }

            if (kpis.ContainsKey("digitalAdoption"))
            {
                kpis["digitalAdoption"] = Math.Clamp(kpis["digitalAdoption"], 0, 100);
            }

            if (kpis.ContainsKey("cartAbandonment"))
            {
                kpis["cartAbandonment"] = Math.Clamp(kpis["cartAbandonment"], 0, 100);
            }

            if (kpis.ContainsKey("conversionRate") && kpis["conversionRate"] < 0)
            {
                kpis["conversionRate"] = 0;
            }

            if (kpis.ContainsKey("purchaseTime"))
            {
                kpis["purchaseTime"] = Math.Max(1, kpis["purchaseTime"]);
            }

            return kpis;
        }

        public List<SimulationKpiResult> BuildKpiResults(
            int attemptId,
            string initialKpisJson,
            string currentKpisJson)
        {
            var initial = DeserializeKpis(initialKpisJson);
            var current = DeserializeKpis(currentKpisJson);

            return new List<SimulationKpiResult>
            {
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Abandono de carrito",
                    InitialValue = GetValue(initial, "cartAbandonment"),
                    FinalValue = GetValue(current, "cartAbandonment"),
                    Unit = "%"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Conversión",
                    InitialValue = GetValue(initial, "conversionRate"),
                    FinalValue = GetValue(current, "conversionRate"),
                    Unit = "%"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Satisfacción del usuario",
                    InitialValue = GetValue(initial, "satisfaction"),
                    FinalValue = GetValue(current, "satisfaction"),
                    Unit = "/100"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Tiempo promedio de compra",
                    InitialValue = GetValue(initial, "purchaseTime"),
                    FinalValue = GetValue(current, "purchaseTime"),
                    Unit = "min"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Adopción digital",
                    InitialValue = GetValue(initial, "digitalAdoption"),
                    FinalValue = GetValue(current, "digitalAdoption"),
                    Unit = "/100"
                }
            };
        }

        private static decimal GetValue(Dictionary<string, decimal> kpis, string key)
        {
            return kpis.ContainsKey(key) ? Math.Round(kpis[key], 2) : 0;
        }
    }
}