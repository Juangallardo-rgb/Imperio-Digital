import { useEffect, useState } from "react";
import api from "../api/api";
import { getToken } from "../utils/auth";

function CreateVariablePage() {
  const [scenarios, setScenarios] = useState([]);
  const [scenarioId, setScenarioId] = useState("");
  const [name, setName] = useState("");
  const [methodology, setMethodology] = useState("");
  const [phase, setPhase] = useState("");
  const [targetKpi, setTargetKpi] = useState("");
  const [weight, setWeight] = useState("");
  const [baseValue, setBaseValue] = useState("");
  const [minValue, setMinValue] = useState("");
  const [maxValue, setMaxValue] = useState("");
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [loadingScenarios, setLoadingScenarios] = useState(true);

  useEffect(() => {
    loadScenarios();
  }, []);

  const loadScenarios = async () => {
    setLoadingScenarios(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/Scenario", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenarios(response.data);
    } catch (error) {
      console.error("Error cargando escenarios:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else if (error.request) {
        setMessage("No hubo respuesta del backend al cargar escenarios. Intenta recargar.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    } finally {
      setLoadingScenarios(false);
    }
  };

  const handleCreate = async (e) => {
    e.preventDefault();

    if (isSubmitting) return;

    setIsSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const payload = {
        scenarioId: Number(scenarioId),
        name,
        methodology,
        phase,
        targetKpi,
        weight: Number(weight),
        baseValue: Number(baseValue),
        minValue: Number(minValue),
        maxValue: Number(maxValue),
      };

      const response = await api.post("/ScenarioVariable", payload, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      console.log("Respuesta backend:", response.data);

      setMessage("Variable creada correctamente");

      setScenarioId("");
      setName("");
      setMethodology("");
      setPhase("");
      setTargetKpi("");
      setWeight("");
      setBaseValue("");
      setMinValue("");
      setMaxValue("");

      await loadScenarios();
    } catch (error) {
      console.error("Error creando variable:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else if (error.request) {
        setMessage("La solicitud se envió, pero no hubo respuesta del backend.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Crear variable metodológica</h1>

        <button onClick={loadScenarios} style={{ maxWidth: "220px", marginBottom: "1rem" }}>
          Recargar escenarios
        </button>

        <form onSubmit={handleCreate}>
          <div className="form-group">
            <label>Escenario</label>
            <select value={scenarioId} onChange={(e) => setScenarioId(e.target.value)} disabled={loadingScenarios}>
              <option value="">
                {loadingScenarios ? "Cargando escenarios..." : "Seleccione un escenario"}
              </option>
              {scenarios.map((scenario) => (
                <option key={scenario.id} value={scenario.id}>
                  {scenario.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label>Nombre</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ej: Automatización de procesos"
            />
          </div>

          <div className="form-group">
            <label>Metodología</label>
            <select value={methodology} onChange={(e) => setMethodology(e.target.value)}>
              <option value="">Seleccione metodología</option>
              <option value="MadurezDigital">Madurez Digital</option>
              <option value="BPM">BPM</option>
              <option value="DesignThinking">Design Thinking</option>
              <option value="LeanStartup">Lean Startup</option>
            </select>
          </div>

          <div className="form-group">
            <label>Fase</label>
            <input
              type="text"
              value={phase}
              onChange={(e) => setPhase(e.target.value)}
              placeholder="Ej: Diagnostico, Procesos, Usuario, Iteracion"
            />
          </div>

          <div className="form-group">
            <label>KPI objetivo</label>
            <select value={targetKpi} onChange={(e) => setTargetKpi(e.target.value)}>
              <option value="">Seleccione KPI</option>
              <option value="DigitalMaturity">Digital Maturity</option>
              <option value="OperationalEfficiency">Operational Efficiency</option>
              <option value="CustomerExperience">Customer Experience</option>
              <option value="GlobalScore">Global Score</option>
            </select>
          </div>

          <div className="grid grid-2">
            <div className="form-group">
              <label>Peso</label>
              <input
                type="number"
                step="0.01"
                value={weight}
                onChange={(e) => setWeight(e.target.value)}
                placeholder="Ej: 0.40"
              />
            </div>

            <div className="form-group">
              <label>Valor base</label>
              <input
                type="number"
                value={baseValue}
                onChange={(e) => setBaseValue(e.target.value)}
                placeholder="Ej: 50"
              />
            </div>

            <div className="form-group">
              <label>Valor mínimo</label>
              <input
                type="number"
                value={minValue}
                onChange={(e) => setMinValue(e.target.value)}
                placeholder="Ej: 0"
              />
            </div>

            <div className="form-group">
              <label>Valor máximo</label>
              <input
                type="number"
                value={maxValue}
                onChange={(e) => setMaxValue(e.target.value)}
                placeholder="Ej: 100"
              />
            </div>
          </div>

          <button type="submit" disabled={isSubmitting || loadingScenarios}>
            {isSubmitting ? "Creando variable..." : "Crear variable"}
          </button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default CreateVariablePage;