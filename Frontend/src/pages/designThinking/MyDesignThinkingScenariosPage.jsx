import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function MyDesignThinkingScenariosPage() {
  const [scenarios, setScenarios] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadScenarios = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get("/design-thinking/scenarios/my", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenarios(response.data);
    } catch (error) {
      console.error("Error cargando escenarios:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
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
        <h1>Mis escenarios metodológicos</h1>
        <p>Gestiona escenarios de Design Thinking, BPM, Madurez Digital y Lean Startup.</p>

        <Link className="button-link" to="/design-thinking/scenarios/create">
          Crear nuevo escenario
        </Link>

        {message && <div className="message">{message}</div>}
      </div>

      {loading ? (
        <div className="card">
          <p>Cargando escenarios...</p>
        </div>
      ) : scenarios.length === 0 ? (
        <div className="card">
          <p>No tienes escenarios creados todavía.</p>
        </div>
      ) : (
        scenarios.map((scenario) => (
          <div key={scenario.id} className="list-item">
            <h3>{scenario.title}</h3>
            <p>{scenario.description}</p>
            <p><strong>Empresa:</strong> {scenario.companyType}</p>
            <p>
            <strong>Metodología:</strong>{" "}
            {scenario.methodologyName || scenario.methodology || "No definida"}
            </p>
            <p><strong>Dificultad:</strong> {scenario.difficulty}</p>

            {scenario.isPublished ? (
              <span className="badge badge-success">Publicado</span>
            ) : (
              <span className="badge badge-warning">Borrador</span>
            )}

            <div style={{ marginTop: "1rem" }}>
              <Link to={`/design-thinking/scenarios/${scenario.id}`}>
                Ver detalle
              </Link>
            </div>
          </div>
        ))
      )}
    </div>
  );
}

export default MyDesignThinkingScenariosPage;