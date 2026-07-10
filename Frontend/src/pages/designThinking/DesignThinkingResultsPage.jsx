import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import MethodologyJourneyResults from "../../features/methodologyExperience/shared/MethodologyJourneyResults";
import {
  adaptDesignThinkingResults,
  adaptGenericMethodologyResults,
} from "../../features/methodologyExperience/methodologies/designThinking/designThinkingResultsAdapter";
import { isMethodologyExperienceV2Enabled } from "../../features/methodologyExperience/engine/featureFlags";

function DesignThinkingResultsPage() {
  const { attemptId } = useParams();
  const isExperienceV2Enabled = isMethodologyExperienceV2Enabled();

  const [results, setResults] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadResults = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(
        `/design-thinking/simulations/${attemptId}/results`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setResults(response.data);
    } catch (error) {
      console.error("Error cargando resultados:", error);

      if (error.response) {
        setMessage(
          `Error ${error.response.status}: ${JSON.stringify(error.response.data)}`
        );
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadResults();
  }, [attemptId]);

  const phaseInsights = useMemo(() => {
    if (!results?.phaseScores?.length) {
      return {
        strongest: null,
        weakest: null,
        sorted: [],
      };
    }

    const sorted = [...results.phaseScores].sort(
      (a, b) => Number(b.score || 0) - Number(a.score || 0)
    );

    return {
      strongest: sorted[0],
      weakest: sorted[sorted.length - 1],
      sorted,
    };
  }, [results]);

  const methodologyJourney = useMemo(() => {
    if (!isExperienceV2Enabled || !results) return null;

    return results.methodologyCode === "DesignThinking"
      ? adaptDesignThinkingResults(results)
      : adaptGenericMethodologyResults(results);
  }, [isExperienceV2Enabled, results]);

  if (loading) {
    return (
      <div className="results-page-pro">
        <div className="results-loading-card">
          <div className="loader-ring"></div>
          <h2>Cargando resultados...</h2>
          <p>Estamos preparando tu reporte de desempeño metodológico.</p>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className="results-page-pro">
        <div className="results-card-pro">
          <h2>No se encontraron resultados.</h2>
          {message && <div className="message pro-message">{message}</div>}
        </div>
      </div>
    );
  }

  const finalScoreRounded = roundScore(results.finalScore);
  const methodologyName =
    results.methodologyName || inferMethodologyFromPhases(results.phaseScores);
  const statusLabel = translateStatus(results.status);
  const finalTraffic = getTrafficLight(finalScoreRounded);

  return (
    <div className="results-page-pro">
      {message && <div className="message pro-message">{message}</div>}

      <section className="results-hero">
        <div className="results-hero-left">
          <span className="eyebrow">Resultado final</span>

          <h1>{results.scenarioTitle}</h1>

          <p>
            Analiza tu desempeño por fase, interpreta tus KPIs simulados y detecta
            fortalezas y puntos de mejora dentro de la metodología aplicada.
          </p>

          <div className={`traffic-light-pill hero-traffic ${finalTraffic.className}`}>
            <span>{finalTraffic.icon}</span>
            {finalTraffic.label} · {finalTraffic.description}
          </div>
        </div>

        <div className="results-hero-score">
          <span>Score final</span>
          <strong>{finalScoreRounded}</strong>
          <p>Desempeño global de la simulación</p>
        </div>
      </section>

      <section className="results-top-stats">
        <div className="results-mini-stat">
          <span>Metodología</span>
          <strong>{methodologyName}</strong>
        </div>

        <div className="results-mini-stat">
          <span>Estado</span>
          <strong>{statusLabel}</strong>
        </div>

        <div className="results-mini-stat">
          <span>Fases evaluadas</span>
          <strong>{results.phaseScores?.length || 0}</strong>
        </div>

        <div className="results-mini-stat">
          <span>KPIs</span>
          <strong>{results.kpiResults?.length || 0}</strong>
        </div>
      </section>

      <section className="results-summary-grid">
        <div className="results-highlight-card success">
          <span>Fase más dominada</span>
          <h3>{phaseInsights.strongest?.phaseName || "Sin datos"}</h3>
          <strong>
            {phaseInsights.strongest
              ? roundScore(phaseInsights.strongest.score)
              : 0}{" "}
            / 100
          </strong>
          <p>Mayor rendimiento dentro de la simulación.</p>
        </div>

        <div className="results-highlight-card warning">
          <span>Fase con mayor refuerzo</span>
          <h3>{phaseInsights.weakest?.phaseName || "Sin datos"}</h3>
          <strong>
            {phaseInsights.weakest ? roundScore(phaseInsights.weakest.score) : 0}{" "}
            / 100
          </strong>
          <p>Es el punto donde debes mejorar primero.</p>
        </div>
      </section>

      {methodologyJourney && <MethodologyJourneyResults journey={methodologyJourney} />}

      <section className="results-card-pro">
        <div className="results-section-header">
          <div>
            <span className="eyebrow">Retroalimentación</span>
            <h2>Retroalimentación final</h2>
          </div>
        </div>

        <div className="results-feedback-box">
          <p>{results.finalFeedback}</p>
        </div>
      </section>

      <section className="results-grid-two">
        <div className="results-card-pro">
          <div className="results-section-header">
            <div>
              <span className="eyebrow">Gráfico de desempeño</span>
              <h2>Dominio por fase</h2>
            </div>

            <span className="results-chip">Semáforo</span>
          </div>

          {phaseInsights.sorted.length === 0 ? (
            <div className="results-empty-state">No hay fases evaluadas.</div>
          ) : (
            <div className="phase-bar-chart">
              {phaseInsights.sorted.map((phase, index) => {
                const score = roundScore(phase.score);
                const traffic = getTrafficLight(score);

                return (
                  <div key={`${phase.phaseName}-${index}`} className="phase-bar-row">
                    <div className="phase-bar-labels">
                      <div>
                        <h4>{phase.phaseName}</h4>

                        <div className={`traffic-light-pill ${traffic.className}`}>
                          <span>{traffic.icon}</span>
                          {traffic.label} · {traffic.description}
                        </div>
                      </div>

                      <strong>{score}</strong>
                    </div>

                    <div className="phase-bar-track">
                      <div
                        className={`phase-bar-fill traffic-${traffic.className}`}
                        style={{ width: `${Math.min(score, 100)}%` }}
                      ></div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        <div className="results-card-pro">
          <div className="results-section-header">
            <div>
              <span className="eyebrow">Resumen</span>
              <h2>Interpretación rápida</h2>
            </div>
          </div>

          <div className="results-insight-list">
            <div className="results-insight-item">
              <span>Score final</span>
              <strong>{finalScoreRounded}/100</strong>
            </div>

            <div className="results-insight-item">
              <span>Fase mejor valorada</span>
              <strong>{phaseInsights.strongest?.phaseName || "Sin datos"}</strong>
            </div>

            <div className="results-insight-item">
              <span>Fase con menor score</span>
              <strong>{phaseInsights.weakest?.phaseName || "Sin datos"}</strong>
            </div>

            <div className="results-insight-item">
              <span>Nivel general</span>
              <strong>{getPerformanceLabel(finalScoreRounded)}</strong>
            </div>

            <div className="traffic-legend-box">
              <h4>Semáforo de desempeño</h4>

              <div>
                <span className="legend-light strong">●</span>
                <strong>Fuerte:</strong>
                <p>70 a 100 puntos</p>
              </div>

              <div>
                <span className="legend-light medium">●</span>
                <strong>Medio:</strong>
                <p>50 a 69 puntos</p>
              </div>

              <div>
                <span className="legend-light weak">●</span>
                <strong>Débil:</strong>
                <p>0 a 49 puntos</p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="results-card-pro">
        <div className="results-section-header">
          <div>
            <span className="eyebrow">Detalle por fase</span>
            <h2>Puntaje por fase</h2>
          </div>
        </div>

        {results.phaseScores?.length === 0 ? (
          <div className="results-empty-state">No hay puntajes por fase.</div>
        ) : (
          <div className="results-phase-list">
            {results.phaseScores.map((phase) => {
              const score = roundScore(phase.score);
              const traffic = getTrafficLight(score);

              return (
                <div key={phase.phaseName} className="results-phase-item">
                  <div className="results-phase-top">
                    <div>
                      <div className="phase-title-with-light">
                        <h3>{phase.phaseName}</h3>

                        <div className={`traffic-light-pill ${traffic.className}`}>
                          <span>{traffic.icon}</span>
                          {traffic.label}
                        </div>
                      </div>

                      <p>{phase.feedback}</p>
                    </div>

                    <div className={`results-phase-score traffic-score ${traffic.className}`}>
                      {score}
                    </div>
                  </div>

                  <div className="results-phase-progress">
                    <div
                      className={`results-phase-progress-fill traffic-${traffic.className}`}
                      style={{ width: `${Math.min(score, 100)}%` }}
                    ></div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>

      <section className="results-card-pro">
        <div className="results-section-header">
          <div>
            <span className="eyebrow">Revisión académica</span>
            <h2>Qué respondiste bien y qué debes corregir</h2>
          </div>
        </div>

        {!results.phaseReviews?.length ? (
          <div className="results-empty-state">
            No hay información de respuestas disponible para este intento.
          </div>
        ) : (
          <div className="answer-review-list">
            {results.phaseReviews.map((phase) => {
              const selectedOptions = (phase.options || []).filter(
                (option) => option.wasSelected
              );

              const missedCorrectOptions = (phase.options || []).filter(
                (option) => option.isCorrect && !option.wasSelected
              );

              return (
                <article
                  key={phase.phaseName}
                  className="answer-review-phase"
                >
                  <div className="answer-review-phase-header">
                    <div>
                      <span className="answer-review-kicker">Fase</span>
                      <h3>{phase.phaseName}</h3>
                    </div>

                    <div className="answer-review-selection-score">
                      <span>Selección</span>
                      <strong>{roundScore(phase.selectionScore)}/100</strong>
                    </div>
                  </div>

                  <div className="answer-review-grid">
                    <div className="answer-review-column">
                      <h4>Tus selecciones</h4>

                      {selectedOptions.length === 0 ? (
                        <p className="answer-review-empty">
                          No se registraron opciones seleccionadas.
                        </p>
                      ) : (
                        <div className="answer-option-list">
                          {selectedOptions.map((option) => (
                            <div
                              key={option.optionId}
                              className={`answer-option-card ${
                                option.isCorrect
                                  ? "answer-option-correct"
                                  : "answer-option-incorrect"
                              }`}
                            >
                              <div className="answer-option-icon">
                                {option.isCorrect ? "✓" : "✕"}
                              </div>

                              <div>
                                <strong>{option.text}</strong>
                                <span>
                                  {option.isCorrect
                                    ? "Respuesta correcta seleccionada"
                                    : "Respuesta incorrecta seleccionada"}
                                </span>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>

                    <div className="answer-review-column">
                      <h4>Respuestas correctas que faltaron</h4>

                      {missedCorrectOptions.length === 0 ? (
                        <div className="answer-review-success">
                          <strong>No omitiste respuestas correctas.</strong>
                          <p>
                            Todas las opciones correctas disponibles fueron
                            seleccionadas.
                          </p>
                        </div>
                      ) : (
                        <div className="answer-option-list">
                          {missedCorrectOptions.map((option) => (
                            <div
                              key={option.optionId}
                              className="answer-option-card answer-option-missed"
                            >
                              <div className="answer-option-icon">!</div>

                              <div>
                                <strong>{option.text}</strong>
                                <span>
                                  Era correcta, pero no fue seleccionada
                                </span>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>

                  {phase.selectionFeedback && (
                    <div className="answer-review-feedback">
                      <strong>Evaluación de la selección</strong>
                      <p>{phase.selectionFeedback}</p>
                    </div>
                  )}

                  <div className="answer-review-written">
                    <div className="answer-review-written-header">
                      <h4>Justificación estratégica</h4>
                      <span>
                        {roundScore(phase.textAnswerScore)}/100
                      </span>
                    </div>

                    <div className="answer-review-written-response">
                      <strong>Tu respuesta</strong>
                      <p>
                        {phase.textAnswer?.trim()
                          ? phase.textAnswer
                          : "No se registró una justificación escrita."}
                      </p>
                    </div>

                    {phase.textAnswerFeedback && (
                      <div className="answer-review-written-feedback">
                        <strong>Retroalimentación</strong>
                        <p>{phase.textAnswerFeedback}</p>
                      </div>
                    )}
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="results-card-pro">
        <div className="results-section-header">
          <div>
            <span className="eyebrow">Indicadores</span>
            <h2>KPIs simulados</h2>
          </div>
        </div>

        {results.kpiResults?.length === 0 ? (
          <div className="results-empty-state">No hay KPIs calculados.</div>
        ) : (
          <div className="results-kpi-grid">
            {results.kpiResults.map((kpi) => {
              const initial = Number(kpi.initialValue || 0);
              const final = Number(kpi.finalValue || 0);
              const diff = final - initial;

              return (
                <div key={kpi.kpiName} className="results-kpi-card">
                  <span>{kpi.kpiName}</span>

                  <strong>
                    {roundScore(final)} {kpi.unit}
                  </strong>

                  <div className="results-kpi-values">
                    <div>
                      <small>Inicial</small>
                      <p>
                        {roundScore(initial)} {kpi.unit}
                      </p>
                    </div>

                    <div>
                      <small>Final</small>
                      <p>
                        {roundScore(final)} {kpi.unit}
                      </p>
                    </div>

                    <div>
                      <small>Variación</small>
                      <p className={diff >= 0 ? "kpi-positive" : "kpi-negative"}>
                        {diff >= 0 ? "+" : ""}
                        {roundScore(diff)} {kpi.unit}
                      </p>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}

function roundScore(value) {
  return Math.round(Number(value || 0) * 100) / 100;
}

function translateStatus(status) {
  switch (status) {
    case "Finished":
      return "Finalizada";
    case "InProgress":
      return "En progreso";
    case "Pending":
      return "Pendiente";
    default:
      return status || "Sin estado";
  }
}

function getPerformanceLabel(score) {
  if (score >= 85) return "Excelente";
  if (score >= 70) return "Bueno";
  if (score >= 50) return "Intermedio";
  return "Bajo";
}

function getTrafficLight(score) {
  if (score >= 70) {
    return {
      label: "Fuerte",
      className: "strong",
      icon: "●",
      description: "Dominio adecuado de la fase",
    };
  }

  if (score >= 50) {
    return {
      label: "Medio",
      className: "medium",
      icon: "●",
      description: "Requiere mayor profundidad",
    };
  }

  return {
    label: "Débil",
    className: "weak",
    icon: "●",
    description: "Necesita refuerzo prioritario",
  };
}

function inferMethodologyFromPhases(phases) {
  if (!phases || phases.length === 0) return "Metodología";

  const names = phases.map((p) => (p.phaseName || "").toLowerCase());

  if (
    names.some((n) => n.includes("empatizar")) ||
    names.some((n) => n.includes("idear")) ||
    names.some((n) => n.includes("prototipar"))
  ) {
    return "Design Thinking";
  }

  if (
    names.some((n) => n.includes("proceso")) ||
    names.some((n) => n.includes("cuellos")) ||
    names.some((n) => n.includes("indicadores"))
  ) {
    return "Business Process Management";
  }

  if (
    names.some((n) => n.includes("diagnóstico")) ||
    names.some((n) => n.includes("brechas")) ||
    names.some((n) => n.includes("transformación"))
  ) {
    return "Madurez Digital";
  }

  if (
    names.some((n) => n.includes("hipótesis")) ||
    names.some((n) => n.includes("mvp")) ||
    names.some((n) => n.includes("validación"))
  ) {
    return "Lean Startup";
  }

  return "Metodología";
}

export default DesignThinkingResultsPage;
