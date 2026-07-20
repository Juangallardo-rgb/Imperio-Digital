using System.Text.Json;

namespace SimuladorApi.Services.Ai;

public static class AiScenarioJsonSchemas
{
    public static JsonElement BuildDraft(string methodologyCode) => JsonSerializer.SerializeToElement(new
    {
        type = "object",
        additionalProperties = false,
        required = new[]
        {
            "title", "description", "companyType", "problem", "targetUser",
            "constraints", "difficulty", "learningObjective", "methodologyCode"
        },
        properties = new
        {
            title = new { type = "string", minLength = 8, maxLength = 160 },
            description = new { type = "string", minLength = 40, maxLength = 1800 },
            companyType = new { type = "string", minLength = 3, maxLength = 160 },
            problem = new { type = "string", minLength = 25, maxLength = 1200 },
            targetUser = new { type = "string", minLength = 8, maxLength = 600 },
            constraints = new { type = "string", minLength = 8, maxLength = 800 },
            difficulty = new { type = "string", @enum = new[] { "Baja", "Media", "Alta" } },
            learningObjective = new { type = "string", minLength = 15, maxLength = 600 },
            methodologyCode = new { type = "string", @enum = new[] { methodologyCode } }
        }
    });

    public static JsonElement BuildPhaseOptions(
        string methodologyCode,
        string phaseName,
        IReadOnlyCollection<string> optionTypes,
        IReadOnlyCollection<string> allowedKpis)
    {
        var policy = AiScenarioGenerationPolicy.GetRequired(methodologyCode, phaseName);
        var impactProperties = allowedKpis.ToDictionary(
            key => key,
            _ => (object)new { type = "number", minimum = -25, maximum = 25 },
            StringComparer.Ordinal);

        return JsonSerializer.SerializeToElement(new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "phaseName", "options" },
            properties = new
            {
                phaseName = new { type = "string", @enum = new[] { phaseName } },
                options = new
                {
                    type = "array",
                    minItems = policy.ExpectedOptionCount,
                    maxItems = policy.ExpectedOptionCount,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[]
                        {
                            "optionType", "text", "isBestOption", "rationale", "impact", "tags",
                            "cost", "timeCost", "riskImpact", "maxSelections", "expectedImpactLevel",
                            "expectedEffortLevel", "expectedViabilityLevel", "orderIndex"
                        },
                        properties = new
                        {
                            optionType = new { type = "string", @enum = optionTypes },
                            text = new { type = "string", minLength = 15, maxLength = 500 },
                            isBestOption = new { type = "boolean" },
                            rationale = new { type = "string", minLength = 10, maxLength = 600 },
                            impact = new
                            {
                                type = "object",
                                additionalProperties = false,
                                required = allowedKpis,
                                properties = impactProperties
                            },
                            tags = new
                            {
                                type = "array",
                                minItems = 1,
                                maxItems = 6,
                                items = new { type = "string", minLength = 1, maxLength = 40 }
                            },
                            cost = new
                            {
                                type = "number",
                                minimum = policy.Options.Min(option => option.Cost),
                                maximum = policy.Options.Max(option => option.Cost)
                            },
                            timeCost = new
                            {
                                type = "number",
                                minimum = policy.Options.Min(option => option.TimeCost),
                                maximum = policy.Options.Max(option => option.TimeCost)
                            },
                            riskImpact = new
                            {
                                type = "number",
                                minimum = policy.Options.Min(option => option.RiskImpact),
                                maximum = policy.Options.Max(option => option.RiskImpact)
                            },
                            maxSelections = new { type = "integer", @enum = new[] { policy.MaxSelections } },
                            expectedImpactLevel = new { type = "string", @enum = new[] { "Alto", "Medio", "Bajo" } },
                            expectedEffortLevel = new { type = "string", @enum = new[] { "Alto", "Medio", "Bajo" } },
                            expectedViabilityLevel = new { type = "string", @enum = new[] { "Alta", "Media", "Baja" } },
                            orderIndex = new
                            {
                                type = "integer",
                                minimum = 1,
                                maximum = policy.ExpectedOptionCount
                            }
                        }
                    }
                }
            }
        });
    }
}
