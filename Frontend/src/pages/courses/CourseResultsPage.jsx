import { useEffect, useMemo, useState } from "react";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import { Link, useParams } from "react-router-dom";

function CourseResultsPage() {
  const { id } = useParams();

  const [results, setResults] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadResults = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/courses/${id}/results`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      setResults(response.data);
    } catch (error) {
      console.error("Error cargando resultados:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadResults();
  }, [id]);

  const totalAttempts = useMemo(() => {
    if (!results) return 0;
    return results.students.reduce((acc, student) => acc + student.simulations.length, 0);
  }, [results]);

  if (loading) {
    return (
      <div className="pro-page">
        <div className="pro-card"><p>Cargando resultados...</p></div>
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
          <span className="eyebrow">Analítica docente</span>
          <h1>Resultados del curso</h1>
          <p>{results.courseName}</p>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      <div className="dashboard-stats">
        <div className="stat-card-pro">
          <span>Estudiantes</span>
          <strong>{results.studentsCount}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Intentos totales</span>
          <strong>{totalAttempts}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Finalizados</span>
          <strong>{results.finishedAttempts}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Promedio</span>
          <strong>{results.averageScore}</strong>
        </div>
      </div>

      <div className="pro-card">
        <h2>Resumen por estudiante</h2>

        {results.students.length === 0 ? (
          <p>No hay estudiantes inscritos.</p>
        ) : (
          <div className="table-list">
            {results.students.map((student) => (
              <div key={student.studentId} className="student-result-card">
                <div className="student-result-header">
                  <div>
                    <h3>{student.studentName}</h3>
                    <p>{student.studentEmail}</p>
                  </div>
                  <span className="status-pill">
                    {student.simulations.length} simulación(es)
                  </span>
                </div>

                {student.simulations.length === 0 ? (
                  <p className="muted">Este estudiante aún no tiene intentos registrados.</p>
                ) : (
                  <div className="table-list">
                    {student.simulations.map((simulation) => (
                      <div key={simulation.attemptId} className="table-row-card">
                        <div>
                          <strong>{simulation.scenarioTitle}</strong>
                          <p>
                            Estado: {simulation.status} · Inicio:{" "}
                            {new Date(simulation.startedAt).toLocaleString()}
                          </p>
                        </div>

                        <div className="score-chip">
                          {simulation.finalScore}
                        </div>

                        <Link to={`/design-thinking/results/${simulation.attemptId}`}>
                          Ver detalle
                        </Link>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default CourseResultsPage;