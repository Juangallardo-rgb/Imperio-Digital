import { useState } from "react";
import { Link } from "react-router-dom";
import api from "../api/api";

function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [message, setMessage] = useState("");
  const [resetUrl, setResetUrl] = useState("");
  const [loading, setLoading] = useState(false);

  const handleForgotPassword = async (e) => {
    e.preventDefault();

    setLoading(true);
    setMessage("");
    setResetUrl("");

    try {
      const response = await api.post("/Auth/forgot-password", {
        email,
      });

      setMessage(response.data.message);

      if (response.data.resetUrl) {
        setResetUrl(response.data.resetUrl);
      }
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
        <h1>Recuperar contraseña</h1>
        <p>
          Ingresa tu correo y generaremos un enlace temporal para cambiar tu contraseña.
        </p>

        <form onSubmit={handleForgotPassword}>
          <div className="form-group">
            <label>Correo</label>
            <input
              type="email"
              value={email}
              placeholder="docente@test.com"
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <button className="primary-action" type="submit" disabled={loading}>
            {loading ? "Generando enlace..." : "Generar enlace"}
          </button>
        </form>

        {message && <div className="message">{message}</div>}

        {resetUrl && (
          <div className="dev-reset-box">
            <strong>Enlace temporal de desarrollo:</strong>
            <a href={resetUrl}>{resetUrl}</a>
            <small>
              En producción este enlace se enviaría por correo.
            </small>
          </div>
        )}

        <Link className="auth-link" to="/">
          Volver al login
        </Link>
      </div>
    </div>
  );
}

export default ForgotPasswordPage;