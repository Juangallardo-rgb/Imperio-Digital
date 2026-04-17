import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { jwtDecode } from "jwt-decode";
import api from "../api/api";
import { getToken } from "../utils/auth";

function ScenariosPage() {
  const [scenarios, setScenarios] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const token = getToken();
  const decoded = token ? jwtDecode(token) : null;

  const role =
    decoded?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "";

  const loadScenarios = async () => {
    setLoading(true);
    setMessage("");

    try {
      const response = await api.get("/Scenario", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenarios(response.data);
    } catch (error) {
      console.error("Error cargando escenarios:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else if (error.request) {
        setMessage("No hubo respuesta del backend. Intenta de nuevo en unos segundos.");
      } else {
        setMessage(`Error: ${error.message}`);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadScenarios();
  }, []);

  return (
    <div className="page-container">
      <div className="card">
        <h1>Escenarios</h1>
        {message && <div className="message">{message}</div>}
        <button onClick={loadScenarios} style={{ maxWidth: "220px", marginTop: "1rem" }}>
          Recargar escenarios
        </button>
      </div>

      {loading ? (
        <div className="card">
          <p>Cargando escenarios...</p>
        </div>
      ) : scenarios.length === 0 ? (
        <div className="card">
          <p>No hay escenarios registrados.</p>
        </div>
      ) : (
        scenarios.map((scenario) => (
          <div key={scenario.id} className="list-item">
            <h3>{scenario.name}</h3>
            <p>{scenario.description}</p>

            {role === "Estudiante" && (
              <Link to={`/simulate/${scenario.id}`}>Simular este escenario</Link>
            )}
          </div>
        ))
      )}
    </div>
  );
}

export default ScenariosPage;