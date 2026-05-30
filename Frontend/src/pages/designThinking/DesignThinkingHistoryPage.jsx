import { useEffect, useMemo, useState } from "react";
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

      setHistory(response.data || []);
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

  const stats = useMemo(() => {
    const total = history.length;
    const finished = history.filter((item) => item.status === "Finished").length;
    const inProgress = history.filter((item) => item.status === "InProgress").length;

    const finishedItems = history.filter((item) => item.status === "Finished");
    const average =
      finishedItems.length > 0
        ? finishedItems.reduce((acc, item) => acc + Number(item.finalScore || 0), 0) /
          finishedItems.length
        : 0;

    const best =
      finishedItems.length > 0
        ? [...finishedItems].sort((a, b) => Number(b.finalScore || 0) - Number(a.finalScore || 0))[0]
        : null;

    return {
      total,
      finished,
      inProgress,
      average,
      best,
    };
  }, [history]);

  if (loading) {
    return (
      <div className="student-history-page">
        <div className="history-loading-card">
          <div className="loader-ring"></div>
          <h2>Cargando historial...</h2>
          <p>Preparando tus simulaciones, resultados y progreso académico.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="student-history-page">
      {message && <div className="message pro-message">{message}</div>}

      <section className="student-history-hero">
        <div>
          <span className="eyebrow">Panel estudiante</span>
          <h1>Historial de simulaciones</h1>
          <p>
            Revisa tus intentos anteriores, continúa simulaciones pendientes y analiza
            tu progreso en escenarios de transformación digital.
          </p>
        </div>

        <div className="history-hero-score">
          <span>Promedio general</span>
          <strong>{Math.round(stats.average)}</strong>
          <p>Basado en simulaciones finalizadas</p>
        </div>
      </section>

      <section className="history-stats-grid">
        <HistoryStatCard
          label="Intentos totales"
          value={stats.total}
          detail="Simulaciones iniciadas"
          variant="blue"
        />

        <HistoryStatCard
          label="Finalizadas"
          value={stats.finished}
          detail="Simulaciones completadas"
          variant="green"
        />

        <HistoryStatCard
          label="En progreso"
          value={stats.inProgress}
          detail="Pendientes de terminar"
          variant="orange"
        />

        <HistoryStatCard
          label="Mejor score"
          value={stats.best ? Math.round(Number(stats.best.finalScore || 0)) : 0}
          detail={stats.best ? shortenText(stats.best.scenarioTitle, 28) : "Sin resultados"}
          variant="purple"
        />
      </section>

      <section className="history-main-card">
        <div className="history-section-header">
          <div>
            <span className="eyebrow">Actividad académica</span>
            <h2>Mis simulaciones</h2>
          </div>

          <Link className="mini-link" to="/courses/my">
            Ver mis cursos
          </Link>
        </div>

        {history.length === 0 ? (
          <div className="empty-history-panel">
            <span>📊</span>
            <h2>Aún no tienes simulaciones</h2>
            <p>
              Cuando inicies un escenario, tus resultados aparecerán en esta sección.
            </p>

            <Link className="hero-button primary" to="/courses/my">
              Ir a mis cursos
            </Link>
          </div>
        ) : (
          <div className="history-timeline">
            {history.map((item) => {
              const isFinished = item.status === "Finished";
              const score = Math.round(Number(item.finalScore || 0));

              return (
                <article key={item.attemptId || item.id} className="history-attempt-card">
                  <div className="history-attempt-main">
                    <div className="history-status-column">
                      <span
                        className={
                          isFinished
                            ? "history-status-dot finished"
                            : "history-status-dot progress"
                        }
                      ></span>

                      <div className="history-line"></div>
                    </div>

                    <div className="history-attempt-content">
                      <div className="history-attempt-top">
                        <div>
                          <span
                            className={
                              isFinished
                                ? "status-pill green"
                                : "status-pill orange"
                            }
                          >
                            {isFinished ? "Finalizada" : "En progreso"}
                          </span>

                          <h3>{item.scenarioTitle}</h3>
                        </div>

                        <div
                          className={
                            isFinished
                              ? score >= 70
                                ? "history-score good"
                                : "history-score danger"
                              : "history-score pending"
                          }
                        >
                          <strong>{score}</strong>
                          <span>/100</span>
                        </div>
                      </div>

                      <div className="history-meta-grid">
                        <div>
                          <span>Inicio</span>
                          <strong>{formatDateTime(item.startedAt)}</strong>
                        </div>

                        <div>
                          <span>Finalización</span>
                          <strong>
                            {item.finishedAt
                              ? formatDateTime(item.finishedAt)
                              : "Pendiente"}
                          </strong>
                        </div>

                        <div>
                          <span>Estado</span>
                          <strong>{isFinished ? "Completado" : "Pendiente"}</strong>
                        </div>
                      </div>

                      <div className="history-progress-wrapper">
                        <div className="history-progress-head">
                          <span>Rendimiento</span>
                          <strong>{score}%</strong>
                        </div>

                        <div className="history-progress-track">
                          <div
                            className={
                              score >= 70
                                ? "history-progress-fill good"
                                : "history-progress-fill danger"
                            }
                            style={{ width: `${Math.min(100, score)}%` }}
                          ></div>
                        </div>
                      </div>

                      <div className="history-actions">
                        {isFinished ? (
                          <Link
                            className="history-action-primary"
                            to={`/design-thinking/results/${item.attemptId || item.id}`}
                          >
                            Ver resultados
                          </Link>
                        ) : (
                          <Link
                            className="history-action-primary"
                            to={`/design-thinking/simulate/${item.attemptId || item.id}`}
                          >
                            Continuar simulación
                          </Link>
                        )}
                      </div>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </div>
  );
}

function HistoryStatCard({ label, value, detail, variant }) {
  return (
    <div className={`history-stat-card ${variant}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <p>{detail}</p>
    </div>
  );
}

function formatDateTime(value) {
  if (!value) return "Sin fecha";

  try {
    return new Date(value).toLocaleString("es-EC", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return "Sin fecha";
  }
}

function shortenText(text, maxLength) {
  if (!text) return "";
  return text.length > maxLength ? `${text.slice(0, maxLength)}...` : text;
}

export default DesignThinkingHistoryPage;