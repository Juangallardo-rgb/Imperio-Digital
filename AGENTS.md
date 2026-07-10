# AGENTS.md — Imperio Digital

## 1. Instrucción principal

Este repositorio contiene un proyecto académico funcional llamado **Imperio Digital**.

Antes de modificar cualquier archivo:

1. Inspecciona el repositorio completo y localiza las implementaciones reales.
2. No supongas nombres de archivos, rutas, DTOs, entidades, endpoints ni propiedades.
3. Revisa las referencias y dependencias antes de renombrar o eliminar algo.
4. Conserva todo comportamiento que ya funciona.
5. Realiza cambios mínimos, cohesionados y relacionados exclusivamente con la tarea solicitada.
6. No hagas refactorizaciones generales mientras corriges una funcionalidad concreta.
7. No cambies contratos del backend sin actualizar todos sus consumidores.
8. No agregues migraciones de base de datos salvo que exista un cambio real del modelo persistido.
9. No instales nuevas dependencias de producción sin justificarlo y comunicarlo.
10. Al terminar, ejecuta las compilaciones y revisa el diff completo.

Si la tarea es ambigua o afecta varios módulos, primero presenta un plan y espera aprobación antes de escribir código.

---

# 2. Contexto funcional

Imperio Digital es un sistema web educativo para estudiantes de la carrera de Negocios Digitales.

Permite que un docente cree cursos y escenarios de transformación digital, los asigne a estudiantes y revise sus resultados.

Los estudiantes pueden:

- inscribirse en cursos;
- acceder a escenarios asignados;
- realizar simulaciones por fases;
- seleccionar decisiones;
- escribir una justificación estratégica;
- consumir presupuesto y tiempo;
- afectar nivel de riesgo y KPIs;
- recibir puntajes y retroalimentación;
- revisar resultados e historial.

Los únicos roles actualmente utilizados son:

- `Docente`
- `Estudiante`

No existe un rol Administrador implementado en el alcance actual. No agregarlo ni asumir que existe.

---

# 3. Metodologías soportadas

El sistema es multi-metodología.

Códigos internos actualmente utilizados:

- `DesignThinking`
- `BPM`
- `DigitalMaturity`
- `LeanStartup`

Nombres mostrados:

- Design Thinking
- Business Process Management
- Madurez Digital
- Lean Startup

Cada metodología posee fases, criterios y opciones distintas.

Las fases no deben codificarse globalmente suponiendo únicamente Design Thinking. Deben obtenerse desde la configuración o catálogo correspondiente al escenario.

---

# 4. Advertencia crítica sobre nombres heredados

El proyecto comenzó implementando Design Thinking y después evolucionó a una solución multi-metodología.

Por esta razón existen archivos, carpetas, namespaces, rutas y endpoints que contienen el nombre `DesignThinking` o `design-thinking`, pero actualmente son utilizados para todas las metodologías.

Ejemplos importantes:

- `src/pages/designThinking/DesignThinkingSimulationPage.jsx`
- `src/pages/designThinking/DesignThinkingResultsPage.jsx`
- `src/pages/designThinking/DesignThinkingHistoryPage.jsx`
- `DTOs/DesignThinking`
- rutas frontend `/design-thinking/...`
- endpoints backend `/api/design-thinking/...`

No asumir que estos archivos manejan exclusivamente Design Thinking.

No renombrar estas rutas, carpetas, clases o namespaces de manera automática.

Muchos enlaces, controladores, componentes y servicios dependen de esos nombres heredados. Cualquier renombrado requiere primero:

1. buscar todas las referencias;
2. presentar un plan;
3. mantener compatibilidad;
4. actualizar frontend y backend conjuntamente;
5. ejecutar pruebas de regresión.

Mientras no exista una tarea específica de refactorización, conservar los nombres actuales.

---

# 5. Stack técnico

## Backend

- ASP.NET Core Web API
- .NET 8
- Entity Framework Core
- PostgreSQL
- Npgsql
- Supabase como base de datos PostgreSQL
- JWT Bearer Authentication
- SignalR para sincronización en tiempo real
- OpenRouter para funciones asistidas por IA
- Swagger/OpenAPI

## Frontend

- React
- Vite
- React Router
- Axios
- Cliente JavaScript de Microsoft SignalR
- CSS propio
- No reemplazar el diseño actual ni agregar una librería visual sin autorización

## Despliegue

