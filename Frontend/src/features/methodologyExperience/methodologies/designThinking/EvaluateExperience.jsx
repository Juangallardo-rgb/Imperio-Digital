import { useMemo, useState } from "react";
import userTestingIllustration from "../../../../assets/methodologyExperience/user-testing.svg";
import {
  buildTestPlan,
  createTestCard,
  getTraceForPhase,
} from "./experienceHelpers";

const actionOptions = ["Mantener", "Modificar", "Eliminar", "Volver a probar"];

function EvaluateExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const [actions, setActions] = useState({});
  const prototypeTrace = useMemo(
    () => getTraceForPhase(model.decisionTrace, "Prototipar"),
    [model.decisionTrace]
  );
  const testCards = useMemo(
    () => model.options.map(createTestCard),
    [model.options]
  );
  const selectedCards = testCards.filter((card) =>
    model.selection.selectedOptionIds.includes(card.id)
  );
  const testPlan = buildTestPlan(selectedCards, actions);

  const updateAction = (cardId, action) => {
    setActions((current) => ({ ...current, [cardId]: action }));
  };

  return (
    <section className="dt-experience dt-evaluate" aria-labelledby="evaluate-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Laboratorio de pruebas con usuarios</span>
          <h2 id="evaluate-title">Decide la siguiente iteracion con evidencia</h2>
          <p>
            Revisa las senales disponibles, prioriza hallazgos y define que debe
            mantenerse, modificarse, eliminarse o volver a probarse.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={userTestingIllustration}
          alt="Pruebas de usuario y analisis de metricas"
        />
      </header>

      <section className="dt-continuity-brief">
        <span className="experience-eyebrow">Prototipo sometido a prueba</span>
        <p>{prototypeTrace?.selectedTexts?.join(" | ") || "No hay modulos registrados en el trazado."}</p>
      </section>

      <section className="dt-test-metrics" aria-labelledby="test-metrics-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Metricas actuales</span>
            <h3 id="test-metrics-title">Senales recibidas por la simulacion</h3>
          </div>
          <span>Los valores provienen del estado actual del intento</span>
        </div>
        {model.kpis.length > 0 ? (
          <div>
            {model.kpis.map((kpi) => (
              <article key={kpi.key}>
                <span>{kpi.label}</span>
                <strong>{kpi.value}{kpi.suffix}</strong>
              </article>
            ))}
          </div>
        ) : (
          <p className="dt-trace-empty">No hay KPIs configurados para este escenario.</p>
        )}
      </section>

      <section className="dt-test-results" aria-labelledby="test-results-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultados de prueba</span>
            <h3 id="test-results-title">Clasifica acciones para los hallazgos</h3>
          </div>
          <span>{selectedCards.length}/{model.selection.maxSelections} priorizados</span>
        </div>
        <div className="dt-test-card-grid">
          {testCards.map((card, index) => {
            const isSelected = model.selection.selectedOptionIds.includes(card.id);

            return (
              <article
                key={card.id}
                className={`dt-test-card ${isSelected ? "is-selected" : ""}`}
                style={{ "--card-index": index }}
              >
                <span>{card.lens}</span>
                <p>{card.text}</p>
                {card.tags.length > 0 && <small>Etiquetas: {card.tags.join(", ")}</small>}
                <label>
                  Accion propuesta
                  <select value={actions[card.id] || "Volver a probar"} onChange={(event) => updateAction(card.id, event.target.value)}>
                    {actionOptions.map((action) => <option key={action}>{action}</option>)}
                  </select>
                </label>
                <button type="button" aria-pressed={isSelected} onClick={() => onToggleOption(card.id)}>
                  {isSelected ? "Retirar hallazgo" : "Priorizar hallazgo"}
                </button>
              </article>
            );
          })}
        </div>
      </section>

      <section className="dt-test-plan">
        <div>
          <span className="experience-eyebrow">Plan de siguiente iteracion</span>
          <p>{testPlan}</p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={() => onTextAnswerChange(testPlan)}>
          Usar plan como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="evaluate-text-answer">Decision de iteracion</label>
        <textarea
          id="evaluate-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica que decision tomas a partir de las senales disponibles y como la comprobaras."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button type="button" className="experience-submit" onClick={onSubmit} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar aprendizaje y ver consecuencias"}
      </button>
    </section>
  );
}

export default EvaluateExperience;
