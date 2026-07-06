import {
  useCallback,
  useEffect,
  useState,
} from "react";

import { Link } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";

const MY_COURSE_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
  "CourseScenariosChanged",
];

function MyCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadCourses = useCallback(
    async (showLoader = false) => {
      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        const token = getToken();

        const response = await api.get(
          "/courses/enrolled",
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        setCourses(
          Array.isArray(response.data)
            ? response.data
            : []
        );
      } catch (error) {
        console.error(
          "Error cargando mis cursos:",
          error
        );

        if (showLoader) {
          setMessage(
            error.response
              ? `Error ${error.response.status}: ${JSON.stringify(
                  error.response.data
                )}`
              : "No hubo respuesta del backend."
          );
        }
      } finally {
        if (showLoader) {
          setLoading(false);
        }
      }
    },
    []
  );

  const refreshCourses = useCallback(() => {
    return loadCourses(false);
  }, [loadCourses]);

  useRealtimeRefresh(
    MY_COURSE_EVENTS,
    refreshCourses,
    15000
  );

  useEffect(() => {
    void loadCourses(true);
  }, [loadCourses]);

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Mis cursos
          </span>

          <h1>Cursos inscritos</h1>

          <p>
            Accede a los escenarios asignados por
            tus docentes y continúa tus simulaciones.
          </p>
        </div>

        <Link
          className="hero-action"
          to="/courses/available"
        >
          Ver disponibles
        </Link>
      </div>

      {message && (
        <div className="message pro-message">
          {message}
        </div>
      )}

      {loading ? (
        <div className="pro-card">
          <p>Cargando cursos...</p>
        </div>
      ) : courses.length === 0 ? (
        <div className="empty-state">
          <h2>No estás inscrito en cursos</h2>

          <p>
            Explora los cursos disponibles para
            comenzar.
          </p>

          <Link
            className="button-link"
            to="/courses/available"
          >
            Ver cursos disponibles
          </Link>
        </div>
      ) : (
        <div className="pro-grid">
          {courses.map((course) => (
            <div
              key={course.id}
              className="course-card"
            >
              <div className="course-card-top">
                <span
                  className={
                    course.isActive
                      ? "status-pill success"
                      : "status-pill warning"
                  }
                >
                  {course.isActive
                    ? "Activo"
                    : "Inactivo"}
                </span>

                <span className="course-code">
                  {course.code}
                </span>
              </div>

              <h2>{course.name}</h2>
              <p>{course.description}</p>

              <div className="course-stats">
                <div>
                  <strong>
                    {course.scenariosCount}
                  </strong>
                  <span>Escenarios</span>
                </div>
              </div>

              <Link
                className="button-link"
                to={`/my-courses/${course.id}`}
              >
                Entrar al curso
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default MyCoursesPage;