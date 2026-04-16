import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import api from "../api/api";
import { getToken } from "../utils/auth";

function SimulationHistoryPage() {
  const [simulations, setSimulations] = useState([]);
  const [message, setMessage] = useState("");

  useEffect(() => {
    loadHistory();
  }, []);

  const loadHistory = async () => {
    try {
      const token = getToken();

      const response = await api.get("/Simulation/history", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setSimulations(response.data);
    } catch (error) {
      console.error("Error cargando historial:", error);
      setMessage("No se pudo cargar el historial");
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Historial de simulaciones</h1>
        {message && <div className="message">{message}</div>}
      </div>

      {simulations.length === 0 ? (
        <div className="card">
          <p>No hay simulaciones registradas.</p>
        </div>
      ) : (
        simulations.map((simulation) => (
          <div key={simulation.id} className="list-item">
            <h3>Simulación #{simulation.id}</h3>
            <p>Madurez digital: {simulation.digitalMaturity}</p>
            <p>Eficiencia operativa: {simulation.operationalEfficiency}</p>
            <p>Experiencia del cliente: {simulation.customerExperience}</p>
            <p>Score global: {simulation.globalScore}</p>
            <Link to={`/simulations/${simulation.id}`}>Ver detalle</Link>
          </div>
        ))
      )}
    </div>
  );
}

export default SimulationHistoryPage;