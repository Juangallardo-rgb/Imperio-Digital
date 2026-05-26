using Microsoft.EntityFrameworkCore;
using SimuladorApi.Data;
using SimuladorApi.Models;

namespace SimuladorApi.Services
{
    public class MethodologyCatalogService
    {
        private readonly AppDbContext _context;

        public MethodologyCatalogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedDefaultMethodologiesAsync()
        {
            await EnsureMethodologyAsync(
                code: "DesignThinking",
                name: "Design Thinking",
                description: "Metodología centrada en el usuario para resolver problemas mediante empatía, definición, ideación, prototipado y evaluación.",
                phases: GetDesignThinkingPhases()
            );

            await EnsureMethodologyAsync(
                code: "BPM",
                name: "Business Process Management",
                description: "Metodología para identificar, modelar, analizar, rediseñar y monitorear procesos de negocio.",
                phases: GetBpmPhases()
            );

            await EnsureMethodologyAsync(
                code: "DigitalMaturity",
                name: "Madurez Digital",
                description: "Metodología para diagnosticar capacidades digitales, identificar brechas y priorizar planes de transformación.",
                phases: GetDigitalMaturityPhases()
            );

            await EnsureMethodologyAsync(
                code: "LeanStartup",
                name: "Lean Startup",
                description: "Metodología para validar hipótesis mediante MVP, medición, aprendizaje y decisiones de pivote o perseverancia.",
                phases: GetLeanStartupPhases()
            );
        }

        public async Task<Methodology?> GetByCodeAsync(string code)
        {
            return await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .FirstOrDefaultAsync(m => m.Code == code && m.IsActive);
        }

        public async Task<List<Methodology>> GetActiveMethodologiesAsync()
        {
            return await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        private async Task EnsureMethodologyAsync(
            string code,
            string name,
            string description,
            List<MethodologyPhase> phases)
        {
            var existing = await _context.Methodologies
                .Include(m => m.Phases)
                    .ThenInclude(p => p.Criteria)
                .FirstOrDefaultAsync(m => m.Code == code);

            if (existing == null)
            {
                var methodology = new Methodology
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Phases = phases
                };

                _context.Methodologies.Add(methodology);
                await _context.SaveChangesAsync();
                return;
            }

            existing.Name = name;
            existing.Description = description;
            existing.IsActive = true;

            foreach (var phaseTemplate in phases)
            {
                var existingPhase = existing.Phases
                    .FirstOrDefault(p => p.PhaseOrder == phaseTemplate.PhaseOrder);

                if (existingPhase == null)
                {
                    phaseTemplate.MethodologyId = existing.Id;
                    _context.MethodologyPhases.Add(phaseTemplate);
                    continue;
                }

                existingPhase.Name = phaseTemplate.Name;
                existingPhase.Description = phaseTemplate.Description;
                existingPhase.DefaultWeight = phaseTemplate.DefaultWeight;
                existingPhase.ActivityType = phaseTemplate.ActivityType;
                existingPhase.DefaultMaxSelections = phaseTemplate.DefaultMaxSelections;
                existingPhase.IsActive = true;

                foreach (var criteriaTemplate in phaseTemplate.Criteria)
                {
                    var existingCriteria = existingPhase.Criteria
                        .FirstOrDefault(c => c.Name == criteriaTemplate.Name);

                    if (existingCriteria == null)
                    {
                        existingPhase.Criteria.Add(new MethodologyPhaseCriteria
                        {
                            Name = criteriaTemplate.Name,
                            DefaultWeight = criteriaTemplate.DefaultWeight,
                            EvaluationType = criteriaTemplate.EvaluationType,
                            IsActive = true
                        });
                    }
                    else
                    {
                        existingCriteria.DefaultWeight = criteriaTemplate.DefaultWeight;
                        existingCriteria.EvaluationType = criteriaTemplate.EvaluationType;
                        existingCriteria.IsActive = true;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private static List<MethodologyPhase> GetDesignThinkingPhases()
        {
            return new List<MethodologyPhase>
            {
                new()
                {
                    Name = "Empatizar",
                    PhaseOrder = 1,
                    Description = "Comprender necesidades, evidencias y dolores del usuario.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 5,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de evidencias relevantes", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Identificación de dolores del usuario", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación centrada en usuario", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Definir",
                    PhaseOrder = 2,
                    Description = "Formular el problema correcto con base en la evidencia.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 2,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Claridad del problema", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Enfoque en usuario", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Relación con evidencia", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Idear",
                    PhaseOrder = 3,
                    Description = "Seleccionar soluciones digitales viables y de impacto.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Creatividad", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Viabilidad", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Impacto esperado", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación estratégica", DefaultWeight = 20, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Prototipar",
                    PhaseOrder = 4,
                    Description = "Construir una propuesta mínima coherente para validar.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Coherencia con solución", DefaultWeight = 35, EvaluationType = "Selection" },
                        new() { Name = "Funcionalidades mínimas", DefaultWeight = 35, EvaluationType = "Selection" },
                        new() { Name = "Claridad del flujo", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Evaluar",
                    PhaseOrder = 5,
                    Description = "Medir resultados, validar KPIs y proponer mejoras.",
                    DefaultWeight = 15,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de KPIs", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Interpretación de resultados", DefaultWeight = 35, EvaluationType = "AIText" },
                        new() { Name = "Mejora propuesta", DefaultWeight = 25, EvaluationType = "AIText" }
                    }
                }
            };
        }

        private static List<MethodologyPhase> GetBpmPhases()
        {
            return new List<MethodologyPhase>
            {
                new()
                {
                    Name = "Identificar proceso",
                    PhaseOrder = 1,
                    Description = "Seleccionar el proceso crítico y reconocer actores, entradas, salidas y objetivos.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Identificación del proceso crítico", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Reconocimiento de actores", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación del impacto del proceso", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Modelar proceso actual",
                    PhaseOrder = 2,
                    Description = "Representar el flujo actual del proceso, actividades, responsables y puntos de espera.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Secuencia correcta del proceso", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Identificación de responsables", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Claridad del modelo actual", DefaultWeight = 35, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Analizar cuellos de botella",
                    PhaseOrder = 3,
                    Description = "Detectar retrasos, reprocesos, errores y actividades que no agregan valor.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Identificación de cuellos de botella", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Priorización de problemas", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Análisis de causa", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Rediseñar proceso",
                    PhaseOrder = 4,
                    Description = "Proponer mejoras, automatizaciones y controles para optimizar el proceso.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Propuesta de mejora", DefaultWeight = 35, EvaluationType = "Selection" },
                        new() { Name = "Viabilidad operativa", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Impacto en eficiencia", DefaultWeight = 35, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Monitorear indicadores",
                    PhaseOrder = 5,
                    Description = "Definir KPIs de proceso para seguimiento y mejora continua.",
                    DefaultWeight = 10,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de indicadores", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Interpretación de desempeño", DefaultWeight = 35, EvaluationType = "AIText" },
                        new() { Name = "Acciones de control", DefaultWeight = 20, EvaluationType = "AIText" }
                    }
                }
            };
        }

