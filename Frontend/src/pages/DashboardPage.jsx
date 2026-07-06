import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import api from "../api/api";
import { getToken, getUserFromToken } from "../utils/auth";
import useRealtimeRefresh from "../hooks/useRealtimeRefresh";

const TEACHER_DASHBOARD_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
  "CourseScenariosChanged",
  "ResultsChanged",
];

const STUDENT_DASHBOARD_EVENTS = [
  "CoursesChanged",
  "EnrollmentsChanged",
  "CourseScenariosChanged",
  "ResultsChanged",
];

const EMPTY_EVENTS = [];

function DashboardPage() {
  const user = useMemo(() => getUserFromToken(), []);
  const token = useMemo(() => getToken(), []);

  const [studentHistory, setStudentHistory] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [teacherAnalytics, setTeacherAnalytics] = useState(null);

  const loadDashboardData = useCallback(
    async (showLoader = false) => {
      if (!user || !token) {
        if (showLoader) {
          setLoading(false);
        }

        return;
      }

      if (showLoader) {
        setLoading(true);
        setMessage("");
      }

      try {
        if (user.role === "Docente") {
          const analyticsResponse = await api.get(
            "/courses/teacher-dashboard",
            {
              headers: {
                Authorization: `Bearer ${token}`,
              },
            }
          );

          setTeacherAnalytics(analyticsResponse.data);
        }

        if (user.role === "Estudiante") {
          const historyResponse = await api.get(
            "/design-thinking/simulations/my-history",
            {
              headers: {
                Authorization: `Bearer ${token}`,
              },
            }
          );

          setStudentHistory(
            Array.isArray(historyResponse.data)
              ? historyResponse.data
              : []
          );
        }
      } catch (error) {
        console.error("Error cargando dashboard:", error);

        if (showLoader) {
          if (error.response) {
            setMessage(
              `Error ${error.response.status}: ${JSON.stringify(
                error.response.data
              )}`
            );
          } else {
            setMessage(
              "No se pudo cargar la información del dashboard."
            );
          }
        }
      } finally {
        if (showLoader) {
          setLoading(false);
        }
      }
    },
    [token, user]
  );

  const refreshDashboard = useCallback(() => {
    return loadDashboardData(false);
  }, [loadDashboardData]);

  const dashboardEvents =
    user?.role === "Docente"
      ? TEACHER_DASHBOARD_EVENTS
      : user?.role === "Estudiante"
      ? STUDENT_DASHBOARD_EVENTS
      : EMPTY_EVENTS;

  useRealtimeRefresh(
    dashboardEvents,
    refreshDashboard,
    15000
  );

  useEffect(() => {
    void loadDashboardData(true);
  }, [loadDashboardData]);

  if (!user) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <h2>No hay sesión iniciada</h2>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="pro-page">
        <div className="dashboard-hero-pro skeleton-hero">
          <div>
            <span className="eyebrow">Imperio Digital</span>
            <h1>Cargando dashboard...</h1>
            <p>
              Preparando indicadores, gráficos y resumen del
              sistema.
            </p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page dashboard-pro-page">
      {message && (
        <div className="message pro-message">
          {message}
        </div>
      )}

      {user.role === "Docente" && (
        <TeacherDashboard
          user={user}
          analytics={teacherAnalytics}
        />
      )}

      {user.role === "Estudiante" && (
        <StudentDashboard
          user={user}
          history={studentHistory}
        />
      )}
    </div>
  );
}

