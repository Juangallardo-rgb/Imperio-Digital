import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function DesignThinkingResultsPage() {
  const { attemptId } = useParams();

  const [results, setResults] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadResults = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/design-thinking/simulations/${attemptId}/results`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setResults(response.data);
    } catch (error) {
      console.error("Error cargando resultados:", error);

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
    loadResults();
  }, [attemptId]);

  if (loading) {
    return (
      <div className="page-container">
        <div className="card">
          <p>Cargando resultados...</p>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className="page-container">
        <div className="card">
          <p>No se encontraron resultados.</p>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="card">
        <h1>Resultados finales</h1>
        <p><strong>Escenario:</strong> {results.scenarioTitle}</p>
        <p><strong>Estado:</strong> {results.status}</p>

        <div className="score-number">{results.finalScore}</div>
        <p>Score final</p>

        <div className="info-box">
          <p>{results.finalFeedback}</p>
        </div>
      </div>

      <div className="card">
        <h2>Puntaje por fase</h2>

        {results.phaseScores.map((phase) => (
          <div key={phase.phaseName} className="list-item">
            <h3>{phase.phaseName}</h3>
            <p><strong>Puntaje:</strong> {phase.score}</p>
            <p>{phase.feedback}</p>
          </div>
        ))}
      </div>

      <div className="card">
        <h2>KPIs simulados de negocio</h2>

        {results.kpiResults.length === 0 ? (
          <p>No hay KPIs calculados.</p>
        ) : (
          results.kpiResults.map((kpi) => (
            <div key={kpi.kpiName} className="list-item">
              <h3>{kpi.kpiName}</h3>
              <p><strong>Inicial:</strong> {kpi.initialValue} {kpi.unit}</p>
              <p><strong>Final:</strong> {kpi.finalValue} {kpi.unit}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

export default DesignThinkingResultsPage;