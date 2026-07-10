import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import { getToken, saveToken } from "../utils/auth";

function ChangeTemporaryPasswordPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (event) => {
    const { name, value } = event.target;

    setForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (isSubmitting) return;

    if (form.newPassword !== form.confirmNewPassword) {
      setMessage("La confirmación de contraseña no coincide.");
      return;
    }

    setIsSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        "/Auth/change-temporary-password",
        form,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (response.data?.token) {
        saveToken(response.data.token);
      }

      navigate("/dashboard", { replace: true });
    } catch (error) {
      setMessage(
        error.response
          ? typeof error.response.data === "string"
            ? error.response.data
            : JSON.stringify(error.response.data)
          : "No se pudo cambiar la contraseña."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">
            Seguridad de cuenta
          </span>

          <h1>Cambia tu contraseña temporal</h1>

          <p>
            Antes de continuar debes reemplazar la contraseña entregada por el
            docente por una contraseña personal.
          </p>
        </div>
      </div>

      <div className="pro-card">
        {message && (
          <div className="message pro-message">
            {message}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Contraseña temporal actual</label>
            <input
              type="password"
              name="currentPassword"
              value={form.currentPassword}
              onChange={handleChange}
              autoComplete="current-password"
              required
            />
          </div>

          <div className="form-group">
            <label>Nueva contraseña</label>
            <input
              type="password"
              name="newPassword"
              value={form.newPassword}
              onChange={handleChange}
              autoComplete="new-password"
              minLength="6"
              required
            />
          </div>

          <div className="form-group">
            <label>Confirmar nueva contraseña</label>
            <input
              type="password"
              name="confirmNewPassword"
              value={form.confirmNewPassword}
              onChange={handleChange}
              autoComplete="new-password"
              minLength="6"
              required
            />
          </div>

          <button
            className="primary-action"
            type="submit"
            disabled={isSubmitting}
          >
            {isSubmitting
              ? "Actualizando..."
              : "Cambiar contraseña"}
          </button>
        </form>
      </div>
    </div>
  );
}

export default ChangeTemporaryPasswordPage;
