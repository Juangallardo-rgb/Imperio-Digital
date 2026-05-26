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
        headers: { Authorization: `Bearer ${token}` },
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
      <div className="pro-page">
        <div className="pro-card">
          <p>Cargando resultados...</p>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <p>No se encontraron resultados.</p>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Resultados · {results.methodologyName || "Simulación"}
          </span>
          <h1>{results.scenarioTitle}</h1>
          <p>
            Revisión final del desempeño, fases completadas, KPIs simulados y
            retroalimentación metodológica.
          </p>
        </div>

        <div className="phase-pill">
          <span>Score final</span>
          <strong>{results.finalScore}</strong>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      <div className="dashboard-stats">
        <div className="stat-card-pro">
          <span>Metodología</span>
          <strong>{results.methodologyName}</strong>
        </div>

        <div className="stat-card-pro">
          <span>Estado</span>
          <strong>{results.status}</strong>
        </div>

        <div className="stat-card-pro">
          <span>Fases evaluadas</span>
          <strong>{results.phaseScores.length}</strong>
        </div>

        <div className="stat-card-pro">
          <span>KPIs</span>
          <strong>{results.kpiResults.length}</strong>
        </div>
      </div>

      <div className="pro-card">
        <h2>Retroalimentación final</h2>
        <div className="info-box">
          <p>{results.finalFeedback}</p>
        </div>
      </div>

      <div className="pro-card">
        <h2>Puntaje por fase</h2>

        {results.phaseScores.map((phase) => (
          <div key={phase.phaseName} className="table-row-card">
            <div>
              <strong>{phase.phaseName}</strong>
              <p>{phase.feedback}</p>
            </div>

            <div className="score-chip">
              {phase.score}
            </div>
          </div>
        ))}
      </div>

      <div className="pro-card">
        <h2>KPIs simulados</h2>

        {results.kpiResults.length === 0 ? (
          <p>No hay KPIs calculados.</p>
        ) : (
          <div className="pro-grid">
            {results.kpiResults.map((kpi) => (
              <div key={kpi.kpiName} className="course-card">
                <h2>{kpi.kpiName}</h2>
                <p>
                  <strong>Inicial:</strong> {kpi.initialValue} {kpi.unit}
                </p>
                <p>
                  <strong>Final:</strong> {kpi.finalValue} {kpi.unit}
                </p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default DesignThinkingResultsPage;