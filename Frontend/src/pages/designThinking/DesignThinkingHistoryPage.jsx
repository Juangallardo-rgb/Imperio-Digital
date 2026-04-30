import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function DesignThinkingHistoryPage() {
  const [history, setHistory] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadHistory = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/design-thinking/simulations/my-history", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setHistory(response.data);
    } catch (error) {
      console.error("Error cargando historial:", error);

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
    loadHistory();
  }, []);

  return (
    <div className="page-container">
      <div className="card">
        <h1>Historial de simulaciones</h1>
        <p>Consulta tus intentos anteriores y resultados obtenidos.</p>
        {message && <div className="message">{message}</div>}
      </div>

      {loading ? (
        <div className="card">
          <p>Cargando historial...</p>
        </div>
      ) : history.length === 0 ? (
        <div className="card">
          <p>No tienes simulaciones registradas.</p>
        </div>
      ) : (
        history.map((item) => (
          <div key={item.attemptId} className="list-item">
            <h3>{item.scenarioTitle}</h3>
            <p><strong>Estado:</strong> {item.status}</p>
            <p><strong>Score final:</strong> {item.finalScore}</p>
            <p><strong>Inicio:</strong> {new Date(item.startedAt).toLocaleString()}</p>

            {item.finishedAt && (
              <p><strong>Finalización:</strong> {new Date(item.finishedAt).toLocaleString()}</p>
            )}

            {item.status === "Finished" ? (
              <Link to={`/design-thinking/results/${item.attemptId}`}>
                Ver resultados
              </Link>
            ) : (
              <Link to={`/design-thinking/simulate/${item.attemptId}`}>
                Continuar simulación
              </Link>
            )}
          </div>
        ))
      )}
    </div>
  );
}

export default DesignThinkingHistoryPage;