import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function CourseDetailPage() {
  const { id } = useParams();

  const [course, setCourse] = useState(null);
  const [scenarios, setScenarios] = useState([]);
  const [selectedScenarioId, setSelectedScenarioId] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [assigning, setAssigning] = useState(false);

  const loadCourse = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/courses/${id}`, {
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

  const loadScenarios = async () => {
    try {
      const token = getToken();

      const response = await api.get("/design-thinking/scenarios/my", {
        headers: { Authorization: `Bearer ${token}` },
      });

      setScenarios(response.data);
    } catch (error) {
      console.error("Error cargando escenarios:", error);
    }
  };

  const assignScenario = async () => {
    if (!selectedScenarioId) {
      setMessage("Selecciona un escenario para asignar.");
      return;
    }

    setAssigning(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/courses/${id}/scenarios/${selectedScenarioId}`,
        {},
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      setMessage(response.data);
      setSelectedScenarioId("");
      await loadCourse();
    } catch (error) {
      console.error("Error asignando escenario:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setAssigning(false);
    }
  };

  useEffect(() => {
    loadCourse();
    loadScenarios();
  }, [id]);

  if (loading) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <p>Cargando curso...</p>
        </div>
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
          <span className="eyebrow">Curso académico</span>
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
          <span>Estudiantes inscritos</span>
          <strong>{course.students.length}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Escenarios asignados</span>
          <strong>{course.scenarios.length}</strong>
        </div>
        <div className="stat-card-pro">
          <span>Estado</span>
          <strong>{course.isActive ? "Activo" : "Inactivo"}</strong>
        </div>
      </div>

      <div className="pro-layout-2">
        <div className="pro-card">
          <div className="section-header">
            <div>
              <span className="eyebrow">Asignación</span>
              <h2>Asignar escenario</h2>
            </div>
          </div>

          <div className="form-group">
            <label>Escenario</label>
            <select value={selectedScenarioId} onChange={(e) => setSelectedScenarioId(e.target.value)}>
              <option value="">Selecciona un escenario</option>
              {scenarios.map((scenario) => (
                <option key={scenario.id} value={scenario.id}>
                  {scenario.title} {scenario.isPublished ? "" : "(Borrador)"}
                </option>
              ))}
            </select>
          </div>

          <button className="primary-action" onClick={assignScenario} disabled={assigning}>
            {assigning ? "Asignando..." : "Asignar al curso"}
          </button>
        </div>

        <div className="pro-card">
          <div className="section-header">
            <div>
              <span className="eyebrow">Analítica</span>
              <h2>Resultados</h2>
            </div>
          </div>

          <p>Revisa el desempeño de estudiantes, intentos finalizados y puntajes.</p>

          <Link className="button-link" to={`/courses/${course.id}/results`}>
            Ver resultados del curso
          </Link>
        </div>
      </div>

      <div className="pro-card">
        <h2>Escenarios asignados</h2>

        {course.scenarios.length === 0 ? (
          <p>No hay escenarios asignados.</p>
        ) : (
          <div className="table-list">
            {course.scenarios.map((scenario) => (
              <div key={scenario.scenarioId} className="table-row-card">
                <div>
                  <strong>{scenario.title}</strong>
                  <p>Dificultad: {scenario.difficulty}</p>
                </div>
                <span className={scenario.isPublished ? "status-pill success" : "status-pill warning"}>
                  {scenario.isPublished ? "Publicado" : "Borrador"}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="pro-card">
        <h2>Estudiantes inscritos</h2>

        {course.students.length === 0 ? (
          <p>No hay estudiantes inscritos todavía.</p>
        ) : (
          <div className="table-list">
            {course.students.map((student) => (
              <div key={student.studentId} className="table-row-card">
                <div>
                  <strong>{student.name}</strong>
                  <p>{student.email}</p>
                </div>
                <span>{new Date(student.enrolledAt).toLocaleDateString()}</span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

export default CourseDetailPage;