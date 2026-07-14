import { useEffect, useRef, useState } from "react";

function ScenarioDeletionConfirmModal({
  scenarioTitle,
  isDeleting,
  onCancel,
  onConfirm,
}) {
  const [isConfirmed, setIsConfirmed] = useState(false);
  const cancelButtonRef = useRef(null);

  useEffect(() => {
    setIsConfirmed(false);
  }, [scenarioTitle]);

  useEffect(() => {
    cancelButtonRef.current?.focus();
  }, []);

  useEffect(() => {
    const handleKeyDown = (event) => {
      if (event.key === "Escape" && !isDeleting) {
        onCancel();
      }
    };

    document.addEventListener("keydown", handleKeyDown);

    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isDeleting, onCancel]);

  if (!scenarioTitle) {
    return null;
  }

  const handleBackdropClick = (event) => {
    if (event.target === event.currentTarget && !isDeleting) {
      onCancel();
    }
  };

  return (
    <div
      className="scenario-delete-modal-backdrop"
      onMouseDown={handleBackdropClick}
    >
      <section
        className="scenario-delete-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="scenario-delete-modal-title"
        aria-describedby="scenario-delete-modal-description"
      >
        <div className="scenario-delete-modal-header">
          <span className="scenario-delete-modal-kicker">Accion irreversible</span>
          <h2 id="scenario-delete-modal-title">Eliminar escenario</h2>
        </div>

        <p id="scenario-delete-modal-description">
          Esta accion eliminara permanentemente <strong>{scenarioTitle}</strong>, sus
          opciones, asignaciones a cursos, intentos de simulacion y resultados
          asociados. No se puede deshacer.
        </p>

        <label className="scenario-delete-confirmation">
          <input
            type="checkbox"
            checked={isConfirmed}
            onChange={(event) => setIsConfirmed(event.target.checked)}
            disabled={isDeleting}
          />
          <span>Entiendo que esta eliminacion es permanente.</span>
        </label>

        <div className="scenario-delete-modal-actions">
          <button
            type="button"
            className="scenario-delete-cancel-button"
            ref={cancelButtonRef}
            onClick={onCancel}
            disabled={isDeleting}
          >
            Cancelar
          </button>

          <button
            type="button"
            className="scenario-delete-confirm-button"
            onClick={onConfirm}
            disabled={!isConfirmed || isDeleting}
          >
            {isDeleting ? "Eliminando..." : "Eliminar definitivamente"}
          </button>
        </div>
      </section>
    </div>
  );
}

export default ScenarioDeletionConfirmModal;
