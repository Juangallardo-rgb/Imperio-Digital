import {
  useCallback,
  useEffect,
  useState,
} from "react";

import api from "../../api/api";
import { getToken } from "../../utils/auth";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";

const AVAILABLE_COURSE_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
];

function AvailableCoursesPage() {
  const [courses, setCourses] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [enrollingId, setEnrollingId] = useState(null);
  const [courseCode, setCourseCode] = useState("");
  const [joiningByCode, setJoiningByCode] = useState(false);

  const loadCourses = useCallback(
    async (showLoader = false) => {
      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        const token = getToken();

        const response = await api.get(
          "/courses/available",
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
          "Error cargando cursos:",
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
    AVAILABLE_COURSE_EVENTS,
    refreshCourses,
    15000
  );

  const enroll = async (courseId) => {
    setEnrollingId(courseId);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/courses/${courseId}/enroll`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(
        typeof response.data === "string"
          ? response.data
          : response.data?.message ||
              "Inscripción realizada correctamente."
      );

      await loadCourses(false);
    } catch (error) {
      console.error(
        "Error inscribiendo:",
        error
      );

      setMessage(
        error.response
          ? `Error ${error.response.status}: ${JSON.stringify(
              error.response.data
            )}`
          : "No hubo respuesta del backend."
      );
    } finally {
      setEnrollingId(null);
    }
  };

  const joinByCode = async (event) => {
    event.preventDefault();

    if (joiningByCode) return;

    const normalizedCode = courseCode.trim().toUpperCase();

    if (!normalizedCode) {
      setMessage("Ingresa un código de curso.");
      return;
    }

    setJoiningByCode(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/courses/join-by-code",
        {
          code: normalizedCode,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(
        typeof response.data === "string"
          ? response.data
          : response.data?.message ||
              "Inscripción realizada correctamente."
      );

      setCourseCode("");
      await loadCourses(false);
    } catch (error) {
      setMessage(
        error.response
          ? typeof error.response.data === "string"
            ? error.response.data
            : JSON.stringify(error.response.data)
          : "No se pudo realizar la inscripción."
      );
    } finally {
      setJoiningByCode(false);
    }
  };

  useEffect(() => {
    void loadCourses(true);
  }, [loadCourses]);

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Inscripción
          </span>

          <h1>Cursos disponibles</h1>

          <p>
            Inscríbete en un curso para acceder a
            los escenarios asignados por tu docente.
          </p>
        </div>
      </div>

      {message && (
        <div className="message pro-message">
          {message}
        </div>
      )}

      <div className="pro-card join-code-card">
        <div className="section-header">
          <div>
            <span className="eyebrow">
              Matrícula directa
            </span>

            <h2>Unirme con código de curso</h2>
          </div>
        </div>

        <form
          className="join-code-form"
          onSubmit={joinByCode}
        >
          <div className="form-group">
            <label>Código de curso</label>
            <input
              value={courseCode}
              onChange={(event) =>
                setCourseCode(
                  event.target.value.toUpperCase()
                )
              }
              placeholder="IMP-XXXXXX"
            />
          </div>

          <button
            type="submit"
            className="primary-action"
            disabled={joiningByCode}
          >
            {joiningByCode
              ? "Inscribiendo..."
              : "Unirme al curso"}
          </button>
        </form>
      </div>

      {loading ? (
        <div className="pro-card">
          <p>Cargando cursos...</p>
        </div>
      ) : courses.length === 0 ? (
        <div className="empty-state">
          <h2>No hay cursos disponibles</h2>

          <p>
            Puede que ya estés inscrito en todos
            los cursos activos.
          </p>
        </div>
      ) : (
        <div className="pro-grid">
          {courses.map((course) => (
            <div
              key={course.id}
              className="course-card"
            >
              <div className="course-card-top">
                <span className="status-pill success">
                  Activo
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
                    {course.studentsCount}
                  </strong>
                  <span>Estudiantes</span>
                </div>

                <div>
                  <strong>
                    {course.scenariosCount}
                  </strong>
                  <span>Escenarios</span>
                </div>
              </div>

              <button
                className="primary-action"
                onClick={() => enroll(course.id)}
                disabled={enrollingId === course.id}
              >
                {enrollingId === course.id
                  ? "Inscribiendo..."
                  : "Inscribirme"}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export default AvailableCoursesPage;
