using SimuladorApi.DTOs.DesignThinking;
using SimuladorApi.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

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

            var isDesignThinking = string.Equals(
                scenario.Methodology,
                "DesignThinking",
                StringComparison.OrdinalIgnoreCase
            );
            var prompt = isDesignThinking
                ? BuildDesignThinkingV2Prompt(scenario)
                : BuildPrompt(scenario);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = isDesignThinking
                            ? "Eres un experto academico en Design Thinking y diseno de simuladores universitarios. Devuelves exclusivamente JSON valido, completo y evaluable."
                            : "Eres un experto académico en Design Thinking, transformación digital y diseño de simuladores educativos. Generas opciones coherentes, evaluables y contextualizadas para casos de estudio."
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

            _httpClient.Timeout = TimeSpan.FromSeconds(180);

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

            if (isDesignThinking)
            {
                ValidateDesignThinkingV2Options(aiOptions, scenario);
            }

            return aiOptions.Select((option, index) => new ScenarioOption
            {
                ScenarioId = scenario.Id,
                PhaseName = option.PhaseName,
                OptionType = option.OptionType,
                Text = option.Text,
                Score = option.IsCorrect ? 100 : 0,
                IsCorrect = option.IsCorrect,
                ImpactJson = SerializeImpact(option),
                OrderIndex = option.OrderIndex > 0 ? option.OrderIndex : index + 1,
                Cost = option.Cost ?? 0,
                TimeCost = option.TimeCost ?? 0,
                RiskImpact = option.RiskImpact ?? 0,
                TagsJson = option.Tags?.Any() == true
                    ? JsonSerializer.Serialize(option.Tags)
                    : string.Empty,
                MaxSelections = option.MaxSelections ?? 0,
                ExpectedImpactLevel = option.ExpectedImpactLevel ?? string.Empty,
                ExpectedEffortLevel = option.ExpectedEffortLevel ?? string.Empty,
                ExpectedViabilityLevel = option.ExpectedViabilityLevel ?? string.Empty
            }).ToList();
        }

        private static string BuildDesignThinkingV2Prompt(Scenario scenario)
        {
            var configuredPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .OrderBy(phase => phase.PhaseOrder)
                .Select(phase => phase.PhaseName)
                .ToList();

            if (!configuredPhases.Any())
            {
                configuredPhases = new List<string>
                {
                    "Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"
                };
            }

            return $@"
Genera contenido estructurado para una simulacion universitaria de Design Thinking.

CASO:
Titulo: {scenario.Title}
Descripcion: {scenario.Description}
Empresa: {scenario.CompanyType}
Problema: {scenario.Problem}
Usuario objetivo: {scenario.TargetUser}
Restricciones: {scenario.Constraints}
Dificultad: {scenario.Difficulty}

Devuelve SOLO un arreglo JSON valido. No uses markdown ni texto adicional.
Cada objeto debe tener exactamente estas propiedades:
phaseName, optionType, text, isCorrect, impact, tags, orderIndex, cost, timeCost, riskImpact, maxSelections, expectedImpactLevel, expectedEffortLevel, expectedViabilityLevel.

REGLAS GENERALES:
- Genera contenido exclusivamente para estas fases activas: {string.Join(", ", configuredPhases)}.
- Aplica las instrucciones detalladas de una fase solo si esa fase esta activa.
- Incluye opciones correctas y distractores evaluables, relacionadas con el caso.
- tags es un arreglo JSON de palabras clave minusculas y utiles para continuidad.
- impact es un objeto JSON con impactos KPI; usa {{}} cuando no aplique.
- cost, timeCost y riskImpact son numeros reales del escenario.
- expectedImpactLevel, expectedEffortLevel y expectedViabilityLevel usan Alto, Medio o Bajo cuando apliquen; en otra fase usa cadena vacia.
- Cada fase debe tener al menos 3 opciones y una correcta.

EMPATIZAR:
- optionType Evidence o PainPoint.
- Genera entrevistas, observaciones, metricas, quejas, comportamientos, dolores y necesidades mediante texto y tags.
- maxSelections entre 2 y 5.

DEFINIR:
- optionType ProblemStatement.
- Incluye formulaciones centradas en usuario, necesidad e insight, junto con sintomas y soluciones anticipadas como distractores.
- maxSelections entre 1 y 2.

IDEAR:
- optionType Solution.
- Incluye impacto, esfuerzo, viabilidad, costo, tiempo, riesgo y tags para cada idea.
- impact debe usar solo claves KPI del caso de Design Thinking: cartAbandonment, conversionRate, satisfaction, purchaseTime, digitalAdoption.
- maxSelections entre 1 y 3.

PROTOTIPAR:
- optionType PrototypeFeature o UserFlowStep.
- Incluye funcionalidades minimas, dependencias en tags, costo, tiempo, riesgo y prioridad mediante expectedImpactLevel y expectedViabilityLevel.
- maxSelections entre 2 y 4.

EVALUAR:
- optionType KPI o Test.
- Incluye metricas de prueba, feedback, problemas, hallazgos y acciones de siguiente iteracion en el texto y tags.
- maxSelections entre 1 y 3.

EJEMPLO DE FORMA:
[
  {{
    ""phaseName"": ""Idear"",
    ""optionType"": ""Solution"",
    ""text"": ""..."",
    ""isCorrect"": true,
    ""impact"": {{""cartAbandonment"": -5, ""conversionRate"": 0.8, ""satisfaction"": 6, ""purchaseTime"": -0.4, ""digitalAdoption"": 3}},
    ""tags"": [""trust"", ""clarity""],
    ""orderIndex"": 1,
    ""cost"": 20,
    ""timeCost"": 2,
    ""riskImpact"": 3,
    ""maxSelections"": 3,
    ""expectedImpactLevel"": ""Alto"",
    ""expectedEffortLevel"": ""Bajo"",
    ""expectedViabilityLevel"": ""Alta""
  }}
]";
        }

        private static void ValidateDesignThinkingV2Options(
            List<AiScenarioOptionDto> options,
            Scenario scenario)
        {
            var expectedPhases = scenario.PhaseSettings
                .Where(phase => phase.IsEnabled)
                .Select(phase => phase.PhaseName)
                .ToList();

            if (!expectedPhases.Any())
            {
                expectedPhases = new List<string>
                {
                    "Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"
                };
            }

            if (options.Any(option =>
                string.IsNullOrWhiteSpace(option.PhaseName) ||
                string.IsNullOrWhiteSpace(option.OptionType) ||
                string.IsNullOrWhiteSpace(option.Text) ||
                option.Tags == null || !option.Tags.Any()))
            {
                throw new Exception("La IA devolvio opciones incompletas para la experiencia V2.");
            }

            foreach (var phaseName in expectedPhases)
            {
                var phaseOptions = options
                    .Where(option => NormalizePhase(option.PhaseName) == NormalizePhase(phaseName))
                    .ToList();

                if (phaseOptions.Count < 3 || !phaseOptions.Any(option => option.IsCorrect))
                {
                    throw new Exception($"La IA no genero contenido suficiente para la fase {phaseName}.");
                }

                ValidatePhaseMetadata(phaseName, phaseOptions);
            }
        }

        private static void ValidatePhaseMetadata(
            string phaseName,
            List<AiScenarioOptionDto> options)
        {
            var phase = NormalizePhase(phaseName);
            var types = options
                .Select(option => option.OptionType.Trim().ToLowerInvariant())
                .ToList();

            if (phase == "empatizar" && !types.Any(type => type is "evidence" or "painpoint"))
                throw new Exception("Empatizar requiere evidencias o dolores de usuario.");

            if (phase == "definir" && !types.Contains("problemstatement"))
                throw new Exception("Definir requiere formulaciones de problema.");

            if (phase == "idear")
            {
                if (!types.Contains("solution") || options.Any(option =>
                    string.IsNullOrWhiteSpace(option.ExpectedImpactLevel) ||
                    string.IsNullOrWhiteSpace(option.ExpectedEffortLevel) ||
                    string.IsNullOrWhiteSpace(option.ExpectedViabilityLevel) ||
                    option.Cost == null || option.TimeCost == null || option.RiskImpact == null ||
                    option.Impact == null || option.Impact.Value.ValueKind != JsonValueKind.Object))
                {
                    throw new Exception("Idear requiere soluciones con metadata de priorizacion completa.");
                }
            }

            if (phase == "prototipar" && !types.Any(type => type is "prototypefeature" or "userflowstep"))
                throw new Exception("Prototipar requiere modulos o pasos de flujo.");

            if (phase == "evaluar" && !types.Any(type => type is "kpi" or "test"))
                throw new Exception("Evaluar requiere KPIs o pruebas.");
        }

        private static string SerializeImpact(AiScenarioOptionDto option)
        {
            if (option.Impact.HasValue && option.Impact.Value.ValueKind == JsonValueKind.Object)
                return option.Impact.Value.GetRawText();

            return option.ImpactJson ?? string.Empty;
        }

        private static string NormalizePhase(string value)
        {
            return value
                .Trim()
                .ToLowerInvariant()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u");
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

            public JsonElement? Impact { get; set; }

            public List<string> Tags { get; set; } = new();

            public decimal? Cost { get; set; }

            public decimal? TimeCost { get; set; }

            public decimal? RiskImpact { get; set; }

            public int? MaxSelections { get; set; }

            public string ExpectedImpactLevel { get; set; } = string.Empty;

            public string ExpectedEffortLevel { get; set; } = string.Empty;

            public string ExpectedViabilityLevel { get; set; } = string.Empty;

            public int OrderIndex { get; set; }
        }
        public Task<GeneratedScenarioDraftDto> GenerateScenarioDraftAsync(string methodology)
        {
            var normalizedMethodology = methodology?.Trim() ?? "DesignThinking";

            GeneratedScenarioDraftDto draft = normalizedMethodology switch
            {
                "BPM" => new GeneratedScenarioDraftDto
                {
                    Title = "Optimización del proceso de atención de pedidos en restaurante",
                    Description = "Un restaurante familiar recibe pedidos por WhatsApp, llamadas telefónicas y atención presencial. En horas pico, los pedidos se duplican, se confunden comandas, se retrasan entregas y el personal no cuenta con una vista clara del estado de cada orden. La gerencia busca rediseñar el proceso operativo mediante herramientas digitales que permitan mejorar tiempos, trazabilidad y satisfacción del cliente.",
                    CompanyType = "Restaurante familiar",
                    Problem = "El proceso de recepción, preparación y entrega de pedidos depende de pasos manuales, comunicación informal y poca trazabilidad, generando retrasos, errores en comandas y baja eficiencia operativa.",
                    TargetUser = "Personal de cocina, cajeros, repartidores internos y clientes que realizan pedidos en horas pico.",
                    Constraints = "Presupuesto limitado, equipo pequeño, resistencia inicial del personal, operación continua sin posibilidad de detener el restaurante y necesidad de implementar mejoras en menos de 6 semanas.",
                    Difficulty = "Alta"
                },

                "DigitalMaturity" => new GeneratedScenarioDraftDto
                {
                    Title = "Diagnóstico de madurez digital en una clínica privada",
                    Description = "Una clínica privada atiende pacientes por llamadas, mensajes y recepción presencial. Aunque utiliza algunas herramientas digitales, la información se encuentra dispersa entre agendas físicas, hojas de cálculo y sistemas no integrados. La dirección desea conocer su nivel de madurez digital para priorizar inversiones tecnológicas que mejoren la atención, la eficiencia administrativa y el uso de datos para la toma de decisiones.",
                    CompanyType = "Clínica privada",
                    Problem = "La organización utiliza herramientas digitales aisladas, no cuenta con integración de datos ni indicadores consolidados, lo que dificulta medir desempeño, reducir tiempos administrativos y mejorar la experiencia del paciente.",
                    TargetUser = "Personal administrativo, médicos, pacientes recurrentes y responsables de gestión de la clínica.",
                    Constraints = "Presupuesto moderado, datos dispersos, personal con diferentes niveles de habilidad digital, procesos sensibles por información médica y necesidad de priorizar iniciativas de alto impacto.",
                    Difficulty = "Media"
                },

                "LeanStartup" => new GeneratedScenarioDraftDto
                {
                    Title = "Validación de una plataforma digital para reservas de servicios de belleza",
                    Description = "Un emprendimiento de servicios de belleza quiere lanzar una plataforma que permita reservar citas, pagar anticipos y recibir recordatorios automáticos. El equipo aún no sabe si las clientas realmente usarían la solución ni qué funcionalidades son prioritarias. Antes de invertir en el desarrollo completo, necesita validar hipótesis mediante un MVP y medir señales reales de adopción.",
                    CompanyType = "Emprendimiento de servicios de belleza",
                    Problem = "El negocio quiere digitalizar la reserva de citas, pero no ha validado si sus clientas usarían una plataforma digital, qué problema les duele más ni qué propuesta de valor generaría adopción real.",
                    TargetUser = "Mujeres entre 25 y 40 años que reservan servicios de belleza, maquillaje, uñas o tratamientos estéticos.",
                    Constraints = "Presupuesto inicial bajo, tiempo de validación de 4 semanas, equipo pequeño, necesidad de evitar construir funcionalidades innecesarias y dependencia de feedback rápido de usuarias reales.",
                    Difficulty = "Media"
                },

                _ => new GeneratedScenarioDraftDto
                {
                    Title = "Rediseño de la experiencia digital en una tienda online",
                    Description = "Una tienda online de productos de consumo recibe tráfico constante, pero muchos usuarios abandonan el proceso de compra antes de finalizar el pago. Los clientes reportan dudas sobre costos de envío, tiempos de entrega y seguridad del pago. La empresa necesita comprender mejor la experiencia del usuario y proponer soluciones digitales centradas en sus necesidades reales.",
                    CompanyType = "E-commerce",
                    Problem = "La tienda presenta una alta tasa de abandono durante el checkout debido a falta de claridad, fricción en el proceso de compra y baja confianza del usuario al momento de pagar.",
                    TargetUser = "Clientes digitales que compran productos en línea desde dispositivos móviles.",
                    Constraints = "Presupuesto limitado, equipo de desarrollo pequeño, plazo máximo de 4 semanas, necesidad de mejorar la experiencia sin reconstruir toda la plataforma.",
                    Difficulty = "Media"
                }
            };

            return Task.FromResult(draft);
        }
    }
}
