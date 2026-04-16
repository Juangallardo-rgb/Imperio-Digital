import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../api/api";
import { getToken } from "../utils/auth";

function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    const token = getToken();

    if (token) {
      navigate("/dashboard");
    }
  }, [navigate]);

  const handleLogin = async (e) => {
    e.preventDefault();

    try {
      const response = await api.post("/Auth/login", {
        email,
        password,
      });

      const token = response.data.token;
      localStorage.setItem("token", token);
      setMessage("Login exitoso");
      navigate("/dashboard");
    } catch (error) {
      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else if (error.request) {
        setMessage("No hubo respuesta del backend.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    }
  };

  return (
    <div className="page-container">
      <div className="card" style={{ maxWidth: "450px", margin: "4rem auto" }}>
        <h1>Iniciar sesión</h1>

        <form onSubmit={handleLogin}>
          <div className="form-group">
            <label>Correo</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="docente@test.com o juan@test.com"
            />
          </div>

          <div className="form-group">
            <label>Contraseña</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="123456"
            />
          </div>

          <button type="submit">Iniciar sesión</button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default LoginPage;