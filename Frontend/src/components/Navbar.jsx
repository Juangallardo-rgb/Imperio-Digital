import { Link, useLocation, useNavigate } from "react-router-dom";
import { getToken, getUserFromToken, logout } from "../utils/auth";

function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();

  const token = getToken();
  const user = getUserFromToken();

  if (!token || location.pathname === "/") return null;

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  return (
    <div className="navbar">
      <div className="navbar-content">
        <div>
          <strong>Imperio Digital</strong>
        </div>

        <div className="navbar-links">
          <Link to="/dashboard">Dashboard</Link>

          {user?.role === "Docente" && (
            <>
              <Link to="/design-thinking/scenarios">Mis escenarios</Link>
              <Link to="/design-thinking/scenarios/create">Crear escenario DT</Link>
            </>
          )}

          {user?.role === "Estudiante" && (
            <>
              <Link to="/design-thinking/published">Escenarios publicados</Link>
              <Link to="/design-thinking/history">Historial</Link>
            </>
          )}

          <span>{user?.name}</span>

          <button
            onClick={handleLogout}
            style={{ width: "auto", padding: "0.5rem 1rem" }}
          >
            Cerrar sesión
          </button>
        </div>
      </div>
    </div>
  );
}

export default Navbar;