import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

const phases = ["Empatizar", "Definir", "Idear", "Prototipar", "Evaluar", "Resultado"];

function DesignThinkingSimulationPage() {
  const { attemptId } = useParams();
  const navigate = useNavigate();

  const [current, setCurrent] = useState(null);
  const [selectedOptionIds, setSelectedOptionIds] = useState([]);
  const [textAnswer, setTextAnswer] = useState("");
  const [phaseFeedback, setPhaseFeedback] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const loadCurrent = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/design-thinking/simulations/${attemptId}/current`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.data.currentPhaseName === "Resultado") {
        navigate(`/design-thinking/results/${attemptId}`);
        return;
      }

      setCurrent(response.data);
      setSelectedOptionIds([]);
      setTextAnswer("");
      setPhaseFeedback(null);
    } catch (error) {
      console.error("Error cargando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadCurrent();
  }, [attemptId]);

  const activeKpis = useMemo(() => {
    const raw = phaseFeedback?.currentKpisJson || current?.currentKpisJson;

    if (!raw) return {};

    try {
      return JSON.parse(raw);
    } catch {
      return {};
    }
  }, [current, phaseFeedback]);

  const triggeredEvent = useMemo(() => {
    if (!phaseFeedback?.triggeredEventJson) return null;

    try {
      return JSON.parse(phaseFeedback.triggeredEventJson);
    } catch {
      return null;
    }
  }, [phaseFeedback]);

  const groupedOptions = useMemo(() => {
    if (!current) return {};

    return current.currentPhaseOptions.reduce((acc, option) => {
      if (!acc[option.optionType]) acc[option.optionType] = [];
      acc[option.optionType].push(option);
      return acc;
    }, {});
  }, [current]);

  const selectedOptions = useMemo(() => {
    if (!current) return [];

    return current.currentPhaseOptions.filter((option) =>
      selectedOptionIds.includes(option.id)
    );
  }, [current, selectedOptionIds]);

  const totals = useMemo(() => {
    return selectedOptions.reduce(
      (acc, option) => {
        acc.cost += Number(option.cost || 0);
        acc.time += Number(option.timeCost || 0);
        acc.risk += Number(option.riskImpact || 0);
        return acc;
      },
      { cost: 0, time: 0, risk: 0 }
    );
  }, [selectedOptions]);

  const maxSelections = current ? getPhaseLimit(current.currentPhaseName) : 0;

  const toggleOption = (optionId) => {
    setMessage("");

    setSelectedOptionIds((prev) => {
      if (prev.includes(optionId)) {
        return prev.filter((id) => id !== optionId);
      }

      if (prev.length >= maxSelections) {
        setMessage(`En esta fase solo puedes seleccionar máximo ${maxSelections} opciones.`);
        return prev;
      }

      return [...prev, optionId];
    });
  };

  const submitPhase = async () => {
    if (!current) return;

    if (selectedOptionIds.length === 0) {
      setMessage("Selecciona al menos una opción antes de enviar la fase.");
      return;
    }

    if (textAnswer.trim().length < 80) {
      setMessage("Escribe una justificación más completa. Mínimo recomendado: 80 caracteres.");
      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/simulations/${attemptId}/phase/${current.currentPhaseName}/submit`,
        {
          selectedOptionIds,
          textAnswer,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setPhaseFeedback(response.data);
    } catch (error) {
      console.error("Error enviando fase:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const continueNext = async () => {
    if (phaseFeedback?.isLastPhase) {
      await finishSimulation();
    } else {
      await loadCurrent();
    }
  };

  const finishSimulation = async () => {
    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      await api.post(
        `/design-thinking/simulations/${attemptId}/finish`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      navigate(`/design-thinking/results/${attemptId}`);
    } catch (error) {
      console.error("Error finalizando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="page-container">
        <div className="simulation-loading-card">
          <div className="loader-ring"></div>
          <h2>Cargando simulación...</h2>
          <p>Preparando el caso, recursos, KPIs y decisiones disponibles.</p>
        </div>
      </div>
    );
  }

  if (!current) {
    return (
      <div className="page-container">
        <div className="card">
          <h2>No se encontró la simulación</h2>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  const currentPhaseIndex = phases.indexOf(current.currentPhaseName);
  const budgetPercent = getPercent(current.remainingBudget, current.initialBudget);
  const timePercent = getPercent(current.remainingTimeWeeks, current.initialTimeWeeks);
  const riskPercent = Math.min(100, Math.max(0, Number(phaseFeedback?.riskLevel ?? current.riskLevel ?? 0)));

  return (
    <div className="simulation-page">
      <div className="simulation-hero">
        <div>
          <span className="eyebrow">Imperio Digital · Simulación Design Thinking</span>
          <h1>{current.scenarioTitle}</h1>
          <p>
            Toma decisiones bajo restricciones reales. Cada elección puede afectar tu
            presupuesto, tiempo, riesgo y KPIs de negocio.
          </p>
        </div>

        <div className="phase-pill">
          <span>Fase actual</span>
          <strong>{current.currentPhaseName}</strong>
        </div>
      </div>

      <div className="phase-stepper professional-stepper">
        {phases.map((phase, index) => {
          let className = "phase-step";

          if (index < currentPhaseIndex) className += " done";
          if (index === currentPhaseIndex) className += " active";

          return (
            <span key={phase} className={className}>
              {index + 1}. {phase}
            </span>
          );
        })}
      </div>

      {message && <div className="message simulation-message">{message}</div>}

      <div className="simulation-layout">
        <aside className="simulation-sidebar">
          <ResourceCard
            title="Presupuesto"
            value={phaseFeedback?.remainingBudget ?? current.remainingBudget}
            total={current.initialBudget}
            suffix="pts"
            percent={budgetPercent}
          />

          <ResourceCard
            title="Tiempo"
            value={phaseFeedback?.remainingTimeWeeks ?? current.remainingTimeWeeks}
            total={current.initialTimeWeeks}
            suffix="sem"
            percent={timePercent}
          />

          <div className="resource-card">
            <div className="resource-header">
              <span>Riesgo</span>
              <strong>{riskPercent}/100</strong>
            </div>
            <div className="meter">
              <div
                className={`meter-fill ${riskPercent >= 70 ? "danger" : riskPercent >= 40 ? "warning" : "success"}`}
                style={{ width: `${riskPercent}%` }}
              ></div>
            </div>
            <small>
              {riskPercent >= 70
                ? "Riesgo alto: justifica muy bien tus decisiones."
                : riskPercent >= 40
                ? "Riesgo moderado: revisa viabilidad y coherencia."
                : "Riesgo controlado."}
            </small>
          </div>

          <div className="kpi-panel">
            <h3>KPIs actuales</h3>
            <KpiItem label="Abandono" value={activeKpis.cartAbandonment} suffix="%" inverted />
            <KpiItem label="Conversión" value={activeKpis.conversionRate} suffix="%" />
            <KpiItem label="Satisfacción" value={activeKpis.satisfaction} suffix="/100" />
            <KpiItem label="Tiempo compra" value={activeKpis.purchaseTime} suffix="min" inverted />
            <KpiItem label="Adopción digital" value={activeKpis.digitalAdoption} suffix="/100" />
          </div>

          {!phaseFeedback && (
            <div className="decision-summary">
              <h3>Decisión actual</h3>
              <p><strong>Seleccionadas:</strong> {selectedOptionIds.length}/{maxSelections}</p>
              <p><strong>Costo:</strong> {totals.cost} pts</p>
              <p><strong>Tiempo:</strong> {totals.time} sem</p>
              <p><strong>Riesgo:</strong> {totals.risk >= 0 ? "+" : ""}{totals.risk}</p>
            </div>
          )}
        </aside>

        <main className="simulation-main">
          {!phaseFeedback ? (
            <div className="simulation-card">
              <div className="section-header">
                <div>
                  <span className="eyebrow">Actividad guiada</span>
                  <h2>{getPhaseTitle(current.currentPhaseName)}</h2>
                </div>
                <span className="selection-limit">
                  Máximo {maxSelections} selecciones
                </span>
              </div>

              <p className="phase-instruction">
                {getPhaseInstruction(current.currentPhaseName)}
              </p>

              {Object.keys(groupedOptions).map((type) => (
                <section key={type} className="option-group">
                  <h3>{getOptionTypeLabel(type)}</h3>

                  <div className="decision-grid">
                    {groupedOptions[type].map((option) => {
                      const isSelected = selectedOptionIds.includes(option.id);

                      return (
                        <button
                          key={option.id}
                          type="button"
                          className={`decision-card ${isSelected ? "selected" : ""}`}
                          onClick={() => toggleOption(option.id)}
                        >
                          <div className="decision-card-top">
                            <span className="option-type-badge">{getOptionTypeLabel(option.optionType)}</span>
                            {isSelected && <span className="selected-badge">Seleccionada</span>}
                          </div>

                          <p>{option.text}</p>

                          <div className="decision-meta">
                            {Number(option.cost || 0) > 0 && (
                              <span>Costo: {option.cost}</span>
                            )}
                            {Number(option.timeCost || 0) > 0 && (
                              <span>Tiempo: {option.timeCost} sem</span>
                            )}
                            {Number(option.riskImpact || 0) !== 0 && (
                              <span>Riesgo: {option.riskImpact > 0 ? "+" : ""}{option.riskImpact}</span>
                            )}
                          </div>

                          {(option.expectedImpactLevel ||
                            option.expectedEffortLevel ||
                            option.expectedViabilityLevel) && (
                            <div className="decision-extra">
                              {option.expectedImpactLevel && <small>Impacto: {option.expectedImpactLevel}</small>}
                              {option.expectedEffortLevel && <small>Esfuerzo: {option.expectedEffortLevel}</small>}
                              {option.expectedViabilityLevel && <small>Viabilidad: {option.expectedViabilityLevel}</small>}
                            </div>
                          )}
                        </button>
                      );
                    })}
                  </div>
                </section>
              ))}

              <div className="form-group simulation-textarea">
                <label>Justificación estratégica</label>
                <textarea
                  value={textAnswer}
                  onChange={(e) => setTextAnswer(e.target.value)}
                  placeholder={getTextareaPlaceholder(current.currentPhaseName)}
                />
                <small>{textAnswer.length} caracteres · mínimo recomendado: 80</small>
              </div>

              <button className="primary-action" onClick={submitPhase} disabled={submitting}>
                {submitting ? "Evaluando fase..." : "Enviar fase y ver consecuencias"}
              </button>
            </div>
          ) : (
            <div className="simulation-card feedback-card">
              <span className="eyebrow">Resultado de fase</span>
              <h2>{phaseFeedback.phaseName}</h2>

              <div className="phase-score">
                <strong>{phaseFeedback.score}</strong>
                <span>/100</span>
              </div>

              <div className="info-box">
                <p>{phaseFeedback.feedback}</p>
              </div>

              {triggeredEvent && (
                <div className="event-alert">
                  <span>Evento sorpresa</span>
                  <h3>{triggeredEvent.Title || triggeredEvent.title}</h3>
                  <p>{triggeredEvent.Description || triggeredEvent.description}</p>
                </div>
              )}

              <div className="consequence-grid">
                <div>
                  <span>Presupuesto restante</span>
                  <strong>{phaseFeedback.remainingBudget}</strong>
                </div>
                <div>
                  <span>Tiempo restante</span>
                  <strong>{phaseFeedback.remainingTimeWeeks} sem</strong>
                </div>
                <div>
                  <span>Riesgo actual</span>
                  <strong>{phaseFeedback.riskLevel}/100</strong>
                </div>
              </div>

              <button className="primary-action" onClick={continueNext} disabled={submitting}>
                {phaseFeedback.isLastPhase ? "Finalizar simulación" : "Continuar a la siguiente fase"}
              </button>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}

function ResourceCard({ title, value, total, suffix, percent }) {
  return (
    <div className="resource-card">
      <div className="resource-header">
        <span>{title}</span>
        <strong>{value}/{total} {suffix}</strong>
      </div>
      <div className="meter">
        <div className="meter-fill" style={{ width: `${percent}%` }}></div>
      </div>
    </div>
  );
}

function KpiItem({ label, value, suffix, inverted }) {
  const numericValue = Number(value ?? 0);

  return (
    <div className="kpi-item">
      <span>{label}</span>
      <strong className={inverted ? "kpi-inverted" : ""}>
        {Number.isFinite(numericValue) ? Math.round(numericValue * 100) / 100 : 0}{suffix}
      </strong>
    </div>
  );
}

function getPercent(value, total) {
  if (!total || total <= 0) return 0;
  return Math.min(100, Math.max(0, (Number(value) / Number(total)) * 100));
}

function getPhaseLimit(phaseName) {
  const limits = {
    Empatizar: 5,
    Definir: 2,
    Idear: 3,
    Prototipar: 4,
    Evaluar: 3,
  };

  return limits[phaseName] || 3;
}

function getPhaseTitle(phaseName) {
  const titles = {
    Empatizar: "Comprende al usuario antes de decidir",
    Definir: "Formula el problema correcto",
    Idear: "Elige soluciones viables y de impacto",
    Prototipar: "Construye un MVP coherente",
    Evaluar: "Mide resultados y aprende",
  };

  return titles[phaseName] || "Actividad";
}

function getPhaseInstruction(phaseName) {
  const instructions = {
    Empatizar:
      "Selecciona únicamente la evidencia y dolores más relevantes. No todo lo que parece útil realmente aporta al problema.",
    Definir:
      "Elige el problema que mejor conecta con la evidencia detectada. Una mala definición afecta todas las fases siguientes.",
    Idear:
      "Selecciona soluciones considerando impacto, costo, tiempo y riesgo. No puedes hacerlo todo: prioriza estratégicamente.",
    Prototipar:
      "Selecciona funcionalidades mínimas y flujo de usuario coherente con la solución que elegiste.",
    Evaluar:
      "Escoge KPIs que realmente permitan medir si la solución funcionó y justifica tu interpretación.",
  };

  return instructions[phaseName] || "";
}

function getTextareaPlaceholder(phaseName) {
  const placeholders = {
    Empatizar:
      "Explica qué revelan las evidencias y dolores seleccionados sobre las necesidades reales del usuario...",
    Definir:
      "Explica por qué ese problema es prioritario y cómo se conecta con la evidencia previa...",
    Idear:
      "Justifica por qué tus soluciones son viables y responden al problema definido...",
    Prototipar:
      "Describe cómo funcionaría el prototipo y por qué sus funcionalidades son suficientes para validar la solución...",
    Evaluar:
      "Interpreta los KPIs obtenidos y propone una mejora para la siguiente iteración...",
  };

  return placeholders[phaseName] || "Escribe tu justificación...";
}

function getOptionTypeLabel(type) {
  const labels = {
    Evidence: "Evidencias",
    PainPoint: "Dolores del usuario",
    ProblemStatement: "Declaración del problema",
    Solution: "Soluciones digitales",
    PrototypeFeature: "Funcionalidades del prototipo",
    UserFlowStep: "Flujo de usuario",
    KPI: "KPIs",
  };

  return labels[type] || type;
}

export default DesignThinkingSimulationPage;