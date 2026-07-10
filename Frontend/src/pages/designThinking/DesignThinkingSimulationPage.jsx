import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import MethodologyExperienceEngine from "../../features/methodologyExperience/engine/MethodologyExperienceEngine";
import { createPhaseSubmission } from "../../features/methodologyExperience/engine/experienceContracts";

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

      const response = await api.get(
        `/design-thinking/simulations/${attemptId}/current`,
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

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

  const phaseOptions = useMemo(() => {
    return current?.currentPhaseOptions || [];
  }, [current]);

  const activeKpis = useMemo(() => {
    const raw = phaseFeedback?.currentKpisJson || current?.currentKpisJson;
    if (!raw) return {};

    try {
      return JSON.parse(raw);
    } catch {
      return {};
    }
  }, [current, phaseFeedback]);

  const kpiItems = useMemo(() => {
    if (!current) return [];
    return getKpiDisplayItems(current.methodologyCode, activeKpis);
  }, [current, activeKpis]);

  const triggeredEvent = useMemo(() => {
    if (!phaseFeedback?.triggeredEventJson) return null;

    try {
      return JSON.parse(phaseFeedback.triggeredEventJson);
    } catch {
      return null;
    }
  }, [phaseFeedback]);

  const groupedOptions = useMemo(() => {
    return phaseOptions.reduce((acc, option) => {
      const key = option.optionType || "General";
      if (!acc[key]) acc[key] = [];
      acc[key].push(option);
      return acc;
    }, {});
  }, [phaseOptions]);

  const selectedOptions = useMemo(() => {
    return phaseOptions.filter((option) => selectedOptionIds.includes(option.id));
  }, [phaseOptions, selectedOptionIds]);

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

  const maxSelections = useMemo(() => {
    if (!phaseOptions.length) return 3;

    const configured = phaseOptions
      .map((option) => Number(option.maxSelections || 0))
      .filter((value) => value > 0);

    return configured.length > 0 ? Math.max(...configured) : 3;
  }, [phaseOptions]);

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

      const nextSelectionIds = [...prev, optionId];
      const nextSelection = phaseOptions.filter((option) =>
        nextSelectionIds.includes(option.id)
      );
      const nextCost = nextSelection.reduce(
        (total, option) => total + Number(option.cost || 0),
        0
      );
      const nextTime = nextSelection.reduce(
        (total, option) => total + Number(option.timeCost || 0),
        0
      );

      if (nextCost > Number(current?.remainingBudget || 0)) {
        setMessage("Esta seleccion excede el presupuesto disponible.");
        return prev;
      }

      if (nextTime > Number(current?.remainingTimeWeeks || 0)) {
        setMessage("Esta seleccion excede el tiempo disponible.");
        return prev;
      }

      return nextSelectionIds;
    });
  };

  const submitPhase = async () => {
    if (!current) return;

    if (selectedOptionIds.length === 0) {
      setMessage("Selecciona al menos una opción antes de enviar la fase.");
      return;
    }

    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/simulations/${attemptId}/phase/${current.currentPhaseName}/submit`,
        createPhaseSubmission({ selectedOptionIds, textAnswer }),
        {
          headers: { Authorization: `Bearer ${token}` },
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
          headers: { Authorization: `Bearer ${token}` },
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

  const phases = [...current.phaseOrder.map((phase) => phase.phaseName), "Resultado"];
  const currentPhaseIndex = phases.indexOf(current.currentPhaseName);

  const budgetPercent = getPercent(
    phaseFeedback?.remainingBudget ?? current.remainingBudget,
    current.initialBudget
  );

  const timePercent = getPercent(
    phaseFeedback?.remainingTimeWeeks ?? current.remainingTimeWeeks,
    current.initialTimeWeeks
  );

  const riskPercent = Math.min(
    100,
    Math.max(0, Number(phaseFeedback?.riskLevel ?? current.riskLevel ?? 0))
  );

  const legacyExperience = (
    <div className="simulation-page">
      <div className="simulation-hero">
        <div>
          <span className="eyebrow">
            Imperio Digital · Simulación {current.methodologyName}
          </span>

          <h1>{current.scenarioTitle}</h1>

<p>
  {current.scenarioDescription ||
    "Toma decisiones bajo restricciones reales. Cada elección puede afectar presupuesto, tiempo, riesgo y KPIs según la metodología seleccionada."}
</p>

<div className="simulation-context-summary">
  {current.scenarioCompanyType && (
    <div>
      <span>Empresa</span>
      <strong>{current.scenarioCompanyType}</strong>
    </div>
  )}

  {current.scenarioProblem && (
    <div>
      <span>Problema</span>
      <strong>{current.scenarioProblem}</strong>
    </div>
  )}

  {current.scenarioTargetUser && (
    <div>
      <span>Usuario objetivo</span>
      <strong>{current.scenarioTargetUser}</strong>
    </div>
  )}

  {current.scenarioConstraints && (
    <div>
      <span>Restricciones</span>
      <strong>{current.scenarioConstraints}</strong>
    </div>
  )}
</div>
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
                className={`meter-fill ${
                  riskPercent >= 70
                    ? "danger"
                    : riskPercent >= 40
                    ? "warning"
                    : "success"
                }`}
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

            {kpiItems.map((kpi) => (
              <KpiItem
                key={kpi.key}
                label={kpi.label}
                value={kpi.value}
                suffix={kpi.suffix}
                inverted={kpi.inverted}
              />
            ))}
          </div>

          {!phaseFeedback && (
            <div className="decision-summary">
              <h3>Decisión actual</h3>
              <p>
                <strong>Seleccionadas:</strong> {selectedOptionIds.length}/{maxSelections}
              </p>
              <p>
                <strong>Costo:</strong> {totals.cost} pts
              </p>
              <p>
                <strong>Tiempo:</strong> {totals.time} sem
              </p>
              <p>
                <strong>Riesgo:</strong> {totals.risk >= 0 ? "+" : ""}
                {totals.risk}
              </p>
            </div>
          )}
        </aside>

        <main className="simulation-main">
          {!phaseFeedback ? (
            <div className="simulation-card">
              <div className="section-header">
                <div>
                  <span className="eyebrow">Actividad guiada</span>
                  <h2>
                    {getPhaseTitle(
                      current.methodologyCode,
                      current.currentPhaseName
                    )}
                  </h2>
                </div>

                <span className="selection-limit">
                  Máximo {maxSelections} selecciones
                </span>
              </div>

              <p className="phase-instruction">
                {getPhaseInstruction(
                  current.methodologyCode,
                  current.currentPhaseName
                )}
              </p>

              {phaseOptions.length === 0 ? (
                <div className="empty-state">
                  No hay opciones configuradas para esta fase. El docente debe
                  regenerar opciones base del escenario.
                </div>
              ) : (
                Object.keys(groupedOptions).map((type) => (
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
                              <span className="option-type-badge">
                                {getOptionTypeLabel(option.optionType)}
                              </span>

                              {isSelected && (
                                <span className="selected-badge">
                                  Seleccionada
                                </span>
                              )}
                            </div>

                            <p>{option.text}</p>

                            <div className="decision-meta">
                              <span>Costo: {option.cost ?? 0}</span>
                              <span>Tiempo: {option.timeCost ?? 0} sem</span>
                              <span>
                                Riesgo: {Number(option.riskImpact || 0) > 0 ? "+" : ""}
                                {option.riskImpact ?? 0}
                              </span>
                            </div>

                            {(option.expectedImpactLevel ||
                              option.expectedEffortLevel ||
                              option.expectedViabilityLevel) && (
                              <div className="decision-extra">
                                {option.expectedImpactLevel && (
                                  <small>
                                    Impacto: {option.expectedImpactLevel}
                                  </small>
                                )}

                                {option.expectedEffortLevel && (
                                  <small>
                                    Esfuerzo: {option.expectedEffortLevel}
                                  </small>
                                )}

                                {option.expectedViabilityLevel && (
                                  <small>
                                    Viabilidad: {option.expectedViabilityLevel}
                                  </small>
                                )}
                              </div>
                            )}
                          </button>
                        );
                      })}
                    </div>
                  </section>
                ))
              )}

              <div className="form-group simulation-textarea">
                <label>Justificación estratégica <span className="optional-label">Opcional</span></label>

                <textarea
                  value={textAnswer}
                  onChange={(e) => setTextAnswer(e.target.value)}
                  placeholder={getTextareaPlaceholder(
                    current.methodologyCode,
                    current.currentPhaseName
                  )}
                />

                <small>
                {textAnswer.length} caracteres · puedes dejarlo vacío si tu decisión ya está clara
                </small>
              </div>

              <button
                className="primary-action"
                onClick={submitPhase}
                disabled={submitting}
              >
                {submitting
                  ? "Evaluando fase..."
                  : "Enviar fase y ver consecuencias"}
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

              <button
                className="primary-action"
                onClick={continueNext}
                disabled={submitting}
              >
                {phaseFeedback.isLastPhase
                  ? "Finalizar simulación"
                  : "Continuar a la siguiente fase"}
              </button>
            </div>
          )}
        </main>
      </div>
    </div>
  );

  return (
    <MethodologyExperienceEngine
      current={current}
      selectedOptionIds={selectedOptionIds}
      textAnswer={textAnswer}
      phaseFeedback={phaseFeedback}
      message={message}
      maxSelections={maxSelections}
      totals={totals}
      kpiItems={kpiItems}
      triggeredEvent={triggeredEvent}
      submitting={submitting}
      onToggleOption={toggleOption}
      onTextAnswerChange={setTextAnswer}
      onSubmit={submitPhase}
      onContinue={continueNext}
      fallback={legacyExperience}
    />
  );
}

function ResourceCard({ title, value, total, suffix, percent }) {
  return (
    <div className="resource-card">
      <div className="resource-header">
        <span>{title}</span>
        <strong>
          {value}/{total} {suffix}
        </strong>
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
        {Number.isFinite(numericValue)
          ? Math.round(numericValue * 100) / 100
          : 0}
        {suffix}
      </strong>
    </div>
  );
}

function getKpiDisplayItems(methodologyCode, kpis) {
  const catalog = {
    BPM: [
      ["processEfficiency", "Eficiencia", "/100"],
      ["cycleTime", "Tiempo ciclo", " días", true],
      ["errorRate", "Errores", "%", true],
      ["satisfaction", "Satisfacción", "/100"],
      ["digitalAdoption", "Adopción digital", "/100"],
    ],
    DigitalMaturity: [
      ["digitalMaturity", "Madurez digital", "/100"],
      ["processEfficiency", "Eficiencia", "/100"],
      ["dataUsage", "Uso de datos", "/100"],
      ["satisfaction", "Satisfacción", "/100"],
      ["digitalAdoption", "Adopción digital", "/100"],
    ],
    LeanStartup: [
      ["validatedLearning", "Aprendizaje", "/100"],
      ["conversionRate", "Conversión", "%"],
      ["satisfaction", "Satisfacción", "/100"],
      ["experimentVelocity", "Velocidad exp.", "/100"],
      ["digitalAdoption", "Adopción digital", "/100"],
    ],
    DesignThinking: [
      ["cartAbandonment", "Abandono", "%", true],
      ["conversionRate", "Conversión", "%"],
      ["satisfaction", "Satisfacción", "/100"],
      ["purchaseTime", "Tiempo compra", " min", true],
      ["digitalAdoption", "Adopción digital", "/100"],
    ],
  };

  return (catalog[methodologyCode] || catalog.DesignThinking).map((item) => ({
    key: item[0],
    label: item[1],
    suffix: item[2],
    inverted: Boolean(item[3]),
    value: kpis[item[0]],
  }));
}

function getPercent(value, total) {
  if (!total || total <= 0) return 0;
  return Math.min(100, Math.max(0, (Number(value) / Number(total)) * 100));
}

function getPhaseTitle(methodologyCode, phaseName) {
  const titles = {
    Empatizar: "Comprende al usuario antes de decidir",
    Definir: "Formula el problema correcto",
    Idear: "Elige soluciones viables y de impacto",
    Prototipar: "Construye un prototipo coherente",
    Evaluar: "Mide resultados y aprende",

    "Identificar proceso": "Identifica el proceso crítico",
    "Modelar proceso actual": "Representa el flujo actual",
    "Analizar cuellos de botella": "Detecta fricciones del proceso",
    "Rediseñar proceso": "Propón mejoras viables",
    "Monitorear indicadores": "Define control y seguimiento",

    "Diagnóstico inicial": "Evalúa el estado digital actual",
    "Evaluar capacidades": "Analiza capacidades digitales",
    "Priorizar brechas": "Prioriza brechas críticas",
    "Plan de transformación": "Diseña iniciativas digitales",
    "Seguimiento de madurez": "Mide avance de madurez",

    Hipótesis: "Formula hipótesis críticas",
    MVP: "Diseña el producto mínimo viable",
    Medición: "Define métricas accionables",
    Aprendizaje: "Interpreta aprendizaje validado",
    "Pivote o perseverancia": "Decide con evidencia",
  };

  return titles[phaseName] || `Resolver fase: ${phaseName}`;
}

function getPhaseInstruction(methodologyCode, phaseName) {
  return `Selecciona las opciones más relevantes para la fase "${phaseName}" y justifica tu decisión con base en el caso, las restricciones y la metodología aplicada.`;
}

function getTextareaPlaceholder(methodologyCode, phaseName) {
  return `Explica por qué tus decisiones en la fase "${phaseName}" son coherentes con el problema, la metodología y los resultados esperados...`;
}

function getOptionTypeLabel(type) {
  const labels = {
    Evidence: "Evidencias",
    PainPoint: "Dolores del usuario",
    ProblemStatement: "Declaración del problema",
    Solution: "Soluciones digitales",
    PrototypeFeature: "Funcionalidades del prototipo",
    UserFlowStep: "Flujo de usuario",
    Test: "Evaluación",
    KPI: "KPIs",

    ProcessEvidence: "Evidencias del proceso",
    ProcessSelection: "Selección del proceso",
    CurrentProcessStep: "Proceso actual",
    CurrentProcess: "Proceso actual",
    Bottleneck: "Cuellos de botella",
    Redesign: "Rediseño del proceso",
    ProcessImprovement: "Mejoras del proceso",
    KpiSelection: "Indicadores de proceso",

    CurrentState: "Estado actual",
    Capability: "Capacidades",
    Gap: "Brechas",
    TransformationInitiative: "Iniciativas",
    MaturityKpi: "Indicadores de madurez",

    Hypothesis: "Hipótesis",
    MvpFeature: "MVP",
    Metric: "Métricas",
    Learning: "Aprendizajes",
    Decision: "Decisiones",
    PivotDecision: "Pivote o perseverancia",

    General: "Opciones",
  };

  return labels[type] || type || "Opciones";
}

export default DesignThinkingSimulationPage;
