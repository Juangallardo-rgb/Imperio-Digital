import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function StudentCourseDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [course, setCourse] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [startingScenarioId, setStartingScenarioId] = useState(null);

  const loadCourse = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/courses/${id}/student-detail`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      setCourse(response.data);
    } catch (error) {
      console.error("Error cargando curso:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setLoading(false);
    }
  };

  const startSimulation = async (scenarioId) => {
    setStartingScenarioId(scenarioId);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/design-thinking/simulations/start",
        {
          scenarioId,
          courseId: Number(id),
        },
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      navigate(`/design-thinking/simulate/${response.data.attemptId}`);
    } catch (error) {
      console.error("Error iniciando simulación:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setStartingScenarioId(null);
    }
  };

  useEffect(() => {
    loadCourse();
  }, [id]);

  if (loading) {
    return (
      <div className="pro-page">
        <div className="pro-card"><p>Cargando curso...</p></div>
      </div>
    );
  }

  if (!course) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <h2>No se encontró el curso</h2>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Curso inscrito</span>
          <h1>{course.name}</h1>
          <p>{course.description}</p>
        </div>

        <div className="phase-pill">
          <span>Código</span>
          <strong>{course.code}</strong>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      <div className="dashboard-stats">
        <div className="stat-card-pro">
          <span>Escenarios disponibles</span>
          <strong>{course.scenarios.length}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Estado del curso</span>
          <strong>{course.isActive ? "Activo" : "Inactivo"}</strong>
        </div>
      </div>

      <div className="pro-card">
        <h2>Escenarios asignados</h2>

        {course.scenarios.length === 0 ? (
          <p>El docente todavía no ha asignado escenarios.</p>
        ) : (
          <div className="pro-grid">
            {course.scenarios.map((scenario) => (
              <div key={scenario.scenarioId} className="course-card">
                <div className="course-card-top">
                  <span className="status-pill success">
                    {scenario.isPublished ? "Publicado" : "No publicado"}
                  </span>
                  <span className="course-code">{scenario.difficulty}</span>
                </div>

                <h2>{scenario.title}</h2>
                <p>
  Resuelve este caso aplicando{" "}
  <strong>{scenario.methodologyName || scenario.methodology || "una metodología"}</strong>{" "}
  bajo restricciones de presupuesto, tiempo y riesgo.
</p>

                <button
                  className="primary-action"
                  onClick={() => startSimulation(scenario.scenarioId)}
                  disabled={startingScenarioId === scenario.scenarioId || !scenario.isPublished}
                >
                  {startingScenarioId === scenario.scenarioId ? "Iniciando..." : "Iniciar simulación"}
                </button>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default StudentCourseDetailPage;