import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../api/api";
import { getApiErrorMessage } from "../api/apiErrors";
import AppIcon from "../components/AppIcon";
import logo from "../assets/imperio-logo.png";
import { getToken, saveToken } from "../utils/auth";

function LoginPage() {
  const [email, setEmail] = useState("docente@test.com");
  const [password, setPassword] = useState("123456");
  const [message, setMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (getToken()) {
      navigate("/dashboard", { replace: true });
    }
  }, [navigate]);

  const handleLogin = async (event) => {
    event.preventDefault();

    if (isLoading) return;

    setIsLoading(true);
    setMessage("");

    try {
      const response = await api.post("/Auth/login", { email, password });
      const token = response.data?.token;

      if (!token) {
        throw new Error("El backend no devolvió un token válido.");
      }

      saveToken(token);

      if (response.data?.mustChangePassword) {
        navigate("/change-temporary-password", { replace: true });
        return;
      }

      navigate("/dashboard", { replace: true });
    } catch (error) {
      setMessage(getApiErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-intro" aria-labelledby="login-intro-title">
        <div className="login-brand">
          <img src={logo} alt="Imperio Digital" />
        </div>

        <div className="login-intro-content">
          <span className="login-kicker">Simulación académica</span>
          <h1 id="login-intro-title">Aprende tomando decisiones</h1>
          <p>
            Toma decisiones estratégicas y observa su impacto en tiempo real.
            Aprende con escenarios, indicadores y retroalimentación aplicada.
          </p>

          <div className="login-feature-list" aria-label="Características de la plataforma">
            <span><AppIcon name="courses" size={18} /> Aprendizaje práctico</span>
            <span><AppIcon name="scenarios" size={18} /> Casos reales</span>
          </div>
        </div>

        <p className="login-intro-footer">Imperio Digital · Plataforma educativa</p>
      </section>

      <section className="login-access" aria-labelledby="login-title">
        <div className="login-form-card">
          <div className="login-form-heading">
            <span className="login-kicker">Acceso seguro</span>
            <h2 id="login-title">Bienvenido de nuevo</h2>
            <p>Ingresa tus credenciales para acceder al sistema.</p>
          </div>

          <form onSubmit={handleLogin}>
            <div className="login-field">
              <label htmlFor="login-email">Correo electrónico</label>
              <input
                id="login-email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                placeholder="correo@universidad.edu"
                autoComplete="email"
                required
              />
            </div>

            <div className="login-field">
              <label htmlFor="login-password">Contraseña</label>
              <input
                id="login-password"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                placeholder="Ingresa tu contraseña"
                autoComplete="current-password"
                required
              />
            </div>

            <button className="login-submit" type="submit" disabled={isLoading}>
              {isLoading ? "Verificando..." : "Iniciar sesión"}
              {!isLoading && <AppIcon name="chevron" size={18} />}
            </button>
          </form>

          <div className="login-form-links">
            <Link to="/forgot-password">¿Problemas para acceder? Contacta soporte</Link>
          </div>

          {message && <div className="login-error" role="alert">{message}</div>}
        </div>
      </section>
    </main>
  );
}

export default LoginPage;
