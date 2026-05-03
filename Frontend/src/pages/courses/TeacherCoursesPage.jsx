import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function TeacherCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadCourses = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/courses/my", {
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

  useEffect(() => {
    loadCourses();
  }, []);

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Gestión académica</span>
          <h1>Mis cursos</h1>
          <p>
            Administra tus cursos, asigna escenarios de simulación y revisa el progreso
            de tus estudiantes.
          </p>
        </div>

        <Link className="hero-action" to="/courses/create">
          Crear curso
        </Link>
      </div>

      {message && <div className="message pro-message">{message}</div>}

      {loading ? (
        <div className="pro-card">
          <p>Cargando cursos...</p>
        </div>
      ) : courses.length === 0 ? (
        <div className="empty-state">
          <h2>Aún no tienes cursos</h2>
          <p>Crea tu primer curso para inscribir estudiantes y asignar escenarios.</p>
          <Link className="button-link" to="/courses/create">Crear curso</Link>
        </div>
      ) : (
        <div className="pro-grid">
          {courses.map((course) => (
            <div key={course.id} className="course-card">
              <div className="course-card-top">
                <span className={course.isActive ? "status-pill success" : "status-pill warning"}>
                  {course.isActive ? "Activo" : "Inactivo"}
                </span>
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

              <div className="course-actions">
                <Link to={`/courses/${course.id}`}>Ver curso</Link>
                <Link to={`/courses/${course.id}/results`}>Resultados</Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default TeacherCoursesPage;