- Frontend: Vercel
- Backend: Render
- Base de datos: Supabase PostgreSQL

Los cambios deben funcionar tanto localmente como desplegados.

---

# 6. Arquitectura general

El frontend React consume la API REST de ASP.NET Core mediante Axios.

Archivo principal de Axios:

- `src/api/api.js`

La URL del backend se obtiene de:

- `VITE_API_URL`

El backend utiliza controladores, servicios, DTOs, modelos y `AppDbContext`.

Servicios importantes:

- `CourseService`
- `ScenarioService`
- `SimulationService`
- `ScoringService`
- `KpiSimulationService`
- `AiFeedbackService`
- `AiScenarioContentService`
- `OpenRouterService`
- `MethodologyCatalogService`
- `PasswordResetService`
- `RealtimeNotificationService`

No mezclar responsabilidades innecesariamente:

- Controllers: reciben y validan solicitudes HTTP.
- Services: reglas de negocio y casos de uso.
- Models/Entities: estado persistido.
- DTOs: contratos de entrada y salida.
- Infrastructure: EF Core, PostgreSQL, SignalR y servicios externos.

Mantener bajo acoplamiento y responsabilidad única.

---

# 7. Entidades principales

Antes de modificar entidades, inspeccionar las implementaciones actuales.

Entidades relevantes:

- `User`
- `Course`
- `CourseEnrollment`
- `CourseScenario`
- `Scenario`
- `Methodology`
- `MethodologyPhase`
- `ScenarioPhaseSetting`
- `ScenarioOption`
- `SimulationAttempt`
- `SimulationPhaseResponse`
- `SimulationAnswer`
- `SimulationKpiResult`

Relaciones principales:

- Un docente crea muchos cursos.
- Un estudiante puede inscribirse en muchos cursos.
- Un curso tiene muchos estudiantes mediante `CourseEnrollment`.
- Un escenario puede asignarse a varios cursos mediante `CourseScenario`.
- Un escenario utiliza una metodología.
- Un escenario tiene fases configuradas y opciones.
- Un estudiante puede realizar varios intentos.
- Un intento pertenece a un escenario y opcionalmente a un curso.
- Un intento contiene respuestas por fase.
- Una respuesta de fase contiene respuestas de selección y texto.
- Un intento finalizado contiene resultados de KPIs.

No modificar cardinalidades, claves foráneas ni reglas de eliminación sin revisar `AppDbContext` y las migraciones existentes.

---

# 8. Autenticación

La autenticación utiliza JWT.

Claims relevantes:

- identificador del usuario;
- nombre;
- correo;
- rol.

Frontend:

- `src/utils/auth.js`
- funciones esperadas:
  - `saveToken`
  - `getToken`
  - `logout`
  - `getUserFromToken`

El token se guarda actualmente en `sessionStorage`, no en `localStorage`.

Esto es intencional para permitir que una pestaña tenga sesión de docente y otra tenga sesión de estudiante sin sobrescribir el token.

No volver a guardar el token únicamente en `localStorage`.

Componentes como `Navbar`, `ProtectedRoute` y páginas del sistema pueden depender de los nombres actuales de las funciones de `auth.js`. No renombrarlas sin buscar todas sus referencias.

---

# 9. Rutas frontend críticas

Revisar siempre `App.jsx` antes de generar navegación.

Rutas actuales importantes:

- `/dashboard`
- `/courses`
- `/courses/create`
- `/courses/:id`
- `/courses/:id/results`
- `/courses/:courseId/results/:attemptId`
- `/courses/available`
- `/my-courses`
- `/my-courses/:id`
- `/design-thinking/scenarios`
- `/design-thinking/scenarios/create`
- `/design-thinking/scenarios/:id`
- `/design-thinking/published`
- `/design-thinking/simulate/:attemptId`
- `/design-thinking/results/:attemptId`
- `/design-thinking/history`

La ruta correcta para abrir una simulación es:

`/design-thinking/simulate/:attemptId`

No usar:

`/design-thinking/simulations/:attemptId`

Aunque el sistema sea multi-metodología, esas rutas heredadas se utilizan globalmente.

---

# 10. Cursos

El docente puede:

- crear cursos;
- editar cursos;
- activar o desactivar cursos;
- revisar inscritos;
- asignar escenarios;
- consultar resultados;
- consultar analítica del dashboard.

El estudiante puede:

