import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function CourseSimulationResultDetailPage() {
  const { courseId, attemptId } = useParams();

  const [results, setResults] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadResults = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(
        `/courses/${courseId}/attempts/${attemptId}/results`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setResults(response.data);
    } catch (error) {
      console.error("Error cargando detalle docente:", error);

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
  }, [courseId, attemptId]);

  if (loading) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <p>Cargando detalle de simulación...</p>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <h2>No se encontraron resultados</h2>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Detalle de simulación</span>
          <h1>{results.scenarioTitle}</h1>
          <p>
            Revisión docente del desempeño del estudiante en la simulación.
          </p>
        </div>

        <div className="phase-pill">
          <span>Score final</span>
          <strong>{results.finalScore}</strong>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      <div className="pro-card">
        <h2>Retroalimentación final</h2>
        <div className="info-box">
          <p>{results.finalFeedback || "No hay retroalimentación final registrada."}</p>
        </div>
      </div>

      <div className="pro-card">
        <h2>Puntaje por fase</h2>

        {results.phaseScores.length === 0 ? (
          <p>No hay fases registradas.</p>
        ) : (
          <div className="table-list">
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
        )}
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
                <p><strong>Inicial:</strong> {kpi.initialValue} {kpi.unit}</p>
                <p><strong>Final:</strong> {kpi.finalValue} {kpi.unit}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default CourseSimulationResultDetailPage;