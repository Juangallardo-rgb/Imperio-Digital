import { useMemo, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import api from "../api/api";

function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = useMemo(() => searchParams.get("token") || "", [searchParams]);

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);

  const handleResetPassword = async (e) => {
    e.preventDefault();

    if (!token) {
      setMessage("Token inválido o no encontrado.");
      return;
    }

    if (newPassword.length < 6) {
      setMessage("La contraseña debe tener al menos 6 caracteres.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setMessage("Las contraseñas no coinciden.");
      return;
    }

    setLoading(true);
    setMessage("");

    try {
      await api.post("/Auth/reset-password", {
        token,
        newPassword,
      });

      setMessage("Contraseña actualizada correctamente. Redirigiendo al login...");

      setTimeout(() => {
        navigate("/");
      }, 1500);
    } catch (error) {
      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-pro-page">
      <div className="auth-pro-card">
        <span className="eyebrow">Imperio Digital</span>
        <h1>Nueva contraseña</h1>
        <p>
          Ingresa una nueva contraseña para recuperar el acceso a tu cuenta.
        </p>

        <form onSubmit={handleResetPassword}>
          <div className="form-group">
            <label>Nueva contraseña</label>
            <input
              type="password"
              value={newPassword}
              placeholder="Mínimo 6 caracteres"
              onChange={(e) => setNewPassword(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label>Confirmar contraseña</label>
            <input
              type="password"
              value={confirmPassword}
              placeholder="Repite la contraseña"
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
            />
          </div>

          <button className="primary-action" type="submit" disabled={loading}>
            {loading ? "Actualizando..." : "Cambiar contraseña"}
          </button>
        </form>

        {message && <div className="message">{message}</div>}

        <Link className="auth-link" to="/">
          Volver al login
        </Link>
      </div>
    </div>
  );
}

export default ResetPasswordPage;