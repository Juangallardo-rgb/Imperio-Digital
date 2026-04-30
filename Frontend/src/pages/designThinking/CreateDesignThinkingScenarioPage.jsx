import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function CreateDesignThinkingScenarioPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    title: "",
    description: "",
    companyType: "",
    problem: "",
    targetUser: "",
    constraints: "",
    difficulty: "Media",
  });

  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (e) => {
    setForm((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (isSubmitting) return;

    setIsSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post("/design-thinking/scenarios", form, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      setMessage("Escenario creado correctamente");

      setTimeout(() => {
        navigate(`/design-thinking/scenarios/${response.data.id}`);
      }, 700);
    } catch (error) {
      console.error("Error creando escenario:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No hubo respuesta del backend.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Crear escenario Design Thinking</h1>
        <p>
          Define el caso de estudio que el estudiante resolverá recorriendo las fases:
          Empatizar, Definir, Idear, Prototipar y Evaluar.
        </p>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Título</label>
            <input
              name="title"
              value={form.title}
              onChange={handleChange}
              placeholder="Ej: Abandono de carrito en tienda online"
              required
            />
          </div>

          <div className="form-group">
            <label>Descripción</label>
            <textarea
              name="description"
              value={form.description}
              onChange={handleChange}
              placeholder="Describe el contexto general del caso"
              required
            />
          </div>

          <div className="form-group">
            <label>Tipo de empresa</label>
            <input
              name="companyType"
              value={form.companyType}
              onChange={handleChange}
              placeholder="Ej: E-commerce, clínica, universidad, restaurante"
              required
            />
          </div>

          <div className="form-group">
            <label>Problema principal</label>
            <textarea
              name="problem"
              value={form.problem}
              onChange={handleChange}
              placeholder="Ej: Alto abandono durante el proceso de checkout"
              required
            />
          </div>

          <div className="form-group">
            <label>Usuario objetivo</label>
            <input
              name="targetUser"
              value={form.targetUser}
              onChange={handleChange}
              placeholder="Ej: Clientes digitales que compran productos en línea"
              required
            />
          </div>

          <div className="form-group">
            <label>Restricciones</label>
            <textarea
              name="constraints"
              value={form.constraints}
              onChange={handleChange}
              placeholder="Ej: Presupuesto limitado, equipo pequeño, plazo de 4 semanas"
            />
          </div>

          <div className="form-group">
            <label>Dificultad</label>
            <select name="difficulty" value={form.difficulty} onChange={handleChange}>
              <option value="Baja">Baja</option>
              <option value="Media">Media</option>
              <option value="Alta">Alta</option>
            </select>
          </div>

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creando..." : "Crear escenario"}
          </button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default CreateDesignThinkingScenarioPage;