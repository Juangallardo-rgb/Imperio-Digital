import { useEffect, useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";
import ScenarioDeletionConfirmModal from "../../components/ScenarioDeletionConfirmModal";

function MyDesignThinkingScenariosPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [scenarios, setScenarios] = useState([]);
  const [message, setMessage] = useState(() => location.state?.message || "");
  const [loading, setLoading] = useState(true);
  const [scenarioToDelete, setScenarioToDelete] = useState(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [methodologyFilter, setMethodologyFilter] = useState("Todas");
  const [statusFilter, setStatusFilter] = useState("Todos");

  const loadScenarios = async ({ resetMessage = true } = {}) => {
    setLoading(true);
    if (resetMessage) {
      setMessage("");
    }

    try {
      const token = getToken();

      const response = await api.get("/design-thinking/scenarios/my", {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setScenarios(response.data || []);
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
    const hasSuccessMessage = Boolean(location.state?.message);
    loadScenarios({ resetMessage: !hasSuccessMessage });

    if (hasSuccessMessage) {
      navigate(location.pathname, { replace: true, state: null });
    }
  }, []);

  const deleteScenario = async () => {
    if (!scenarioToDelete || isDeleting) return;

    setIsDeleting(true);
    setMessage("");

    try {
      const token = getToken();
      const response = await api.delete(
        `/design-thinking/scenarios/${scenarioToDelete.id}`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setScenarios((currentScenarios) =>
        currentScenarios.filter((scenario) => scenario.id !== scenarioToDelete.id)
      );
      setScenarioToDelete(null);
      setMessage(response.data || "Escenario eliminado correctamente.");
    } catch (error) {
      console.error("Error eliminando escenario:", error);
      setMessage(getScenarioDeletionErrorMessage(error));
    } finally {
      setIsDeleting(false);
    }
  };

  const stats = useMemo(() => {
    const total = scenarios.length;
    const published = scenarios.filter((scenario) => scenario.isPublished).length;
    const drafts = total - published;

    const methodologies = new Set(
      scenarios.map((scenario) =>
        getMethodologyName(scenario.methodologyName || scenario.methodology)
      )
    );

    return {
      total,
      published,
      drafts,
      methodologies: methodologies.size,
    };
  }, [scenarios]);

  const methodologyOptions = useMemo(() => {
    const names = scenarios.map((scenario) =>
      getMethodologyName(scenario.methodologyName || scenario.methodology)
    );

    return ["Todas", ...Array.from(new Set(names))];
  }, [scenarios]);

  const filteredScenarios = useMemo(() => {
    return scenarios.filter((scenario) => {
      const methodologyName = getMethodologyName(
        scenario.methodologyName || scenario.methodology
      );

      const matchesMethodology =
        methodologyFilter === "Todas" || methodologyName === methodologyFilter;

      const matchesStatus =
        statusFilter === "Todos" ||
        (statusFilter === "Publicados" && scenario.isPublished) ||
        (statusFilter === "Borradores" && !scenario.isPublished);

      return matchesMethodology && matchesStatus;
    });
  }, [scenarios, methodologyFilter, statusFilter]);

  if (loading) {
    return (
      <div className="scenarios-pro-page">
        <div className="scenarios-hero skeleton-hero">
          <span className="eyebrow">Escenarios</span>
          <h1>Cargando escenarios...</h1>
          <p>Preparando tus casos metodológicos.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="scenarios-pro-page">
      {message && <div className="message pro-message">{message}</div>}

      <section className="scenarios-hero">
        <div className="scenarios-hero-content">
          <span className="eyebrow">Panel docente</span>
          <h1>Mis escenarios metodológicos</h1>
          <p>
            Administra tus casos de simulación, revisa su metodología, controla
            su publicación y prepara experiencias para tus cursos.
          </p>

          <div className="hero-actions">
            <Link className="hero-button primary" to="/design-thinking/scenarios/create">
              Crear nuevo escenario
            </Link>

            <Link className="hero-button secondary" to="/courses">
              Gestionar cursos
            </Link>
          </div>
        </div>

        <div className="scenario-hero-panel">
          <span>Escenarios creados</span>
          <strong>{stats.total}</strong>
          <p>{stats.published} publicados · {stats.drafts} borradores</p>
        </div>
      </section>

      <section className="scenario-stats-grid">
        <ScenarioStatCard
          label="Total escenarios"
          value={stats.total}
          detail="Casos creados"
          variant="blue"
        />

        <ScenarioStatCard
          label="Publicados"
          value={stats.published}
          detail="Disponibles para estudiantes"
          variant="green"
        />

        <ScenarioStatCard
          label="Borradores"
          value={stats.drafts}
          detail="Pendientes de publicar"
          variant="orange"
        />

        <ScenarioStatCard
          label="Metodologías"
          value={stats.methodologies}
          detail="Usadas en escenarios"
          variant="purple"
        />
      </section>

      <section className="scenario-toolbar">
        <div>
          <h2>Biblioteca de escenarios</h2>
          <p>
            Visualiza, filtra y abre el detalle de tus simulaciones metodológicas.
          </p>
        </div>

        <div className="scenario-filters">
          <select
            value={methodologyFilter}
            onChange={(e) => setMethodologyFilter(e.target.value)}
          >
            {methodologyOptions.map((methodology) => (
              <option key={methodology} value={methodology}>
                {methodology}
              </option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="Todos">Todos</option>
            <option value="Publicados">Publicados</option>
            <option value="Borradores">Borradores</option>
          </select>
        </div>
      </section>

      {filteredScenarios.length === 0 ? (
        <section className="empty-scenarios-panel">
          <div>
            <span>📚</span>
            <h2>No hay escenarios para mostrar</h2>
            <p>
              Crea un nuevo escenario o cambia los filtros seleccionados.
            </p>

            <Link className="hero-button primary" to="/design-thinking/scenarios/create">
              Crear escenario
            </Link>
          </div>
        </section>
      ) : (
        <section className="scenario-card-grid">
          {filteredScenarios.map((scenario) => {
            const methodologyName = getMethodologyName(
              scenario.methodologyName || scenario.methodology
            );

            return (
              <article key={scenario.id} className="scenario-pro-card">
                <div className="scenario-card-top">
                  <span className={scenario.isPublished ? "status-pill green" : "status-pill gray"}>
                    {scenario.isPublished ? "Publicado" : "Borrador"}
                  </span>

                  <span className={`difficulty-pill ${getDifficultyClass(scenario.difficulty)}`}>
                    {scenario.difficulty || "Media"}
                  </span>
                </div>

                <div className="methodology-chip">
                  {methodologyName}
                </div>

                <h2>{scenario.title || scenario.name}</h2>

                <p className="scenario-description">
                  {scenario.description || "Sin descripción registrada."}
                </p>

                <div className="scenario-meta-grid">
                  <div>
                    <span>Empresa</span>
                    <strong>{scenario.companyType || "No definida"}</strong>
                  </div>

                  <div>
                    <span>Usuario objetivo</span>
                    <strong>{scenario.targetUser || "No definido"}</strong>
                  </div>
                </div>

                <div className="scenario-problem-box">
                  <span>Problema principal</span>
                  <p>{scenario.problem || "Sin problema registrado."}</p>
                </div>

                <div className="scenario-card-footer">
                  <div>
                    <span>Creado</span>
                    <strong>{formatDate(scenario.createdAt)}</strong>
                  </div>

                  <div className="scenario-card-actions">
                    <Link
                      className="scenario-detail-button"
                      to={`/design-thinking/scenarios/${scenario.id}`}
                    >
                      Ver detalle
                    </Link>

                    <button
                      type="button"
                      className="scenario-delete-button"
                      onClick={() => setScenarioToDelete(scenario)}
                    >
                      Eliminar
                    </button>
                  </div>
                </div>
              </article>
            );
          })}
        </section>
      )}

      {scenarioToDelete && (
        <ScenarioDeletionConfirmModal
          scenarioTitle={scenarioToDelete.title || scenarioToDelete.name || "Escenario sin nombre"}
          isDeleting={isDeleting}
          onCancel={() => setScenarioToDelete(null)}
          onConfirm={deleteScenario}
        />
      )}
    </div>
  );
}

function ScenarioStatCard({ label, value, detail, variant }) {
  return (
    <div className={`scenario-stat-card ${variant}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      <p>{detail}</p>
    </div>
  );
}

function getMethodologyName(value) {
  const names = {
    DesignThinking: "Design Thinking",
    "Design Thinking": "Design Thinking",
    BPM: "Business Process Management",
    "Business Process Management": "Business Process Management",
    DigitalMaturity: "Madurez Digital",
    "Madurez Digital": "Madurez Digital",
    LeanStartup: "Lean Startup",
    "Lean Startup": "Lean Startup",
  };

  return names[value] || value || "No definida";
}

function getDifficultyClass(difficulty) {
  const value = String(difficulty || "").toLowerCase();

  if (value.includes("alta")) return "high";
  if (value.includes("baja")) return "low";

  return "medium";
}

function formatDate(date) {
  if (!date) return "Sin fecha";

  try {
    return new Date(date).toLocaleDateString();
  } catch {
    return "Sin fecha";
  }
}

function getScenarioDeletionErrorMessage(error) {
  const status = error.response?.status;

  if (status === 404) return "Escenario no encontrado.";
  if (status === 403) return "No tienes permiso para eliminar este escenario.";
  if (status === 500) return "No se pudo eliminar el escenario. Intenta nuevamente.";

  return typeof error.response?.data === "string"
    ? error.response.data
    : "No hubo respuesta del backend.";
}

export default MyDesignThinkingScenariosPage;
