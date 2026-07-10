import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import { getScenarioExperienceStatus } from "../../features/methodologyExperience/adapters/legacyScenarioAdapter";
import { isMethodologyExperienceV2Enabled } from "../../features/methodologyExperience/engine/featureFlags";

function DesignThinkingScenarioDetailPage() {
  const { id } = useParams();
  const isExperienceV2Enabled = isMethodologyExperienceV2Enabled();

  const [scenario, setScenario] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [isRegenerating, setIsRegenerating] = useState(false);

  const loadScenario = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/design-thinking/scenarios/${id}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenario(response.data);
    } catch (error) {
      console.error("Error cargando detalle:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  const publishScenario = async () => {
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/scenarios/${id}/publish`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);
      await loadScenario();
    } catch (error) {
      console.error("Error publicando:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    }
  };

  const regenerateOptions = async () => {
    if (isRegenerating) return;

    setIsRegenerating(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/scenarios/${id}/generate-ai-content`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);
      await loadScenario();
    } catch (error) {
      console.error("Error regenerando opciones:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setIsRegenerating(false);
    }
  };

  useEffect(() => {
    loadScenario();
  }, [id]);

  const groupedOptions = useMemo(() => {
    if (!scenario?.options) return {};

    return scenario.options.reduce((acc, option) => {
      const key = `${option.phaseName} - ${option.optionType}`;
      if (!acc[key]) acc[key] = [];
      acc[key].push(option);
      return acc;
    }, {});
  }, [scenario]);

  const totalOptions = scenario?.options?.length || 0;
  const correctOptions = scenario?.options?.filter((option) => option.isCorrect).length || 0;
  const incorrectOptions = totalOptions - correctOptions;
  const totalPhases = scenario?.phaseSettings?.length || 0;
  const experienceStatus = useMemo(
    () => getScenarioExperienceStatus(scenario),
    [scenario]
  );

  if (loading) {
    return (
      <div className="scenario-detail-pro-page">
        <div className="scenario-detail-hero skeleton-hero">
          <span className="eyebrow">Detalle del escenario</span>
          <h1>Cargando escenario...</h1>
          <p>Preparando fases, criterios y opciones metodológicas.</p>
        </div>
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="scenario-detail-pro-page">
        <div className="scenario-empty-card">
          <h2>No se encontró el escenario.</h2>
          {message && <div className="message">{message}</div>}
          <Link className="scenario-action-secondary" to="/design-thinking/scenarios">
            Volver a escenarios
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="scenario-detail-pro-page">
      {message && <div className="message pro-message">{message}</div>}

      <section className="scenario-detail-hero">
        <div>
          <span className="eyebrow">Escenario metodológico</span>
          <h1>{scenario.title || scenario.name}</h1>
          <p>{scenario.description}</p>

          <div className="scenario-hero-badges">
            <span className={scenario.isPublished ? "status-pill green" : "status-pill gray"}>
              {scenario.isPublished ? "Publicado" : "Borrador"}
            </span>

            <span className={`difficulty-pill ${getDifficultyClass(scenario.difficulty)}`}>
              {scenario.difficulty || "Media"}
            </span>

            <span className="methodology-chip light">
              {getMethodologyName(scenario.methodologyName || scenario.methodology)}
            </span>
          </div>

          <div className="scenario-detail-actions">
            <button className="scenario-action-primary" onClick={publishScenario}>
              Publicar escenario
            </button>

            <button
              className="scenario-action-secondary"
              onClick={regenerateOptions}
              disabled={isRegenerating}
            >
              {isRegenerating ? "Generando con IA..." : "Regenerar opciones con IA"}
            </button>

            <Link className="scenario-action-link" to="/design-thinking/scenarios">
              Volver a escenarios
            </Link>
          </div>
        </div>

        <div className="scenario-detail-glass">
          <span>Estado del caso</span>
          <strong>{scenario.isPublished ? "Activo" : "Borrador"}</strong>
          <p>
            {totalPhases} fases · {totalOptions} opciones · {correctOptions} correctas
          </p>
        </div>
      </section>

      <section className="scenario-detail-kpi-grid">
        <ScenarioDetailKpi
          label="Fases"
          value={totalPhases}
          detail="Etapas metodológicas"
          variant="blue"
        />

        <ScenarioDetailKpi
          label="Opciones"
          value={totalOptions}
          detail="Decisiones del estudiante"
          variant="purple"
        />

        <ScenarioDetailKpi
          label="Correctas"
          value={correctOptions}
          detail="Opciones recomendadas"
          variant="green"
        />

        <ScenarioDetailKpi
          label="Distractores"
          value={incorrectOptions}
          detail="Opciones incorrectas"
          variant="orange"
        />
      </section>

      {isExperienceV2Enabled && experienceStatus.isDesignThinking && (
        <section className="scenario-section-card experience-status-panel">
          <div className="scenario-section-header">
            <div>
              <span className="eyebrow">Compatibilidad V2</span>
              <h2>Estado del contenido interactivo</h2>
            </div>
            <span className={`analytics-badge experience-status-${experienceStatus.status}`}>
              {experienceStatus.status === "complete"
                ? "Completo"
                : experienceStatus.status === "adapted"
                ? "Adaptable"
                : "Fallback generico"}
            </span>
          </div>
          <div className="experience-status-grid">
            {experienceStatus.phaseStatuses.map((phase) => (
              <article key={phase.phaseName}>
                <span>{phase.phaseName}</span>
                <strong>{phase.interaction}</strong>
                <p>{phase.optionCount} opciones, {phase.richOptionCount} con metadata interactiva.</p>
                <small>
                  {phase.status === "complete"
                    ? "Contenido completo para V2"
                    : phase.status === "adapted"
                    ? "Se adaptara el contenido existente"
                    : "Se mostrara actividad generica"}
                </small>
              </article>
            ))}
          </div>
        </section>
      )}

      <section className="scenario-info-grid">
        <div className="scenario-info-card large">
          <span className="eyebrow">Contexto del caso</span>
          <h2>Resumen empresarial</h2>

          <div className="scenario-info-list">
            <InfoRow label="Tipo de empresa" value={scenario.companyType} />
            <InfoRow label="Problema" value={scenario.problem} />
            <InfoRow label="Usuario objetivo" value={scenario.targetUser} />
            <InfoRow label="Restricciones" value={scenario.constraints} />
          </div>
        </div>

        <div className="scenario-info-card">
          <span className="eyebrow">Configuración</span>
          <h2>Parámetros</h2>

          <div className="scenario-config-stack">
            <div>
              <span>Metodología</span>
              <strong>{getMethodologyName(scenario.methodologyName || scenario.methodology)}</strong>
            </div>

            <div>
              <span>Dificultad</span>
              <strong>{scenario.difficulty || "Media"}</strong>
            </div>

            <div>
              <span>Estado</span>
              <strong>{scenario.isPublished ? "Publicado" : "Borrador"}</strong>
            </div>

            <div>
              <span>Creación</span>
              <strong>{formatDate(scenario.createdAt)}</strong>
            </div>
          </div>
        </div>
      </section>

      <section className="scenario-section-card">
        <div className="scenario-section-header">
          <div>
            <span className="eyebrow">Rúbrica metodológica</span>
            <h2>Fases, pesos y criterios</h2>
          </div>

          <span className="analytics-badge">{totalPhases} fases</span>
        </div>

        <div className="phase-timeline-grid">
          {scenario.phaseSettings.map((phase) => (
            <article key={phase.id} className="phase-pro-card">
              <div className="phase-card-header">
                <span className="phase-order">{phase.phaseOrder}</span>
                <div>
                  <h3>{phase.phaseName}</h3>
                  <p>Peso de la fase: <strong>{phase.phaseWeight}%</strong></p>
                </div>
              </div>

              <div className="phase-weight-bar">
                <div style={{ width: `${Math.min(100, Number(phase.phaseWeight || 0))}%` }}></div>
              </div>

              <div className="criteria-list">
                {phase.criteria.map((criterion) => (
                  <div key={criterion.id} className="criteria-row">
                    <div>
                      <strong>{criterion.criterionName}</strong>
                      <span>{criterion.evaluationType}</span>
                    </div>

                    <b>{criterion.criterionWeight}%</b>
                  </div>
                ))}
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="scenario-section-card">
        <div className="scenario-section-header">
          <div>
            <span className="eyebrow">Opciones de simulación</span>
            <h2>Decisiones disponibles por fase</h2>
          </div>

          <span className="analytics-badge">{totalOptions} opciones</span>
        </div>

        <div className="option-group-grid">
          {Object.keys(groupedOptions).map((group) => (
            <article key={group} className="option-group-card">
              <div className="option-group-header">
                <h3>{group}</h3>
                <span>{groupedOptions[group].length} opciones</span>
              </div>

              <div className="options-list-pro">
                {groupedOptions[group].map((option) => (
                  <div
                    key={option.id}
                    className={option.isCorrect ? "option-pro-row correct" : "option-pro-row incorrect"}
                  >
                    <div className="option-icon">
                      {option.isCorrect ? "✓" : "×"}
                    </div>

                    <div className="option-content">
                      <p>{option.text}</p>

                      <div className="option-meta">
                        <span>Score: {option.score ?? 0}</span>
                        <span>Costo: {option.cost ?? 0}</span>
                        <span>Tiempo: {option.timeCost ?? 0}</span>
                        <span>Riesgo: {option.riskImpact ?? 0}</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}

function ScenarioDetailKpi({ label, value, detail, variant }) {
  return (
    <div className={`scenario-detail-kpi ${variant}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <p>{detail}</p>
    </div>
  );
}

function InfoRow({ label, value }) {
  return (
    <div className="scenario-info-row">
      <span>{label}</span>
      <strong>{value || "No definido"}</strong>
    </div>
  );
}

function getMethodologyName(value) {
  const names = {
    DesignThinking: "Design Thinking",
    "Design Thinking": "Design Thinking",
    BPM: "Business Process Management",
    "Business Process Management": "Business Process Management",
    DigitalMaturity: "Madurez Digital",
    "Madurez Digital": "Madurez Digital",
    LeanStartup: "Lean Startup",
    "Lean Startup": "Lean Startup",
  };

  return names[value] || value || "No definida";
}

function getDifficultyClass(difficulty) {
  const value = String(difficulty || "").toLowerCase();

  if (value.includes("alta")) return "high";
  if (value.includes("baja")) return "low";

  return "medium";
}

function formatDate(date) {
  if (!date) return "Sin fecha";

  try {
    return new Date(date).toLocaleDateString();
  } catch {
    return "Sin fecha";
  }
}

export default DesignThinkingScenarioDetailPage;
