import { useState } from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { getToken, getUserFromToken, logout } from "../utils/auth";
import logo from "../assets/imperio-logo.png";
import AppIcon from "./AppIcon";

const NAVIGATION_BY_ROLE = {
  Docente: [
    { to: "/dashboard", label: "Panel principal", icon: "dashboard", end: true },
    { to: "/courses", label: "Cursos", icon: "courses", end: false },
    { to: "/design-thinking/scenarios", label: "Escenarios", icon: "scenarios", end: false },
    { to: "/design-thinking/scenarios/create", label: "Crear escenario", icon: "plus", end: true },
  ],
  Estudiante: [
    { to: "/dashboard", label: "Panel principal", icon: "dashboard", end: true },
    { to: "/courses/available", label: "Cursos disponibles", icon: "courses", end: true },
    { to: "/my-courses", label: "Mis cursos", icon: "scenarios", end: false },
    { to: "/design-thinking/history", label: "Historial", icon: "history", end: true },
  ],
};

function getPageTitle(pathname) {
  if (pathname.startsWith("/courses/available")) return "Cursos disponibles";
  if (pathname.startsWith("/courses/create")) return "Crear curso";
  if (pathname.startsWith("/courses")) return "Cursos";
  if (pathname.startsWith("/my-courses")) return "Mis cursos";
  if (pathname.startsWith("/design-thinking/scenarios/create")) return "Crear escenario";
  if (pathname.startsWith("/design-thinking/scenarios")) return "Escenarios";
  if (pathname.startsWith("/design-thinking/simulate")) return "Simulación";
  if (pathname.startsWith("/design-thinking/results")) return "Resultados";
  if (pathname.startsWith("/design-thinking/history")) return "Historial";
  return "Panel principal";
}

function isNavigationActive(item, pathname, isActive) {
  if (item.to === "/design-thinking/scenarios") {
    return (
      pathname === item.to ||
      (pathname !== "/design-thinking/scenarios/create" &&
        /^\/design-thinking\/scenarios\/[^/]+$/.test(pathname))
    );
  }

  if (item.to === "/courses") {
    return pathname === "/courses" || pathname.startsWith("/courses/");
  }

  if (item.to === "/my-courses") {
    return pathname === "/my-courses" || pathname.startsWith("/my-courses/");
  }

  return isActive;
}

function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();
  const [isOpen, setIsOpen] = useState(false);
  const token = getToken();
  const user = getUserFromToken();

  if (!token) {
    return null;
  }

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  const closeMenu = () => setIsOpen(false);
  const navigation = NAVIGATION_BY_ROLE[user?.role] || [];

  return (
    <>
      <header className="app-topbar">
        <button
          className="app-mobile-menu-button"
          type="button"
          onClick={() => setIsOpen(true)}
          aria-label="Abrir navegación"
          aria-controls="app-sidebar"
          aria-expanded={isOpen}
        >
          <AppIcon name="menu" />
        </button>

        <div className="app-topbar-context">
          <span>Imperio Digital</span>
          <strong>{getPageTitle(location.pathname)}</strong>
        </div>

        <div className="app-topbar-user" aria-label={`Sesión de ${user?.name || "usuario"}`}>
          <span>{user?.name || "Usuario"}</span>
          <div className="user-avatar">{user?.name?.charAt(0)?.toUpperCase() || "U"}</div>
        </div>
      </header>

      <button
        className={`app-sidebar-backdrop ${isOpen ? "is-visible" : ""}`}
        type="button"
        aria-label="Cerrar navegación"
        onClick={closeMenu}
      />

      <aside className={`app-sidebar ${isOpen ? "is-open" : ""}`} id="app-sidebar">
        <div className="app-sidebar-header">
          <NavLink to="/dashboard" className="brand-area" onClick={closeMenu}>
            <img src={logo} alt="Imperio Digital" className="brand-logo" />
          </NavLink>

          <button className="app-sidebar-close" type="button" onClick={closeMenu} aria-label="Cerrar navegación">
            <AppIcon name="close" />
          </button>
        </div>

        <nav className="app-sidebar-nav" aria-label="Navegación principal">
          <span className="app-sidebar-label">{user?.role || "Navegación"}</span>

          {navigation.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.end}
              onClick={closeMenu}
              className={({ isActive }) => `app-sidebar-link ${
                isNavigationActive(item, location.pathname, isActive)
                  ? "is-active"
                  : ""
              }`}
            >
              <AppIcon name={item.icon} />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="app-sidebar-footer">
          <div className="app-sidebar-user">
            <div className="user-avatar">{user?.name?.charAt(0)?.toUpperCase() || "U"}</div>
            <div>
              <strong>{user?.name || "Usuario"}</strong>
              <span>{user?.role || ""}</span>
            </div>
          </div>

          <button onClick={handleLogout} className="logout-button" type="button">
            <AppIcon name="logout" />
            Cerrar sesión
          </button>
        </div>
      </aside>
    </>
  );
}

export default Navbar;
