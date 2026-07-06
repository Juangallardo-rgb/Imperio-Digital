import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import api from "../api/api";
import { getToken, saveToken } from "../utils/auth";
import logo from "../assets/imperio-logo.png";

function LoginPage() {
  const [email, setEmail] = useState("docente@test.com");
  const [password, setPassword] = useState("123456");
  const [message, setMessage] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    const token = getToken();

    if (token) {
      navigate("/dashboard", { replace: true });
    }
  }, [navigate]);

  const handleLogin = async (e) => {
    e.preventDefault();

    if (isLoading) return;

    setIsLoading(true);
    setMessage("");

    try {
      const response = await api.post("/Auth/login", {
        email,
        password,
      });

      const token = response.data?.token;

      if (!token) {
        throw new Error("El backend no devolvió un token válido.");
      }

      saveToken(token);

      navigate("/dashboard", { replace: true });
    } catch (error) {
      if (error.response) {
        setMessage(
          `Error ${error.response.status}: ${JSON.stringify(
            error.response.data
          )}`
        );
      } else if (error.request) {
        setMessage("No hubo respuesta del backend.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <main className="capstone-login-page">
      <header className="capstone-login-header">
        <div className="capstone-brand">
          <img src={logo} alt="Imperio Digital" />

          <div>
            <strong>Imperio Digital</strong>
            <span>Simulador de Transformación Digital</span>
          </div>
        </div>

        <div className="capstone-header-badge">
          Universidad de las Américas · Negocios Digitales
        </div>
      </header>

      <section className="capstone-login-shell">
        <div className="capstone-login-copy">
          <span className="capstone-tag">
            Simulación académica aplicada
          </span>

          <h1>
            Entrena decisiones de transformación digital en escenarios de
            negocio.
          </h1>

          <p>
            Plataforma educativa para que estudiantes de Negocios Digitales
            analicen casos, apliquen metodologías empresariales y reciban
            resultados basados en indicadores, desempeño y retroalimentación.
          </p>

          <div className="capstone-method-grid">
            <div>
              <b>01</b>
              <span>Design Thinking</span>
            </div>

            <div>
              <b>02</b>
              <span>BPM</span>
            </div>

            <div>
              <b>03</b>
              <span>Madurez Digital</span>
            </div>

            <div>
              <b>04</b>
              <span>Lean Startup</span>
            </div>
          </div>
        </div>

        <div className="capstone-access-panel">
          <div className="capstone-login-card">
            <div className="capstone-card-title">
              <span>Acceso seguro</span>
              <h2>Iniciar sesión</h2>

              <p>
                Ingresa para continuar con escenarios, cursos, simulaciones y
                resultados.
              </p>
            </div>

            <form onSubmit={handleLogin}>
              <div className="capstone-field">
                <label htmlFor="login-email">Correo</label>

                <input
                  id="login-email"
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="correo@udla.edu.ec"
                  autoComplete="email"
                  required
                />
              </div>

              <div className="capstone-field">
                <label htmlFor="login-password">Contraseña</label>

                <input
                  id="login-password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Ingresa tu contraseña"
                  autoComplete="current-password"
                  required
                />
              </div>

              <button
                className="capstone-login-action"
                type="submit"
                disabled={isLoading}
              >
                {isLoading ? "Verificando..." : "Acceder"}
              </button>
            </form>

            <div className="capstone-login-links">
              <Link to="/forgot-password">
                Recuperar contraseña
              </Link>
            </div>

            {message && (
              <div className="capstone-error">
                {message}
              </div>
            )}
          </div>
        </div>
      </section>
    </main>
  );
}

export default LoginPage;