import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/api";
import { getToken } from "../../utils/auth";

function CreateCoursePage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    name: "",
    description: "",
  });

  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const handleChange = (e) => {
    setForm((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const createCourse = async (e) => {
    e.preventDefault();

    if (submitting) return;

    setSubmitting(true);
    setMessage("");

    try {
      const token = getToken();

      const response = await api.post("/courses", form, {
        headers: { Authorization: `Bearer ${token}` },
      });

      navigate(`/courses/${response.data.id}`);
    } catch (error) {
      console.error("Error creando curso:", error);
      setMessage(error.response ? `Error ${error.response.status}: ${JSON.stringify(error.response.data)}` : "No hubo respuesta del backend.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="pro-page">
      <div className="pro-hero">
        <div>
          <span className="eyebrow">Nuevo curso</span>
          <h1>Crear curso</h1>
          <p>
            Crea un espacio académico para inscribir estudiantes y asignar simulaciones.
          </p>
        </div>
      </div>

      <div className="pro-card">
        <form onSubmit={createCourse}>
          <div className="form-group">
            <label>Nombre del curso</label>
            <input
              name="name"
              value={form.name}
              onChange={handleChange}
              placeholder="Ej: Transformación Digital - Paralelo A"
              required
            />
          </div>

          <div className="form-group">
            <label>Descripción</label>
            <textarea
              name="description"
              value={form.description}
              onChange={handleChange}
              placeholder="Describe el objetivo del curso"
              required
            />
          </div>

          <button className="primary-action" type="submit" disabled={submitting}>
            {submitting ? "Creando curso..." : "Crear curso"}
          </button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default CreateCoursePage;