- consultar cursos disponibles;
- inscribirse;
- consultar cursos inscritos;
- abrir el detalle de un curso;
- acceder a escenarios asignados.

Endpoints relevantes a verificar en `CoursesController`:

- `POST /api/courses`
- `GET /api/courses/my`
- `GET /api/courses/{id}`
- `PUT /api/courses/{id}`
- `GET /api/courses/available`
- `GET /api/courses/enrolled`
- `POST /api/courses/{courseId}/enroll`
- `GET /api/courses/{courseId}/student-detail`
- asignación de escenarios;
- resultados del curso;
- dashboard docente.

Regla crítica:

- El detalle docente usa el endpoint protegido para docente.
- El detalle estudiante debe usar:
  `GET /api/courses/{courseId}/student-detail`

No hacer que `StudentCourseDetailPage.jsx` consuma el endpoint exclusivo del docente.

No eliminar accidentalmente el botón para iniciar simulaciones cuando se modifica esta página.

---

# 11. Simulaciones

El flujo general es:

1. El estudiante selecciona un escenario asignado.
2. El frontend envía `scenarioId` y `courseId`.
3. El backend crea un `SimulationAttempt`.
4. El intento inicia en la primera fase habilitada.
5. El estudiante selecciona opciones y puede escribir una justificación.
6. El backend calcula puntajes, presupuesto, tiempo, riesgo y KPIs.
7. El estudiante avanza por todas las fases.
8. El backend finaliza el intento.
9. El frontend navega a la pantalla de resultados.

`SimulationService` contiene reglas sensibles. No reescribirlo completamente para una modificación pequeña.

Al modificarlo:

- conservar validación de fechas;
- conservar máximo de intentos;
- conservar validación de inscripción;
- conservar asignación del escenario al curso;
- conservar fases configuradas dinámicamente;
- conservar presupuesto;
- conservar tiempo;
- conservar riesgo;
- conservar KPIs;
- conservar trazabilidad de decisiones;
- conservar retroalimentación;
- conservar notificaciones SignalR;
- conservar orden de fases por metodología.

No codificar únicamente las fases:

- Empatizar
- Definir
- Idear
- Prototipar
- Evaluar

Esas fases pertenecen a Design Thinking. Las demás metodologías usan fases diferentes.

---

# 12. Evaluación y KPIs

Los resultados numéricos no deben depender completamente de la IA.

Responsabilidades:

- `ScoringService`: puntajes deterministas.
- `KpiSimulationService`: cálculo y simulación de KPIs.
- `AiFeedbackService`: evaluación textual y retroalimentación.
- `OpenRouter`: contenido asistido y feedback.

La IA no debe controlar directamente todas las reglas de puntuación ni reemplazar las validaciones del backend.

Los KPIs, presupuesto, tiempo, riesgo y puntajes finales deben mantenerse controlados por reglas del sistema.

---

# 13. OpenRouter e IA

OpenRouter se utiliza para:

- generación de borradores de escenarios;
- generación de opciones por metodología;
- evaluación de respuestas textuales;
- retroalimentación por fase;
- retroalimentación final.

Reglas críticas:

- Los prompts deben incluir contexto del escenario.
- Deben incluir metodología y fases exactas.
- Las opciones generadas deben validarse contra las fases habilitadas.
- Se pueden normalizar alias válidos de fases.
- No aceptar fases completamente ajenas.
- No eliminar opciones anteriores hasta tener una respuesta nueva válida.
- Usar transacciones cuando se reemplacen datos.
- Mantener el timeout ampliado utilizado para OpenRouter.
- Una falla temporal de IA no debe corromper el escenario ni dejarlo parcialmente guardado.

Inspeccionar la configuración actual antes de cambiar modelo, timeout o formato esperado.

---

# 14. SignalR y tiempo real

El sistema utiliza SignalR para que los paneles docente y estudiante se actualicen sin recargar.

Hub esperado:

- `/hubs/realtime`

Componentes esperados:

- `RealtimeHub`
- `IRealtimeNotificationService`
- `RealtimeNotificationService`
- `src/realtime/realtimeConnection.js`
- `src/hooks/useRealtimeRefresh.js`

Eventos actuales:

- `CoursesChanged`
- `EnrollmentsChanged`
- `CourseScenariosChanged`
- `ResultsChanged`

Comportamiento esperado:

- crear o editar curso actualiza los paneles;
- una inscripción actualiza cursos y estudiantes;
- asignar un escenario actualiza el detalle del curso;
- iniciar, avanzar o finalizar simulaciones actualiza dashboards y resultados.

