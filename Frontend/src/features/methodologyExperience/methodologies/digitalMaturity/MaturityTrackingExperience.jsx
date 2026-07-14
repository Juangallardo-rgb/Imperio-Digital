import { useMemo, useState } from "react";
import {
  buildMaturityContext,
  buildTrackingDraft,
  buildTrackingPanel,
  createTrackingCard,
  getChoiceLabel,
  getEffectiveSelectionLimit,
  TRACKING_AREAS,
} from "./digitalMaturityHelpers";

function MaturityTrackingExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(() => model.options.map(createTrackingCard), [model.options]);
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) => model.selection.selectedOptionIds.includes(card.id));
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const planContext = useMemo(
    () => buildMaturityContext(model.decisionTrace, ["Plan de transformacion"]),
    [model.decisionTrace]
  );
  const trackingPanel = buildTrackingPanel(selectedCards, classifications);
  const trackingDraft = buildTrackingDraft(selectedCards, classifications, planContext);

  const getCardValue = (card) => classifications[card.id]?.area ?? card.area ?? "";

  const updateArea = (cardId, area) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], area },
    }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("El panel de seguimiento se actualizo.");
    }
  };

  const toggleIndicator = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} indicador${effectiveMax === 1 ? "" : "es"} al seguimiento.`
      );
      return;
    }

    if (!isSelected) {
      setClassifications((current) => ({
        ...current,
        [card.id]: { ...current[card.id], area: getCardValue(card) || "maturity" },
      }));
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "El indicador se quito del seguimiento."
        : "El indicador se agrego al area de madurez digital. Puedes reclasificarlo."
    );
  };

  const useTrackingDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos un indicador antes de crear un borrador.");
      return;
    }

    onTextAnswerChange(trackingDraft);
    setInteractionMessage("El resumen de seguimiento se copio como borrador de justificacion.");
  };

  const submitTracking = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos un indicador al panel antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="dm-experience dm-maturity-tracking" aria-labelledby="maturity-tracking-title">
      <header className="dm-phase-intro">
        <div>
          <span className="experience-eyebrow">Seguimiento de madurez</span>
          <h2 id="maturity-tracking-title">Mide el avance de madurez</h2>
          <p>
            Tu objetivo es elegir indicadores que permitan comprobar si la madurez digital esta mejorando.
            Selecciona indicadores claros y agrupados por el area que ayudan a observar.
          </p>
        </div>
        <div className="dm-maturity-badge" aria-label="Fase de seguimiento de madurez">
          <span>Fase 5</span>
          <strong>Seguimiento</strong>
        </div>
      </header>

      <section className="dm-action-guide" aria-labelledby="tracking-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="tracking-guide-title">Cierra el ciclo con indicadores utiles</h3>
          <p>
            Escoge los indicadores que ayudaran a comprobar el progreso del plan y clasificalos
            por el area de madurez que permiten observar.
          </p>
        </div>
        <ol>
          <li>Revisa las iniciativas de tu roadmap.</li>
          <li>Elige indicadores que muestren cambios concretos.</li>
          <li>Clasifica cada indicador por area.</li>
          <li>Verifica que el panel cubra tus prioridades.</li>
          <li>Explica como usaras el seguimiento para mejorar.</li>
        </ol>
      </section>

      <section className="dm-context-bridge" aria-labelledby="tracking-context-title">
        <div>
          <span className="experience-eyebrow">Roadmap de transformacion</span>
          <h3 id="tracking-context-title">Los indicadores deben ayudar a revisar el plan</h3>
          <p>Conecta el seguimiento con las iniciativas seleccionadas para saber si la transformacion avanza como esperabas.</p>
        </div>
        {planContext.texts.length > 0 ? (
          <ul>
            {planContext.texts.slice(0, 3).map((text, index) => <li key={`${text}-${index}`}>{text}</li>)}
          </ul>
        ) : (
          <p className="dm-bridge-empty">Aun no hay iniciativas recuperadas. Usa el contexto del escenario para proponer indicadores relevantes.</p>
        )}
      </section>

      <section className="dm-tracking-workspace" aria-labelledby="tracking-cards-title">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Indicadores disponibles</span>
            <h3 id="tracking-cards-title">Elige que observar durante la transformacion</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} indicador{effectiveMax === 1 ? "" : "es"} agregado{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="dm-empty-state" role="status">
            <h4>No se encontraron indicadores para esta fase</h4>
            <p>Usa el contexto del escenario y tu justificacion para definir como mediras el avance de la madurez digital.</p>
          </div>
        ) : (
          <div className="dm-tracking-card-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const area = getCardValue(card);

              return (
                <article key={card.id} className={`dm-tracking-card ${isSelected ? "is-selected" : ""}`} style={{ "--card-index": index }}>
                  <div className="dm-card-heading">
                    <span>Indicador</span>
                    <strong>{isSelected ? "En seguimiento" : "Por clasificar"}</strong>
                  </div>
                  <h4>{getChoiceLabel(area, TRACKING_AREAS, "Indicador por clasificar")}</h4>
                  <p><strong>Que mide:</strong> {card.text}</p>
                  <div className="dm-card-controls dm-one-control">
                    <label htmlFor={`indicator-area-${card.id}`}>
                      Area de seguimiento
                      <select id={`indicator-area-${card.id}`} value={area} onChange={(event) => updateArea(card.id, event.target.value)}>
                        <option value="">Por clasificar</option>
                        {TRACKING_AREAS.map((item) => <option key={item.key} value={item.key}>{item.label}</option>)}
                      </select>
                    </label>
                  </div>
                  <button type="button" className="dm-toggle-button" aria-pressed={isSelected} onClick={() => toggleIndicator(card)}>
                    {isSelected ? "Quitar del panel" : "Agregar al panel"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="dm-tracking-board" aria-labelledby="tracking-board-title" aria-live="polite">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Panel de seguimiento</span>
            <h3 id="tracking-board-title">Indicadores por area de madurez</h3>
          </div>
          <p>El panel se actualiza al agregar, quitar o reclasificar indicadores.</p>
        </div>
        <div className="dm-tracking-board-grid">
          {TRACKING_AREAS.map((area) => (
            <article key={area.key}>
              <h4>{area.label}</h4>
              <strong className="dm-indicator-count">{trackingPanel[area.key].length}</strong>
              <span>indicador{trackingPanel[area.key].length === 1 ? "" : "es"}</span>
              {trackingPanel[area.key].length > 0 ? (
                <ul>
                  {trackingPanel[area.key].map((card) => <li key={card.id}>{card.text}</li>)}
                </ul>
              ) : <p className="dm-map-empty">Sin indicadores agregados todavia.</p>}
            </article>
          ))}
        </div>
      </section>

      <section className="dm-draft-summary" aria-labelledby="tracking-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen de seguimiento</span>
          <h3 id="tracking-draft-title">Base para tu justificacion</h3>
          <p>{trackingDraft}</p>
        </div>
        <button type="button" className="dm-secondary-action" onClick={useTrackingDraft} disabled={selectedCards.length === 0}>
          Usar seguimiento como borrador
        </button>
      </section>

      <div className="experience-text-answer dm-text-answer">
        <label htmlFor="digital-tracking-text-answer">Justificacion del seguimiento de madurez</label>
        <p id="digital-tracking-text-answer-help" className="dm-text-answer-help">
          Explica que mostraran los indicadores elegidos, con que frecuencia los revisarias y como ayudaran a ajustar el plan de transformacion.
        </p>
        <textarea
          id="digital-tracking-text-answer"
          aria-describedby="digital-tracking-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Revisaria mensualmente la adopcion de las nuevas herramientas y la calidad de los datos para ajustar las iniciativas antes de que se acumulen problemas."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitTracking} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar seguimiento y ver consecuencias"}
      </button>
    </section>
  );
}

export default MaturityTrackingExperience;
