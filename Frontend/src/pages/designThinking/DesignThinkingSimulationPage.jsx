import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

const phases = ["Empatizar", "Definir", "Idear", "Prototipar", "Evaluar", "Resultado"];

function DesignThinkingSimulationPage() {
  const { attemptId } = useParams();
  const navigate = useNavigate();

  const [current, setCurrent] = useState(null);
  const [selectedOptionIds, setSelectedOptionIds] = useState([]);
  const [textAnswer, setTextAnswer] = useState("");
  const [phaseFeedback, setPhaseFeedback] = useState(null);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const loadCurrent = async () => {
    setLoading(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.get(`/design-thinking/simulations/${attemptId}/current`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setCurrent(response.data);
      setSelectedOptionIds([]);
      setTextAnswer("");
      setPhaseFeedback(null);

      if (response.data.currentPhaseName === "Resultado") {
        navigate(`/design-thinking/results/${attemptId}`);
      }
    } catch (error) {
      console.error("Error cargando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setLoading(false);
    }
  };

  const groupedOptions = useMemo(() => {
    if (!current) return {};

    return current.currentPhaseOptions.reduce((acc, option) => {
      if (!acc[option.optionType]) acc[option.optionType] = [];
      acc[option.optionType].push(option);
      return acc;
    }, {});
  }, [current]);

  const toggleOption = (optionId) => {
    setSelectedOptionIds((prev) => {
      if (prev.includes(optionId)) {
        return prev.filter((id) => id !== optionId);
      }

      return [...prev, optionId];
    });
  };

  const submitPhase = async () => {
    if (!current) return;

    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post(
        `/design-thinking/simulations/${attemptId}/phase/${current.currentPhaseName}/submit`,
        {
          selectedOptionIds,
          textAnswer,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setPhaseFeedback(response.data);
    } catch (error) {
      console.error("Error enviando fase:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  const continueNext = async () => {
    if (phaseFeedback?.isLastPhase) {
      await finishSimulation();
    } else {
      await loadCurrent();
    }
  };

  const finishSimulation = async () => {
    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      await api.post(
        `/design-thinking/simulations/${attemptId}/finish`,
        {},
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      navigate(`/design-thinking/results/${attemptId}`);
    } catch (error) {
      console.error("Error finalizando simulación:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setSubmitting(false);
    }
  };

  useEffect(() => {
    loadCurrent();
  }, [attemptId]);

  if (loading) {
    return (
      <div className="page-container">
        <div className="card">
          <p>Cargando simulación...</p>
        </div>
      </div>
    );
  }

  if (!current) {
    return (
      <div className="page-container">
        <div className="card">
          <p>No se encontró la simulación.</p>
          {message && <div className="message">{message}</div>}
        </div>
      </div>
    );
  }

  const currentPhaseIndex = phases.indexOf(current.currentPhaseName);

  return (
    <div className="page-container">
      <div className="card">
        <h1>{current.scenarioTitle}</h1>
        <p><strong>Fase actual:</strong> {current.currentPhaseName}</p>

        <div className="phase-stepper">
          {phases.map((phase, index) => {
            let className = "phase-step";

            if (index < currentPhaseIndex) className += " done";
            if (index === currentPhaseIndex) className += " active";

            return (
              <span key={phase} className={className}>
                {phase}
              </span>
            );
          })}
        </div>

        {message && <div className="message">{message}</div>}
      </div>

      {!phaseFeedback ? (
        <div className="card">
          <h2>Actividad de la fase {current.currentPhaseName}</h2>

          {Object.keys(groupedOptions).map((type) => (
            <div key={type}>
              <h3>{getOptionTypeLabel(type)}</h3>

              {groupedOptions[type].map((option) => (
                <div key={option.id} className="option-card">
                  <label>
                    <input
                      type="checkbox"
                      checked={selectedOptionIds.includes(option.id)}
                      onChange={() => toggleOption(option.id)}
                    />
                    <span>{option.text}</span>
                  </label>
                </div>
              ))}
            </div>
          ))}

          <div className="form-group">
            <label>Justificación / respuesta escrita</label>
            <textarea
              value={textAnswer}
              onChange={(e) => setTextAnswer(e.target.value)}
              placeholder="Explica brevemente por qué tomaste estas decisiones..."
            />
          </div>

          <button onClick={submitPhase} disabled={submitting}>
            {submitting ? "Enviando..." : "Enviar fase"}
          </button>
        </div>
      ) : (
        <div className="card">
          <h2>Feedback de fase</h2>

          <p><strong>Fase:</strong> {phaseFeedback.phaseName}</p>
          <p><strong>Puntaje:</strong> {phaseFeedback.score}</p>
          <div className="info-box">
            <p>{phaseFeedback.feedback}</p>
          </div>

          <button onClick={continueNext} disabled={submitting}>
            {phaseFeedback.isLastPhase ? "Finalizar simulación" : "Continuar"}
          </button>
        </div>
      )}
    </div>
  );
}

function getOptionTypeLabel(type) {
  const labels = {
    Evidence: "Evidencias",
    PainPoint: "Dolores del usuario",
    ProblemStatement: "Declaración del problema",
    Solution: "Soluciones digitales",
    PrototypeFeature: "Funcionalidades del prototipo",
    UserFlowStep: "Flujo de usuario",
    KPI: "KPIs",
  };

  return labels[type] || type;
}

export default DesignThinkingSimulationPage;