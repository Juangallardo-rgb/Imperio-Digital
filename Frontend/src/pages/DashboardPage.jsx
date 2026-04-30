import { Link } from "react-router-dom";
import { getUserFromToken } from "../utils/auth";

function DashboardPage() {
  const user = getUserFromToken();

  if (!user) {
    return (
      <div className="page-container">
        <div className="card">
          <h2>No hay sesión iniciada</h2>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="card">
        <h1>Dashboard</h1>
        <p><strong>Nombre:</strong> {user.name}</p>
        <p><strong>Correo:</strong> {user.email}</p>
        <p><strong>Rol:</strong> {user.role}</p>
      </div>

      {user.role === "Docente" && (
        <div className="card">
          <h2>Panel docente</h2>
          <p>
            Desde aquí puedes crear escenarios educativos basados en Design Thinking,
            configurar fases, revisar opciones y publicar simulaciones para los estudiantes.
          </p>

          <div className="grid grid-2">
            <Link className="button-link" to="/design-thinking/scenarios/create">
              Crear escenario Design Thinking
            </Link>

            <Link className="button-link" to="/design-thinking/scenarios">
              Ver mis escenarios
            </Link>
          </div>
        </div>
      )}

      {user.role === "Estudiante" && (
        <div className="card">
          <h2>Panel estudiante</h2>
          <p>
            Selecciona un escenario publicado, recorre las fases de Design Thinking y
            recibe resultados con puntaje, KPIs y retroalimentación.
          </p>

          <div className="grid grid-2">
            <Link className="button-link" to="/design-thinking/published">
              Ver escenarios publicados
            </Link>

            <Link className="button-link" to="/design-thinking/history">
              Ver historial
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}

export default DashboardPage;