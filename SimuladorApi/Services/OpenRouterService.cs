using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SimuladorApi.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public OpenRouterService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateFeedbackAsync(
            decimal digitalMaturity,
            decimal operationalEfficiency,
            decimal customerExperience,
            decimal globalScore,
            string feedbackRule)
        {
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var model = _configuration["OpenRouter:Model"];
            var siteUrl = _configuration["OpenRouter:SiteUrl"];
            var siteName = _configuration["OpenRouter:SiteName"];

            if (string.IsNullOrWhiteSpace(apiKey))
                return "No se configuró la API key de OpenRouter.";

            var prompt = $@"
Eres un asistente académico especializado en transformación digital.

Con base en estos KPIs de una simulación:
- Madurez digital: {digitalMaturity}
- Eficiencia operativa: {operationalEfficiency}
- Experiencia del cliente: {customerExperience}
- Score global: {globalScore}

Feedback base del sistema:
{feedbackRule}

Tu tarea:
Redacta un feedback breve, claro y profesional en español.
Debe:
1. Interpretar los resultados.
2. Relacionarlos con metodologías de transformación digital.
3. Recomendar acciones concretas.
4. Mantener un tono académico y útil para un estudiante.

Máximo 120 palabras.
";

            var requestBody = new
            {
                model = model,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Eres un experto en transformación digital, BPM, Design Thinking, Lean Startup y madurez digital."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.7,
                max_tokens = 220
            };

            var requestJson = JsonSerializer.Serialize(requestBody);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "https://openrouter.ai/api/v1/chat/completions"
            );

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            requestMessage.Headers.Add("HTTP-Referer", siteUrl);
            requestMessage.Headers.Add("X-OpenRouter-Title", siteName);

            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"No se pudo generar feedback con IA. Respuesta de OpenRouter: {response.StatusCode}";
            }

            using var document = JsonDocument.Parse(responseContent);

            var aiText = document
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiText ?? "No se recibió contenido desde OpenRouter.";
        }
    }
}