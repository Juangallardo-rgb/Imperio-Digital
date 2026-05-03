import { Link } from "react-router-dom";
import { getUserFromToken } from "../utils/auth";

function DashboardPage() {
  const user = getUserFromToken();

  if (!user) {
    return (
      <div className="pro-page">
        <div className="pro-card">
          <h2>No hay sesión iniciada</h2>
        </div>
      </div>
    );
  }

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Imperio Digital</span>
          <h1>Bienvenido, {user.name}</h1>
          <p>
            Plataforma educativa para simular procesos de transformación digital
            mediante Design Thinking, decisiones estratégicas, KPIs y retroalimentación.
          </p>
        </div>

        <div className="phase-pill">
          <span>Rol actual</span>
          <strong>{user.role}</strong>
        </div>
      </div>

      {user.role === "Docente" && (
        <>
          <div className="dashboard-stats">
            <div className="stat-card-pro">
              <span>Gestión</span>
              <strong>Cursos</strong>
            </div>
            <div className="stat-card-pro">
              <span>Simulación</span>
              <strong>Escenarios</strong>
            </div>
            <div className="stat-card-pro">
              <span>Evaluación</span>
              <strong>Resultados</strong>
            </div>
          </div>

          <div className="pro-grid">
            <Link className="action-card-pro" to="/courses">
              <span>01</span>
              <h2>Gestionar cursos</h2>
              <p>Crea cursos, revisa inscritos y asigna escenarios a tus paralelos.</p>
            </Link>

            <Link className="action-card-pro" to="/courses/create">
              <span>02</span>
              <h2>Crear curso</h2>
              <p>Configura un nuevo espacio académico para tus estudiantes.</p>
            </Link>

            <Link className="action-card-pro" to="/design-thinking/scenarios/create">
              <span>03</span>
              <h2>Crear escenario</h2>
              <p>Diseña casos de estudio basados en Design Thinking.</p>
            </Link>

            <Link className="action-card-pro" to="/design-thinking/scenarios">
              <span>04</span>
              <h2>Mis escenarios</h2>
              <p>Revisa, publica y administra tus escenarios de simulación.</p>
            </Link>
          </div>
        </>
      )}

      {user.role === "Estudiante" && (
        <>
          <div className="dashboard-stats">
            <div className="stat-card-pro">
              <span>Aprendizaje</span>
              <strong>Cursos</strong>
            </div>
            <div className="stat-card-pro">
              <span>Práctica</span>
              <strong>Simulación</strong>
            </div>
            <div className="stat-card-pro">
              <span>Seguimiento</span>
              <strong>Historial</strong>
            </div>
          </div>

          <div className="pro-grid">
            <Link className="action-card-pro" to="/my-courses">
              <span>01</span>
              <h2>Mis cursos</h2>
              <p>Entra a los cursos donde estás inscrito y revisa escenarios asignados.</p>
            </Link>

            <Link className="action-card-pro" to="/courses/available">
              <span>02</span>
              <h2>Cursos disponibles</h2>
              <p>Inscríbete en nuevos cursos activos de tus docentes.</p>
            </Link>

            <Link className="action-card-pro" to="/design-thinking/history">
              <span>03</span>
              <h2>Historial</h2>
              <p>Consulta tus simulaciones anteriores y resultados obtenidos.</p>
            </Link>

            <Link className="action-card-pro" to="/design-thinking/published">
              <span>04</span>
              <h2>Escenarios abiertos</h2>
              <p>Accede a escenarios publicados fuera de un curso específico.</p>
            </Link>
          </div>
        </>
      )}
    </div>
  );
}

export default DashboardPage;