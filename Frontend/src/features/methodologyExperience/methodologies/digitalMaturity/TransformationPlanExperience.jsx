import { useMemo, useState } from "react";
import {
  buildMaturityContext,
  buildRoadmap,
  buildTransformationDraft,
  createInitiativeCard,
  getChoiceLabel,
  getDimensionLabelFromKey,
  getEffectiveSelectionLimit,
  RELEVANCE_LEVELS,
  ROADMAP_PERIODS,
} from "./digitalMaturityHelpers";

function TransformationPlanExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(() => model.options.map(createInitiativeCard), [model.options]);
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) => model.selection.selectedOptionIds.includes(card.id));
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const gapContext = useMemo(
    () => buildMaturityContext(model.decisionTrace, ["Priorizar brechas"]),
    [model.decisionTrace]
  );
  const roadmap = buildRoadmap(selectedCards, classifications);
  const transformationDraft = buildTransformationDraft(selectedCards, classifications, gapContext);

  const getCardValue = (card, key) => classifications[card.id]?.[key] ?? card[key] ?? "";

  const updatePeriod = (cardId, period) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], period },
    }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("El roadmap de transformacion se actualizo.");
    }
  };

  const toggleInitiative = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} iniciativa${effectiveMax === 1 ? "" : "s"} al roadmap.`
      );
      return;
    }

    if (!isSelected) {
      setClassifications((current) => ({
        ...current,
        [card.id]: { ...current[card.id], period: getCardValue(card, "period") || "medium" },
      }));
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La iniciativa se quito del roadmap."
        : "La iniciativa se agrego al mediano plazo. Puedes ajustar su horizonte."
    );
  };

  const useTransformationDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una iniciativa antes de crear un borrador.");
      return;
    }

    onTextAnswerChange(transformationDraft);
    setInteractionMessage("El resumen del roadmap se copio como borrador de justificacion.");
  };

  const submitPlan = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una iniciativa al plan antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="dm-experience dm-transformation-plan" aria-labelledby="transformation-plan-title">
      <header className="dm-phase-intro">
        <div>
          <span className="experience-eyebrow">Plan de transformacion</span>
          <h2 id="transformation-plan-title">Disena un plan de transformacion</h2>
          <p>
            Tu objetivo es convertir las brechas prioritarias en iniciativas digitales realistas.
            Un roadmap organiza esas iniciativas en una secuencia gradual y coherente.
          </p>
        </div>
        <div className="dm-maturity-badge" aria-label="Fase de plan de transformacion">
          <span>Fase 4</span>
          <strong>Roadmap</strong>
        </div>
      </header>

      <section className="dm-action-guide" aria-labelledby="plan-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="plan-guide-title">Convierte las prioridades en acciones ordenadas</h3>
          <p>
            Selecciona iniciativas que respondan a las brechas priorizadas y distribuyelas en un
            horizonte de corto, mediano o largo plazo.
          </p>
        </div>
        <ol>
          <li>Revisa las brechas que priorizaste.</li>
          <li>Elige iniciativas que ayuden a cerrarlas.</li>
          <li>Ubica cada iniciativa en un horizonte.</li>
          <li>Verifica que el orden permita avanzar paso a paso.</li>
          <li>Justifica tu secuencia de transformacion.</li>
        </ol>
      </section>

      <section className="dm-context-bridge" aria-labelledby="plan-context-title">
        <div>
          <span className="experience-eyebrow">Brechas priorizadas</span>
          <h3 id="plan-context-title">Las iniciativas deben responder a tus prioridades</h3>
          <p>Usa las decisiones de la fase anterior para mantener el roadmap enfocado en las necesidades reales de la empresa.</p>
        </div>
        {gapContext.texts.length > 0 ? (
          <ul>
            {gapContext.texts.slice(0, 3).map((text, index) => <li key={`${text}-${index}`}>{text}</li>)}
          </ul>
        ) : (
          <p className="dm-bridge-empty">Aun no hay brechas recuperadas. Usa el contexto general del escenario para construir el roadmap.</p>
        )}
      </section>

      <section className="dm-roadmap-workspace" aria-labelledby="initiative-cards-title">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Iniciativas disponibles</span>
            <h3 id="initiative-cards-title">Elige acciones para el plan de transformacion</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} iniciativa{effectiveMax === 1 ? "" : "s"} agregada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="dm-empty-state" role="status">
            <h4>No se encontraron iniciativas para esta fase</h4>
            <p>Usa el contexto del escenario y tu justificacion para proponer una secuencia de transformacion.</p>
          </div>
        ) : (
          <div className="dm-roadmap-card-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const period = getCardValue(card, "period");

              return (
                <article key={card.id} className={`dm-roadmap-card ${isSelected ? "is-selected" : ""}`} style={{ "--card-index": index }}>
                  <div className="dm-card-heading">
                    <span>Iniciativa</span>
                    <strong>{isSelected ? "En el roadmap" : "Por planificar"}</strong>
                  </div>
                  <h4>{getDimensionLabelFromKey(card.dimension, "Iniciativa de transformacion")}</h4>
                  <p>{card.text}</p>
                  <p className="dm-card-context"><strong>Brecha que puede atender:</strong> {getDimensionLabelFromKey(card.dimension, "Por relacionar")}</p>
                  <p className="dm-card-context"><strong>Esfuerzo:</strong> {getChoiceLabel(card.effort, RELEVANCE_LEVELS, "Por evaluar")}</p>
                  <div className="dm-card-controls dm-one-control">
                    <label htmlFor={`initiative-period-${card.id}`}>
                      Plazo sugerido
                      <select id={`initiative-period-${card.id}`} value={period} onChange={(event) => updatePeriod(card.id, event.target.value)}>
                        <option value="">Por definir</option>
                        {ROADMAP_PERIODS.map((item) => <option key={item.key} value={item.key}>{item.label}</option>)}
                      </select>
                    </label>
                  </div>
                  <button type="button" className="dm-toggle-button" aria-pressed={isSelected} onClick={() => toggleInitiative(card)}>
                    {isSelected ? "Quitar del plan" : "Agregar al plan"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="dm-roadmap-board" aria-labelledby="roadmap-board-title" aria-live="polite">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultado de tu plan</span>
            <h3 id="roadmap-board-title">Roadmap de transformacion</h3>
          </div>
          <p>El plan se actualiza cuando agregas, quitas o reubicas una iniciativa.</p>
        </div>
        <div className="dm-roadmap-board-grid">
          {ROADMAP_PERIODS.map((period) => (
            <article key={period.key}>
              <h4>{period.label}</h4>
              {roadmap[period.key].length > 0 ? roadmap[period.key].map((card) => (
                <div key={card.id}>
                  <strong>{getDimensionLabelFromKey(card.dimension, "Iniciativa priorizada")}</strong>
                  <span>{getChoiceLabel(card.period, ROADMAP_PERIODS, period.label)}</span>
                  <p>{card.text}</p>
                </div>
              )) : <p className="dm-map-empty">Sin iniciativas en este horizonte.</p>}
            </article>
          ))}
        </div>
      </section>

      <section className="dm-draft-summary" aria-labelledby="plan-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del roadmap</span>
          <h3 id="plan-draft-title">Base para tu justificacion</h3>
          <p>{transformationDraft}</p>
        </div>
        <button type="button" className="dm-secondary-action" onClick={useTransformationDraft} disabled={selectedCards.length === 0}>
          Usar roadmap como borrador
        </button>
      </section>

      <div className="experience-text-answer dm-text-answer">
        <label htmlFor="digital-plan-text-answer">Justificacion del plan de transformacion</label>
        <p id="digital-plan-text-answer-help" className="dm-text-answer-help">
          Explica por que las iniciativas elegidas y su orden ayudan a cerrar las brechas prioritarias de la empresa.
        </p>
        <textarea
          id="digital-plan-text-answer"
          aria-describedby="digital-plan-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Inicio por integrar los datos porque permite tomar mejores decisiones. Luego automatizo los procesos prioritarios para consolidar la mejora operativa."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitPlan} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar plan y ver consecuencias"}
      </button>
    </section>
  );
}

export default TransformationPlanExperience;
