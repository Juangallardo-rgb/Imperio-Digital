import { useMemo, useState } from "react";
import {
  buildCapabilitiesDraft,
  buildCapabilityMatrix,
  buildDiagnosisSnapshot,
  createCapabilityCard,
  DIGITAL_CAPABILITIES,
  getCapabilityLabelForCard,
  getCapabilityRelation,
  getChoiceLabel,
  getEffectiveSelectionLimit,
  OBSERVED_LEVELS,
  RELEVANCE_LEVELS,
} from "./digitalMaturityHelpers";

function CapabilityAssessmentExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createCapabilityCard),
    [model.options]
  );
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const diagnosisSnapshot = useMemo(
    () => buildDiagnosisSnapshot(model.decisionTrace),
    [model.decisionTrace]
  );
  const effectiveMax = getEffectiveSelectionLimit(cards, model.selection.maxSelections);
  const capabilityMatrix = buildCapabilityMatrix(selectedCards, classifications);
  const capabilitiesDraft = buildCapabilitiesDraft(
    selectedCards,
    classifications,
    diagnosisSnapshot
  );

  const getCardValue = (card, key) => classifications[card.id]?.[key] ?? card[key] ?? "";

  const updateCardValue = (cardId, key, value) => {
    setClassifications((current) => ({
      ...current,
      [cardId]: { ...current[cardId], [key]: value },
    }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("La matriz de capacidades se actualizo.");
    }
  };

  const toggleCapability = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);
    const capability = getCardValue(card, "capability");
    const level = getCardValue(card, "level");
    const priority = getCardValue(card, "priority");

    if (!isSelected && (!capability || !level || !priority)) {
      setInteractionMessage("Completa capacidad, nivel actual y prioridad antes de agregarla a la matriz.");
      return;
    }
    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} capacidad${effectiveMax === 1 ? "" : "es"} a la matriz.`
      );
      return;
    }

    onToggleOption(card.id);
    setInteractionMessage(
      isSelected
        ? "La capacidad se quito de la matriz digital."
        : "La capacidad se agrego a la matriz digital."
    );
  };

  const useCapabilitiesDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una capacidad a la matriz antes de crear un borrador.");
      return;
    }

    onTextAnswerChange(capabilitiesDraft);
    setInteractionMessage("El resumen de la matriz se copio como borrador de justificacion.");
  };

  const submitAssessment = () => {
    if (cards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos una capacidad a la matriz antes de continuar.");
      return;
    }

    const hasIncompleteClassification = selectedCards.some((card) =>
      !getCardValue(card, "capability") ||
      !getCardValue(card, "level") ||
      !getCardValue(card, "priority")
    );
    if (hasIncompleteClassification) {
      setInteractionMessage("Completa nivel y prioridad de las capacidades seleccionadas antes de continuar.");
      return;
    }

    onSubmit();
  };

  return (
    <section className="dm-experience dm-capability-assessment" aria-labelledby="capability-assessment-title">
      <header className="dm-phase-intro">
        <div>
          <span className="experience-eyebrow">Matriz de capacidades digitales</span>
          <h2 id="capability-assessment-title">Evalua las capacidades que habilitan la transformacion</h2>
          <p>
            Una capacidad digital es una habilidad organizacional para operar, decidir y competir
            usando tecnologia, datos, procesos y cultura digital.
          </p>
        </div>
        <div className="dm-maturity-badge" aria-label="Fase de evaluar capacidades">
          <span>Fase 2</span>
          <strong>Capacidades</strong>
        </div>
      </header>

      <section className="dm-action-guide" aria-labelledby="capability-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="capability-guide-title">Convierte el diagnostico en una matriz de fortalecimiento</h3>
          <p>
            Evalua las capacidades digitales de la empresa, identifica su nivel actual y define
            cuales necesitan fortalecerse primero.
          </p>
        </div>
        <ol>
          <li>Revisa el diagnostico inicial.</li>
          <li>Analiza las capacidades disponibles.</li>
          <li>Clasifica el nivel de cada capacidad.</li>
          <li>Define su prioridad.</li>
          <li>Construye la matriz digital.</li>
          <li>Justifica que fortalecer primero.</li>
        </ol>
      </section>

      <section className="dm-diagnosis-bridge" aria-labelledby="diagnosis-bridge-title">
        <div>
          <span className="experience-eyebrow">Del diagnostico a las capacidades</span>
          <h3 id="diagnosis-bridge-title">Las senales previas orientan esta evaluacion</h3>
          <p>
            Con base en las senales detectadas en el diagnostico inicial, evalua que capacidades
            digitales necesita fortalecer la empresa.
          </p>
        </div>
        {diagnosisSnapshot.texts.length > 0 ? (
          <div className="dm-diagnosis-bridge-content">
            <div className="dm-dimension-chip-list" aria-label="Dimensiones detectadas">
              {diagnosisSnapshot.dimensions.length > 0 ? diagnosisSnapshot.dimensions.map((dimension) => {
                const label = {
                  processes: "Procesos",
                  data: "Datos",
                  technology: "Tecnologia",
                  peopleCulture: "Personas y cultura",
                  customerExperience: "Experiencia del cliente",
                  digitalStrategy: "Estrategia digital",
                }[dimension];

                return <span key={dimension}>{label}</span>;
              }) : <span>Senales por analizar</span>}
            </div>
            <ul>
              {diagnosisSnapshot.texts.map((text, index) => <li key={`${text}-${index}`}>{text}</li>)}
            </ul>
          </div>
        ) : (
          <p className="dm-bridge-empty">
            Aun no hay senales recuperadas del diagnostico. Usa el contexto del escenario para construir esta matriz.
          </p>
        )}
      </section>

      <section className="dm-capability-workspace" aria-labelledby="capability-cards-title">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Capacidades disponibles</span>
            <h3 id="capability-cards-title">Valora las capacidades digitales criticas</h3>
          </div>
          <p aria-live="polite">
            {selectedCards.length} de {effectiveMax} capacidad{effectiveMax === 1 ? "" : "es"} agregada{selectedCards.length === 1 ? "" : "s"}
          </p>
        </div>

        {cards.length === 0 ? (
          <div className="dm-empty-state" role="status">
            <h4>No se encontraron capacidades para esta fase</h4>
            <p>Usa el contexto del escenario y tu justificacion para valorar las capacidades prioritarias.</p>
          </div>
        ) : (
          <div className="dm-capability-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);
              const capability = getCardValue(card, "capability");
              const level = getCardValue(card, "level");
              const priority = getCardValue(card, "priority");

              return (
                <article
                  key={card.id}
                  className={`dm-capability-card ${isSelected ? "is-selected" : ""}`}
                  style={{ "--card-index": index }}
                >
                  <div className="dm-card-heading">
                    <span>Capacidad digital</span>
                    <strong>{isSelected ? "En la matriz" : "Por evaluar"}</strong>
                  </div>
                  <h4>{getCapabilityLabelForCard(card, classifications)}</h4>
                  <p>{card.text}</p>
                  <p className="dm-capability-relation"><strong>Relacion con el diagnostico:</strong> {getCapabilityRelation({ ...card, capability }, diagnosisSnapshot)}</p>
                  <div className="dm-card-controls">
                    <label htmlFor={`capability-name-${card.id}`}>
                      Capacidad
                      <select
                        id={`capability-name-${card.id}`}
                        value={capability}
                        onChange={(event) => updateCardValue(card.id, "capability", event.target.value)}
                      >
                        <option value="">Selecciona una capacidad</option>
                        {DIGITAL_CAPABILITIES.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                    <label htmlFor={`capability-level-${card.id}`}>
                      Nivel actual
                      <select
                        id={`capability-level-${card.id}`}
                        value={level}
                        onChange={(event) => updateCardValue(card.id, "level", event.target.value)}
                      >
                        <option value="">Por evaluar</option>
                        {OBSERVED_LEVELS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                    <label htmlFor={`capability-priority-${card.id}`}>
                      Prioridad
                      <select
                        id={`capability-priority-${card.id}`}
                        value={priority}
                        onChange={(event) => updateCardValue(card.id, "priority", event.target.value)}
                      >
                        <option value="">Por definir</option>
                        {RELEVANCE_LEVELS.map((item) => (
                          <option key={item.key} value={item.key}>{item.label}</option>
                        ))}
                      </select>
                    </label>
                  </div>
                  <button
                    type="button"
                    className="dm-toggle-button"
                    aria-pressed={isSelected}
                    onClick={() => toggleCapability(card)}
                  >
                    {isSelected ? "Quitar de matriz" : "Agregar a matriz"}
                  </button>
                </article>
              );
            })}
          </div>
        )}
      </section>

      <section className="dm-capability-matrix" aria-labelledby="capability-matrix-title" aria-live="polite">
        <div className="dm-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultado de tu analisis</span>
            <h3 id="capability-matrix-title">Matriz de capacidades digitales</h3>
          </div>
          <p>La matriz se actualiza al clasificar, agregar o quitar capacidades.</p>
        </div>
        <div className="dm-capability-matrix-grid">
          {RELEVANCE_LEVELS.map((priority) => (
            <article key={priority.key}>
              <h4>{priority.label} prioridad</h4>
              {capabilityMatrix[priority.key].length > 0 ? (
                capabilityMatrix[priority.key].map((card) => (
                  <div key={card.id}>
                    <strong>{getCapabilityLabelForCard(card, classifications)}</strong>
                    <span>Nivel actual: {getChoiceLabel(getCardValue(card, "level"), OBSERVED_LEVELS, "Por evaluar")}</span>
                    <p>{card.text}</p>
                  </div>
                ))
              ) : (
                <p className="dm-map-empty">Sin capacidades agregadas todavia.</p>
              )}
            </article>
          ))}
        </div>
      </section>

      <section className="dm-draft-summary" aria-labelledby="capability-draft-title">
        <div>
          <span className="experience-eyebrow">Resumen de la matriz</span>
          <h3 id="capability-draft-title">Base para tu justificacion</h3>
          <p>{capabilitiesDraft}</p>
        </div>
        <button type="button" className="dm-secondary-action" onClick={useCapabilitiesDraft} disabled={selectedCards.length === 0}>
          Usar matriz como borrador
        </button>
      </section>

      <div className="experience-text-answer dm-text-answer">
        <label htmlFor="digital-capability-text-answer">Justificacion de capacidades criticas</label>
        <p id="digital-capability-text-answer-help" className="dm-text-answer-help">
          Explica que capacidades deben fortalecerse primero y por que. Relaciona tu decision con el diagnostico inicial y con las necesidades de transformacion de la empresa.
        </p>
        <textarea
          id="digital-capability-text-answer"
          aria-describedby="digital-capability-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Las capacidades mas criticas son procesos digitales y gestion de datos, porque el diagnostico muestra tareas manuales y ausencia de informacion confiable para decidir."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dm-interaction-message" role="status">{interactionMessage}</p>}

      <button type="button" className="experience-submit" onClick={submitAssessment} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar evaluacion y ver consecuencias"}
      </button>
    </section>
  );
}

export default CapabilityAssessmentExperience;
