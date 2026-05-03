using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class AiScenarioContentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AiScenarioContentService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<ScenarioOption>> GenerateOptionsForScenarioAsync(Scenario scenario)
        {
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"] ?? "openrouter/auto";
            var siteUrl = _configuration["OpenRouter:SiteUrl"] ?? "http://localhost:7160";
            var siteName = _configuration["OpenRouter:SiteName"] ?? "SimuladorApi";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "TU_API_KEY")
            {
                throw new Exception("OpenRouter API Key no configurada.");
            }

            var prompt = BuildPrompt(scenario);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Eres un experto académico en Design Thinking, transformación digital y diseño de simuladores educativos. Generas opciones coherentes, evaluables y contextualizadas para casos de estudio."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.4
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", siteUrl);
            request.Headers.Add("X-Title", siteName);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            _httpClient.Timeout = TimeSpan.FromSeconds(60);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error OpenRouter: {response.StatusCode} - {error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var aiText = ExtractAssistantContent(responseContent);

            var cleanJson = CleanJson(aiText);

            var aiOptions = JsonSerializer.Deserialize<List<AiScenarioOptionDto>>(
                cleanJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            if (aiOptions == null || aiOptions.Count == 0)
            {
                throw new Exception("La IA no devolvió opciones válidas.");
            }

            return aiOptions.Select((option, index) => new ScenarioOption
            {
                ScenarioId = scenario.Id,
                PhaseName = option.PhaseName,
                OptionType = option.OptionType,
                Text = option.Text,
                Score = option.IsCorrect ? 100 : 0,
                IsCorrect = option.IsCorrect,
                ImpactJson = option.ImpactJson ?? string.Empty,
                OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1
            }).ToList();
        }

        private static string BuildPrompt(Scenario scenario)
        {
            return $@"
Genera opciones personalizadas para un simulador educativo basado en Design Thinking.

CASO DE ESTUDIO:
Título: {scenario.Title}
Descripción: {scenario.Description}
Tipo de empresa: {scenario.CompanyType}
Problema principal: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

FASES:
1. Empatizar
2. Definir
3. Idear
4. Prototipar
5. Evaluar

Necesito opciones para que un estudiante resuelva el caso fase por fase.

REGLAS:
- Las opciones deben estar totalmente relacionadas con el caso.
- Incluye opciones correctas y distractores.
- No uses opciones genéricas.
- Las opciones deben servir para evaluar al estudiante.
- En Idear, las soluciones correctas deben tener ImpactJson.
- En Evaluar, los KPIs deben estar relacionados con el problema.
- Devuelve SOLO JSON válido.
- No uses markdown.
- No escribas explicaciones fuera del JSON.

FORMATO EXACTO:
[
  {{
    ""phaseName"": ""Empatizar"",
    ""optionType"": ""Evidence"",
    ""text"": ""..."",
    ""isCorrect"": true,
    ""impactJson"": """",
    ""orderIndex"": 1
  }}
]

CANTIDAD REQUERIDA:
Empatizar:
- 3 Evidence correctas
- 2 Evidence distractoras
- 3 PainPoint correctos
- 2 PainPoint distractores

Definir:
- 2 ProblemStatement correctos
- 2 ProblemStatement distractores

Idear:
- 4 Solution correctas con ImpactJson
- 2 Solution distractoras con ImpactJson neutro

Prototipar:
- 3 PrototypeFeature correctas
- 2 PrototypeFeature distractoras
- 2 UserFlowStep correctos
- 1 UserFlowStep distractor

Evaluar:
- 4 KPI correctos
- 2 KPI distractores

ImpactJson para soluciones:
Debe tener esta estructura:
{{""cartAbandonment"": -5, ""conversionRate"": 0.8, ""satisfaction"": 7, ""purchaseTime"": -0.5}}

Si el caso no es e-commerce, adapta los impactos conceptualmente pero conserva esas mismas claves para que el motor de KPIs funcione.
";
        }

        private static string ExtractAssistantContent(string openRouterResponse)
        {
            using var document = JsonDocument.Parse(openRouterResponse);

            var root = document.RootElement;

            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("OpenRouter devolvió contenido vacío.");
            }

            return content;
        }

        private static string CleanJson(string text)
        {
            var clean = text.Trim();

            if (clean.StartsWith("```json"))
            {
                clean = clean.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (clean.StartsWith("```"))
            {
                clean = clean.Replace("```", "").Trim();
            }

            var firstBracket = clean.IndexOf('[');
            var lastBracket = clean.LastIndexOf(']');

            if (firstBracket >= 0 && lastBracket > firstBracket)
            {
                clean = clean.Substring(firstBracket, lastBracket - firstBracket + 1);
            }

            return clean;
        }

        private class AiScenarioOptionDto
        {
            public string PhaseName { get; set; } = string.Empty;

            public string OptionType { get; set; } = string.Empty;

            public string Text { get; set; } = string.Empty;

            public bool IsCorrect { get; set; }

            public string ImpactJson { get; set; } = string.Empty;

            public int OrderIndex { get; set; }
        }
    }
}