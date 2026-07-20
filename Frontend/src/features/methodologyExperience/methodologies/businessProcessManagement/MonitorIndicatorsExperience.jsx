import { useMemo, useState } from "react";
import BpmChoiceChips from "./BpmChoiceChips";
import {
  buildBpmPreviousContext,
  buildKpiDashboard,
  buildMonitoringDraft,
  createKpiCard,
  getEffectiveSelectionLimit,
  getKpiFocusLabel,
  KPI_FOCUS_AREAS,
} from "./bpmHelpers";

function MonitorIndicatorsExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(() => model.options.map(createKpiCard), [model.options]);
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const previousContext = buildBpmPreviousContext(model.decisionTrace);
  const dashboard = buildKpiDashboard(selectedCards, classifications);
  const draft = buildMonitoringDraft(selectedCards, classifications, previousContext);

  const getFocus = (card) => classifications[card.id]?.focus || card.focus;

  const updateFocus = (cardId, focus) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], focus },
    }));
    setInteractionMessage("El enfoque del indicador se actualizo en el tablero.");
  };

  const toggleIndicator = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} indicador${effectiveMax === 1 ? "" : "es"} al tablero.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "El indicador se quito del tablero."
        : "El indicador se agrego al tablero."
    );
  };

  const submitMonitoring = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos un indicador al tablero antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="bpm-experience bpm-monitor-indicators" aria-labelledby="bpm-monitor-title">
      <header className="bpm-phase-intro">
        <div>
          <span className="experience-eyebrow">Seguimiento del proceso</span>
          <h2 id="bpm-monitor-title">Monitorea indicadores</h2>
          <p>
            Define que evidencias permitiran saber si el proceso rediseñado mejora
            de forma sostenida para el equipo y las personas usuarias.
          </p>
        </div>
        <div className="bpm-phase-marker" aria-label="Fase cinco de BPM">
          <span>Fase 5</span>
          <strong>Seguimiento</strong>
        </div>
      </header>

      <section className="bpm-action-guide" aria-labelledby="bpm-monitor-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer</span>
          <h3 id="bpm-monitor-guide-title">Elige evidencias para seguir la mejora</h3>
          <p>
            Agrega indicadores que permitan revisar la eficiencia, calidad, trazabilidad
            o satisfaccion despues de aplicar los cambios al proceso.
          </p>
        </div>
        <ol>
          <li>Revisa los cambios que propusiste.</li>
          <li>Selecciona indicadores relacionados con el proceso.</li>
          <li>Observa el tablero que se formara.</li>
          <li>Explica como usaras la informacion para aprender.</li>
        </ol>
      </section>

      <section className="bpm-previous-flow" aria-labelledby="bpm-monitor-context-title">
        <div>
          <span className="experience-eyebrow">Contexto del rediseño</span>
          <h3 id="bpm-monitor-context-title">Cambios que se deben observar</h3>
        </div>
        {previousContext.redesigns.length > 0 ? (
          <ul className="bpm-previous-flow-list">
            {previousContext.redesigns.map((item, index) => <li key={`${item}-${index}`}>{item}</li>)}
          </ul>
        ) : (
          <p>Define indicadores que permitan observar la evolucion del proceso.</p>
        )}
      </section>

      <section className="bpm-card-workspace" aria-labelledby="bpm-indicators-title">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Indicadores disponibles</span>
            <h3 id="bpm-indicators-title">Que conviene observar</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} indicador{effectiveMax === 1 ? "" : "es"} agregado{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="bpm-empty-state" role="status">
            <h4>No se encontraron opciones configuradas para esta fase</h4>
            <p>Usa tu justificacion para describir que evidencias revisarias despues del rediseño.</p>
          </div>
        ) : (
          <div className="bpm-card-grid">
            {cards.map((card) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const focus = getFocus(card);

              return (
                <article key={card.id} className={`bpm-process-card ${isSelected ? "is-selected" : ""}`}>
                  <div className="bpm-card-heading">
                    <span>{card.type}</span>
                    <strong>{isSelected ? "En el tablero" : "Por revisar"}</strong>
                  </div>
                  <p>{card.text}</p>
                  <dl className="bpm-card-facts">
                    <div><dt>Enfoque</dt><dd>{getKpiFocusLabel(focus)}</dd></div>
                    <div><dt>Uso esperado</dt><dd>{focus ? "Seguimiento continuo" : "Por definir"}</dd></div>
                  </dl>

                  {isSelected && (
                    <BpmChoiceChips
                      label="Clasifica el enfoque"
                      choices={KPI_FOCUS_AREAS}
                      value={focus}
                      onChange={(value) => updateFocus(card.id, value)}
                    />
                  )}

                  <button
                    type="button"
                    className="bpm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleIndicator(card)}
                  >
                    {isSelected ? "Quitar del tablero" : "Agregar al tablero"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="bpm-kpi-dashboard" aria-labelledby="bpm-dashboard-title" aria-live="polite">
        <div className="bpm-panel-heading">
          <div>
            <span className="experience-eyebrow">Tablero de seguimiento</span>
            <h3 id="bpm-dashboard-title">Indicadores para revisar el proceso</h3>
          </div>
          <p>{selectedCards.length} indicador{selectedCards.length === 1 ? "" : "es"} listo{selectedCards.length === 1 ? "" : "s"} para seguimiento</p>
        </div>
        <div className="bpm-kpi-grid">
          {dashboard.map((area) => (
            <article key={area.key} className={`bpm-kpi-card ${area.metrics.length > 0 ? "has-metric" : ""}`}>
              <span>{area.label}</span>
              <strong>{area.metrics.length > 0 ? "En seguimiento" : "Sin indicador"}</strong>
              {area.metrics.length > 0 ? (
                <ul>
                  {area.metrics.map((metric) => <li key={metric.id}>{metric.text}</li>)}
                </ul>
              ) : (
                <p>Agrega una evidencia para incluirla en el tablero.</p>
              )}
            </article>
          ))}
        </div>
      </section>

      <section className="bpm-draft-summary" aria-labelledby="bpm-monitor-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del seguimiento</span>
          <h3 id="bpm-monitor-draft-title">Base para tu justificacion</h3>
          <p>{draft}</p>
        </div>
        <button
          type="button"
          className="bpm-secondary-action"
          onClick={() => onTextAnswerChange(draft)}
          disabled={selectedCards.length === 0}
        >
          Usar tablero como borrador
        </button>
      </section>

      <div className="experience-text-answer bpm-text-answer">
        <label htmlFor="bpm-monitor-text-answer">Justificacion del seguimiento</label>
        <p id="bpm-monitor-text-answer-help">
          Explica que revisaras, con que frecuencia y como los indicadores ayudaran a mantener las mejoras del proceso.
        </p>
        <textarea
          id="bpm-monitor-text-answer"
          aria-describedby="bpm-monitor-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Revisaremos semanalmente el tiempo de ciclo y los reprocesos para detectar desajustes temprano y comparar los resultados con la situacion anterior al rediseño."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="bpm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitMonitoring} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar seguimiento y ver resultados"}
      </button>
    </section>
  );
}

export default MonitorIndicatorsExperience;
