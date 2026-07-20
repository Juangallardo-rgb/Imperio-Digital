import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api from "../../api/api";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";
import { getToken } from "../../utils/auth";
import { CourseResultsBarChart } from "./components/CourseResultsCharts";
import "./courseResults.css";

const DETAIL_EVENTS = ["ResultsChanged"];

function CourseSimulationResultDetailPage() {
  const { courseId, attemptId } = useParams();
  const [results, setResults] = useState(null);
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
        const response = await api.get(
          `/courses/${courseId}/attempts/${attemptId}/results`,
          { headers: { Authorization: `Bearer ${token}` } }
        );

        setResults(response.data);
        setMessage("");
      } catch (error) {
        console.error("Error cargando detalle docente:", error);

        if (showLoader) setResults(null);
        setMessage(getErrorMessage(error));
      } finally {
        if (showLoader) setLoading(false);
      }
    },
    [attemptId, courseId]
  );

  const refreshResults = useCallback(() => loadResults(false), [loadResults]);

  useRealtimeRefresh(DETAIL_EVENTS, refreshResults, 15000);

  useEffect(() => {
    void loadResults(true);
  }, [loadResults]);

  const chartPhases = useMemo(
    () =>
      (results?.phaseScores || []).map((phase, index) => ({
        phaseName: phase.phaseName,
        phaseOrder: index + 1,
        averageScore: Number(phase.score),
        studentsEvaluated: 1,
      })),
    [results]
  );

  if (loading) {
    return (
      <div className="pro-page course-results-page" aria-busy="true">
        <div className="course-results-loading-header" />
        <div className="course-results-loading-tabs" />
      </div>
    );
  }

  if (!results) {
    return (
      <div className="pro-page course-results-page">
        <div className="course-results-state-card" role="alert">
          <span className="eyebrow">Reporte docente</span>
          <h1>No se pudo abrir este reporte</h1>
          <p>{message}</p>
          <Link to={`/courses/${courseId}/results`}>Volver a resultados</Link>
        </div>
      </div>
    );
  }

  const status = getStatusPresentation(results.status);
  const finalScore = results.finalScore;

  return (
    <div className="pro-page course-results-page student-attempt-report-page">
      <section className="student-report-hero">
        <div>
          <span className="eyebrow">Reporte individual</span>
          <h1>{results.studentName || "Estudiante"}</h1>
          <p>{results.studentEmail}</p>
          <div className="student-report-context">
            <span>{results.scenarioTitle}</span>
            <span>{results.methodologyName}</span>
            <span className={`course-attempt-status ${status.className}`}>
              {status.label}
            </span>
          </div>
        </div>

        <div className="student-report-hero-score">
          <span>Resultado general</span>
          <strong>{finalScore === null ? "--" : Math.round(finalScore)}</strong>
          <small>{finalScore === null ? "Intento no finalizado" : "Sobre 100 puntos"}</small>
        </div>

        <Link className="course-results-back" to={`/courses/${courseId}/results`}>
          Volver al curso
        </Link>
      </section>

      {message && <div className="message pro-message">{message}</div>}

      {!results.isCompleteReport && (
        <section className="incomplete-report-notice" role="status">
          <strong>Reporte en progreso</strong>
          <p>
            Sólo se muestran las fases respondidas. El resultado final, las decisiones
            esperadas y la retroalimentación completa estarán disponibles cuando el
            estudiante finalice este intento.
          </p>
        </section>
      )}

      <section className="course-results-panel attempt-navigation-panel">
        <div className="course-results-panel-heading">
          <div>
            <span className="eyebrow">Historial del escenario</span>
            <h2>Intentos del estudiante</h2>
          </div>
          <span>{results.attempts.length} intento(s)</span>
        </div>

        <div className="student-attempt-tabs" aria-label="Intentos del estudiante">
          {results.attempts.map((attempt, index) => {
            const attemptStatus = getStatusPresentation(attempt.status);
            const isCurrent = Number(attempt.attemptId) === Number(attemptId);

            return (
              <Link
                key={attempt.attemptId}
                className={isCurrent ? "active" : ""}
                to={`/courses/${courseId}/results/${attempt.attemptId}`}
                aria-current={isCurrent ? "page" : undefined}
              >
                <strong>Intento {results.attempts.length - index}</strong>
                <span>{formatDateTime(attempt.startedAt)}</span>
                <small className={attemptStatus.className}>
                  {attemptStatus.label}
                  {attempt.finalScore !== null
                    ? ` · ${Math.round(attempt.finalScore)}/100`
                    : ""}
                </small>
              </Link>
            );
          })}
        </div>
      </section>

      <section className="course-results-metrics student-report-metrics">
        <ReportMetric label="Estado" value={status.label} />
        <ReportMetric label="Inicio" value={formatDateTime(results.startedAt)} />
        <ReportMetric
          label="Finalización"
          value={results.finishedAt ? formatDateTime(results.finishedAt) : "En progreso"}
        />
        <ReportMetric
          label="Resultado"
          value={finalScore === null ? "No finalizado" : `${Math.round(finalScore)}/100`}
        />
        <ReportMetric
          label="Mejor fase"
          value={results.strongestPhase || "Sin datos"}
        />
        <ReportMetric
          label="Fase a reforzar"
          value={results.phaseToReinforce || "Sin datos"}
        />
      </section>

      <section className="course-results-chart-grid student-report-overview-grid">
        <article className="course-results-panel">
          <div className="course-results-panel-heading">
            <div>
              <span className="eyebrow">Desempeño individual</span>
              <h2>Resultado por fase</h2>
            </div>
            <span>Escala 0-100</span>
          </div>
          <CourseResultsBarChart
            phases={chartPhases}
            ariaLabel="Desempeño individual por fase, de cero a cien"
            showStudentCount={false}
          />
        </article>

        <article className="course-results-panel final-feedback-panel">
          <div className="course-results-panel-heading">
            <div>
              <span className="eyebrow">Síntesis</span>
              <h2>Retroalimentación final</h2>
            </div>
          </div>
          <div className="student-final-feedback">
            <p>
              {results.finalFeedback ||
                (results.isCompleteReport
                  ? "No hay retroalimentación final registrada."
                  : "La retroalimentación final se generará al completar el intento.")}
            </p>
          </div>
        </article>
      </section>

      <section className="course-results-panel">
        <div className="course-results-panel-heading">
          <div>
            <span className="eyebrow">Indicadores simulados</span>
            <h2>KPI obtenidos</h2>
          </div>
        </div>

        {results.kpiResults.length === 0 ? (
          <div className="course-results-empty compact">
            <strong>Sin KPI disponibles</strong>
            <p>Este intento no tiene indicadores calculados.</p>
          </div>
        ) : (
          <div className="student-kpi-grid">
            {results.kpiResults.map((kpi) => (
              <article key={kpi.kpiName} className="student-kpi-card">
                <h3>{kpi.kpiName}</h3>
                <div>
                  <span>Inicial</span>
                  <strong>{formatKpiValue(kpi.initialValue, kpi.unit)}</strong>
                </div>
                <div>
                  <span>Final</span>
                  <strong>{formatKpiValue(kpi.finalValue, kpi.unit)}</strong>
                </div>
                <p>
                  Variación: {formatSignedValue(kpi.finalValue - kpi.initialValue)}{" "}
                  {kpi.unit}
                </p>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="course-results-panel">
        <div className="course-results-panel-heading">
          <div>
            <span className="eyebrow">Lectura por etapa</span>
            <h2>Detalle de fases respondidas</h2>
          </div>
        </div>

        {results.phaseScores.length === 0 ? (
          <div className="course-results-empty compact">
            <strong>No hay fases respondidas</strong>
            <p>El estudiante todavía no ha registrado una respuesta en este intento.</p>
          </div>
        ) : (
          <div className="student-phase-report-list">
            {results.phaseScores.map((phase) => (
              <article key={phase.phaseName} className="student-phase-report-row">
                <div>
                  <h3>{phase.phaseName}</h3>
                  <p>{phase.feedback || "Sin retroalimentación registrada."}</p>
                </div>
                <strong>{Math.round(phase.score)}/100</strong>
              </article>
            ))}
          </div>
        )}
      </section>

      {results.isCompleteReport && (
        <section className="course-results-panel">
          <div className="course-results-panel-heading">
            <div>
              <span className="eyebrow">Revisión académica</span>
              <h2>Decisiones y justificación por fase</h2>
            </div>
          </div>

          {results.phaseReviews.length === 0 ? (
            <div className="course-results-empty compact">
              <strong>Sin respuestas detalladas</strong>
              <p>Este intento no cuenta con una revisión de decisiones guardada.</p>
            </div>
          ) : (
            <div className="teacher-answer-review-list">
              {results.phaseReviews.map((phase) => (
                <PhaseAnswerReview key={phase.phaseName} phase={phase} />
              ))}
            </div>
          )}
        </section>
      )}
    </div>
  );
}

function ReportMetric({ label, value }) {
  return (
    <article className="course-results-metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </article>
  );
}

function PhaseAnswerReview({ phase }) {
  const selectedOptions = (phase.options || []).filter(
    (option) => option.wasSelected
  );
  const expectedOptions = (phase.options || []).filter(
    (option) => option.isCorrect && !option.wasSelected
  );

  return (
    <article className="teacher-answer-review-phase">
      <div className="teacher-answer-review-header">
        <div>
          <span>Fase</span>
          <h3>{phase.phaseName}</h3>
        </div>
        <strong>{Math.round(phase.selectionScore)}/100</strong>
      </div>

      <div className="teacher-answer-columns">
        <div>
          <h4>Decisiones seleccionadas</h4>
          {selectedOptions.length === 0 ? (
            <p className="teacher-answer-empty">No se registraron selecciones.</p>
          ) : (
            <div className="teacher-decision-list">
              {selectedOptions.map((option) => (
                <DecisionItem key={option.optionId} option={option} />
              ))}
            </div>
          )}
        </div>

        <div>
          <h4>Criterios esperados no seleccionados</h4>
          {expectedOptions.length === 0 ? (
            <p className="teacher-answer-empty positive">
              No quedaron decisiones adecuadas pendientes.
            </p>
          ) : (
            <div className="teacher-decision-list">
              {expectedOptions.map((option) => (
                <DecisionItem key={option.optionId} option={option} missed />
              ))}
            </div>
          )}
        </div>
      </div>

      {phase.selectionFeedback && (
        <div className="teacher-selection-feedback">
          <strong>Retroalimentación sobre las decisiones</strong>
          <p>{phase.selectionFeedback}</p>
        </div>
      )}

      <div className="teacher-written-answer">
        <div>
          <h4>Justificación estratégica</h4>
          <span>{Math.round(phase.textAnswerScore)}/100</span>
        </div>
        <blockquote>
          {phase.textAnswer?.trim()
            ? phase.textAnswer
            : "No se registró una respuesta textual."}
        </blockquote>
        <p>
          {phase.textAnswerFeedback ||
            "No hay retroalimentación específica para la respuesta textual."}
        </p>
      </div>
    </article>
  );
}

function DecisionItem({ option, missed = false }) {
  const expectedCriteria = [
    option.expectedImpactLevel && `Impacto ${option.expectedImpactLevel}`,
    option.expectedEffortLevel && `Esfuerzo ${option.expectedEffortLevel}`,
    option.expectedViabilityLevel && `Viabilidad ${option.expectedViabilityLevel}`,
  ].filter(Boolean);
  const tone = missed ? "expected" : option.isCorrect ? "adequate" : "improve";
  const label = missed
    ? "Criterio esperado"
    : option.isCorrect
      ? "Decisión adecuada"
      : "Decisión por mejorar";

  return (
    <div className={`teacher-decision-item ${tone}`}>
      <span>{label}</span>
      <strong>{option.text}</strong>
      {expectedCriteria.length > 0 && <small>{expectedCriteria.join(" · ")}</small>}
    </div>
  );
}

function getStatusPresentation(status) {
  const normalizedStatus = String(status || "").trim().toLowerCase();

  if (["finished", "finalizada", "completed"].includes(normalizedStatus)) {
    return { label: "Finalizado", className: "finished" };
  }

  return { label: "En progreso", className: "in-progress" };
}

function getErrorMessage(error) {
  if (error?.response?.status === 403) {
    return "No tienes permiso para revisar este intento.";
  }

  if (error?.response?.status === 404) {
    return "El intento no pertenece a este curso o ya no está disponible.";
  }

  return "No se pudo cargar el reporte. Comprueba la conexión e inténtalo nuevamente.";
}

function formatDateTime(value) {
  if (!value) return "Sin fecha";

  return new Intl.DateTimeFormat("es-EC", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatKpiValue(value, unit) {
  const numericValue = Number(value);
  const formattedValue = Number.isInteger(numericValue)
    ? numericValue
    : numericValue.toFixed(2);

  return `${formattedValue} ${unit || ""}`.trim();
}

function formatSignedValue(value) {
  const numericValue = Number(value);
  const formattedValue = Number.isInteger(numericValue)
    ? numericValue
    : numericValue.toFixed(2);

  return numericValue > 0 ? `+${formattedValue}` : formattedValue;
}

export default CourseSimulationResultDetailPage;
