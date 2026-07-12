import { useMemo, useState } from "react";
import userResearchIllustration from "../../../../assets/methodologyExperience/user-research.svg";
import {
  buildEmpathyCounts,
  buildEmpathySummary,
  createEvidenceCard,
  EMPATHY_CATEGORIES,
  getEffectiveEvidenceLimit,
  getEvidenceGuidance,
} from "./experienceHelpers";

function EmpathizeExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const cards = useMemo(
    () => model.options.map(createEvidenceCard),
    [model.options]
  );
  const [activeCardId, setActiveCardId] = useState(null);
  const [classifications, setClassifications] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const activeCard = cards.find((card) => card.id === activeCardId);
  const effectiveMax = getEffectiveEvidenceLimit(
    cards,
    model.selection.maxSelections
  );
  const empathyCounts = buildEmpathyCounts(selectedCards, classifications);
  const summary = buildEmpathySummary(selectedCards, classifications);

  const getCategory = (card) => classifications[card.id] || card.category;
  const getCategoryInfo = (category) =>
    EMPATHY_CATEGORIES.find((item) => item.key === category) ||
    EMPATHY_CATEGORIES[3];

  const updateCategory = (cardId, category) => {
    setClassifications((current) => ({ ...current, [cardId]: category }));

    if (model.selection.selectedOptionIds.includes(cardId)) {
      setInteractionMessage("La clasificacion se actualizo en el mapa de empatia.");
    }
  };

  const toggleEvidence = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        `Puedes agregar hasta ${effectiveMax} hallazgo${effectiveMax === 1 ? "" : "s"} al mapa en esta fase.`
      );
      return;
    }

    onToggleOption(card.id);
    setActiveCardId(card.id);
    setInteractionMessage(
      isSelected
        ? "El hallazgo se quito del mapa de empatia."
        : "El hallazgo se agrego al mapa de empatia."
    );
  };

  const useSummaryAsDraft = () => {
    if (selectedCards.length === 0) {
      setInteractionMessage(
        "Agrega al menos un hallazgo relevante al mapa antes de crear una justificacion."
      );
      return;
    }

    const selectedTexts = selectedCards.map((card) => card.text).join(" ");
    onTextAnswerChange(`${summary} Hallazgos priorizados: ${selectedTexts}`.trim());
    setInteractionMessage("El resumen del mapa se copio como borrador de justificacion.");
  };

  const submitEmpathyPhase = () => {
    if (cards.length === 0) {
      setInteractionMessage(
        "No se encontraron evidencias para esta fase. Revisa la configuracion del escenario."
      );
      return;
    }

    if (selectedCards.length === 0) {
      setInteractionMessage(
        "Agrega al menos un hallazgo relevante al mapa de empatia antes de continuar."
      );
      return;
    }

    onSubmit();
  };

  const counterLabel = effectiveMax > 0
    ? `${selectedCards.length} hallazgo${selectedCards.length === 1 ? "" : "s"} agregado${selectedCards.length === 1 ? "" : "s"} al mapa - maximo ${effectiveMax}`
    : "No hay hallazgos disponibles";

  return (
    <section className="dt-experience dt-empathize" aria-labelledby="empathize-title">
      <header className="dt-phase-intro dt-empathize-intro">
        <div>
          <span className="experience-eyebrow">Sala de investigacion del usuario</span>
          <h2 id="empathize-title">Comprende antes de proponer</h2>
          <p>
            Tu mision es comprender por que los usuarios experimentan friccion
            antes de proponer soluciones.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={userResearchIllustration}
          alt="Investigacion de usuarios y analisis de evidencia"
        />
      </header>

      <section className="dt-action-guide" aria-labelledby="empathy-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="empathy-guide-title">Construye un mapa de empatia con evidencia</h3>
          <p>
            Revisa las evidencias, clasificalas y agrega al mapa solo los
            hallazgos que expliquen mejor el dolor principal del usuario.
          </p>
        </div>
        <ol>
          <li>Lee cada evidencia.</li>
          <li>Clasificala segun lo que representa.</li>
          <li>Agrega al mapa los hallazgos relevantes.</li>
          <li>Revisa el mapa en construccion.</li>
          <li>Justifica el dolor principal con evidencia.</li>
        </ol>
      </section>

      <section className="dt-research-brief" aria-label="Contexto de investigacion">
        <article>
          <span>Usuario objetivo</span>
          <strong>{model.scenario.targetUser || "Usuario no especificado"}</strong>
        </article>
        <article>
          <span>Problema principal</span>
          <strong>{model.scenario.problem || "Problema no especificado"}</strong>
        </article>
        <article>
          <span>Restricciones</span>
          <strong>{model.scenario.constraints || "No se registraron restricciones"}</strong>
        </article>
      </section>

      <section className="dt-map-progress" aria-labelledby="empathy-progress-title">
        <div>
          <span className="experience-eyebrow">Objetivo de la fase</span>
          <h3 id="empathy-progress-title">Mapa de empatia en construccion</h3>
          <p>Los hallazgos que agregues construiran este mapa y sostendran tu justificacion final.</p>
        </div>
        <dl>
          {EMPATHY_CATEGORIES.map((category) => (
            <div key={category.key}>
              <dt>{category.label}</dt>
              <dd>{empathyCounts[category.key]}</dd>
            </div>
          ))}
        </dl>
      </section>

      <section className="dt-category-guide" aria-labelledby="classification-guide-title">
        <div>
          <span className="experience-eyebrow">Guia de clasificacion</span>
          <h3 id="classification-guide-title">Que representa cada hallazgo</h3>
        </div>
        <div className="dt-category-guide-list" id="empathy-category-guide">
          {EMPATHY_CATEGORIES.map((category) => (
            <article key={category.key}>
              <strong>{category.label}</strong>
              <span>{category.description}</span>
            </article>
          ))}
        </div>
      </section>

      <div className="dt-empathy-layout">
        <section className="dt-evidence-panel" aria-labelledby="evidence-title">
          <div className="dt-panel-heading">
            <div>
              <span className="experience-eyebrow">Evidencia disponible</span>
              <h3 id="evidence-title">Revisa y prioriza hallazgos</h3>
            </div>
            <span className="dt-selection-counter">{counterLabel}</span>
          </div>

          <p className="dt-evidence-guidance">
            No todas las evidencias tienen el mismo valor. Prioriza las que
            realmente expliquen el dolor, la necesidad o el comportamiento del usuario.
          </p>

          {cards.length === 0 ? (
            <div className="dt-empty-evidence" role="status">
              <h4>No se encontraron evidencias para esta fase</h4>
              <p>Revisa la configuracion del escenario antes de continuar.</p>
            </div>
          ) : (
            <div className="dt-evidence-grid">
              {cards.map((card, index) => {
                const isSelected = model.selection.selectedOptionIds.includes(card.id);
                const category = getCategory(card);
                const categoryInfo = getCategoryInfo(category);

                return (
                  <article
                    key={card.id}
                    className={`dt-evidence-card ${isSelected ? "is-selected" : ""}`}
                    style={{ "--card-index": index }}
                  >
                    <div className="dt-evidence-card-top">
                      <div className="dt-evidence-badges">
                        <span>{card.source}</span>
                        <span className="dt-evidence-category">{categoryInfo.label}</span>
                      </div>
                      <button
                        type="button"
                        className="dt-text-button"
                        onClick={() => setActiveCardId(card.id)}
                        aria-expanded={activeCardId === card.id}
                        aria-controls="empathy-evidence-detail"
                      >
                        Ver detalle
                      </button>
                    </div>
                    <p>{card.text}</p>
                    {card.tags.length > 0 && (
                      <div className="dt-tag-list" aria-label="Etiquetas de la evidencia">
                        {card.tags.map((tag) => <span key={tag}>{tag}</span>)}
                      </div>
                    )}
                    <div className="dt-evidence-actions">
                      <div className="dt-classification-control">
                        <label htmlFor={`empathy-category-${card.id}`}>
                          Clasificar hallazgo
                        </label>
                        <select
                          id={`empathy-category-${card.id}`}
                          value={category}
                          aria-describedby="empathy-category-guide"
                          onChange={(event) => updateCategory(card.id, event.target.value)}
                        >
                          {EMPATHY_CATEGORIES.map((item) => (
                            <option key={item.key} value={item.key}>{item.label}</option>
                          ))}
                        </select>
                      </div>
                      <button
                        type="button"
                        className="dt-select-evidence"
                        aria-pressed={isSelected}
                        onClick={() => toggleEvidence(card)}
                      >
                        {isSelected ? "Quitar del mapa" : "Agregar al mapa"}
                      </button>
                    </div>
                    <span className="dt-card-state">
                      {isSelected ? "Agregado al mapa de empatia" : "Aun no agregado al mapa"}
                    </span>
                  </article>
                );
              })}
            </div>
          )}
        </section>

        <aside
          id="empathy-evidence-detail"
          className="dt-evidence-detail"
          aria-live="polite"
          aria-label="Detalle de evidencia"
        >
          <span className="experience-eyebrow">Detalle de evidencia</span>
          {activeCard ? (
            <>
              <h3>{activeCard.source}</h3>
              <p>{activeCard.text}</p>
              <dl className="dt-detail-facts">
                <div>
                  <dt>Clasificacion actual</dt>
                  <dd>{getCategoryInfo(getCategory(activeCard)).label}</dd>
                </div>
                <div>
                  <dt>Estado</dt>
                  <dd>{selectedCards.some((card) => card.id === activeCard.id) ? "Agregado al mapa" : "Aun no agregado"}</dd>
                </div>
              </dl>
              {activeCard.tags.length > 0 && (
                <div className="dt-detail-tags" aria-label="Etiquetas de la evidencia">
                  {activeCard.tags.map((tag) => <span key={tag}>{tag}</span>)}
                </div>
              )}
              <p className="dt-detail-note">
                {getEvidenceGuidance(activeCard, getCategory(activeCard))}
              </p>
            </>
          ) : (
            <p>Abre una tarjeta para revisar su contexto antes de agregarla al mapa.</p>
          )}
        </aside>
      </div>

      <section className="dt-empathy-map" aria-labelledby="empathy-map-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultado de tu analisis</span>
            <h3 id="empathy-map-title">Mapa de empatia</h3>
          </div>
          <span>Se actualiza al clasificar, agregar o quitar hallazgos</span>
        </div>
        <div className="dt-empathy-map-grid">
          {EMPATHY_CATEGORIES.map((category) => {
            const entries = selectedCards.filter(
              (card) => getCategory(card) === category.key
            );

            return (
              <article key={category.key}>
                <h4>{category.label}</h4>
                {entries.length > 0 ? (
                  entries.map((entry) => (
                    <p key={entry.id} className="dt-map-entry">{entry.text}</p>
                  ))
                ) : (
                  <p className="dt-map-empty">Sin hallazgos en esta categoria.</p>
                )}
              </article>
            );
          })}
        </div>
      </section>

      <section className="dt-empathy-summary">
        <div>
          <span className="experience-eyebrow">Resumen del mapa</span>
          <p>{summary}</p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={useSummaryAsDraft}>
          Usar mapa como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="empathize-text-answer">Justificacion del dolor principal</label>
        <p className="dt-text-answer-help">
          Con base en el mapa de empatia, explica cual es el dolor principal del usuario,
          que evidencia lo respalda y por que debe priorizarse antes de proponer una solucion.
        </p>
        <textarea
          id="empathize-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: El dolor principal es..., porque las evidencias muestran que..., por eso se debe priorizar..."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && (
        <p className="dt-interaction-message" role="status">{interactionMessage}</p>
      )}

      <button
        type="button"
        className="experience-submit"
        onClick={submitEmpathyPhase}
        disabled={submitting || cards.length === 0}
      >
        {submitting ? "Evaluando fase..." : "Enviar hallazgos y ver consecuencias"}
      </button>
    </section>
  );
}

export default EmpathizeExperience;
