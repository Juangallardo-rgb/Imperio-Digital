import {
  useCallback,
  useEffect,
  useState,
} from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import useRealtimeRefresh from "../../hooks/useRealtimeRefresh";

const STUDENT_COURSE_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
  "CourseScenariosChanged",
  "ResultsChanged",
];

function isTrue(value) {
  return value === true || value === "true";
}

function StudentCourseDetailPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const courseId = Number(id);

  const [course, setCourse] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [startingScenarioId, setStartingScenarioId] = useState(null);

  const loadCourse = useCallback(
    async (showLoader = false) => {
      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        const token = getToken();

        const response = await api.get(
          `/courses/${courseId}/student-detail`,
          {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }
        );

        setCourse(response.data);
      } catch (error) {
        console.error(
          "Error cargando curso del estudiante:",
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
    [courseId]
  );

  const refreshCourse = useCallback(
    (payload) => {
      if (
        payload?.courseId &&
        Number(payload.courseId) !== courseId
      ) {
        return Promise.resolve();
      }

      return loadCourse(false);
    },
    [courseId, loadCourse]
  );

  useRealtimeRefresh(
    STUDENT_COURSE_EVENTS,
    refreshCourse,
    15000
  );

  const startSimulation = async (scenarioId) => {
    if (startingScenarioId) return;

    setStartingScenarioId(scenarioId);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/design-thinking/simulations/start",
        {
          scenarioId: Number(scenarioId),
          courseId,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      const attemptId =
        response.data?.attemptId ??
        response.data?.AttemptId ??
        (typeof response.data === "number"
          ? response.data
          : null);

      if (!attemptId) {
        throw new Error(
          "El backend inició la solicitud, pero no devolvió el identificador del intento."
        );
      }

      navigate(`/design-thinking/simulate/${attemptId}`);
    } catch (error) {
      console.error(
        "Error iniciando simulación:",
        error
      );

      setMessage(
        error.response
          ? typeof error.response.data === "string"
            ? error.response.data
            : error.response.data?.message ||
              JSON.stringify(error.response.data)
          : error.message ||
            "No se pudo iniciar la simulación."
      );
    } finally {
      setStartingScenarioId(null);
    }
  };

  useEffect(() => {
    void loadCourse(true);
  }, [loadCourse]);

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

          {message && (
            <div className="message">
              {message}
            </div>
          )}
        </div>
      </div>
    );
  }

  const scenarios = Array.isArray(course.scenarios)
    ? course.scenarios
    : Array.isArray(course.Scenarios)
    ? course.Scenarios
    : [];

  const publishedScenarios = scenarios.filter(
    (scenario) => isTrue(scenario.isPublished ?? scenario.IsPublished)
  );
  const isCourseActive = isTrue(course.isActive ?? course.IsActive);

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Mi curso
          </span>

          <h1>{course.name}</h1>
          <p>{course.description}</p>
        </div>

        <div className="phase-pill">
          <span>Código</span>
          <strong>{course.code}</strong>
        </div>
      </div>

      {message && (
        <div className="message pro-message">
          {message}
        </div>
      )}

      <div className="dashboard-stats">
        <div className="stat-card-pro">
          <span>Escenarios disponibles</span>
          <strong>
            {publishedScenarios.length}
          </strong>
        </div>

        <div className="stat-card-pro">
          <span>Escenarios asignados</span>
          <strong>
            {scenarios.length}
          </strong>
        </div>

        <div className="stat-card-pro">
          <span>Estado del curso</span>
          <strong>{isCourseActive ? "Activo" : "Inactivo"}</strong>
        </div>
      </div>

      <div className="pro-card">
        <div className="section-header">
          <div>
            <span className="eyebrow">
              Simulaciones
            </span>

            <h2>Escenarios asignados</h2>
          </div>
        </div>

        {scenarios.length === 0 ? (
          <div className="empty-state">
            <h2>
              No hay escenarios asignados
            </h2>

            <p>
              El docente todavía no ha publicado
              escenarios para este curso.
            </p>
          </div>
        ) : (
          <div className="table-list">
            {scenarios.map((scenario) => {
              const scenarioId = Number(
                scenario.scenarioId ??
                  scenario.ScenarioId ??
                  scenario.id ??
                  scenario.Id
              );
              const isPublished = isTrue(
                scenario.isPublished ?? scenario.IsPublished
              );

              const isStarting =
                startingScenarioId === scenarioId;

              return (
                <div
                  key={scenarioId}
                  className="table-row-card"
                >
                  <div>
                    <strong>{scenario.title ?? scenario.Title}</strong>

                    <p>
                      {scenario.description ||
                        "Sin descripción"}
                    </p>

                    <span>
                      {scenario.methodologyName ||
                        scenario.methodology ||
                        "Metodología no definida"}{" "}
                      · Dificultad:{" "}
                      {scenario.difficulty}
                    </span>
                  </div>

                  <div
                    style={{
                      display: "flex",
                      alignItems: "center",
                      gap: "12px",
                      flexWrap: "wrap",
                      justifyContent: "flex-end",
                    }}
                  >
                    <span
                      className={
                        isPublished
                          ? "status-pill success"
                          : "status-pill warning"
                      }
                    >
                      {isPublished
                        ? "Disponible"
                        : "Pendiente de publicar"}
                    </span>

                    <button
                      type="button"
                      className="primary-action"
                      onClick={() =>
                        startSimulation(scenarioId)
                      }
                      disabled={
                        !isCourseActive ||
                        !isPublished ||
                        isStarting ||
                        Boolean(startingScenarioId)
                      }
                    >
                      {isStarting
                        ? "Iniciando..."
                        : "Iniciar simulación"}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

export default StudentCourseDetailPage;
