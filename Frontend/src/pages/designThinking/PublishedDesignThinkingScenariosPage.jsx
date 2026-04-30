import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function PublishedDesignThinkingScenariosPage() {
  const navigate = useNavigate();

  const [scenarios, setScenarios] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [startingId, setStartingId] = useState(null);

  const loadScenarios = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/design-thinking/scenarios/published", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenarios(response.data);
    } catch (error) {
      console.error("Error cargando escenarios publicados:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  const startSimulation = async (scenarioId) => {
    setStartingId(scenarioId);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/design-thinking/simulations/start",
        { scenarioId },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      navigate(`/design-thinking/simulate/${response.data.attemptId}`);
    } catch (error) {
      console.error("Error iniciando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setStartingId(null);
    }
  };

  useEffect(() => {
    loadScenarios();
  }, []);

  return (
    <div className="page-container">
      <div className="card">
        <h1>Escenarios publicados</h1>
        <p>Selecciona un caso de estudio y resuélvelo mediante Design Thinking.</p>

        {message && <div className="message">{message}</div>}
      </div>

      {loading ? (
        <div className="card">
          <p>Cargando escenarios...</p>
        </div>
      ) : scenarios.length === 0 ? (
        <div className="card">
          <p>No hay escenarios publicados todavía.</p>
        </div>
      ) : (
        scenarios.map((scenario) => (
          <div key={scenario.id} className="list-item">
            <h3>{scenario.title}</h3>
            <p>{scenario.description}</p>
            <p><strong>Empresa:</strong> {scenario.companyType}</p>
            <p><strong>Problema:</strong> {scenario.problem}</p>
            <p><strong>Usuario:</strong> {scenario.targetUser}</p>
            <p><strong>Dificultad:</strong> {scenario.difficulty}</p>

            <button
              onClick={() => startSimulation(scenario.id)}
              disabled={startingId === scenario.id}
            >
              {startingId === scenario.id ? "Iniciando..." : "Iniciar simulación"}
            </button>
          </div>
        ))
      )}
    </div>
  );
}

export default PublishedDesignThinkingScenariosPage;