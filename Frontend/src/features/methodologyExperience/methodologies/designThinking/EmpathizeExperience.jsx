import { useMemo, useState } from "react";
import userResearchIllustration from "../../../../assets/methodologyExperience/user-research.svg";
import {
  buildEmpathySummary,
  createEvidenceCard,
} from "./experienceHelpers";

const empathyAreas = [
  ["Piensa", "need"],
  ["Siente", "pain"],
  ["Dice", "evidence"],
  ["Hace", "behavior"],
  ["Dolores", "pain"],
  ["Necesidades", "need"],
];

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
  const selectedCards = cards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const activeCard = cards.find((card) => card.id === activeCardId);
  const summary = buildEmpathySummary(selectedCards, classifications);

  const getCategory = (card) => classifications[card.id] || card.category;

  const updateCategory = (cardId, category) => {
    setClassifications((current) => ({ ...current, [cardId]: category }));
  };

  const useSummaryAsDraft = () => {
    const selectedTexts = selectedCards.map((card) => card.text).join(" ");
    onTextAnswerChange(
      `${summary} Hallazgos priorizados: ${selectedTexts}`.trim()
    );
  };

  return (
    <section className="dt-experience dt-empathize" aria-labelledby="empathize-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Sala de investigacion del usuario</span>
          <h2 id="empathize-title">Comprende antes de proponer</h2>
          <p>
            Examina evidencias del caso, clasifica los hallazgos relevantes y
            construye una lectura compartida del usuario.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={userResearchIllustration}
          alt="Investigacion de usuarios y analisis de evidencia"
        />
      </header>

      <section className="dt-research-brief" aria-label="Briefing de investigacion">
        <article>
          <span>Perfil prioritario</span>
          <strong>{model.scenario.targetUser || "Usuario objetivo no especificado"}</strong>
        </article>
        <article>
          <span>Pregunta de investigacion</span>
          <strong>{model.scenario.problem || "Comprender el desafio planteado"}</strong>
        </article>
        <article>
          <span>Limite del caso</span>
          <strong>{model.scenario.constraints || "No se registraron restricciones"}</strong>
        </article>
      </section>

      <div className="dt-empathy-layout">
        <section className="dt-evidence-panel" aria-labelledby="evidence-title">
          <div className="dt-panel-heading">
            <div>
              <span className="experience-eyebrow">Evidencia disponible</span>
              <h3 id="evidence-title">Examinar hallazgos</h3>
            </div>
            <span>{selectedCards.length}/{model.selection.maxSelections} priorizados</span>
          </div>

          <div className="dt-evidence-grid">
            {cards.map((card, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(card.id);

              return (
                <article
                  key={card.id}
                  className={`dt-evidence-card ${isSelected ? "is-selected" : ""}`}
                  style={{ "--card-index": index }}
                >
                  <div className="dt-evidence-card-top">
                    <span>{card.source}</span>
                    <button
                      type="button"
                      className="dt-text-button"
                      onClick={() => setActiveCardId(card.id)}
                      aria-expanded={activeCardId === card.id}
                    >
                      Ver detalle
                    </button>
                  </div>
                  <p>{card.text}</p>
                  {card.tags.length > 0 && (
                    <div className="dt-tag-list">
                      {card.tags.map((tag) => <span key={tag}>{tag}</span>)}
                    </div>
                  )}
                  <div className="dt-evidence-actions">
                    <label>
                      Clasificar como
                      <select
                        value={getCategory(card)}
                        onChange={(event) => updateCategory(card.id, event.target.value)}
                      >
                        <option value="pain">Dolor</option>
                        <option value="need">Necesidad</option>
                        <option value="behavior">Comportamiento</option>
                        <option value="evidence">Evidencia</option>
                      </select>
                    </label>
                    <button
                      type="button"
                      className="dt-select-evidence"
                      aria-pressed={isSelected}
                      onClick={() => onToggleOption(card.id)}
                    >
                      {isSelected ? "Retirar hallazgo" : "Priorizar hallazgo"}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        </section>

        <aside className="dt-evidence-detail" aria-live="polite">
          <span className="experience-eyebrow">Detalle de evidencia</span>
          {activeCard ? (
            <>
              <h3>{activeCard.source}</h3>
              <p>{activeCard.text}</p>
              <p className="dt-detail-note">
                Esta lectura es una ayuda de clasificacion; la evaluacion se realiza
                cuando envias la fase.
              </p>
            </>
          ) : (
            <p>Abre una tarjeta para revisar su contexto antes de priorizarla.</p>
          )}
        </aside>
      </div>

      <section className="dt-empathy-map" aria-labelledby="empathy-map-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Sintesis</span>
            <h3 id="empathy-map-title">Mapa de empatia</h3>
          </div>
          <span>Se actualiza con los hallazgos priorizados</span>
        </div>
        <div className="dt-empathy-map-grid">
          {empathyAreas.map(([label, category]) => {
            const entries = selectedCards.filter(
              (card) => getCategory(card) === category
            );

            return (
              <article key={label}>
                <h4>{label}</h4>
                {entries.length > 0 ? (
                  entries.map((entry) => (
                    <p key={`${label}-${entry.id}`} className="dt-map-entry">{entry.text}</p>
                  ))
                ) : (
                  <p className="dt-map-empty">Sin hallazgos clasificados aqui.</p>
                )}
              </article>
            );
          })}
        </div>
      </section>

      <section className="dt-empathy-summary">
        <div>
          <span className="experience-eyebrow">Resumen de empatia</span>
          <p>{summary}</p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={useSummaryAsDraft}>
          Usar resumen como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="empathize-text-answer">Justificacion de los hallazgos</label>
        <textarea
          id="empathize-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica que comportamiento, dolor o necesidad merece prioridad y por que."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button
        type="button"
        className="experience-submit"
        onClick={onSubmit}
        disabled={submitting}
      >
        {submitting ? "Evaluando fase..." : "Enviar hallazgos y ver consecuencias"}
      </button>
    </section>
  );
}

export default EmpathizeExperience;
