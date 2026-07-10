import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function CreateDesignThinkingScenarioPage() {
  const navigate = useNavigate();

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
  const [creationMode, setCreationMode] = useState("manual");
  const [phaseWeights, setPhaseWeights] = useState([]);

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

  const generateScenarioWithAi = async () => {
    if (isGenerating) return;

    setIsGenerating(true);
    setCreationMode("ai");
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/design-thinking/scenarios/generate-draft",
        {
          methodology: form.methodologyCode,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      const generated = response.data;

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

      setMessage(
        "Escenario generado correctamente. Revisa los campos, configura disponibilidad e intentos, y luego créalo."
      );
    } catch (error) {
      console.error("Error generando escenario:", error);

      if (error.response) {
        setMessage(
          `Error ${error.response.status}: ${JSON.stringify(error.response.data)}`
        );
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setIsGenerating(false);
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
    };
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isSubmitting) return;

    if (!isPhaseDistributionValid) {
      setMessage(
        `No se puede crear el escenario. ${phaseWeightMessage}.`
      );
      return;
    }

    setIsSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post("/design-thinking/scenarios", buildPayload(), {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setMessage("Escenario creado correctamente");

      setTimeout(() => {
        navigate(`/design-thinking/scenarios/${response.data.id}`);
      }, 700);
    } catch (error) {
      console.error("Error creando escenario:", error);

      if (error.response) {
        setMessage(
          `Error ${error.response.status}: ${JSON.stringify(error.response.data)}`
        );
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
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

      <div className="pro-layout-2">
        <div className="pro-card">
          <div className="scenario-mode-panel compact">
            <button
              type="button"
              className={
                creationMode === "manual"
                  ? "scenario-mode-button active"
                  : "scenario-mode-button"
              }
              onClick={() => setCreationMode("manual")}
            >
              <span>Manual</span>
              <strong>Completar campos</strong>
            </button>

            <button
              type="button"
              className={
                creationMode === "ai"
                  ? "scenario-mode-button active"
                  : "scenario-mode-button"
              }
              onClick={() => setCreationMode("ai")}
            >
              <span>IA</span>
              <strong>Generar borrador</strong>
            </button>
          </div>

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

            <div className="generate-ai-box compact">
              <div>
                <h3>Generar escenario con IA</h3>
                <p>
                  Selecciona una metodología y genera automáticamente un caso de
                  transformación digital. Luego puedes editarlo antes de guardarlo.
                </p>
              </div>

              <button
                type="button"
                onClick={generateScenarioWithAi}
                disabled={isGenerating}
              >
                {isGenerating ? "Generando..." : "Generar escenario"}
              </button>
            </div>

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
              disabled={isSubmitting || !isPhaseDistributionValid}
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
    </div>
  );
}

export default CreateDesignThinkingScenarioPage;
