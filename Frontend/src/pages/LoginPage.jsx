import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import api from "../api/api";
import { getToken } from "../utils/auth";
import logo from "../assets/imperio-logo.png";

function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    const token = getToken();

    if (token) {
      navigate("/dashboard");
    }
  }, [navigate]);

  const handleLogin = async (e) => {
    e.preventDefault();

    if (isSubmitting) return;

    setIsSubmitting(true);
    setMessage("");

    try {
      const response = await api.post("/Auth/login", {
        email,
        password,
      });

      const token = response.data.token;
      localStorage.setItem("token", token);

      navigate("/dashboard");
    } catch (error) {
      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else if (error.request) {
        setMessage("No hubo respuesta del backend.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="login-shell">
      <section className="login-left-panel">
        <div className="login-brand">
          <img src={logo} alt="Imperio Digital" />
          <div>
            <h1>Imperio Digital</h1>
            <p>Simulador de transformación digital</p>
          </div>
        </div>

        <div className="login-copy">
          <span className="eyebrow">Aprendizaje basado en simulación</span>
          <h2>Decide, simula y aprende metodologías empresariales.</h2>
          <p>
            Resuelve escenarios aplicando Design Thinking, BPM, Madurez Digital
            y Lean Startup con KPIs, retroalimentación y decisiones estratégicas.
          </p>
        </div>

        <div className="login-features">
          <div>
            <strong>4</strong>
            <span>Metodologías</span>
          </div>
          <div>
            <strong>KPIs</strong>
            <span>Resultados medibles</span>
          </div>
          <div>
            <strong>IA</strong>
            <span>Opciones y feedback</span>
          </div>
        </div>
      </section>

      <section className="login-right-panel">
        <div className="login-card-pro">
          <div className="mobile-login-logo">
            <img src={logo} alt="Imperio Digital" />
          </div>

          <span className="eyebrow">Bienvenido</span>
          <h1>Iniciar sesión</h1>
          <p className="login-subtitle">
            Accede como docente o estudiante para continuar con tus simulaciones.
          </p>

          <form onSubmit={handleLogin}>
            <div className="form-group">
              <label>Correo electrónico</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="docente@test.com"
                required
              />
            </div>

            <div className="form-group">
              <label>Contraseña</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ingresa tu contraseña"
                required
              />
            </div>

            <button className="primary-action login-action" type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Ingresando..." : "Iniciar sesión"}
            </button>
          </form>

          <Link className="auth-link" to="/forgot-password">
            ¿Olvidaste tu contraseña?
          </Link>

          {message && <div className="message login-message">{message}</div>}
        </div>
      </section>
    </main>
  );
}

export default LoginPage;