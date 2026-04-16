import { jwtDecode } from "jwt-decode";
import { Link } from "react-router-dom";

function DashboardPage() {
  const token = localStorage.getItem("token");

  if (!token) {
    return (
      <div className="page-container">
        <div className="card">
          <h2>No hay sesión iniciada</h2>
        </div>
      </div>
    );
  }

  const decoded = jwtDecode(token);

  const userName =
    decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "Usuario";

  const email =
    decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "";

  const role =
    decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "";

  return (
    <div className="page-container">
      <div className="card">
        <h1>Dashboard</h1>
        <p><strong>Nombre:</strong> {userName}</p>
        <p><strong>Correo:</strong> {email}</p>
        <p><strong>Rol:</strong> {role}</p>
      </div>

      {role === "Docente" && (
        <div className="card">
          <h2>Opciones de Docente</h2>
          <ul>
            <li><Link to="/scenarios">Ver escenarios</Link></li>
            <li><Link to="/scenarios/create">Crear escenario</Link></li>
            <li><Link to="/variables/create">Crear variable metodológica</Link></li>
          </ul>
        </div>
      )}

      {role === "Estudiante" && (
        <div className="card">
          <h2>Opciones de Estudiante</h2>
          <ul>
            <li><Link to="/scenarios">Ver escenarios y simular</Link></li>
            <li><Link to="/simulations/history">Ver historial</Link></li>
          </ul>
        </div>
      )}
    </div>
  );
}

export default DashboardPage;