import { useState } from "react";
import api from "../api/api";
import { getToken } from "../utils/auth";

function CreateScenarioPage() {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [message, setMessage] = useState("");

  const handleCreate = async (e) => {
    e.preventDefault();

    try {
      const token = getToken();

      const response = await api.post(
        "/Scenario",
        {
          name,
          description,
        },
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      setMessage(response.data);
      setName("");
      setDescription("");
    } catch (error) {
      console.error("Error creando escenario:", error);

      if (error.response) {
        setMessage(`Error ${error.response.status}: ${JSON.stringify(error.response.data)}`);
      } else {
        setMessage("No se pudo crear el escenario");
      }
    }
  };

  return (
    <div className="page-container">
      <div className="card">
        <h1>Crear escenario</h1>

        <form onSubmit={handleCreate}>
          <div className="form-group">
            <label>Nombre</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ej: Transformación digital de una pyme"
            />
          </div>

          <div className="form-group">
            <label>Descripción</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe el escenario"
            />
          </div>

          <button type="submit">Crear escenario</button>
        </form>

        {message && <div className="message">{message}</div>}
      </div>
    </div>
  );
}

export default CreateScenarioPage;