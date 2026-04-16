import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../api/api";
import { getToken } from "../utils/auth";

function SimulationPage() {
  const { scenarioId } = useParams();

  const [variables, setVariables] = useState([]);
  const [message, setMessage] = useState("");
  const [result, setResult] = useState(null);

  useEffect(() => {
    loadVariables();
  }, []);

  const loadVariables = async () => {
    try {
      const token = getToken();

      const response = await api.get(`/ScenarioVariable/by-scenario/${scenarioId}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      const variablesWithValues = response.data.map((variable) => ({
        ...variable,
        value: variable.baseValue,
      }));

      setVariables(variablesWithValues);
    } catch (error) {
      console.error("Error cargando variables:", error);
      setMessage("No se pudieron cargar las variables del escenario");
    }
  };

  const handleChangeValue = (id, newValue) => {
    setVariables((prev) =>
      prev.map((variable) =>
        variable.id === id
          ? { ...variable, value: Number(newValue) }
          : variable
      )
    );
  };

  const handleSimulate = async (e) => {
    e.preventDefault();

    try {
      const token = getToken();

      const payload = {
        scenarioId: Number(scenarioId),
        variables: variables.map((variable) => ({
          scenarioVariableId: variable.id,
          value: Number(variable.value),
        })),
      };

      const response = await api.post("/Simulation", payload, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setResult(response.data);
      setMessage("Simulación ejecutada correctamente");
    } catch (error) {
      console.error("Error ejecutando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No se pudo ejecutar la simulación");
      }
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Ejecutar simulación</h1>
        {message && <div className="message">{message}</div>}
      </div>

      {variables.length === 0 ? (
        <div className="card">
          <p>No hay variables para este escenario.</p>
        </div>
      ) : (
        <form onSubmit={handleSimulate}>
          {variables.map((variable) => (
            <div key={variable.id} className="card">
              <h3>{variable.name}</h3>
              <p><strong>Metodología:</strong> {variable.methodology}</p>
              <p><strong>Fase:</strong> {variable.phase}</p>
              <p><strong>KPI:</strong> {variable.targetKpi}</p>
              <p><strong>Rango:</strong> {variable.minValue} - {variable.maxValue}</p>

              <div className="form-group">
                <label>Valor</label>
                <input
                  type="number"
                  min={variable.minValue}
                  max={variable.maxValue}
                  value={variable.value}
                  onChange={(e) => handleChangeValue(variable.id, e.target.value)}
                />
              </div>
            </div>
          ))}

          <button type="submit">Ejecutar simulación</button>
        </form>
      )}

      {result && (
        <div className="card">
          <h2>Resultados</h2>
          <div className="info-box">
            <p><strong>Madurez digital:</strong> {result.digitalMaturity}</p>
            <p><strong>Eficiencia operativa:</strong> {result.operationalEfficiency}</p>
            <p><strong>Experiencia del cliente:</strong> {result.customerExperience}</p>
            <p><strong>Score global:</strong> {result.globalScore}</p>
          </div>

          <div className="info-box">
            <p><strong>Feedback base:</strong> {result.feedbackRule}</p>
          </div>

          <div className="info-box">
            <p><strong>Feedback IA:</strong> {result.aiFeedback}</p>
          </div>
        </div>
      )}
    </div>
  );
}

export default SimulationPage;