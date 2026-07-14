import { useMemo, useState } from "react";
import {
  buildDiagnosisDraft,
  buildDiagnosisMap,
  createDiagnosisCard,
  DIAGNOSIS_DIMENSIONS,
  getChoiceLabel,
  getDimensionLabelForCard,
  getEffectiveSelectionLimit,
  MATURITY_LEVELS,
  OBSERVED_LEVELS,
  RELEVANCE_LEVELS,
} from "./digitalMaturityHelpers";

function InitialDiagnosisExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createDiagnosisCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [maturityLevel, setMaturityLevel] = useState("");
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const diagnosisMap = buildDiagnosisMap(selectedCards, classifications);
  const diagnosisDraft = buildDiagnosisDraft(
    selectedCards,
    classifications,
    maturityLevel
  );

  const getCardValue = (card, key) => classifications[card.id]?.[key] ?? card[key] ?? "";

  const updateCardValue = (cardId, key, value) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], [key]: value },
    }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("La clasificacion se actualizo en el mapa de diagnostico.");
    }
  };

  const toggleDiagnosisSignal = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);
    const dimension = getCardValue(card, "dimension");

    if (!isSelected && !dimension) {
      setInteractionMessage("Selecciona una dimension antes de agregar esta senal al diagnostico.");
      return;
    }
    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} senal${effectiveMax === 1 ? "" : "es"} al diagnostico.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La senal se quito del diagnostico digital."
        : "La senal se agrego al diagnostico digital."
    );
  };

  const useDiagnosisDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una senal al diagnostico antes de crear un borrador.");
      return;
    }

    onTextAnswerChange(diagnosisDraft);
    setInteractionMessage("El resumen del diagnostico se copio como borrador de justificacion.");
  };

  const submitDiagnosis = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una senal al diagnostico antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="dm-experience dm-initial-diagnosis" aria-labelledby="digital-diagnosis-title">
      <header className="dm-phase-intro">
        <div>
          <span className="experience-eyebrow">Mapa de diagnostico digital</span>
          <h2 id="digital-diagnosis-title">Comprende el estado digital antes de proponer mejoras</h2>
          <p>
            En Madurez Digital no estas diseniando una solucion todavia. Primero
            diagnosticas el estado actual y evaluas las capacidades que la empresa
            necesita fortalecer.
          </p>
        </div>
        <div className="dm-maturity-badge" aria-label="Fase de diagnostico inicial">
          <span>Fase 1</span>
          <strong>Diagnostico</strong>
        </div>
      </header>

      <section className="dm-action-guide" aria-labelledby="diagnosis-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="diagnosis-guide-title">Construye una lectura consultiva del estado actual</h3>
          <p>
            Identifica las senales que muestran el estado digital de la empresa,
            clasificalas por dimension y construye un diagnostico antes de proponer mejoras.
          </p>
        </div>
        <ol>
          <li>Revisa las senales del caso.</li>
          <li>Clasifica cada senal por dimension.</li>
          <li>Agrega las evidencias mas relevantes.</li>
          <li>Construye el mapa de diagnostico.</li>
          <li>Estima el nivel de madurez inicial.</li>
          <li>Justifica el diagnostico con evidencia.</li>
        </ol>
      </section>

      <section className="dm-consulting-note" aria-label="Criterio de diagnostico">
        <strong>Una mirada consultiva</strong>
        <p>
          No todas las senales tienen el mismo peso. Da prioridad a las que muestran
          problemas estructurales en procesos, datos, tecnologia, cultura o experiencia del cliente.
        </p>
      </section>

      <section className="dm-maturity-scale" aria-labelledby="maturity-scale-title">
        <div>
          <span className="experience-eyebrow">Nivel de madurez estimado</span>
          <h3 id="maturity-scale-title">Como describirias a la empresa hoy</h3>
          <p>Esta estimacion enriquece tu justificacion sin cambiar la calificacion del backend.</p>
        </div>
        <div className="dm-maturity-options" role="group" aria-label="Nivel de madurez estimado">
          {MATURITY_LEVELS.map((level, index) => (
            <button
              key={level.key}
              type="button"
              className={maturityLevel === level.key ? "is-selected" : ""}
              aria-pressed={maturityLevel === level.key}
              onClick={() => {
                setMaturityLevel(level.key);
                setInteractionMessage(`Nivel de madurez estimado: ${level.label}.`);
              }}
            >
              <span>{index + 1}</span>
              {level.label}
            </button>
          ))}
        </div>
      </section>

      <section className="dm-diagnosis-workspace" aria-labelledby="diagnosis-signals-title">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Senales disponibles</span>
            <h3 id="diagnosis-signals-title">Clasifica las evidencias del estado digital</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} senal{effectiveMax === 1 ? "" : "es"} agregada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="dm-empty-state" role="status">
            <h4>No se encontraron senales para esta fase</h4>
            <p>Usa el contexto del escenario y tu justificacion para construir un diagnostico inicial.</p>
          </div>
        ) : (
          <div className="dm-signal-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const dimension = getCardValue(card, "dimension");
              const relevance = getCardValue(card, "relevance");
              const observedLevel = getCardValue(card, "observedLevel");

              return (
                <article
                  key={card.id}
                  className={`dm-signal-card ${isSelected ? "is-selected" : ""}`}
                  style={{ "--card-index": index }}
                >
                  <div className="dm-card-heading">
                    <span>{card.signalType}</span>
                    <strong>{isSelected ? "En el diagnostico" : "Por revisar"}</strong>
                  </div>
                  <p>{card.text}</p>
                  <div className="dm-card-controls">
                    <label htmlFor={`diagnosis-dimension-${card.id}`}>
                      Dimension
                      <select
                        id={`diagnosis-dimension-${card.id}`}
                        value={dimension}
                        onChange={(event) => updateCardValue(card.id, "dimension", event.target.value)}
                      >
                        <option value="">Selecciona una dimension</option>
                        {DIAGNOSIS_DIMENSIONS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                    <label htmlFor={`diagnosis-relevance-${card.id}`}>
                      Relevancia
                      <select
                        id={`diagnosis-relevance-${card.id}`}
                        value={relevance}
                        onChange={(event) => updateCardValue(card.id, "relevance", event.target.value)}
                      >
                        <option value="">Por evaluar</option>
                        {RELEVANCE_LEVELS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                    <label htmlFor={`diagnosis-level-${card.id}`}>
                      Nivel observado
                      <select
                        id={`diagnosis-level-${card.id}`}
                        value={observedLevel}
                        onChange={(event) => updateCardValue(card.id, "observedLevel", event.target.value)}
                      >
                        <option value="">Por estimar</option>
                        {OBSERVED_LEVELS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                  </div>
                  <div className="dm-card-summary" aria-label="Resumen de clasificacion">
                    <span>Dimension: {getDimensionLabelForCard(card, classifications)}</span>
                    <span>Relevancia: {getChoiceLabel(relevance, RELEVANCE_LEVELS, "Por evaluar")}</span>
                    <span>Nivel: {getChoiceLabel(observedLevel, OBSERVED_LEVELS, "Por estimar")}</span>
                  </div>
                  <button
                    type="button"
                    className="dm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleDiagnosisSignal(card)}
                  >
                    {isSelected ? "Quitar del diagnostico" : "Agregar al diagnostico"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="dm-diagnosis-map" aria-labelledby="diagnosis-map-title" aria-live="polite">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultado de tu analisis</span>
            <h3 id="diagnosis-map-title">Mapa de diagnostico digital</h3>
          </div>
          <p>{selectedCards.length} senal{selectedCards.length === 1 ? "" : "es"} agregada{selectedCards.length === 1 ? "" : "s"} al diagnostico</p>
        </div>
        <div className="dm-diagnosis-map-grid">
          {DIAGNOSIS_DIMENSIONS.map((dimension) => (
            <article key={dimension.key}>
              <h4>{dimension.label}</h4>
              {diagnosisMap[dimension.key].length > 0 ? (
                diagnosisMap[dimension.key].map((card) => (
                  <div key={card.id}>
                    <p>{card.text}</p>
                    <span>{getChoiceLabel(getCardValue(card, "observedLevel"), OBSERVED_LEVELS, "Nivel por estimar")}</span>
                  </div>
                ))
              ) : (
                <p className="dm-map-empty">Sin senales agregadas todavia.</p>
              )}
            </article>
          ))}
        </div>
      </section>

      <section className="dm-draft-summary" aria-labelledby="diagnosis-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen del diagnostico</span>
          <h3 id="diagnosis-draft-title">Base para tu justificacion</h3>
          <p>{diagnosisDraft}</p>
        </div>
        <button type="button" className="dm-secondary-action" onClick={useDiagnosisDraft} disabled={selectedCards.length === 0}>
          Usar mapa como borrador
        </button>
      </section>

      <div className="experience-text-answer dm-text-answer">
        <label htmlFor="digital-diagnosis-text-answer">Justificacion del diagnostico inicial</label>
        <p id="digital-diagnosis-text-answer-help" className="dm-text-answer-help">
          Explica el estado digital actual de la empresa. Menciona las dimensiones mas afectadas,
          las evidencias que lo demuestran y el nivel de madurez estimado.
        </p>
        <textarea
          id="digital-diagnosis-text-answer"
          aria-describedby="digital-diagnosis-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: La empresa se encuentra en un nivel inicial porque sus procesos dependen de tareas manuales, los datos no se usan para tomar decisiones y las herramientas digitales no estan integradas."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitDiagnosis} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar diagnostico y ver consecuencias"}
      </button>
    </section>
  );
}

export default InitialDiagnosisExperience;
