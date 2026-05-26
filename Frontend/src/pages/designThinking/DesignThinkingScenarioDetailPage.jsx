import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function DesignThinkingScenarioDetailPage() {
  const { id } = useParams();

  const [scenario, setScenario] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);

  const loadScenario = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/design-thinking/scenarios/${id}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenario(response.data);
    } catch (error) {
      console.error("Error cargando detalle:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  const publishScenario = async () => {
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/scenarios/${id}/publish`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);
      await loadScenario();
    } catch (error) {
      console.error("Error publicando:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    }
  };

  const regenerateOptions = async () => {
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/scenarios/${id}/generate-ai-content`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);
      await loadScenario();
    } catch (error) {
      console.error("Error regenerando opciones:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    }
  };

  useEffect(() => {
    loadScenario();
  }, [id]);

  if (loading) {
    return (
      <div className="page-container">
        <div className="card">
          <p>Cargando escenario...</p>
        </div>
      </div>
    );
  }

  if (!scenario) {
    return (
      <div className="page-container">
        <div className="card">
          <p>No se encontró el escenario.</p>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  const groupedOptions = scenario.options.reduce((acc, option) => {
    const key = `${option.phaseName} - ${option.optionType}`;
    if (!acc[key]) acc[key] = [];
    acc[key].push(option);
    return acc;
  }, {});

  return (
    <div className="page-container">
      <div className="card">
        <h1>{scenario.title}</h1>
        <p>{scenario.description}</p>

        <p><strong>Tipo de empresa:</strong> {scenario.companyType}</p>
        <p><strong>Problema:</strong> {scenario.problem}</p>
        <p><strong>Usuario objetivo:</strong> {scenario.targetUser}</p>
        <p><strong>Restricciones:</strong> {scenario.constraints}</p>
        <p>
          <strong>Metodología:</strong>{" "}
          {scenario.methodologyName || scenario.methodology}
        </p>
        <p><strong>Dificultad:</strong> {scenario.difficulty}</p>

        {scenario.isPublished ? (
          <span className="badge badge-success">Publicado</span>
        ) : (
          <span className="badge badge-warning">Borrador</span>
        )}

        <div className="grid grid-2" style={{ marginTop: "1rem" }}>
          <button onClick={publishScenario}>
            Publicar escenario
          </button>

          <button onClick={regenerateOptions}>
            Regenerar opciones base
          </button>
        </div>

        {message && <div className="message">{message}</div>}
      </div>

      <div className="card">
        <h2>Fases y pesos</h2>

        {scenario.phaseSettings.map((phase) => (
          <div key={phase.id} className="list-item">
            <h3>{phase.phaseOrder}. {phase.phaseName}</h3>
            <p><strong>Peso:</strong> {phase.phaseWeight}%</p>

            <h4>Criterios</h4>
            {phase.criteria.map((criterion) => (
              <p key={criterion.id}>
                {criterion.criterionName} — {criterion.criterionWeight}% — {criterion.evaluationType}
              </p>
            ))}
          </div>
        ))}
      </div>

      <div className="card">
        <h2>Opciones de simulación metodológica</h2>

        {Object.keys(groupedOptions).map((group) => (
          <div key={group} className="list-item">
            <h3>{group}</h3>

            {groupedOptions[group].map((option) => (
              <p key={option.id}>
                {option.isCorrect ? "✅" : "❌"} {option.text}
              </p>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}

export default DesignThinkingScenarioDetailPage;