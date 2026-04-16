import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../api/api";
import { getToken } from "../utils/auth";

function SimulationDetailPage() {
  const { id } = useParams();
  const [simulation, setSimulation] = useState(null);
  const [message, setMessage] = useState("");

  useEffect(() => {
    loadSimulation();
  }, []);

  const loadSimulation = async () => {
    try {
      const token = getToken();

      const response = await api.get(`/Simulation/${id}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setSimulation(response.data);
    } catch (error) {
      console.error("Error cargando detalle:", error);
      setMessage("No se pudo cargar el detalle de la simulación");
    }
  };

  if (message) {
    return (
      <div className="page-container">
        <div className="card">
          <div className="message">{message}</div>
        </div>
      </div>
    );
  }

  if (!simulation) {
    return (
      <div className="page-container">
        <div className="card">
          <p>Cargando...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="page-container">
      <div className="card">
        <h1>Detalle de simulación #{simulation.id}</h1>
        <p><strong>Escenario:</strong> {simulation.scenarioName}</p>
        <p><strong>Fecha:</strong> {new Date(simulation.createdAt).toLocaleString()}</p>
      </div>

      <div className="card">
        <h2>KPIs</h2>
        <div className="info-box">
          <p><strong>Madurez digital:</strong> {simulation.digitalMaturity}</p>
          <p><strong>Eficiencia operativa:</strong> {simulation.operationalEfficiency}</p>
          <p><strong>Experiencia del cliente:</strong> {simulation.customerExperience}</p>
          <p><strong>Score global:</strong> {simulation.globalScore}</p>
        </div>
      </div>

      <div className="card">
        <h2>Feedback</h2>
        <div className="info-box">
          <p><strong>Base:</strong> {simulation.feedbackRule}</p>
        </div>
        <div className="info-box">
          <p><strong>IA:</strong> {simulation.aiFeedback}</p>
        </div>
      </div>

      <div className="card">
        <h2>Variables utilizadas</h2>
        {simulation.variables.length === 0 ? (
          <p>No hay variables registradas.</p>
        ) : (
          simulation.variables.map((variable) => (
            <div key={variable.scenarioVariableId} className="list-item">
              <h3>{variable.name}</h3>
              <p>Metodología: {variable.methodology}</p>
              <p>Fase: {variable.phase}</p>
              <p>KPI: {variable.targetKpi}</p>
              <p>Valor usado: {variable.value}</p>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

export default SimulationDetailPage;