        private static List<MethodologyPhase> GetDigitalMaturityPhases()
        {
            return new List<MethodologyPhase>
            {
                new()
                {
                    Name = "Diagnóstico inicial",
                    PhaseOrder = 1,
                    Description = "Evaluar el estado actual de digitalización de la organización.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Identificación de situación actual", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Reconocimiento de capacidades existentes", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación del diagnóstico", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Evaluar capacidades",
                    PhaseOrder = 2,
                    Description = "Analizar capacidades digitales en personas, procesos, tecnología, datos y cultura.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 5,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Evaluación de capacidades digitales", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Análisis de cultura y talento", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Coherencia del análisis", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Priorizar brechas",
                    PhaseOrder = 3,
                    Description = "Identificar brechas críticas y priorizarlas según impacto y esfuerzo.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Identificación de brechas", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Priorización estratégica", DefaultWeight = 35, EvaluationType = "Selection" },
                        new() { Name = "Justificación de prioridades", DefaultWeight = 25, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Plan de transformación",
                    PhaseOrder = 4,
                    Description = "Diseñar iniciativas digitales para cerrar brechas y mejorar madurez.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de iniciativas", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Viabilidad del plan", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Alineación estratégica", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Seguimiento de madurez",
                    PhaseOrder = 5,
                    Description = "Definir indicadores para medir avance de madurez digital.",
                    DefaultWeight = 10,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de indicadores de madurez", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Interpretación de avance", DefaultWeight = 35, EvaluationType = "AIText" },
                        new() { Name = "Mejora continua", DefaultWeight = 20, EvaluationType = "AIText" }
                    }
                }
            };
        }

        private static List<MethodologyPhase> GetLeanStartupPhases()
        {
            return new List<MethodologyPhase>
            {
                new()
                {
                    Name = "Hipótesis",
                    PhaseOrder = 1,
                    Description = "Formular hipótesis de problema, cliente, solución y valor.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Claridad de hipótesis", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Enfoque en cliente", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación de supuesto crítico", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "MVP",
                    PhaseOrder = 2,
                    Description = "Diseñar un producto mínimo viable para validar el aprendizaje más importante.",
                    DefaultWeight = 25,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 4,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Diseño mínimo viable", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Viabilidad del experimento", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Relación con hipótesis", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Medición",
                    PhaseOrder = 3,
                    Description = "Definir métricas accionables para validar o invalidar hipótesis.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Selección de métricas accionables", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Diseño de medición", DefaultWeight = 25, EvaluationType = "Selection" },
                        new() { Name = "Interpretación de datos", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Aprendizaje",
                    PhaseOrder = 4,
                    Description = "Extraer aprendizajes validados a partir de resultados del MVP.",
                    DefaultWeight = 20,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 3,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Identificación de aprendizaje validado", DefaultWeight = 40, EvaluationType = "Selection" },
                        new() { Name = "Análisis de evidencia", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Conclusión de aprendizaje", DefaultWeight = 30, EvaluationType = "AIText" }
                    }
                },
                new()
                {
                    Name = "Pivote o perseverancia",
                    PhaseOrder = 5,
                    Description = "Decidir si continuar, ajustar o cambiar la estrategia según evidencia.",
                    DefaultWeight = 15,
                    ActivityType = "SelectionAndText",
                    DefaultMaxSelections = 2,
                    Criteria = new List<MethodologyPhaseCriteria>
                    {
                        new() { Name = "Decisión basada en evidencia", DefaultWeight = 45, EvaluationType = "Selection" },
                        new() { Name = "Coherencia estratégica", DefaultWeight = 30, EvaluationType = "Selection" },
                        new() { Name = "Justificación de decisión", DefaultWeight = 25, EvaluationType = "AIText" }
                    }
                }
            };
        }
    }
}