Las notificaciones SignalR no son la fuente oficial de datos.

SignalR debe enviar una señal y el frontend debe volver a consultar la API REST.

La base de datos sigue siendo la fuente de verdad.

Una falla al enviar SignalR no debe hacer que una operación correctamente guardada en PostgreSQL se reporte como fallida.

Evitar registrar varias veces el mismo listener.

Eliminar listeners en el cleanup de React.

Conservar reconexión automática y actualización al recuperar el foco cuando ya estén implementadas.

---

# 15. Dashboard

Existe un único `DashboardPage.jsx` que muestra contenido distinto según el rol.

Docente:

- cantidad de cursos;
- estudiantes;
- intentos;
- simulaciones finalizadas;
- promedio general;
- tasa de finalización;
- rendimiento por curso;
- rendimiento por metodología;
- cursos con bajo desempeño.

Estudiante:

- historial;
- intentos;
- simulaciones finalizadas;
- simulaciones en progreso;
- promedio;
- mejor puntaje.

El dashboard debe actualizarse mediante SignalR sin recargar manualmente.

No eliminar gráficos, tarjetas o cálculos mientras se añade actualización en tiempo real.

---

# 16. Resultados finales

La pantalla principal es:

- `DesignThinkingResultsPage.jsx`

A pesar del nombre, representa resultados de todas las metodologías.

Actualmente muestra:

- score final;
- metodología;
- estado;
- fases;
- KPIs;
- mejor fase;
- fase que requiere refuerzo;
- retroalimentación final;
- puntaje y feedback por fase.

Se está incorporando o puede encontrarse parcialmente incorporada una revisión detallada de respuestas.

Comportamiento esperado después de finalizar:

- mostrar opciones correctas seleccionadas;
- mostrar opciones incorrectas seleccionadas;
- mostrar opciones correctas omitidas;
- mostrar respuesta textual del estudiante;
- mostrar puntaje de la respuesta textual;
- mostrar retroalimentación específica.

DTO esperado o en proceso:

- `SimulationResultsDto`
- `PhaseScoreDto`
- `PhaseAnswerReviewDto`
- `OptionAnswerReviewDto`
- `KpiResultDto`

Antes de modificar esta funcionalidad, inspecciona el estado real de la rama. No asumas que `PhaseReviews` ya está implementado o que todavía no lo está.

Regla de seguridad:

Mientras una simulación está en progreso, el backend no debe revelar al frontend:

- `IsCorrect`
- el puntaje interno de cada opción, cuando permita deducir la respuesta.

Esos datos pueden mostrarse únicamente en el resultado final del intento correspondiente al estudiante autenticado.

---

# 17. Escenarios

El docente puede crear escenarios manualmente o mediante IA.

Un escenario incluye, según la implementación actual:

- título;
- descripción;
- empresa o tipo de empresa;
- problema;
- usuario objetivo;
- restricciones;
- dificultad;
- metodología;
- fechas de disponibilidad;
- intentos máximos;
- fases;
- criterios;
- opciones;
- estado publicado o borrador.

Solo los escenarios publicados deben estar disponibles para iniciar una simulación.

No publicar automáticamente un escenario como efecto colateral de generar opciones.

No regenerar opciones durante una operación de publicación salvo que la implementación actual lo requiera expresamente.

---

# 18. Reglas de interfaz

Mantener el diseño visual existente.

No reemplazar páginas completas por versiones simplificadas sin comparar toda la funcionalidad anterior.

Antes de reemplazar un componente React:

1. enumera sus acciones actuales;
2. identifica botones y enlaces;
3. identifica endpoints usados;
4. identifica estados;
5. identifica eventos SignalR;
6. conserva todo lo que no esté relacionado con el cambio.

Regresiones que deben evitarse:

- eliminar el botón “Iniciar simulación”;
- navegar a una ruta inexistente;
- usar un endpoint de docente como estudiante;
- borrar secciones de resultados;
- alterar las metodologías disponibles;
- eliminar acciones durante un rediseño;
- cambiar nombres de propiedades sin actualizar el backend.

No mostrar emojis en interfaces académicas salvo que ya formen parte del diseño y el usuario lo solicite.

---

# 19. Comandos de validación

Identifica primero las carpetas reales del backend y frontend.

Después de cambios en backend:

```bash
dotnet build