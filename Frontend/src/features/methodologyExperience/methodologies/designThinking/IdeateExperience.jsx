import { useMemo } from "react";
import ideationMatrixIllustration from "../../../../assets/methodologyExperience/ideation-matrix.svg";
import {
  buildStrategySummary,
  getIdeaQuadrant,
  getTraceForPhase,
} from "./experienceHelpers";

const quadrants = [
  ["high-low", "Alto impacto / Bajo esfuerzo"],
  ["high-high", "Alto impacto / Alto esfuerzo"],
  ["low-low", "Bajo impacto / Bajo esfuerzo"],
  ["low-high", "Bajo impacto / Alto esfuerzo"],
];

function IdeateExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const definitionTrace = useMemo(
    () => getTraceForPhase(model.decisionTrace, "Definir"),
    [model.decisionTrace]
  );
  const selectedIdeas = model.options.filter((option) =>
    model.selection.selectedOptionIds.includes(option.id)
  );
  const ideasByQuadrant = quadrants.reduce((groups, [key]) => {
    groups[key] = model.options.filter((option) => getIdeaQuadrant(option) === key);
    return groups;
  }, {});
  const unclassifiedIdeas = model.options.filter(
    (option) => getIdeaQuadrant(option) === "unclassified"
  );
  const strategySummary = buildStrategySummary(selectedIdeas);
  const problemContext = definitionTrace?.selectedTexts?.join(" | ") || model.scenario.problem;

  return (
    <section className="dt-experience dt-ideate" aria-labelledby="ideate-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Estudio de ideacion estrategica</span>
          <h2 id="ideate-title">Prioriza ideas con criterio</h2>
          <p>
            Compara las alternativas con los niveles configurados de impacto,
            esfuerzo y viabilidad. La eleccion final conserva las reglas de la simulacion.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={ideationMatrixIllustration}
          alt="Matriz de impacto y esfuerzo para priorizar ideas"
        />
      </header>

      <section className="dt-continuity-brief">
        <span className="experience-eyebrow">Problema a resolver</span>
        <p>{problemContext || "No hay una formulacion anterior disponible."}</p>
      </section>

      <section className="dt-resource-strip" aria-label="Recursos disponibles">
        <div><span>Presupuesto disponible</span><strong>{model.resources.remainingBudget} pts</strong></div>
        <div><span>Tiempo disponible</span><strong>{model.resources.remainingTimeWeeks} sem</strong></div>
        <div><span>Riesgo actual</span><strong>{model.resources.riskLevel}/100</strong></div>
        <div><span>Votos de priorizacion</span><strong>{selectedIdeas.length}/{model.selection.maxSelections}</strong></div>
      </section>

      <section className="dt-matrix-section" aria-labelledby="matrix-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Matriz impacto-esfuerzo</span>
            <h3 id="matrix-title">Ubicacion basada en metadata del escenario</h3>
          </div>
          <span>Selecciona una cartera limitada</span>
        </div>
        <div className="dt-idea-matrix">
          {quadrants.map(([key, title]) => (
            <section key={key} className={`dt-matrix-quadrant ${key}`}>
              <h4>{title}</h4>
              {ideasByQuadrant[key].length > 0 ? (
                ideasByQuadrant[key].map((idea, index) => {
                  const isSelected = model.selection.selectedOptionIds.includes(idea.id);

                  return (
                    <button
                      key={idea.id}
                      type="button"
                      className={`dt-idea-card ${isSelected ? "is-selected" : ""}`}
                      style={{ "--card-index": index }}
                      aria-pressed={isSelected}
                      onClick={() => onToggleOption(idea.id)}
                    >
                      <span>{idea.optionType || "Idea"}</span>
                      <strong>{idea.text}</strong>
                      <small>
                        Viabilidad: {idea.expectedViabilityLevel || "No configurada"}
                      </small>
                    </button>
                  );
                })
              ) : (
                <p>Sin ideas con esta combinacion configurada.</p>
              )}
            </section>
          ))}
        </div>
      </section>

      {unclassifiedIdeas.length > 0 && (
        <section className="dt-unclassified-ideas">
          <div>
            <span className="experience-eyebrow">Datos por completar</span>
            <h3>Ideas sin niveles suficientes para ubicarse en la matriz</h3>
          </div>
          <div>
            {unclassifiedIdeas.map((idea) => {
              const isSelected = model.selection.selectedOptionIds.includes(idea.id);

              return (
                <button
                  key={idea.id}
                  type="button"
                  aria-pressed={isSelected}
                  className={isSelected ? "is-selected" : ""}
                  onClick={() => onToggleOption(idea.id)}
                >
                  <span>{idea.text}</span>
                  <small>Impacto, esfuerzo o ambos no estan configurados.</small>
                </button>
              );
            })}
          </div>
        </section>
      )}

      <section className="dt-idea-portfolio">
        <div>
          <span className="experience-eyebrow">Cartera seleccionada</span>
          <p>{strategySummary}</p>
        </div>
        <button
          type="button"
          className="dt-secondary-action"
          onClick={() => onTextAnswerChange(strategySummary)}
        >
          Usar resumen como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="ideate-text-answer">Estrategia de priorizacion</label>
        <textarea
          id="ideate-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica por que la cartera seleccionada responde al problema y a los recursos disponibles."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button type="button" className="experience-submit" onClick={onSubmit} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar cartera y ver consecuencias"}
      </button>
    </section>
  );
}

export default IdeateExperience;
