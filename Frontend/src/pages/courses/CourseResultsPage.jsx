import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api from "../../api/api";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";
import { getToken } from "../../utils/auth";
import {
  AnimatedMetric,
  CourseResultsBarChart,
  PerformanceLegend,
  PhaseDistributionChart,
  StudentPhaseHeatmap,
} from "./components/CourseResultsCharts";
import "./courseResults.css";

const RESULTS_EVENTS = [
  "EnrollmentsChanged",
  "CourseScenariosChanged",
  "ResultsChanged",
];

function CourseResultsPage() {
  const { id } = useParams();
  const [results, setResults] = useState(null);
  const [selectedScenarioId, setSelectedScenarioId] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadResults = useCallback(
    async (showLoader = false) => {
      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        const token = getToken();
        const response = await api.get(`/courses/${id}/results/analytics`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        const nextResults = response.data;

        setMessage("");
        setResults(nextResults);
        setSelectedScenarioId((currentScenarioId) => {
          const scenarios = nextResults?.scenarios || [];
          const currentScenarioExists = scenarios.some(
            (scenario) => scenario.scenarioId === currentScenarioId
          );

          return currentScenarioExists
            ? currentScenarioId
            : scenarios[0]?.scenarioId ?? null;
        });
      } catch (error) {
        console.error("Error cargando analítica del curso:", error);

        if (showLoader) {
          setResults(null);
        }

        setMessage(getErrorMessage(error));
      } finally {
        if (showLoader) {
          setLoading(false);
        }
      }
    },
    [id]
  );

  const refreshResults = useCallback(() => loadResults(false), [loadResults]);

  useRealtimeRefresh(RESULTS_EVENTS, refreshResults, 15000);

  useEffect(() => {
    void loadResults(true);
  }, [loadResults]);

  const selectedScenario = useMemo(
    () =>
      results?.scenarios?.find(
        (scenario) => scenario.scenarioId === selectedScenarioId
      ) || null,
    [results, selectedScenarioId]
  );

  const filteredStudents = useMemo(() => {
    const students = selectedScenario?.students || [];
    const normalizedSearch = normalizeText(searchTerm);

    if (!normalizedSearch) return students;

    return students.filter((student) =>
      normalizeText(`${student.studentName} ${student.studentEmail}`).includes(
        normalizedSearch
      )
    );
  }, [searchTerm, selectedScenario]);

  const findings = useMemo(
    () => buildGroupFindings(selectedScenario),
    [selectedScenario]
  );

  if (loading) {
    return <CourseResultsLoading />;
  }

  if (!results) {
    return (
      <div className="pro-page course-results-page">
        <div className="course-results-state-card" role="alert">
          <span className="eyebrow">Analítica docente</span>
          <h1>No se pudieron cargar los resultados</h1>
          <p>{message || "No existe información disponible para este curso."}</p>
          <button type="button" onClick={() => void loadResults(true)}>
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page course-results-page">
      <section className="course-results-header">
        <div>
          <span className="eyebrow">Analítica docente</span>
          <h1>{results.courseName}</h1>
          <p>
            Analiza el desempeño del grupo por escenario y fase para identificar
            fortalezas, avances y oportunidades de refuerzo.
          </p>
          <div className="course-results-course-meta">
            <span>Código {results.courseCode || "Sin código"}</span>
            <span>{results.totalStudents} estudiante(s)</span>
            <span>{results.totalScenarios} escenario(s)</span>
          </div>
        </div>

        <Link className="course-results-back" to={`/courses/${id}`}>
          Volver al curso
        </Link>
      </section>

      {message && <div className="message pro-message">{message}</div>}

      {results.scenarios.length === 0 ? (
        <section className="course-results-state-card">
          <span className="eyebrow">Escenarios</span>
          <h2>Este curso todavía no tiene escenarios asignados</h2>
          <p>
            Asigna un escenario desde el detalle del curso para comenzar a reunir
            resultados académicos.
          </p>
          <Link to={`/courses/${id}`}>Ir al detalle del curso</Link>
        </section>
      ) : (
        <>
          <section className="course-results-section scenario-navigation-section">
            <div className="course-results-section-heading">
              <div>
                <span className="eyebrow">Escenarios asignados</span>
                <h2>Selecciona una simulación</h2>
              </div>
              <p>Los indicadores se actualizan para el escenario elegido.</p>
            </div>

            <div
              className="scenario-results-tabs"
              role="tablist"
              aria-label="Escenarios del curso"
            >
              {results.scenarios.map((scenario) => {
                const isSelected = scenario.scenarioId === selectedScenarioId;

                return (
                  <button
                    key={scenario.scenarioId}
                    type="button"
                    className={isSelected ? "active" : ""}
                    role="tab"
                    aria-selected={isSelected}
                    onClick={() => setSelectedScenarioId(scenario.scenarioId)}
                  >
                    <strong>{scenario.scenarioTitle}</strong>
                    <span>{scenario.methodologyName}</span>
                    <small>
                      {scenario.startedStudents} iniciaron · {scenario.completedStudents}{" "}
                      finalizaron
                    </small>
                  </button>
                );
              })}
            </div>
          </section>

          {selectedScenario && (
            <ScenarioResultsDashboard
              key={selectedScenario.scenarioId}
              courseId={id}
              scenario={selectedScenario}
              findings={findings}
              filteredStudents={filteredStudents}
              searchTerm={searchTerm}
              onSearchChange={setSearchTerm}
            />
          )}
        </>
      )}
    </div>
  );
}

function ScenarioResultsDashboard({
  courseId,
  scenario,
  findings,
  filteredStudents,
  searchTerm,
  onSearchChange,
}) {
  const phases = scenario.phaseAnalytics || [];
  const students = scenario.students || [];
  const averageScore = scenario.averageScore;
  const hasAttempts = scenario.startedStudents > 0;

  return (
    <div className="course-results-dashboard">
      <section className="course-results-scenario-summary">
        <div>
          <span className="eyebrow">Escenario seleccionado</span>
          <h2>{scenario.scenarioTitle}</h2>
          <p>{scenario.methodologyName}</p>
        </div>
        <span className="scenario-methodology-badge">{scenario.methodologyCode}</span>
      </section>

      <section className="course-results-metrics" aria-label="Resumen del escenario">
        <AnimatedMetric
          label="Estudiantes del curso"
          value={scenario.totalStudents}
          detail="Inscritos actualmente"
        />
        <AnimatedMetric
          label="Iniciaron"
          value={scenario.startedStudents}
          detail="Con al menos un intento"
          tone="blue"
        />
        <AnimatedMetric
          label="Finalizaron"
          value={scenario.completedStudents}
          detail="Con al menos un intento finalizado"
          tone="green"
        />
        <AnimatedMetric
          label="En progreso"
          value={scenario.inProgressStudents}
          detail="Intento más reciente abierto"
          tone="orange"
        />
        <AnimatedMetric
          label="Finalización"
          value={hasAttempts ? `${Math.round(scenario.completionRate)}%` : "Sin datos"}
          detail="Al menos una finalización entre quienes iniciaron"
          tone="blue"
        />
        <AnimatedMetric
          label="Fase más sólida"
          value={scenario.strongestPhase || "Sin datos"}
          detail="Mayor promedio grupal"
          tone="green"
        />
        <AnimatedMetric
          label="Fase a reforzar"
          value={scenario.phaseToReinforce || "Sin datos"}
          detail="Oportunidad formativa"
          tone="orange"
        />
        <AnimatedMetric
          label="Promedio general"
          value={averageScore === null ? "Sin datos" : Math.round(averageScore)}
          detail="Último intento finalizado por estudiante"
        />
      </section>

      {!hasAttempts && (
        <section className="course-results-state-card inline">
          <h2>Este escenario aún no tiene intentos</h2>
          <p>
            Los indicadores, gráficos y hallazgos aparecerán cuando los estudiantes
            comiencen la simulación.
          </p>
        </section>
      )}

      <section className="course-results-chart-grid">
        <article className="course-results-panel">
          <div className="course-results-panel-heading">
            <div>
              <span className="eyebrow">Desempeño grupal</span>
              <h2>Promedio por fase</h2>
            </div>
            <span>Escala 0-100</span>
          </div>
          <CourseResultsBarChart phases={phases} />
        </article>

        <article className="course-results-panel">
          <div className="course-results-panel-heading">
            <div>
              <span className="eyebrow">Distribución</span>
              <h2>Niveles por fase</h2>
            </div>
          </div>
          <PhaseDistributionChart phases={phases} />
          <PerformanceLegend />
        </article>
      </section>

      <section className="course-results-panel heatmap-panel">
        <div className="course-results-panel-heading">
          <div>
            <span className="eyebrow">Vista diagnóstica</span>
            <h2>Mapa de desempeño por estudiante y fase</h2>
          </div>
          <p>Se usa el intento finalizado más reciente de cada estudiante.</p>
        </div>
        <StudentPhaseHeatmap students={students} phases={phases} />
        <PerformanceLegend />
      </section>

      <section className="course-results-panel group-findings-panel">
        <div className="course-results-panel-heading">
          <div>
            <span className="eyebrow">Lectura automática</span>
            <h2>Hallazgos del grupo</h2>
          </div>
          <span>Reglas deterministas</span>
        </div>

        <div className="group-findings-list">
          {findings.map((finding, index) => (
            <div key={`${finding}-${index}`}>
              <span aria-hidden="true">{index + 1}</span>
              <p>{finding}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="course-results-panel individual-reports-panel">
        <div className="course-results-panel-heading reports-heading">
          <div>
            <span className="eyebrow">Seguimiento formativo</span>
            <h2>Reportes individuales</h2>
            <p>
              Consulta decisiones, retroalimentación, fases, KPI e intentos de cada
              estudiante.
            </p>
          </div>

          <label className="student-report-search">
            <span>Buscar estudiante</span>
            <input
              type="search"
              value={searchTerm}
              onChange={(event) => onSearchChange(event.target.value)}
              placeholder="Nombre o correo"
            />
          </label>
        </div>

        {filteredStudents.length === 0 ? (
          <div className="course-results-empty compact">
            <strong>No hay coincidencias</strong>
            <p>Prueba con otro nombre o correo electrónico.</p>
          </div>
        ) : (
          <div className="student-report-grid">
            {filteredStudents.map((student) => (
              <StudentReportCard
                key={student.studentId}
                courseId={courseId}
                methodologyName={scenario.methodologyName}
                student={student}
              />
            ))}
          </div>
        )}
      </section>
    </div>
  );
}

function StudentReportCard({ courseId, methodologyName, student }) {
  const status = getStatusPresentation(student.latestAttemptStatus);
  const lastAttemptDate = student.latestAttemptStartedAt
    ? formatDateTime(student.latestAttemptStartedAt)
    : "Sin intentos";

  return (
    <article className="student-report-card">
      <div className="student-report-card-header">
        <div>
          <h3>{student.studentName}</h3>
          <p>{student.studentEmail}</p>
        </div>
        <span className={`course-attempt-status ${status.className}`}>
          {status.label}
        </span>
      </div>

      <dl>
        <div>
          <dt>Intentos</dt>
          <dd>{student.attemptCount}</dd>
        </div>
        <div>
          <dt>Último intento</dt>
          <dd>{lastAttemptDate}</dd>
        </div>
        <div>
          <dt>Metodología</dt>
          <dd>{methodologyName}</dd>
        </div>
      </dl>

      {student.reportAttemptId ? (
        <Link to={`/courses/${courseId}/results/${student.reportAttemptId}`}>
          Ver reporte
        </Link>
      ) : (
        <span className="student-report-disabled">Sin reporte disponible</span>
      )}
    </article>
  );
}

function CourseResultsLoading() {
  return (
    <div className="pro-page course-results-page" aria-busy="true">
      <div className="course-results-loading-header" />
      <div className="course-results-loading-tabs" />
      <div className="course-results-loading-metrics">
        {Array.from({ length: 8 }).map((_, index) => (
          <div key={index} />
        ))}
      </div>
    </div>
  );
}

function buildGroupFindings(scenario) {
  if (!scenario) return [];

  if (scenario.startedStudents === 0) {
    return [
      "El escenario todavía no registra intentos; aún no es posible identificar patrones grupales.",
      `El curso tiene ${scenario.totalStudents} estudiante(s) que podrán participar en esta simulación.`,
    ];
  }

  const findings = [];
  const evaluatedPhases = (scenario.phaseAnalytics || []).filter(
    (phase) => phase.averageScore !== null
  );

  if (scenario.strongestPhase) {
    findings.push(
      `La fase con mejor desempeño grupal fue ${scenario.strongestPhase}.`
    );
  }

  if (scenario.phaseToReinforce) {
    findings.push(
      `La principal oportunidad de refuerzo del grupo se encuentra en ${scenario.phaseToReinforce}.`
    );
  }

  if (scenario.completionRate >= 50) {
    findings.push(
      "La mayoría de quienes iniciaron el escenario ya finalizó al menos un intento."
    );
  } else {
    findings.push(
      "La mayoría de quienes iniciaron todavía no ha finalizado ningún intento."
    );
  }

  if (scenario.inProgressStudents > 0) {
    findings.push(
      `${scenario.inProgressStudents} estudiante(s) mantiene(n) su intento más reciente en progreso.`
    );
  }

  if (evaluatedPhases.length >= 3) {
    const scores = evaluatedPhases.map((phase) => Number(phase.averageScore));
    const spread = Math.max(...scores) - Math.min(...scores);

    findings.push(
      spread <= 10
        ? "El desempeño es estable entre las fases evaluadas del escenario."
        : "Existen diferencias relevantes entre fases que pueden orientar el refuerzo académico."
    );
  }

  return findings;
}

function getStatusPresentation(status) {
  const normalizedStatus = String(status || "").trim().toLowerCase();

  if (["finished", "finalizada", "completed"].includes(normalizedStatus)) {
    return { label: "Finalizado", className: "finished" };
  }

  if (normalizedStatus === "notstarted") {
    return { label: "Sin iniciar", className: "not-started" };
  }

  return { label: "En progreso", className: "in-progress" };
}

function getErrorMessage(error) {
  if (error?.response?.status === 403) {
    return "No tienes permiso para consultar los resultados de este curso.";
  }

  if (error?.response?.status === 404) {
    return "El curso solicitado no existe o no está bajo tu responsabilidad.";
  }

  return "No se pudo cargar la analítica. Comprueba la conexión e inténtalo nuevamente.";
}

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function formatDateTime(value) {
  if (!value) return "Sin fecha";

  return new Intl.DateTimeFormat("es-EC", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export default CourseResultsPage;
