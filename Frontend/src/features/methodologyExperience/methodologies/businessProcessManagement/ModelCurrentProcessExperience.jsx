import { useMemo, useState } from "react";
import {
  buildCurrentFlowDraft,
  buildCurrentProcessFlow,
  createCurrentProcessStepCard,
  getEffectiveSelectionLimit,
  getProcessAreaLabel,
  PROCESS_AREAS,
} from "./bpmHelpers";
import BpmChoiceChips from "./BpmChoiceChips";

function ModelCurrentProcessExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createCurrentProcessStepCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const flow = buildCurrentProcessFlow(selectedCards, classifications);
  const draft = buildCurrentFlowDraft(selectedCards, classifications);

  const getStage = (card) => classifications[card.id]?.stage || card.stage;

  const updateStage = (cardId, stage) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], stage },
    }));
    setInteractionMessage("La etapa se actualizo en el flujo actual.");
  };

  const toggleStep = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} paso${effectiveMax === 1 ? "" : "s"} al flujo.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "El paso se quito del flujo actual."
        : "El paso se agrego al flujo actual."
    );
  };

  const submitFlow = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos un paso al flujo actual antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="bpm-experience bpm-model-current-process" aria-labelledby="bpm-model-title">
      <header className="bpm-phase-intro">
        <div>
          <span className="experience-eyebrow">Mapa del proceso actual</span>
          <h2 id="bpm-model-title">Representa el flujo actual</h2>
          <p>
            Tu objetivo es reconstruir el flujo actual del proceso para entender
            donde se generan errores y retrasos.
          </p>
        </div>
        <div className="bpm-phase-marker" aria-label="Fase dos de BPM">
          <span>Fase 2</span>
          <strong>Flujo actual</strong>
        </div>
      </header>

      <section className="bpm-action-guide" aria-labelledby="bpm-model-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer</span>
          <h3 id="bpm-model-guide-title">Reconstruye como funciona hoy</h3>
          <p>
            Selecciona los pasos que describen como funciona el proceso actualmente.
            Evita elementos que no formen parte del flujo operativo principal.
          </p>
        </div>
        <ol>
          <li>Revisa los pasos disponibles.</li>
          <li>Agrega los que describen el flujo actual.</li>
          <li>Observa como se conectan.</li>
          <li>Explica donde aparecen las fricciones.</li>
        </ol>
      </section>

      <section className="bpm-card-workspace" aria-labelledby="bpm-flow-steps-title">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Pasos disponibles</span>
            <h3 id="bpm-flow-steps-title">Describe el proceso tal como funciona</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} paso{effectiveMax === 1 ? "" : "s"} agregado{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="bpm-empty-state" role="status">
            <h4>No se encontraron opciones configuradas para esta fase</h4>
            <p>Usa tu justificacion para describir el flujo actual del proceso.</p>
          </div>
        ) : (
          <div className="bpm-card-grid">
            {cards.map((card) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const stage = getStage(card);

              return (
                <article key={card.id} className={`bpm-process-card ${isSelected ? "is-selected" : ""}`}>
                  <div className="bpm-card-heading">
                    <span>{card.type}</span>
                    <strong>{isSelected ? "En el flujo" : "Por revisar"}</strong>
                  </div>
                  <div className="bpm-step-text">
                    {card.flowSegments.map((segment, index) => (
                      <span key={`${card.id}-${segment}-${index}`}>{segment}</span>
                    ))}
                  </div>
                  <dl className="bpm-card-facts">
                    <div><dt>Etapa sugerida</dt><dd>{getProcessAreaLabel(stage)}</dd></div>
                    <div><dt>Relacion con el flujo</dt><dd>{card.isLessOperational ? "Por evaluar" : "Proceso actual"}</dd></div>
                  </dl>

                  {isSelected && (
                    <BpmChoiceChips
                      label="Clasifica la etapa"
                      choices={PROCESS_AREAS}
                      value={stage}
                      onChange={(value) => updateStage(card.id, value)}
                    />
                  )}

                  {isSelected && card.isLessOperational && (
                    <p className="bpm-neutral-warning">
                      Este elemento parece menos conectado con el flujo operativo principal.
                    </p>
                  )}

                  <button
                    type="button"
                    className="bpm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleStep(card)}
                  >
                    {isSelected ? "Quitar del flujo" : "Agregar al flujo"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="bpm-flow-visual" aria-labelledby="bpm-flow-visual-title" aria-live="polite">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Visual del proceso</span>
            <h3 id="bpm-flow-visual-title">Flujo actual del proceso</h3>
          </div>
          <p>{flow.length} paso{flow.length === 1 ? "" : "s"} conectado{flow.length === 1 ? "" : "s"}</p>
        </div>
        {flow.length > 0 ? (
          <ol className="bpm-flow-chain">
            {flow.map((step) => (
              <li key={step.id}>
                <span>{getProcessAreaLabel(step.stage)}</span>
                <strong>{step.text}</strong>
              </li>
            ))}
          </ol>
        ) : (
          <p className="bpm-visual-empty">Aun no has agregado pasos al flujo actual.</p>
        )}
      </section>

      <section className="bpm-draft-summary" aria-labelledby="bpm-flow-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del flujo</span>
          <h3 id="bpm-flow-draft-title">Base para tu justificacion</h3>
          <p>{draft}</p>
        </div>
        <button
          type="button"
          className="bpm-secondary-action"
          onClick={() => onTextAnswerChange(draft)}
          disabled={selectedCards.length === 0}
        >
          Usar flujo como borrador
        </button>
      </section>

      <div className="experience-text-answer bpm-text-answer">
        <label htmlFor="bpm-model-text-answer">Justificacion del flujo actual</label>
        <p id="bpm-model-text-answer-help">
          Explica por que los pasos seleccionados representan el proceso actual y como ayudan a comprender el problema.
        </p>
        <textarea
          id="bpm-model-text-answer"
          aria-describedby="bpm-model-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: El flujo actual depende de canales informales, registro manual y comunicacion poco clara entre recepcion y preparacion, lo que genera errores y retrasos."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="bpm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitFlow} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar flujo y ver consecuencias"}
      </button>
    </section>
  );
}

export default ModelCurrentProcessExperience;
