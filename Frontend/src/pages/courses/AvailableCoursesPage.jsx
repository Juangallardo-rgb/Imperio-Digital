import { useEffect, useState } from "react";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function AvailableCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [enrollingId, setEnrollingId] = useState(null);

  const loadCourses = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/courses/available", {
        headers: { Authorization: `Bearer ${token}` },
      });

      setCourses(response.data);
    } catch (error) {
      console.error("Error cargando cursos:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setLoading(false);
    }
  };

  const enroll = async (courseId) => {
    setEnrollingId(courseId);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/courses/${courseId}/enroll`,
        {},
        {
          headers: { Authorization: `Bearer ${token}` },
        }
      );

      setMessage(response.data);
      await loadCourses();
    } catch (error) {
      console.error("Error inscribiendo:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setEnrollingId(null);
    }
  };

  useEffect(() => {
    loadCourses();
  }, []);

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Inscripción</span>
          <h1>Cursos disponibles</h1>
          <p>
            Inscríbete en un curso para acceder a los escenarios asignados por tu docente.
          </p>
        </div>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      {loading ? (
        <div className="pro-card"><p>Cargando cursos...</p></div>
      ) : courses.length === 0 ? (
        <div className="empty-state">
          <h2>No hay cursos disponibles</h2>
          <p>Puede que ya estés inscrito en todos los cursos activos.</p>
        </div>
      ) : (
        <div className="pro-grid">
          {courses.map((course) => (
            <div key={course.id} className="course-card">
              <div className="course-card-top">
                <span className="status-pill success">Activo</span>
                <span className="course-code">{course.code}</span>
              </div>

              <h2>{course.name}</h2>
              <p>{course.description}</p>

              <div className="course-stats">
                <div>
                  <strong>{course.studentsCount}</strong>
                  <span>Estudiantes</span>
                </div>
                <div>
                  <strong>{course.scenariosCount}</strong>
                  <span>Escenarios</span>
                </div>
              </div>

              <button
                className="primary-action"
                onClick={() => enroll(course.id)}
                disabled={enrollingId === course.id}
              >
                {enrollingId === course.id ? "Inscribiendo..." : "Inscribirme"}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default AvailableCoursesPage;