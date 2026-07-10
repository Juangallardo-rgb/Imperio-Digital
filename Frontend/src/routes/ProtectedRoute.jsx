import { Navigate, useLocation } from "react-router-dom";
import { getToken, getUserFromToken } from "../utils/auth";
import AppShell from "../components/AppShell";

function ProtectedRoute({ children }) {
  const location = useLocation();
  const token = getToken();
  const user = getUserFromToken();

  if (!token) {
    return <Navigate to="/" replace />;
  }

  if (
    user?.mustChangePassword &&
    location.pathname !== "/change-temporary-password"
  ) {
    return (
      <Navigate
        to="/change-temporary-password"
        replace
      />
    );
  }

  return <AppShell>{children}</AppShell>;
}

export default ProtectedRoute;
