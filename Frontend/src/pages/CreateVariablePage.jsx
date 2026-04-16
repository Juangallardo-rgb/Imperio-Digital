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

  useEffect(() => {
    loadScenarios();
  }, []);

  const loadScenarios = async () => {
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
      setMessage("No se pudieron cargar los escenarios");
    }
  };

  const handleCreate = async (e) => {
    e.preventDefault();

    try {
      const token = getToken();

      const response = await api.post(
        "/ScenarioVariable",
        {
          scenarioId: Number(scenarioId),
          name,
          methodology,
          phase,
          targetKpi,
          weight: Number(weight),
          baseValue: Number(baseValue),
          minValue: Number(minValue),
          maxValue: Number(maxValue),
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);

      setScenarioId("");
      setName("");
      setMethodology("");
      setPhase("");
      setTargetKpi("");
      setWeight("");
      setBaseValue("");
      setMinValue("");
      setMaxValue("");
    } catch (error) {
      console.error("Error creando variable:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No se pudo crear la variable");
      }
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Crear variable metodológica</h1>

        <form onSubmit={handleCreate}>
          <div className="form-group">
            <label>Escenario</label>
            <select value={scenarioId} onChange={(e) => setScenarioId(e.target.value)}>
              <option value="">Seleccione un escenario</option>
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

          <button type="submit">Crear variable</button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default CreateVariablePage;