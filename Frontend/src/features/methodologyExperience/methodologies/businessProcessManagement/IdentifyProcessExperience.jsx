import { useMemo, useState } from "react";
import {
  buildCriticalProcessDraft,
  createProcessEvidenceCard,
  getEffectiveSelectionLimit,
  getProcessAreaLabel,
  getRelationshipLabel,
  PROCESS_AREAS,
} from "./bpmHelpers";

function IdentifyProcessExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createProcessEvidenceCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(
    cards,
    model.selection.maxSelections
  );
  const draft = buildCriticalProcessDraft(selectedCards, classifications);

  const getArea = (card) => classifications[card.id]?.area || card.area;

  const updateArea = (cardId, area) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], area },
    }));
    setInteractionMessage("El area se actualizo en el diagnostico del proceso.");
  };

  const toggleEvidence = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} evidencia${effectiveMax === 1 ? "" : "s"} al diagnostico.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La evidencia se quito del diagnostico del proceso."
        : "La evidencia se agrego al diagnostico del proceso."
    );
  };

  const submitProcess = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage(
        "Agrega al menos una evidencia del proceso antes de continuar."
      );
      return;
    }

    onSubmit();
  };

  return (
    <section className="bpm-experience bpm-identify-process" aria-labelledby="bpm-identify-title">
      <header className="bpm-phase-intro">
        <div>
          <span className="experience-eyebrow">Analisis de proceso</span>
          <h2 id="bpm-identify-title">Identifica el proceso critico</h2>
          <p>
            Tu objetivo es identificar las senales que demuestran cual es el
            proceso operativo mas problematico.
          </p>
        </div>
        <div className="bpm-phase-marker" aria-label="Fase uno de BPM">
          <span>Fase 1</span>
          <strong>Diagnostico</strong>
        </div>
      </header>

      <section className="bpm-action-guide" aria-labelledby="bpm-identify-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer</span>
          <h3 id="bpm-identify-guide-title">Busca evidencias, no soluciones</h3>
          <p>
            Selecciona las evidencias que muestran que proceso operativo esta
            generando retrasos, errores o perdida de trazabilidad.
          </p>
        </div>
        <ol>
          <li>Revisa las senales disponibles.</li>
          <li>Relaciona cada una con el proceso.</li>
          <li>Agrega las evidencias mas relevantes.</li>
          <li>Explica cual proceso requiere analisis.</li>
        </ol>
      </section>

      <section className="bpm-guidance-note" aria-label="Criterio de analisis">
        <strong>Orientacion para el analisis</strong>
        <p>
          No todo problema visible es un problema de proceso. Prioriza senales
          que afecten tiempos, errores, responsables o trazabilidad.
        </p>
      </section>

      <section className="bpm-card-workspace" aria-labelledby="bpm-evidence-title">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Evidencias disponibles</span>
            <h3 id="bpm-evidence-title">Senales del proceso operativo</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} evidencia{effectiveMax === 1 ? "" : "s"} agregada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="bpm-empty-state" role="status">
            <h4>No se encontraron opciones configuradas para esta fase</h4>
            <p>Usa tu justificacion para documentar el proceso que necesita analisis.</p>
          </div>
        ) : (
          <div className="bpm-card-grid">
            {cards.map((card) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const area = getArea(card);

              return (
                <article key={card.id} className={`bpm-process-card ${isSelected ? "is-selected" : ""}`}>
                  <div className="bpm-card-heading">
                    <span>{card.type}</span>
                    <strong>{isSelected ? "En el diagnostico" : "Por revisar"}</strong>
                  </div>
                  <p>{card.text}</p>
                  <dl className="bpm-card-facts">
                    <div><dt>Relacion con el proceso</dt><dd>{getRelationshipLabel(card.relationship)}</dd></div>
                    <div><dt>Area afectada</dt><dd>{getProcessAreaLabel(area)}</dd></div>
                  </dl>

                  {isSelected && !area && (
                    <label className="bpm-card-select" htmlFor={`bpm-evidence-area-${card.id}`}>
                      Clasifica el area afectada
                      <select
                        id={`bpm-evidence-area-${card.id}`}
                        value={area}
                        onChange={(event) => updateArea(card.id, event.target.value)}
                      >
                        <option value="">Por clasificar</option>
                        {PROCESS_AREAS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                  )}

                  <button
                    type="button"
                    className="bpm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleEvidence(card)}
                  >
                    {isSelected ? "Quitar del diagnostico" : "Agregar al diagnostico"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="bpm-process-visual" aria-labelledby="bpm-critical-process-title" aria-live="polite">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Visual del proceso</span>
            <h3 id="bpm-critical-process-title">Proceso critico detectado</h3>
          </div>
          <p>{selectedCards.length} senal{selectedCards.length === 1 ? "" : "es"} agregada{selectedCards.length === 1 ? "" : "s"} al diagnostico</p>
        </div>
        <div className="bpm-process-lane" aria-hidden="true">
          <span>Entrada</span><i></i><span>Registro</span><i></i><span>Preparacion</span><i></i><span>Entrega</span><i></i><span>Seguimiento</span>
        </div>
        {selectedCards.length > 0 ? (
          <ul className="bpm-selected-list">
            {selectedCards.map((card) => (
              <li key={card.id}>
                <strong>{getProcessAreaLabel(getArea(card))}</strong>
                <span>{card.text}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="bpm-visual-empty">Aun no has agregado senales del proceso.</p>
        )}
      </section>

      <section className="bpm-draft-summary" aria-labelledby="bpm-critical-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del diagnostico</span>
          <h3 id="bpm-critical-draft-title">Base para tu justificacion</h3>
          <p>{draft}</p>
        </div>
        <button
          type="button"
          className="bpm-secondary-action"
          onClick={() => onTextAnswerChange(draft)}
          disabled={selectedCards.length === 0}
        >
          Usar resumen como borrador
        </button>
      </section>

      <div className="experience-text-answer bpm-text-answer">
        <label htmlFor="bpm-identify-text-answer">Justificacion del proceso critico</label>
        <p id="bpm-identify-text-answer-help">
          Explica por que las evidencias seleccionadas muestran el proceso operativo que debe analizarse.
        </p>
        <textarea
          id="bpm-identify-text-answer"
          aria-describedby="bpm-identify-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: El proceso critico es la atencion de pedidos porque existen demoras, comunicacion informal y falta de trazabilidad entre recepcion, preparacion y entrega."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="bpm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitProcess} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar proceso y ver consecuencias"}
      </button>
    </section>
  );
}

export default IdentifyProcessExperience;
