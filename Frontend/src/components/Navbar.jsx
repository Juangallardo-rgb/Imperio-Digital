import { Link, useLocation, useNavigate } from "react-router-dom";
import { getToken, getUserFromToken, logout } from "../utils/auth";
import logo from "../assets/imperio-logo.png";

function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();

  const token = getToken();
  const user = getUserFromToken();

  if (!token || location.pathname === "/" || location.pathname === "/forgot-password" || location.pathname === "/reset-password") {
    return null;
  }

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  return (
    <header className="app-navbar">
      <div className="app-navbar-inner">
        <Link to="/dashboard" className="brand-area">
          <img src={logo} alt="Imperio Digital" className="brand-logo" />
          <div>
            <strong>Imperio Digital</strong>
            <span>Simulador educativo</span>
          </div>
        </Link>

        <nav className="navbar-links">
          <Link to="/dashboard">Dashboard</Link>

          {user?.role === "Docente" && (
            <>
              <Link to="/courses">Cursos</Link>
              <Link to="/design-thinking/scenarios">Escenarios</Link>
              <Link to="/design-thinking/scenarios/create">Crear escenario</Link>
            </>
          )}

          {user?.role === "Estudiante" && (
            <>
              <Link to="/courses/available">Cursos disponibles</Link>
              <Link to="/my-courses">Mis cursos</Link>
              <Link to="/design-thinking/history">Historial</Link>
            </>
          )}
        </nav>

        <div className="user-menu">
          <div className="user-avatar">
            {user?.name?.charAt(0)?.toUpperCase() || "U"}
          </div>

          <div className="user-info">
            <strong>{user?.name}</strong>
            <span>{user?.role}</span>
          </div>

          <button onClick={handleLogout} className="logout-button">
            Cerrar sesión
          </button>
        </div>
      </div>
    </header>
  );
}

export default Navbar;