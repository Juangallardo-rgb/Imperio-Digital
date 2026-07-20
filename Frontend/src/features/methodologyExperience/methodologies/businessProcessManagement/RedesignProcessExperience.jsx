import { useMemo, useState } from "react";
import BpmChoiceChips from "./BpmChoiceChips";
import {
  buildBpmPreviousContext,
  buildRedesignBoard,
  buildRedesignDraft,
  createProcessImprovementCard,
  getEffectiveSelectionLimit,
  getProcessImprovementTypeLabel,
  PROCESS_IMPROVEMENT_TYPES,
} from "./bpmHelpers";

function RedesignProcessExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createProcessImprovementCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const previousContext = buildBpmPreviousContext(model.decisionTrace);
  const board = buildRedesignBoard(selectedCards, classifications, previousContext);
  const draft = buildRedesignDraft(selectedCards, classifications, previousContext);

  const getImprovementType = (card) =>
    classifications[card.id]?.improvementType || card.improvementType;

  const updateImprovementType = (cardId, improvementType) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], improvementType },
    }));
    setInteractionMessage("El tipo de mejora se actualizo en el rediseño.");
  };

  const toggleImprovement = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} mejora${effectiveMax === 1 ? "" : "s"} al rediseño.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La mejora se quito del rediseño."
        : "La mejora se agrego al rediseño."
    );
  };

  const submitRedesign = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una mejora al rediseño antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="bpm-experience bpm-redesign-process" aria-labelledby="bpm-redesign-title">
      <header className="bpm-phase-intro">
        <div>
          <span className="experience-eyebrow">Rediseño operativo</span>
          <h2 id="bpm-redesign-title">Rediseña el proceso</h2>
          <p>
            Convierte el analisis previo en cambios concretos que simplifiquen
            el trabajo y hagan visible el avance de cada solicitud.
          </p>
        </div>
        <div className="bpm-phase-marker" aria-label="Fase cuatro de BPM">
          <span>Fase 4</span>
          <strong>Rediseño</strong>
        </div>
      </header>

      <section className="bpm-action-guide" aria-labelledby="bpm-redesign-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer</span>
          <h3 id="bpm-redesign-guide-title">Propone cambios que mejoren el flujo</h3>
          <p>
            Agrega mejoras que reduzcan pasos innecesarios, aclaren responsables
            o permitan seguir el estado del proceso sin depender de mensajes informales.
          </p>
        </div>
        <ol>
          <li>Recuerda las fricciones detectadas.</li>
          <li>Revisa las mejoras disponibles.</li>
          <li>Agrega las que cambian el flujo operativo.</li>
          <li>Explica por que el nuevo flujo sera mas claro.</li>
        </ol>
      </section>

      <section className="bpm-previous-flow" aria-labelledby="bpm-redesign-context-title">
        <div>
          <span className="experience-eyebrow">Punto de partida</span>
          <h3 id="bpm-redesign-context-title">Flujo y fricciones anteriores</h3>
        </div>
        {board.currentSteps.length > 0 || board.bottlenecks.length > 0 ? (
          <div className="bpm-context-columns">
            <div>
              <strong>Flujo actual</strong>
              <ul className="bpm-previous-flow-list">
                {board.currentSteps.length > 0
                  ? board.currentSteps.map((step, index) => <li key={`${step}-${index}`}>{step}</li>)
                  : <li>Sin pasos registrados</li>}
              </ul>
            </div>
            <div>
              <strong>Fricciones</strong>
              <ul className="bpm-previous-flow-list">
                {board.bottlenecks.length > 0
                  ? board.bottlenecks.map((item, index) => <li key={`${item}-${index}`}>{item}</li>)
                  : <li>Sin fricciones registradas</li>}
              </ul>
            </div>
          </div>
        ) : (
          <p>Usa las mejoras para proponer un flujo operativo mas claro.</p>
        )}
      </section>

      <section className="bpm-card-workspace" aria-labelledby="bpm-improvements-title">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Mejoras disponibles</span>
            <h3 id="bpm-improvements-title">Cambios para el nuevo proceso</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} mejora{effectiveMax === 1 ? "" : "s"} agregada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="bpm-empty-state" role="status">
            <h4>No se encontraron opciones configuradas para esta fase</h4>
            <p>Usa tu justificacion para describir los cambios que harian mas claro el proceso.</p>
          </div>
        ) : (
          <div className="bpm-card-grid">
            {cards.map((card) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const improvementType = getImprovementType(card);

              return (
                <article key={card.id} className={`bpm-process-card ${isSelected ? "is-selected" : ""}`}>
                  <div className="bpm-card-heading">
                    <span>{card.type}</span>
                    <strong>{isSelected ? "En el rediseño" : "Por revisar"}</strong>
                  </div>
                  <p>{card.text}</p>
                  <dl className="bpm-card-facts">
                    <div><dt>Tipo de cambio</dt><dd>{getProcessImprovementTypeLabel(improvementType)}</dd></div>
                    <div><dt>Relacion con el proceso</dt><dd>{card.isLessOperational ? "Por evaluar" : "Mejora operativa"}</dd></div>
                  </dl>

                  {isSelected && (
                    <BpmChoiceChips
                      label="Clasifica el cambio propuesto"
                      choices={PROCESS_IMPROVEMENT_TYPES}
                      value={improvementType}
                      onChange={(value) => updateImprovementType(card.id, value)}
                    />
                  )}

                  {isSelected && card.isLessOperational && (
                    <p className="bpm-neutral-warning">
                      Este cambio parece menos conectado con el flujo operativo que estas rediseñando.
                    </p>
                  )}

                  <button
                    type="button"
                    className="bpm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleImprovement(card)}
                  >
                    {isSelected ? "Quitar del rediseño" : "Agregar al rediseño"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="bpm-redesign-board" aria-labelledby="bpm-redesign-board-title" aria-live="polite">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Tablero visual</span>
            <h3 id="bpm-redesign-board-title">Proceso rediseñado</h3>
          </div>
          <p>{board.improvements.length} cambio{board.improvements.length === 1 ? "" : "s"} incluido{board.improvements.length === 1 ? "" : "s"} en la propuesta</p>
        </div>
        <div className="bpm-redesign-columns">
          <article className="bpm-board-column">
            <span>Hoy</span>
            <h4>Flujo actual</h4>
            <ul>
              {board.currentSteps.length > 0
                ? board.currentSteps.map((step, index) => <li key={`${step}-${index}`}>{step}</li>)
                : <li>Proceso por documentar</li>}
            </ul>
          </article>
          <article className="bpm-board-column bpm-board-column-emphasis">
            <span>Cambio</span>
            <h4>Mejoras seleccionadas</h4>
            <ul>
              {board.improvements.length > 0
                ? board.improvements.map((item) => (
                  <li key={item.id}><strong>{item.label}</strong><span>{item.text}</span></li>
                ))
                : <li>Agrega una mejora para construir la propuesta</li>}
            </ul>
          </article>
          <article className="bpm-board-column">
            <span>Propuesta</span>
            <h4>Flujo mas claro</h4>
            <ul>
              {board.improvements.length > 0
                ? board.improvements.map((item) => <li key={`future-${item.id}`}>{item.label}: {item.text}</li>)
                : <li>Los cambios seleccionados apareceran aqui</li>}
            </ul>
          </article>
        </div>
      </section>

      <section className="bpm-draft-summary" aria-labelledby="bpm-redesign-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del rediseño</span>
          <h3 id="bpm-redesign-draft-title">Base para tu justificacion</h3>
          <p>{draft}</p>
        </div>
        <button
          type="button"
          className="bpm-secondary-action"
          onClick={() => onTextAnswerChange(draft)}
          disabled={selectedCards.length === 0}
        >
          Usar rediseño como borrador
        </button>
      </section>

      <div className="experience-text-answer bpm-text-answer">
        <label htmlFor="bpm-redesign-text-answer">Justificacion del rediseño</label>
        <p id="bpm-redesign-text-answer-help">
          Explica como los cambios seleccionados reducen las fricciones y hacen mas claro el proceso para el equipo y las personas usuarias.
        </p>
        <textarea
          id="bpm-redesign-text-answer"
          aria-describedby="bpm-redesign-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: El nuevo flujo elimina revisiones duplicadas, deja responsables claros por etapa y permite que cada solicitud tenga un estado visible para el equipo y el cliente."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="bpm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitRedesign} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar rediseño y ver consecuencias"}
      </button>
    </section>
  );
}

export default RedesignProcessExperience;
