import { useMemo, useState } from "react";
import {
  buildGapDraft,
  buildGapPriorityMap,
  buildMaturityContext,
  createGapCard,
  getChoiceLabel,
  getDimensionLabelFromKey,
  getEffectiveSelectionLimit,
  RELEVANCE_LEVELS,
} from "./digitalMaturityHelpers";

function PrioritizeGapsExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(() => model.options.map(createGapCard), [model.options]);
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) => model.selection.selectedOptionIds.includes(card.id));
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const previousContext = useMemo(
    () => buildMaturityContext(model.decisionTrace, ["Diagnostico inicial", "Evaluar capacidades"]),
    [model.decisionTrace]
  );
  const priorityMap = buildGapPriorityMap(selectedCards, classifications);
  const gapDraft = buildGapDraft(selectedCards, classifications, previousContext);

  const getCardValue = (card, key) => classifications[card.id]?.[key] ?? card[key] ?? "";

  const updateCardValue = (cardId, key, value) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], [key]: value },
    }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("La priorizacion de brechas se actualizo.");
    }
  };

  const toggleGap = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} brecha${effectiveMax === 1 ? "" : "s"} a la priorizacion.`
      );
      return;
    }

    if (!isSelected) {
      setClassifications((current) => ({
        ...current,
        [card.id]: {
          ...current[card.id],
          impact: getCardValue(card, "impact") || "media",
          urgency: getCardValue(card, "urgency") || "media",
        },
      }));
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La brecha se quito de la priorizacion."
        : "La brecha se agrego con impacto y urgencia media. Puedes ajustarla."
    );
  };

  const useGapDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una brecha antes de crear un borrador.");
      return;
    }

    onTextAnswerChange(gapDraft);
    setInteractionMessage("El resumen de brechas se copio como borrador de justificacion.");
  };

  const submitPrioritization = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una brecha prioritaria antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="dm-experience dm-gap-prioritization" aria-labelledby="gap-prioritization-title">
      <header className="dm-phase-intro">
        <div>
          <span className="experience-eyebrow">Priorizacion de brechas</span>
          <h2 id="gap-prioritization-title">Prioriza brechas criticas</h2>
          <p>
            Tu objetivo es elegir las brechas que mas limitan la madurez digital de la empresa.
            Una brecha es la diferencia entre la situacion actual y la capacidad que necesita para avanzar.
          </p>
        </div>
        <div className="dm-maturity-badge" aria-label="Fase de priorizar brechas">
          <span>Fase 3</span>
          <strong>Brechas</strong>
        </div>
      </header>

      <section className="dm-action-guide" aria-labelledby="gap-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="gap-guide-title">Convierte el diagnostico en prioridades claras</h3>
          <p>
            Revisa las brechas disponibles, estima cuanto afectan a la empresa y define cuales
            necesitan una respuesta mas pronta.
          </p>
        </div>
        <ol>
          <li>Relaciona la brecha con el diagnostico y las capacidades evaluadas.</li>
          <li>Valora su impacto en la transformacion.</li>
          <li>Define su urgencia.</li>
          <li>Agrega las brechas que deben atenderse primero.</li>
          <li>Justifica el orden elegido.</li>
        </ol>
      </section>

      <section className="dm-context-bridge" aria-labelledby="gap-context-title">
        <div>
          <span className="experience-eyebrow">Contexto de fases anteriores</span>
          <h3 id="gap-context-title">Tu diagnostico y matriz orientan esta decision</h3>
          <p>Recuperamos tus decisiones previas para que la priorizacion mantenga una misma linea estrategica.</p>
        </div>
        {previousContext.texts.length > 0 ? (
          <ul>
            {previousContext.texts.slice(0, 3).map((text, index) => <li key={`${text}-${index}`}>{text}</li>)}
          </ul>
        ) : (
          <p className="dm-bridge-empty">Aun no hay decisiones previas recuperadas. Usa el contexto general del escenario.</p>
        )}
      </section>

      <section className="dm-gap-workspace" aria-labelledby="gap-cards-title">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Brechas disponibles</span>
            <h3 id="gap-cards-title">Clasifica las brechas que requieren atencion</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} brecha{effectiveMax === 1 ? "" : "s"} priorizada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="dm-empty-state" role="status">
            <h4>No se encontraron brechas para esta fase</h4>
            <p>Usa el contexto del escenario y tu justificacion para explicar las brechas mas relevantes.</p>
          </div>
        ) : (
          <div className="dm-gap-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const impact = getCardValue(card, "impact");
              const urgency = getCardValue(card, "urgency");

              return (
                <article key={card.id} className={`dm-gap-card ${isSelected ? "is-selected" : ""}`} style={{ "--card-index": index }}>
                  <div className="dm-card-heading">
                    <span>Brecha digital</span>
                    <strong>{isSelected ? "Priorizada" : "Por evaluar"}</strong>
                  </div>
                  <h4>{card.dimension ? `Brecha en ${getDimensionLabelFromKey(card.dimension)}` : "Brecha por relacionar"}</h4>
                  <p>{card.text}</p>
                  <div className="dm-card-controls dm-two-controls">
                    <label htmlFor={`gap-impact-${card.id}`}>
                      Impacto
                      <select id={`gap-impact-${card.id}`} value={impact} onChange={(event) => updateCardValue(card.id, "impact", event.target.value)}>
                        <option value="">Por evaluar</option>
                        {RELEVANCE_LEVELS.map((level) => <option key={level.key} value={level.key}>{level.label}</option>)}
                      </select>
                    </label>
                    <label htmlFor={`gap-urgency-${card.id}`}>
                      Urgencia
                      <select id={`gap-urgency-${card.id}`} value={urgency} onChange={(event) => updateCardValue(card.id, "urgency", event.target.value)}>
                        <option value="">Por evaluar</option>
                        {RELEVANCE_LEVELS.map((level) => <option key={level.key} value={level.key}>{level.label}</option>)}
                      </select>
                    </label>
                  </div>
                  <button type="button" className="dm-toggle-button" aria-pressed={isSelected} onClick={() => toggleGap(card)}>
                    {isSelected ? "Quitar de brechas" : "Agregar a brechas prioritarias"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="dm-priority-board" aria-labelledby="priority-board-title" aria-live="polite">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultado de tu analisis</span>
            <h3 id="priority-board-title">Brechas prioritarias</h3>
          </div>
          <p>Las brechas se agrupan segun el impacto que definiste.</p>
        </div>
        <div className="dm-priority-board-grid">
          {RELEVANCE_LEVELS.map((level) => (
            <article key={level.key}>
              <h4>Impacto {level.label.toLowerCase()}</h4>
              {priorityMap[level.key].length > 0 ? priorityMap[level.key].map((card) => (
                <div key={card.id}>
                  <strong>{getDimensionLabelFromKey(card.dimension, "Brecha priorizada")}</strong>
                  <span>Urgencia: {getChoiceLabel(card.urgency, RELEVANCE_LEVELS, "Media")}</span>
                  <p>{card.text}</p>
                </div>
              )) : <p className="dm-map-empty">Sin brechas agregadas todavia.</p>}
            </article>
          ))}
        </div>
      </section>

      <section className="dm-draft-summary" aria-labelledby="gap-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen de prioridades</span>
          <h3 id="gap-draft-title">Base para tu justificacion</h3>
          <p>{gapDraft}</p>
        </div>
        <button type="button" className="dm-secondary-action" onClick={useGapDraft} disabled={selectedCards.length === 0}>
          Usar prioridades como borrador
        </button>
      </section>

      <div className="experience-text-answer dm-text-answer">
        <label htmlFor="digital-gap-text-answer">Justificacion de las brechas prioritarias</label>
        <p id="digital-gap-text-answer-help" className="dm-text-answer-help">
          Explica por que las brechas seleccionadas deben atenderse primero. Relaciona impacto, urgencia y las decisiones tomadas en las fases anteriores.
        </p>
        <textarea
          id="digital-gap-text-answer"
          aria-describedby="digital-gap-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Priorizo la gestion de datos porque limita las decisiones y retrasa la mejora de los procesos que ya fueron identificados en el diagnostico."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitPrioritization} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar brechas y ver consecuencias"}
      </button>
    </section>
  );
}

export default PrioritizeGapsExperience;
