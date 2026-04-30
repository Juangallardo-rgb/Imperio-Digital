using System.Text.Json;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class KpiSimulationService
    {
        public List<SimulationKpiResult> CalculateKpis(
            int attemptId,
            List<ScenarioOption> selectedSolutions)
        {
            decimal cartAbandonment = 35;
            decimal conversionRate = 3;
            decimal satisfaction = 60;
            decimal purchaseTime = 6;

            foreach (var solution in selectedSolutions)
            {
                if (string.IsNullOrWhiteSpace(solution.ImpactJson))
                    continue;

                try
                {
                    var impact = JsonSerializer.Deserialize<Dictionary<string, decimal>>(solution.ImpactJson);

                    if (impact == null)
                        continue;

                    if (impact.ContainsKey("cartAbandonment"))
                        cartAbandonment += impact["cartAbandonment"];

                    if (impact.ContainsKey("conversionRate"))
                        conversionRate += impact["conversionRate"];

                    if (impact.ContainsKey("satisfaction"))
                        satisfaction += impact["satisfaction"];

                    if (impact.ContainsKey("purchaseTime"))
                        purchaseTime += impact["purchaseTime"];
                }
                catch
                {
                    continue;
                }
            }

            if (cartAbandonment < 0) cartAbandonment = 0;
            if (conversionRate < 0) conversionRate = 0;
            if (satisfaction > 100) satisfaction = 100;
            if (satisfaction < 0) satisfaction = 0;
            if (purchaseTime < 0) purchaseTime = 0;

            return new List<SimulationKpiResult>
            {
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Abandono de carrito",
                    InitialValue = 35,
                    FinalValue = Math.Round(cartAbandonment, 2),
                    Unit = "%"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Conversión",
                    InitialValue = 3,
                    FinalValue = Math.Round(conversionRate, 2),
                    Unit = "%"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Satisfacción del usuario",
                    InitialValue = 60,
                    FinalValue = Math.Round(satisfaction, 2),
                    Unit = "/100"
                },
                new()
                {
                    SimulationAttemptId = attemptId,
                    KpiName = "Tiempo promedio de compra",
                    InitialValue = 6,
                    FinalValue = Math.Round(purchaseTime, 2),
                    Unit = "min"
                }
            };
        }
    }
}