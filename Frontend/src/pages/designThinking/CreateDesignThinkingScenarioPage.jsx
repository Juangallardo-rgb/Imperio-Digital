import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import { getPhaseExperienceDescriptor } from "../../features/methodologyExperience/adapters/legacyScenarioAdapter";
import { isMethodologyExperienceV2Enabled } from "../../features/methodologyExperience/engine/featureFlags";
import {
  CREATION_MODE_ACTIONS,
  createScenarioRequestCoordinator,
  parseScenarioRequestError,
  retainValidDraftAfterFailure,
  resolveAiDraftGenerationId,
} from "./scenarioCreationState";

function AiLoadingModal({ title, message }) {
  return (
    <div className="ai-generation-modal-backdrop" role="presentation">
      <section
        className="ai-generation-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="ai-generation-modal-title"
        aria-describedby="ai-generation-modal-message"
      >
        <div className="ai-generation-spinner" aria-hidden="true" />
        <span className="eyebrow">Procesando con OpenRouter</span>
        <h2 id="ai-generation-modal-title">{title}</h2>
        <p id="ai-generation-modal-message">{message}</p>
        <small>No cierres esta pestaña mientras termina la solicitud.</small>
      </section>
    </div>
  );
}

function CreateDesignThinkingScenarioPage() {
  const navigate = useNavigate();
  const isExperienceV2Enabled = isMethodologyExperienceV2Enabled();

  const [methodologies, setMethodologies] = useState([]);

  const [form, setForm] = useState({
    title: "",
    description: "",
    companyType: "",
    problem: "",
    targetUser: "",
    constraints: "",
    methodologyCode: "DesignThinking",
    difficulty: "Media",
    availableFrom: "",
    availableUntil: "",
    maxAttemptsPerStudent: 1,
    allowLateAttempts: false,
  });

  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [creationMode, setCreationMode] = useState("Manual");
  const [aiDraft, setAiDraft] = useState(null);
  const [phaseWeights, setPhaseWeights] = useState([]);
  const [showExperiencePreview, setShowExperiencePreview] = useState(false);
  const [requestError, setRequestError] = useState(null);
  const requestCoordinator = useRef(createScenarioRequestCoordinator());
  const draftAbortController = useRef(null);

  const selectedMethodology = methodologies.find(
    (methodology) => methodology.code === form.methodologyCode
  );

  const phaseWeightTotal = useMemo(() => {
    return phaseWeights.reduce(
      (sum, phase) => sum + Number(phase.phaseWeight || 0),
      0
    );
  }, [phaseWeights]);

  const phaseWeightBalance = 100 - phaseWeightTotal;

  const isPhaseDistributionValid =
    phaseWeights.length > 0 && phaseWeightTotal === 100;

  const phaseWeightMessage =
    phaseWeightTotal === 100
      ? "Distribución válida: 100%"
      : phaseWeightTotal < 100
      ? `Falta distribuir ${phaseWeightBalance}%`
      : `Has excedido el total por ${Math.abs(phaseWeightBalance)}%`;

  const experiencePreviewPhases = useMemo(() => {
    if (form.methodologyCode !== "DesignThinking") return [];

    return phaseWeights
      .filter((phase) => phase.isEnabled)
      .sort((a, b) => a.phaseOrder - b.phaseOrder)
      .map((phase) => ({
        ...phase,
        ...getPhaseExperienceDescriptor(phase.phaseName),
      }));
  }, [form.methodologyCode, phaseWeights]);

  const loadMethodologies = async () => {
    try {
      const response = await api.get("/methodologies");
      setMethodologies(response.data || []);
    } catch (error) {
      console.error("Error cargando metodologías:", error);
      setMessage("No se pudieron cargar las metodologías.");
    }
  };

  useEffect(() => {
    loadMethodologies();
  }, []);

  useEffect(() => () => {
    draftAbortController.current?.abort();
    requestCoordinator.current.invalidateDraft();
  }, []);

  useEffect(() => {
    if (!selectedMethodology?.phases?.length) {
      setPhaseWeights([]);
      return;
    }

    setPhaseWeights(
      selectedMethodology.phases.map((phase) => ({
        methodologyPhaseId: phase.id,
        phaseName: phase.name,
        phaseOrder: phase.phaseOrder,
        phaseWeight: Number(phase.defaultWeight || 0),
        isEnabled: true,
      }))
    );
  }, [selectedMethodology]);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

    if (name === "methodologyCode" && value !== form.methodologyCode) {
      draftAbortController.current?.abort();
      requestCoordinator.current.invalidateDraft();
      setIsGenerating(false);
      setCreationMode("Manual");
      setAiDraft(null);
      setRequestError(null);
    }

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const handlePhaseWeightChange = (methodologyPhaseId, value) => {
    const normalizedValue = Math.min(
      100,
      Math.max(0, Number(value || 0))
    );

    setPhaseWeights((prev) =>
      prev.map((phase) =>
        phase.methodologyPhaseId === methodologyPhaseId
          ? { ...phase, phaseWeight: normalizedValue }
          : phase
      )
    );
  };

  const resetRecommendedWeights = () => {
    if (!selectedMethodology?.phases?.length) return;

    setPhaseWeights(
      selectedMethodology.phases.map((phase) => ({
        methodologyPhaseId: phase.id,
        phaseName: phase.name,
        phaseOrder: phase.phaseOrder,
        phaseWeight: Number(phase.defaultWeight || 0),
        isEnabled: true,
      }))
    );
  };

  const selectManualMode = () => {
    draftAbortController.current?.abort();
    requestCoordinator.current.invalidateDraft();
    setIsGenerating(false);
    setCreationMode("Manual");
    setAiDraft(null);
    setMessage("");
    setRequestError(null);
  };

  const generateScenarioWithAi = async () => {
    const requestId = requestCoordinator.current.beginDraft();
    if (requestId === null) return;

    const abortController = new AbortController();
    draftAbortController.current = abortController;
    setIsGenerating(true);
    setMessage("");
    setRequestError(null);

    try {
      const token = getToken();

      const response = await api.post(
        "/design-thinking/scenarios/generate-draft",
        {
          methodologyCode: form.methodologyCode,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
          signal: abortController.signal,
        }
      );

      if (!requestCoordinator.current.isCurrentDraft(requestId)) return;

      const generated = response.data;

      if (!generated.generatedByAi || !generated.generationId) {
        throw new Error("El backend no confirmó una generación válida con OpenRouter.");
      }

      setForm((prev) => ({
        ...prev,
        title: generated.title || "",
        description: generated.description || "",
        companyType: generated.companyType || "",
        problem: generated.problem || "",
        targetUser: generated.targetUser || "",
        constraints: generated.constraints || "",
        difficulty: generated.difficulty || "Media",
      }));

      setCreationMode("AiAssisted");
      setAiDraft(generated);

      setMessage(
        "Borrador generado con OpenRouter. Revisa el contenido antes de crear el escenario."
      );
    } catch (error) {
      if (
        abortController.signal.aborted ||
        !requestCoordinator.current.isCurrentDraft(requestId)
      ) {
        return;
      }
      console.error("Error generando escenario:", error);
      const parsedError = parseScenarioRequestError(
        error,
        "No se pudo generar el borrador con OpenRouter. Intenta nuevamente."
      );
      setAiDraft((previousDraft) => retainValidDraftAfterFailure(previousDraft));
      setRequestError(parsedError);
      setMessage(parsedError.message);
    } finally {
      if (requestCoordinator.current.isCurrentDraft(requestId)) {
        requestCoordinator.current.finishDraft(requestId);
        setIsGenerating(false);
        draftAbortController.current = null;
      }
    }
  };

  const buildPayload = () => {
    return {
      ...form,
      maxAttemptsPerStudent: Number(form.maxAttemptsPerStudent || 1),
      availableFrom: form.availableFrom
        ? new Date(form.availableFrom).toISOString()
        : null,
      availableUntil: form.availableUntil
        ? new Date(form.availableUntil).toISOString()
        : null,
      phaseSettings: phaseWeights.map((phase) => ({
        methodologyPhaseId: phase.methodologyPhaseId,
        phaseName: phase.phaseName,
        phaseOrder: phase.phaseOrder,
        phaseWeight: Number(phase.phaseWeight || 0),
        isEnabled: true,
      })),
      creationMode,
      aiDraftGenerationId: resolveAiDraftGenerationId(creationMode, aiDraft),
    };
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!requestCoordinator.current.beginCreation()) return;

    if (!isPhaseDistributionValid) {
      setMessage(
        `No se puede crear el escenario. ${phaseWeightMessage}.`
      );
      requestCoordinator.current.finishCreation();
      return;
    }

    setIsSubmitting(true);
    setRequestError(null);
    setMessage(
      creationMode === "AiAssisted"
        ? "Generando opciones para las fases..."
        : ""
    );

    try {
      const token = getToken();

      const response = await api.post("/design-thinking/scenarios", buildPayload(), {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setMessage(
        creationMode === "AiAssisted"
          ? "Escenario y opciones generados correctamente con OpenRouter."
          : "El escenario fue guardado como borrador. Antes de publicarlo debe agregar o generar opciones."
      );

      setTimeout(() => {
        navigate(`/design-thinking/scenarios/${response.data.id}`);
      }, 700);
    } catch (error) {
      console.error("Error creando escenario:", error);
      const parsedError = parseScenarioRequestError(
        error,
        "No se pudo crear el escenario. Verifica la conexión e intenta nuevamente."
      );
      setRequestError(parsedError);
      setMessage(parsedError.message);
    } finally {
      requestCoordinator.current.finishCreation();
      setIsSubmitting(false);
    }
  };

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Nuevo escenario</span>
          <h1>Crear escenario metodológico</h1>
          <p>
            Define un caso de estudio y selecciona la metodología que guiará la
            simulación. Puedes construirlo manualmente o generar una propuesta
            inicial enfocada en transformación digital.
          </p>
        </div>

        <div className="phase-pill">
          <span>Metodología</span>
          <strong>{selectedMethodology?.name || "Seleccionar"}</strong>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}
      {requestError?.detail && (
        <div className="ai-error-detail" role="alert">
          <span>{requestError.detail}</span>
          {requestError.correlationId && (
            <small>Referencia de diagnóstico: {requestError.correlationId}</small>
          )}
        </div>
      )}

      <div className="pro-layout-2">
        <div className="pro-card">
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Metodología</label>
              <select
                name="methodologyCode"
                value={form.methodologyCode}
                onChange={handleChange}
                required
              >
                {methodologies.map((methodology) => (
                  <option key={methodology.code} value={methodology.code}>
                    {methodology.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="scenario-creation-actions" aria-label="Modo de creación">
              {CREATION_MODE_ACTIONS.map((action) => (
                <button
                  key={action.mode}
                  type="button"
                  className={
                    creationMode === action.mode
                      ? "scenario-creation-action active"
                      : "scenario-creation-action"
                  }
                  onClick={
                    action.mode === "Manual"
                      ? selectManualMode
                      : generateScenarioWithAi
                  }
                  disabled={isGenerating || isSubmitting}
                  aria-pressed={creationMode === action.mode}
                >
                  {action.label}
                </button>
              ))}
            </div>

            {aiDraft?.generatedByAi && creationMode === "AiAssisted" && (
              <p className="ai-draft-status">
                Borrador IA listo. Revísalo antes de crear el escenario; las opciones
                se generarán al guardar.
              </p>
            )}

            <div className="form-group">
              <label>Título</label>
              <input
                name="title"
                value={form.title}
                onChange={handleChange}
                placeholder="Ej: Digitalización del proceso de inscripción universitaria"
                required
              />
            </div>

            <div className="form-group">
              <label>Descripción</label>
              <textarea
                name="description"
                value={form.description}
                onChange={handleChange}
                placeholder="Describe el contexto general del caso"
                required
              />
            </div>

            <div className="form-group">
              <label>Tipo de empresa</label>
              <input
                name="companyType"
                value={form.companyType}
                onChange={handleChange}
                placeholder="Ej: E-commerce, clínica, universidad, restaurante"
                required
              />
            </div>

            <div className="form-group">
              <label>Problema principal</label>
              <textarea
                name="problem"
                value={form.problem}
                onChange={handleChange}
                placeholder="Describe el problema que deberá resolver el estudiante"
                required
              />
            </div>

            <div className="form-group">
              <label>Usuario objetivo</label>
              <input
                name="targetUser"
                value={form.targetUser}
                onChange={handleChange}
                placeholder="Ej: Clientes digitales, pacientes, estudiantes, personal operativo"
                required
              />
            </div>

            <div className="form-group">
              <label>Restricciones</label>
              <textarea
                name="constraints"
                value={form.constraints}
                onChange={handleChange}
                placeholder="Ej: Presupuesto limitado, equipo pequeño, plazo de 4 semanas"
              />
            </div>

            <div className="form-group">
              <label>Dificultad</label>
              <select
                name="difficulty"
                value={form.difficulty}
                onChange={handleChange}
              >
                <option value="Baja">Baja</option>
                <option value="Media">Media</option>
                <option value="Alta">Alta</option>
              </select>
            </div>

            <div className="availability-panel">
              <div>
                <h3>Disponibilidad e intentos</h3>
                <p>
                  Estos campos siempre deben ser definidos por el docente, aunque
                  el escenario haya sido generado con IA.
                </p>
              </div>

              <div className="pro-layout-2">
                <div className="form-group">
                  <label>Disponible desde</label>
                  <input
                    type="datetime-local"
                    name="availableFrom"
                    value={form.availableFrom}
                    onChange={handleChange}
                  />
                </div>

                <div className="form-group">
                  <label>Disponible hasta</label>
                  <input
                    type="datetime-local"
                    name="availableUntil"
                    value={form.availableUntil}
                    onChange={handleChange}
                  />
                </div>
              </div>

              <div className="pro-layout-2">
                <div className="form-group">
                  <label>Intentos máximos por estudiante</label>
                  <input
                    type="number"
                    min="1"
                    name="maxAttemptsPerStudent"
                    value={form.maxAttemptsPerStudent}
                    onChange={handleChange}
                  />
                </div>

                <label className="checkbox-row checkbox-card">
                  <input
                    type="checkbox"
                    name="allowLateAttempts"
                    checked={form.allowLateAttempts}
                    onChange={handleChange}
                  />
                  <span>
                    <strong>Permitir intentos fuera de fecha</strong>
                    <small>
                      El estudiante podrá simular aunque el escenario esté fuera
                      del rango configurado.
                    </small>
                  </span>
                </label>
              </div>
            </div>

            <button
              className="primary-action"
              type="submit"
              disabled={isSubmitting || isGenerating || !isPhaseDistributionValid}
            >
              {isSubmitting ? "Creando..." : "Crear escenario"}
            </button>
          </form>
        </div>

        <div className="pro-card methodology-preview-sticky">
          <span className="eyebrow">Vista metodológica</span>
          <h2>{selectedMethodology?.name || "Metodología"}</h2>
          <p>
            {selectedMethodology?.description ||
              "Selecciona una metodología para ver sus fases."}
          </p>

          {isExperienceV2Enabled && experiencePreviewPhases.length > 0 && (
            <section className="teacher-experience-preview">
              <div>
                <span className="eyebrow">Experiencia V2</span>
                <h3>Vista previa para estudiante</h3>
                <p>
                  El contenido interactivo se generara con las opciones del escenario.
                  El docente solo revisa la cobertura antes de publicar.
                </p>
              </div>
              <button
                type="button"
                className="scenario-action-secondary"
                onClick={() => setShowExperiencePreview((visible) => !visible)}
                aria-expanded={showExperiencePreview}
              >
                {showExperiencePreview ? "Ocultar vista previa" : "Ver experiencias"}
              </button>

              {showExperiencePreview && (
                <div className="teacher-experience-preview-list">
                  {experiencePreviewPhases.map((phase) => (
                    <article key={phase.methodologyPhaseId}>
                      <span>Fase {phase.phaseOrder}</span>
                      <strong>{phase.phaseName}</strong>
                      <p>{phase.interaction}</p>
                      <small>Contenido: se validara al generar las opciones.</small>
                    </article>
                  ))}
                </div>
              )}
            </section>
          )}

          {selectedMethodology && (
            <>
              <div className="phase-weight-toolbar">
                <button
                  type="button"
                  className="scenario-action-secondary"
                  onClick={resetRecommendedWeights}
                >
                  Restablecer pesos recomendados
                </button>
              </div>

              <div className="table-list">
                {selectedMethodology.phases.map((phase) => {
                  const configuredPhase = phaseWeights.find(
                    (item) => item.methodologyPhaseId === phase.id
                  );

                  const phaseWeight = Number(
                    configuredPhase?.phaseWeight ?? phase.defaultWeight ?? 0
                  );

                  return (
                    <div key={phase.id} className="table-row-card phase-weight-row">
                      <div>
                        <div className="phase-weight-title">
                          <strong>
                            {phase.phaseOrder}. {phase.name}
                          </strong>

                          <span className="status-pill">
                            {phaseWeight}%
                          </span>
                        </div>

                        <p>{phase.description}</p>

                        <div className="phase-weight-control">
                          <input
                            type="range"
                            min="0"
                            max="100"
                            step="1"
                            value={phaseWeight}
                            onChange={(event) =>
                              handlePhaseWeightChange(
                                phase.id,
                                event.target.value
                              )
                            }
                          />

                          <input
                            type="number"
                            min="0"
                            max="100"
                            step="1"
                            value={phaseWeight}
                            onChange={(event) =>
                              handlePhaseWeightChange(
                                phase.id,
                                event.target.value
                              )
                            }
                          />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>

              <div
                className={
                  isPhaseDistributionValid
                    ? "phase-weight-summary valid"
                    : "phase-weight-summary invalid"
                }
              >
                <div>
                  <span>Total asignado</span>
                  <strong>{phaseWeightTotal}%</strong>
                </div>

                <div>
                  <span>Por distribuir</span>
                  <strong>
                    {phaseWeightBalance > 0 ? phaseWeightBalance : 0}%
                  </strong>
                </div>

                <p>{phaseWeightMessage}</p>
              </div>
            </>
          )}

          <div className="methodology-helper-box">
            <strong>Recomendación</strong>
            <p>
              Si usas generación con IA, revisa que el contexto, el problema y
              el usuario objetivo sean coherentes con la metodología antes de
              crear el escenario.
            </p>
          </div>
        </div>
      </div>

      {isGenerating && (
        <AiLoadingModal
          title="Generando borrador con IA"
          message="OpenRouter está preparando un caso empresarial para la metodología seleccionada."
        />
      )}

      {isSubmitting && (
        <AiLoadingModal
          title={
            creationMode === "AiAssisted"
              ? "Creando escenario con IA"
              : "Guardando escenario"
          }
          message={
            creationMode === "AiAssisted"
              ? "Generando opciones, validando las fases y preparando el escenario."
              : "Validando la configuración y guardando el borrador."
          }
        />
      )}
    </div>
  );
}

export default CreateDesignThinkingScenarioPage;
