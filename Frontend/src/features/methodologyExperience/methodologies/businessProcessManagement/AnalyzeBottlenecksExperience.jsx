import { useMemo, useState } from "react";
import {
  BOTTLENECK_EFFECTS,
  buildBottleneckDraft,
  buildBpmPreviousContext,
  createBottleneckCard,
  getBottleneckEffectLabel,
  getEffectiveSelectionLimit,
  getProcessAreaLabel,
  PROCESS_AREAS,
} from "./bpmHelpers";

function AnalyzeBottlenecksExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createBottleneckCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const previousContext = buildBpmPreviousContext(model.decisionTrace);
  const draft = buildBottleneckDraft(selectedCards, classifications, previousContext);

  const getLocation = (card) => classifications[card.id]?.location || card.location;
  const getEffect = (card) => classifications[card.id]?.effect || card.effect;

  const updateClassification = (cardId, key, value) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], [key]: value },
    }));
    setInteractionMessage("La clasificacion se actualizo en el analisis de cuello de botella.");
  };

  const toggleBottleneck = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes marcar hasta ${effectiveMax} cuello${effectiveMax === 1 ? "" : "s"} de botella.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La friccion se quito del analisis."
        : "La friccion se marco como cuello de botella."
    );
  };

  const submitAnalysis = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage(
        "Marca al menos una friccion como cuello de botella antes de continuar."
      );
      return;
    }

    onSubmit();
  };

  return (
    <section className="bpm-experience bpm-analyze-bottlenecks" aria-labelledby="bpm-bottleneck-title">
      <header className="bpm-phase-intro">
        <div>
          <span className="experience-eyebrow">Analisis de fricciones</span>
          <h2 id="bpm-bottleneck-title">Detecta fricciones del proceso</h2>
          <p>
            Tu objetivo es encontrar el punto exacto donde el proceso se atasca.
          </p>
        </div>
        <div className="bpm-phase-marker" aria-label="Fase tres de BPM">
          <span>Fase 3</span>
          <strong>Cuello de botella</strong>
        </div>
      </header>

      <section className="bpm-action-guide" aria-labelledby="bpm-bottleneck-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer</span>
          <h3 id="bpm-bottleneck-guide-title">Ubica donde se frena el flujo</h3>
          <p>
            Identifica las fricciones que frenan el proceso, generan acumulacion
            de trabajo, errores o perdida de trazabilidad.
          </p>
        </div>
        <ol>
          <li>Revisa el flujo actual disponible.</li>
          <li>Reconoce las fricciones posibles.</li>
          <li>Marca los puntos que bloquean el avance.</li>
          <li>Explica el efecto operativo.</li>
        </ol>
      </section>

      <section className="bpm-previous-flow" aria-labelledby="bpm-previous-flow-title">
        <div>
          <span className="experience-eyebrow">Contexto de la fase anterior</span>
          <h3 id="bpm-previous-flow-title">Resumen del flujo actual</h3>
        </div>
        {previousContext.flowSteps.length > 0 ? (
          <ol className="bpm-previous-flow-list">
            {previousContext.flowSteps.map((step, index) => <li key={`${step}-${index}`}>{step}</li>)}
          </ol>
        ) : (
          <p>El flujo se construira con las fricciones que analices en esta fase.</p>
        )}
      </section>

      <section className="bpm-card-workspace" aria-labelledby="bpm-frictions-title">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Fricciones disponibles</span>
            <h3 id="bpm-frictions-title">Que puede estar bloqueando el proceso</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} friccion{effectiveMax === 1 ? "" : "es"} marcada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="bpm-empty-state" role="status">
            <h4>No se encontraron opciones configuradas para esta fase</h4>
            <p>Usa tu justificacion para documentar donde el flujo pierde velocidad o trazabilidad.</p>
          </div>
        ) : (
          <div className="bpm-card-grid">
            {cards.map((card) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const location = getLocation(card);
              const effect = getEffect(card);

              return (
                <article key={card.id} className={`bpm-process-card bpm-bottleneck-card ${isSelected ? "is-selected" : ""}`}>
                  <div className="bpm-card-heading">
                    <span>{card.type}</span>
                    <strong>{isSelected ? "Marcada" : "Por revisar"}</strong>
                  </div>
                  <p>{card.text}</p>
                  <dl className="bpm-card-facts">
                    <div><dt>Ubicacion</dt><dd>{getProcessAreaLabel(location)}</dd></div>
                    <div><dt>Efecto</dt><dd>{getBottleneckEffectLabel(effect)}</dd></div>
                  </dl>

                  {isSelected && (!location || !effect) && (
                    <div className="bpm-manual-classification">
                      {!location && (
                        <label htmlFor={`bpm-bottleneck-location-${card.id}`}>
                          Ubicacion
                          <select
                            id={`bpm-bottleneck-location-${card.id}`}
                            value={location}
                            onChange={(event) => updateClassification(card.id, "location", event.target.value)}
                          >
                            <option value="">Por clasificar</option>
                            {PROCESS_AREAS.map((item) => (
                              <option key={item.key} value={item.key}>{item.label}</option>
                            ))}
                          </select>
                        </label>
                      )}
                      {!effect && (
                        <label htmlFor={`bpm-bottleneck-effect-${card.id}`}>
                          Efecto
                          <select
                            id={`bpm-bottleneck-effect-${card.id}`}
                            value={effect}
                            onChange={(event) => updateClassification(card.id, "effect", event.target.value)}
                          >
                            <option value="">Por evaluar</option>
                            {BOTTLENECK_EFFECTS.map((item) => (
                              <option key={item.key} value={item.key}>{item.label}</option>
                            ))}
                          </select>
                        </label>
                      )}
                    </div>
                  )}

                  <button
                    type="button"
                    className="bpm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleBottleneck(card)}
                  >
                    {isSelected ? "Quitar cuello de botella" : "Marcar como cuello de botella"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="bpm-bottleneck-visual" aria-labelledby="bpm-bottleneck-visual-title" aria-live="polite">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Visual del atasco</span>
            <h3 id="bpm-bottleneck-visual-title">Cuello de botella del proceso</h3>
          </div>
          <p>{selectedCards.length} friccion{selectedCards.length === 1 ? "" : "es"} en analisis</p>
        </div>
        <div className={`bpm-bottleneck-pipe ${selectedCards.length > 0 ? "has-bottleneck" : ""}`}>
          <div><span>Pedidos o solicitudes</span><strong>Entrada amplia</strong></div>
          <i></i>
          <div className="bpm-pipe-narrow">
            <span>Zona critica</span>
            <strong>{selectedCards.length > 0 ? "Flujo restringido" : "Por identificar"}</strong>
          </div>
          <i></i>
          <div><span>Respuesta operativa</span><strong>Salida hacia entrega</strong></div>
        </div>
        {selectedCards.length > 0 ? (
          <ul className="bpm-bottleneck-list">
            {selectedCards.map((card) => (
              <li key={card.id}>
                <strong>{getProcessAreaLabel(getLocation(card))}</strong>
                <span>{getBottleneckEffectLabel(getEffect(card))}: {card.text}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="bpm-visual-empty">Aun no has marcado fricciones como cuello de botella.</p>
        )}
      </section>

      <section className="bpm-draft-summary" aria-labelledby="bpm-bottleneck-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del analisis</span>
          <h3 id="bpm-bottleneck-draft-title">Base para tu justificacion</h3>
          <p>{draft}</p>
        </div>
        <button
          type="button"
          className="bpm-secondary-action"
          onClick={() => onTextAnswerChange(draft)}
          disabled={selectedCards.length === 0}
        >
          Usar analisis como borrador
        </button>
      </section>

      <div className="experience-text-answer bpm-text-answer">
        <label htmlFor="bpm-bottleneck-text-answer">Justificacion del cuello de botella</label>
        <p id="bpm-bottleneck-text-answer-help">
          Explica por que las fricciones seleccionadas son el punto donde el proceso pierde velocidad, genera errores o deja de ser trazable.
        </p>
        <textarea
          id="bpm-bottleneck-text-answer"
          aria-describedby="bpm-bottleneck-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: El cuello de botella esta en el registro manual porque concentra informacion de varios canales, depende de una persona y genera retrasos antes de que el equipo pueda continuar."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="bpm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitAnalysis} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar analisis y ver consecuencias"}
      </button>
    </section>
  );
}

export default AnalyzeBottlenecksExperience;
