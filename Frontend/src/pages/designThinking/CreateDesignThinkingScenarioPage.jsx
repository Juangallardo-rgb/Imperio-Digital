import { useEffect, useState } from "react";
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

  const selectedMethodology = methodologies.find(
    (methodology) => methodology.code === form.methodologyCode
  );

  const loadMethodologies = async () => {
    try {
      const response = await api.get("/methodologies");
      setMethodologies(response.data);
    } catch (error) {
      console.error("Error cargando metodologías:", error);
      setMessage("No se pudieron cargar las metodologías.");
    }
  };

  useEffect(() => {
    loadMethodologies();
  }, []);

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const buildPayload = () => {
    return {
      ...form,
      maxAttemptsPerStudent: Number(form.maxAttemptsPerStudent),
      availableFrom: form.availableFrom ? new Date(form.availableFrom).toISOString() : null,
      availableUntil: form.availableUntil ? new Date(form.availableUntil).toISOString() : null,
    };
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isSubmitting) return;

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
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
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
            Define un caso de estudio y selecciona la metodología que guiará la simulación.
            El sistema generará fases, criterios y opciones según la metodología elegida.
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
              <select name="difficulty" value={form.difficulty} onChange={handleChange}>
                <option value="Baja">Baja</option>
                <option value="Media">Media</option>
                <option value="Alta">Alta</option>
              </select>
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

            <label className="checkbox-row">
              <input
                type="checkbox"
                name="allowLateAttempts"
                checked={form.allowLateAttempts}
                onChange={handleChange}
              />
              Permitir intentos fuera de fecha
            </label>

            <button className="primary-action" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creando..." : "Crear escenario"}
            </button>
          </form>
        </div>

        <div className="pro-card">
          <span className="eyebrow">Vista metodológica</span>
          <h2>{selectedMethodology?.name || "Metodología"}</h2>
          <p>{selectedMethodology?.description || "Selecciona una metodología para ver sus fases."}</p>

          {selectedMethodology && (
            <div className="table-list">
              {selectedMethodology.phases.map((phase) => (
                <div key={phase.id} className="table-row-card">
                  <div>
                    <strong>
                      {phase.phaseOrder}. {phase.name}
                    </strong>
                    <p>{phase.description}</p>
                  </div>

                  <span className="status-pill">
                    {phase.defaultWeight}%
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default CreateDesignThinkingScenarioPage;