function TeacherDashboard({ user, analytics }) {
  const summary = analytics?.summary || {};

  const courseAverages = analytics?.courseAverages || [];
  const methodologyAverages = analytics?.methodologyAverages || [];
  const completionStatus = analytics?.completionStatus || [];
  const lowPerformanceCourses = analytics?.lowPerformanceCourses || [];

  return (
    <>
      <section className="dashboard-hero-pro teacher-hero">
        <div>
          <span className="eyebrow">Panel docente</span>
          <h1>Negocios Digitales - UDLA</h1>
          <p>
            Indicadores, KPIs y gráficos de rendimiento por curso, metodología y
            participación estudiantil.
          </p>

          <div className="hero-actions">
            <Link className="hero-button primary" to="/courses">
              Gestionar cursos
            </Link>

            <Link
              className="hero-button secondary"
              to="/design-thinking/scenarios/create"
            >
              Crear escenario
            </Link>
          </div>
        </div>

        <div className="hero-kpi-glass">
          <span>Promedio general</span>
          <strong>{Math.round(Number(summary.averageScore || 0))}</strong>
          <p>Promedio de simulaciones finalizadas</p>
          <MiniRing percent={summary.averageScore || 0} />
        </div>
      </section>

      <section className="power-stats-grid">
        <PowerStatCard
          label="Cursos activos"
          value={summary.coursesCount || 0}
          detail="Cursos creados por el docente"
          variant="blue"
        />

        <PowerStatCard
          label="Estudiantes"
          value={summary.studentsCount || 0}
          detail="Inscritos en tus cursos"
          variant="green"
        />

        <PowerStatCard
          label="Tasa finalización"
          value={`${Math.round(Number(summary.completionRate || 0))}%`}
          detail="Intentos terminados"
          variant="purple"
        />

        <PowerStatCard
          label="Cursos en riesgo"
          value={summary.riskCoursesCount || 0}
          detail="Promedio menor a 70"
          variant="orange"
        />
      </section>

      <section className="teacher-insight-grid">
        <div className="insight-card best">
          <span>Curso con mejor rendimiento</span>

          <h2>{summary.bestCourseName || "Sin datos"}</h2>

          <strong>
            {Math.round(Number(summary.bestCourseScore || 0))} / 100
          </strong>

          <p>Curso con mayor promedio de simulaciones.</p>
        </div>

        <div className="insight-card risk">
          <span>Metodología más dominada</span>

          <h2>{summary.topMethodologyName || "Sin datos"}</h2>

          <strong>
            {Math.round(Number(summary.topMethodologyScore || 0))} / 100
          </strong>

          <p>Metodología con mejor promedio general.</p>
        </div>
      </section>

      <section className="dashboard-analytics-grid">
        <div className="analytics-card wide">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">KPI por curso</span>
              <h2>Promedio de rendimiento por curso</h2>
            </div>

            <span className="analytics-badge">Diagrama de barras</span>
          </div>

          <CourseKpiBarChart data={courseAverages} />
        </div>

        <div className="analytics-card">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Participación</span>
              <h2>Finalizadas vs en progreso</h2>
            </div>

            <span className="analytics-badge">KPI circular</span>
          </div>

          <CompletionDonut data={completionStatus} />
        </div>
      </section>

      <section className="dashboard-analytics-grid">
        <div className="analytics-card wide">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Dominio metodológico</span>
              <h2>Promedio por metodología</h2>
            </div>

            <span className="analytics-badge">KPI horizontal</span>
          </div>

          <MethodologyKpiBars data={methodologyAverages} />
        </div>

        <div className="analytics-card alerts-card">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Alertas</span>
              <h2>Cursos bajo 70 puntos</h2>
            </div>
          </div>

          {lowPerformanceCourses.length === 0 ? (
            <div className="success-panel">
              <strong>Sin alertas críticas</strong>
              <p>No hay cursos con promedio menor a 70.</p>
            </div>
          ) : (
            <div className="dashboard-list">
              {lowPerformanceCourses.map((course) => (
                <div key={course.courseId} className="dashboard-list-row">
                  <div>
                    <strong>{course.courseName}</strong>
                    <span>{course.simulationsCount} simulación(es)</span>
                  </div>

                  <span className="alert-score-red">
                    {Math.round(Number(course.averageScore || 0))}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="analytics-card full-width-card">
        <div className="analytics-header">
          <div>
            <span className="eyebrow">Ranking</span>
            <h2>Comparativo de cursos</h2>
          </div>

          <Link className="mini-link" to="/courses">
            Ver cursos
          </Link>
        </div>

        {courseAverages.length === 0 ? (
          <p className="empty-state">Todavía no tienes cursos creados.</p>
        ) : (
          <div className="course-ranking-table">
            <div className="ranking-header">
              <span>Curso</span>
              <span>Estudiantes</span>
              <span>Simulaciones</span>
              <span>Promedio</span>
            </div>

            {[...courseAverages]
              .sort(
                (a, b) =>
                  Number(b.averageScore || 0) -
                  Number(a.averageScore || 0)
              )
              .map((course) => (
                <div key={course.courseId} className="ranking-row">
                  <span>{course.courseName}</span>
                  <span>{course.studentsCount}</span>
                  <span>{course.simulationsCount}</span>
                  <strong>
                    {Math.round(Number(course.averageScore || 0))}
                  </strong>
                </div>
              ))}
          </div>
        )}
      </section>
    </>
  );
}

function StudentDashboard({ user, history }) {
  const finished = history.filter((item) => item.status === "Finished");
  const inProgress = history.filter((item) => item.status !== "Finished");

  const averageScore =
    finished.length > 0
      ? Math.round(
          finished.reduce((sum, item) => sum + Number(item.finalScore || 0), 0) /
            finished.length
        )
      : 0;

  const bestScore =
    finished.length > 0
      ? Math.max(...finished.map((item) => Number(item.finalScore || 0)))
      : 0;

  const recentProgress = history.slice(0, 6).reverse().map((item) => ({
    label: shortenText(item.scenarioTitle || "Escenario", 18),
    value: Number(item.finalScore || 0),
  }));

  const finishedPercent =
    history.length > 0 ? Math.round((finished.length / history.length) * 100) : 0;

  return (
    <>
      <section className="dashboard-hero-pro student-hero">
        <div>
          <span className="eyebrow">Panel estudiante</span>
          <h1>Bienvenido, {user.name}</h1>
          <p>
            Visualiza tu avance, puntajes, simulaciones finalizadas y desempeño
            acumulado en los escenarios metodológicos.
          </p>

          <div className="hero-actions">
            <Link className="hero-button primary" to="/my-courses">
              Mis cursos
            </Link>

            <Link className="hero-button secondary" to="/courses/available">
              Cursos disponibles
            </Link>
          </div>
        </div>

        <div className="hero-kpi-glass">
          <span>Promedio general</span>
          <strong>{averageScore}</strong>
          <p>Sobre 100 puntos</p>
          <MiniRing percent={averageScore} />
        </div>
      </section>

      <section className="power-stats-grid">
        <PowerStatCard
          label="Simulaciones"
          value={history.length}
          detail="Intentos registrados"
          variant="blue"
        />

        <PowerStatCard
          label="Finalizadas"
          value={finished.length}
          detail="Completadas correctamente"
          variant="green"
        />

        <PowerStatCard
          label="En progreso"
          value={inProgress.length}
          detail="Pendientes de finalizar"
          variant="orange"
        />

        <PowerStatCard
          label="Mejor score"
          value={bestScore}
          detail="Puntaje máximo alcanzado"
          variant="purple"
        />
      </section>

      <section className="dashboard-analytics-grid">
        <div className="analytics-card wide">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Progreso</span>
              <h2>Puntaje por simulación</h2>
            </div>
            <span className="analytics-badge">Estudiante</span>
          </div>

          <BarChart data={recentProgress} emptyText="Aún no tienes simulaciones finalizadas." />
        </div>

        <div className="analytics-card">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Completitud</span>
              <h2>Estado de simulaciones</h2>
            </div>
          </div>

          <DonutSummary
            value={finished.length}
            total={history.length}
            centerText={`${finishedPercent}%`}
            label="finalizadas"
          />
        </div>
      </section>

      <section className="dashboard-analytics-grid">
        <div className="analytics-card">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Rendimiento</span>
              <h2>Nivel actual</h2>
            </div>
          </div>

          <PerformancePanel score={averageScore} />
        </div>

        <div className="analytics-card wide">
          <div className="analytics-header">
            <div>
              <span className="eyebrow">Historial</span>
              <h2>Últimas simulaciones</h2>
            </div>
            <Link className="mini-link" to="/design-thinking/history">
              Ver historial
            </Link>
          </div>

          {history.length === 0 ? (
            <p className="empty-state">Todavía no has realizado simulaciones.</p>
          ) : (
            <div className="dashboard-list">
              {history.slice(0, 5).map((item) => (
                <div key={item.attemptId} className="dashboard-list-row">
                  <div>
                    <strong>{item.scenarioTitle}</strong>
                    <span>{formatDate(item.startedAt)} · {item.status}</span>
                  </div>

                  <span className="score-chip">
                    {Number(item.finalScore || 0)}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </section>
    </>
  );
}

function PowerStatCard({ label, value, detail, variant }) {
  return (
    <div className={`power-stat-card ${variant}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <p>{detail}</p>
    </div>
  );
}

function BarChart({ data, emptyText }) {
  const max = Math.max(...data.map((item) => Number(item.value || 0)), 0);

  if (!data.length || max === 0) {
    return <p className="empty-state">{emptyText}</p>;
  }

  return (
    <div className="bar-chart-pro">
      {data.map((item, index) => {
        const height = max > 0 ? Math.max(12, (item.value / max) * 100) : 0;

        return (
          <div key={`${item.label}-${index}`} className="bar-item-pro">
            <div className="bar-value">{item.value}</div>
            <div className="bar-track">
              <div
                className={`bar-fill gradient-${(index % 5) + 1}`}
                style={{ height: `${height}%` }}
              ></div>
            </div>
            <span title={item.label}>{item.label}</span>
          </div>
        );
      })}
    </div>
  );
}

function HorizontalBarChart({ data }) {
  const max = Math.max(...data.map((item) => Number(item.value || 0)), 1);

  return (
    <div className="horizontal-chart">
      {data.map((item, index) => {
        const width = Math.max(4, (item.value / max) * 100);

        return (
          <div key={item.label} className="horizontal-row">
            <div className="horizontal-label">
              <span>{item.label}</span>
              <strong>{item.value}</strong>
            </div>

            <div className="horizontal-track">
              <div
                className={`horizontal-fill gradient-${(index % 5) + 1}`}
                style={{ width: `${width}%` }}
              ></div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

function DonutSummary({ value, total, centerText, label }) {
  const percent = total > 0 ? Math.round((value / total) * 100) : 0;

  return (
    <div className="donut-layout">
      <div
        className="donut-chart"
        style={{
          background: `conic-gradient(#2563eb ${percent * 3.6}deg, #e2e8f0 0deg)`,
        }}
      >
        <div>
          <strong>{centerText}</strong>
          <span>{label}</span>
        </div>
      </div>

      <div className="donut-info">
        <p><strong>{value}</strong> de {total}</p>
        <span>{label}</span>
      </div>
    </div>
  );
}

function MiniRing({ percent }) {
  const safePercent = Math.min(100, Math.max(0, Number(percent || 0)));

  return (
    <div
      className="mini-ring"
      style={{
        background: `conic-gradient(#ffffff ${safePercent * 3.6}deg, rgba(255,255,255,0.22) 0deg)`,
      }}
    >
      <div></div>
    </div>
  );
}

function PerformancePanel({ score }) {
  let label = "Inicial";
  let description = "Completa más simulaciones para construir tu progreso.";

  if (score >= 90) {
    label = "Excelente";
    description = "Tu desempeño muestra dominio alto en la toma de decisiones.";
  } else if (score >= 75) {
    label = "Avanzado";
    description = "Tienes buen rendimiento, pero aún puedes mejorar la consistencia.";
  } else if (score >= 60) {
    label = "Intermedio";
    description = "Vas avanzando. Refuerza la justificación y coherencia metodológica.";
  } else if (score > 0) {
    label = "En desarrollo";
    description = "Necesitas mejorar selección de decisiones y análisis del caso.";
  }

  return (
    <div className="performance-panel">
      <div className="performance-score">{score}</div>
      <h3>{label}</h3>
      <p>{description}</p>
    </div>
  );
}

function getMethodologyName(methodologyCode) {
  const names = {
    DesignThinking: "Design Thinking",
    BPM: "Business Process Management",
    DigitalMaturity: "Madurez Digital",
    LeanStartup: "Lean Startup",
  };

  return names[methodologyCode] || methodologyCode || "No definida";
}

function shortenText(text, max) {
  if (!text) return "";
  return text.length > max ? `${text.slice(0, max)}...` : text;
}

function formatDate(date) {
  if (!date) return "Sin fecha";

  try {
    return new Date(date).toLocaleDateString();
  } catch {
    return "Sin fecha";
  }
}

function normalizeCourseAnalytics(course, rawResults) {
  const results = extractCourseResults(rawResults);

  const finishedResults = results.filter(
    (item) => item.status === "Finished" || item.status === "Finalizada"
  );

  const scores = finishedResults
    .map((item) => Number(item.finalScore || item.score || 0))
    .filter((score) => score > 0);

  const averageScore =
    scores.length > 0
      ? scores.reduce((sum, score) => sum + score, 0) / scores.length
      : 0;

  const methodologyScores = {};

  finishedResults.forEach((item) => {
    const methodology = normalizeMethodologyName(
  item.methodologyName ||
    item.methodologyCode ||
    item.methodology ||
    item.scenarioMethodology ||
    item.scenario?.methodology
);

    if (!methodologyScores[methodology]) {
      methodologyScores[methodology] = [];
    }

    methodologyScores[methodology].push(Number(item.finalScore || item.score || 0));
  });

  return {
    courseId: course.id || course.courseId,
    courseName: course.name || course.title || "Curso sin nombre",
    studentsCount:
      course.studentsCount ||
      course.students?.length ||
      course.enrollments?.length ||
      0,
    scenariosCount:
      course.scenariosCount ||
      course.scenarios?.length ||
      course.courseScenarios?.length ||
      0,
    simulationsCount: finishedResults.length,
    averageScore,
    methodologyScores,
  };
}

function extractCourseResults(rawResults) {
  if (!rawResults) return [];

  if (Array.isArray(rawResults)) return rawResults;

  if (Array.isArray(rawResults.results)) return rawResults.results;

  if (Array.isArray(rawResults.studentResults)) return rawResults.studentResults;

  if (Array.isArray(rawResults.simulations)) return rawResults.simulations;

  if (Array.isArray(rawResults.attempts)) return rawResults.attempts;

  if (Array.isArray(rawResults.students)) {
    return rawResults.students.flatMap((student) => {
      if (Array.isArray(student.simulations)) return student.simulations;
      if (Array.isArray(student.results)) return student.results;
      if (Array.isArray(student.attempts)) return student.attempts;
      return [];
    });
  }

  return [];
}

function buildMethodologyMasteryData(courseAnalytics, scenarios) {
  const baseMethodologies = {
    "Design Thinking": [],
    "Business Process Management": [],
    "Madurez Digital": [],
    "Lean Startup": [],
  };

  courseAnalytics.forEach((course) => {
    Object.entries(course.methodologyScores || {}).forEach(([methodology, scores]) => {
      const normalizedName = normalizeMethodologyName(methodology);

      if (!baseMethodologies[normalizedName]) {
        baseMethodologies[normalizedName] = [];
      }

      baseMethodologies[normalizedName].push(...scores);
    });
  });

  scenarios.forEach((scenario) => {
    const methodology = normalizeMethodologyName(
      scenario.methodologyName || scenario.methodology
    );

    if (!baseMethodologies[methodology]) {
      baseMethodologies[methodology] = [];
    }
  });

  return Object.entries(baseMethodologies).map(([label, scores]) => {
    const validScores = scores.filter((score) => Number(score) > 0);

    const average =
      validScores.length > 0
        ? Math.round(
            validScores.reduce((sum, score) => sum + Number(score), 0) /
              validScores.length
          )
        : 0;

    return {
      label,
      value: average,
    };
  });
}
function CompactCoursePerformance({ data }) {
  const coursesWithData = data.filter((course) => course.simulationsCount > 0);

  if (coursesWithData.length === 0) {
    return (
      <p className="empty-state">
        Aún no existen cursos con simulaciones finalizadas.
      </p>
    );
  }

  return (
    <div className="compact-course-performance">
      {[...coursesWithData]
        .sort((a, b) => b.averageScore - a.averageScore)
        .map((course) => {
          const score = Math.round(course.averageScore || 0);

          return (
            <div key={course.courseId} className="compact-course-row">
              <div className="compact-course-info">
                <strong>{course.courseName}</strong>
                <span>{course.simulationsCount} simulación(es)</span>
              </div>

              <div className="compact-course-meter">
                <div
                  className={
                    score < 70
                      ? "compact-course-fill low"
                      : score >= 85
                      ? "compact-course-fill high"
                      : "compact-course-fill mid"
                  }
                  style={{ width: `${Math.min(100, score)}%` }}
                ></div>
              </div>

              <div
                className={
                  score < 70
                    ? "compact-score danger"
                    : score >= 85
                    ? "compact-score success"
                    : "compact-score normal"
                }
              >
                {score}
              </div>
            </div>
          );
        })}
    </div>
  );
}

function normalizeMethodologyName(value) {
  if (!value) return "Design Thinking";

  const clean = String(value).trim();

  const names = {
    DesignThinking: "Design Thinking",
    "Design Thinking": "Design Thinking",
    BPM: "Business Process Management",
    "Business Process Management": "Business Process Management",
    DigitalMaturity: "Madurez Digital",
    "Madurez Digital": "Madurez Digital",
    LeanStartup: "Lean Startup",
    "Lean Startup": "Lean Startup",
    "No definida": "Design Thinking",
  };

  return names[clean] || clean;
}
function CourseKpiBarChart({ data }) {
  const coursesWithData = data.filter((course) => Number(course.simulationsCount || 0) > 0);

  if (coursesWithData.length === 0) {
    return (
      <p className="empty-state">
        Aún no existen cursos con simulaciones finalizadas.
      </p>
    );
  }

  const maxValue = Math.max(
    ...coursesWithData.map((course) => Number(course.averageScore || 0)),
    100
  );

  return (
    <div className="css-bar-chart">
      <div className="css-chart-axis">
        <span>100</span>
        <span>75</span>
        <span>50</span>
        <span>25</span>
        <span>0</span>
      </div>

      <div className="css-chart-bars">
        {coursesWithData.map((course) => {
          const score = Math.round(Number(course.averageScore || 0));
          const height = Math.max(8, (score / maxValue) * 100);

          return (
            <div key={course.courseId} className="css-bar-column">
              <div className="css-bar-score">{score}</div>

              <div className="css-bar-track">
                <div
                  className={
                    score < 70
                      ? "css-bar-fill danger"
                      : score >= 85
                      ? "css-bar-fill success"
                      : "css-bar-fill normal"
                  }
                  style={{ height: `${height}%` }}
                ></div>
              </div>

              <strong title={course.courseName}>
                {shortenText(course.courseName, 16)}
              </strong>

              <span>{course.simulationsCount} simulación(es)</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function MethodologyKpiBars({ data }) {
  const methodologies = data || [];

  if (methodologies.length === 0) {
    return (
      <p className="empty-state">
        No hay datos de metodologías todavía.
      </p>
    );
  }

  return (
    <div className="methodology-kpi-list">
      {methodologies.map((methodology, index) => {
        const score = Math.round(Number(methodology.averageScore || 0));

        return (
          <div key={methodology.methodologyCode || methodology.methodologyName} className="methodology-kpi-row">
            <div className="methodology-kpi-head">
              <div>
                <strong>{methodology.methodologyName}</strong>
                <span>{methodology.simulationsCount} simulación(es)</span>
              </div>

              <b>{score}/100</b>
            </div>

            <div className="methodology-kpi-track">
              <div
                className={`methodology-kpi-fill methodology-color-${index + 1}`}
                style={{ width: `${Math.min(100, score)}%` }}
              ></div>
            </div>
          </div>
        );
      })}
    </div>
  );
}

function CompletionDonut({ data }) {
  const finished = Number(data?.find((item) => item.name === "Finalizadas")?.value || 0);
  const inProgress = Number(data?.find((item) => item.name === "En progreso")?.value || 0);
  const total = finished + inProgress;

  const percent = total > 0 ? Math.round((finished / total) * 100) : 0;

  return (
    <div className="completion-donut-layout">
      <div
        className="completion-donut"
        style={{
          background: `conic-gradient(#2563eb ${percent * 3.6}deg, #f97316 0deg)`,
        }}
      >
        <div>
          <strong>{percent}%</strong>
          <span>finalización</span>
        </div>
      </div>

      <div className="completion-legend">
        <div>
          <i className="legend-dot blue"></i>
          <span>Finalizadas</span>
          <strong>{finished}</strong>
        </div>

        <div>
          <i className="legend-dot orange"></i>
          <span>En progreso</span>
          <strong>{inProgress}</strong>
        </div>

        <div className="completion-total">
          <span>Total de intentos</span>
          <strong>{total}</strong>
        </div>
      </div>
    </div>
  );
}

export default DashboardPage;