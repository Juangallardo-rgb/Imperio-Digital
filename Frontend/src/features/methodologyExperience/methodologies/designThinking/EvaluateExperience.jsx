import { useMemo, useState } from "react";
import userTestingIllustration from "../../../../assets/methodologyExperience/user-testing.svg";
import {
  buildTestPlan,
  createTestCard,
  getEffectiveTestLimit,
  getKpiSignal,
  getTraceForPhase,
  groupTestPlan,
  ITERATION_ACTIONS,
} from "./experienceHelpers";

const actionDescriptions = {
  Mantener: "Conservar algo que funciono bien.",
  Modificar: "Ajustar algo que tiene potencial, pero necesita mejora.",
  Eliminar: "Quitar algo que no aporta valor o genera friccion.",
  "Volver a probar": "Repetir la validacion porque la evidencia aun no es suficiente.",
};

function getPrototypeItems(trace) {
  const selectedTexts = Array.isArray(trace?.selectedTexts)
    ? trace.selectedTexts
    : [];

  return [...new Set(
    selectedTexts
      .flatMap((text) => String(text || "").split("|"))
      .map((text) => text.trim())
      .filter(Boolean)
  )];
}

function MetricSignal({ kpi }) {
  const signal = getKpiSignal(kpi);

  return (
    <article className={`dt-test-metric-card is-${signal.tone}`}>
      <div>
        <span>{kpi.label}</span>
        <strong>{kpi.value}{kpi.suffix}</strong>
      </div>
      <p><b>{signal.label}</b>{signal.description}</p>
    </article>
  );
}

function FindingCard({ card, action, index, isSelected, onActionChange, onToggle }) {
  return (
    <article
      className={`dt-test-card ${isSelected ? "is-selected" : ""}`}
      style={{ "--card-index": index }}
    >
      <div className="dt-test-card-heading">
        <span>{card.lens}</span>
        {isSelected && <strong>En el plan</strong>}
      </div>
      <h4>Senal observada</h4>
      <p>{card.text}</p>
      <p className="dt-test-interpretation"><strong>Interpretacion:</strong> {card.interpretation}</p>
      <label htmlFor={`test-action-${card.id}`}>
        Que haras con este hallazgo
        <select
          id={`test-action-${card.id}`}
          value={action}
          onChange={(event) => onActionChange(card.id, event.target.value)}
        >
          {ITERATION_ACTIONS.map((option) => <option key={option}>{option}</option>)}
        </select>
      </label>
      <button type="button" aria-pressed={isSelected} onClick={() => onToggle(card)}>
        {isSelected ? "Quitar del plan" : "Agregar al plan"}
      </button>
    </article>
  );
}

function EvaluateExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const [actions, setActions] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
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
  const effectiveMax = getEffectiveTestLimit(
    testCards,
    model.selection.maxSelections
  );
  const prototypeItems = getPrototypeItems(prototypeTrace);
  const groupedPlan = groupTestPlan(selectedCards, actions);
  const testPlan = buildTestPlan(selectedCards, actions);

  const updateAction = (cardId, action) => {
    setActions((current) => ({ ...current, [cardId]: action }));
    setInteractionMessage("La accion se actualizo en el plan de siguiente iteracion.");
  };

  const toggleFinding = (card) => {
    const isSelected = model.selection.selectedOptionIds.includes(card.id);

    if (!isSelected && selectedCards.length >= effectiveMax) {
      setInteractionMessage(
        "Tu plan supera el numero maximo de hallazgos permitidos. Quita un hallazgo para mantenerlo enfocado."
      );
      return;
    }

    setInteractionMessage("");
    onToggleOption(card.id);
  };

  const submitIteration = () => {
    if (testCards.length > 0 && selectedCards.length === 0) {
      setInteractionMessage("Agrega al menos un hallazgo al plan de siguiente iteracion antes de continuar.");
      return;
    }
    if (selectedCards.length > effectiveMax) {
      setInteractionMessage(
        "Tu plan supera el numero maximo de hallazgos permitidos. Quita un hallazgo para mantenerlo enfocado."
      );
      return;
    }

    onSubmit();
  };

  return (
    <section className="dt-experience dt-evaluate" aria-labelledby="evaluate-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Laboratorio de pruebas con usuarios</span>
          <h2 id="evaluate-title">Interpreta senales y decide la siguiente iteracion</h2>
          <p>Tu objetivo es interpretar las senales del MVP y decidir la siguiente iteracion con evidencia.</p>
        </div>
        <img
          className="dt-phase-illustration"
          src={userTestingIllustration}
          alt="Pruebas de usuario y analisis de metricas"
        />
      </header>

      <section className="dt-evaluate-action-guide" aria-labelledby="evaluate-action-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="evaluate-action-guide-title">Convierte las senales en una siguiente iteracion</h3>
          <p>En esta fase debes revisar las senales obtenidas al probar el MVP. Analiza las metricas y hallazgos, decide que se debe mantener, modificar, eliminar o volver a probar, y construye un plan para la siguiente iteracion.</p>
        </div>
        <ol>
          <li>Revisa el MVP que fue sometido a prueba.</li>
          <li>Interpreta las metricas disponibles.</li>
          <li>Lee cada hallazgo de prueba.</li>
          <li>Decide una accion para cada hallazgo.</li>
          <li>Agrega los hallazgos clave al plan.</li>
          <li>Justifica tu decision con evidencia.</li>
        </ol>
      </section>

      <section className="dt-evaluate-prototype" aria-labelledby="evaluate-prototype-title">
        <div>
          <span className="experience-eyebrow">Prototipo que vas a evaluar</span>
          <h3 id="evaluate-prototype-title">Recorrido previo de la solucion</h3>
          <p>Este es el MVP construido en la fase anterior. Ahora debes interpretar sus senales de prueba y decidir la siguiente iteracion.</p>
        </div>
        {prototypeItems.length > 0 ? (
          <div className="dt-evaluated-module-list" aria-label="Modulos del MVP evaluado">
            {prototypeItems.map((item, index) => <span key={`${item}-${index}`}>{item}</span>)}
          </div>
        ) : (
          <p className="dt-trace-empty">No se encontro un MVP detallado de la fase anterior. Usa las senales disponibles para definir la siguiente iteracion.</p>
        )}
        {model.scenario.problem && (
          <p className="dt-evaluate-problem"><strong>Problema definido:</strong> {model.scenario.problem}</p>
        )}
      </section>

      <section className="dt-test-metrics" aria-labelledby="test-metrics-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Metricas como senales</span>
            <h3 id="test-metrics-title">Que te dicen los resultados de la prueba</h3>
          </div>
          <span>Usa estas senales para orientar tu siguiente iteracion.</span>
        </div>
        {model.kpis.length > 0 ? (
          <div className="dt-test-metric-grid">
            {model.kpis.map((kpi) => <MetricSignal key={kpi.key} kpi={kpi} />)}
          </div>
        ) : (
          <p className="dt-trace-empty">No hay metricas disponibles para este escenario. Usa los hallazgos de prueba como evidencia.</p>
        )}
      </section>

      <section className="dt-iteration-actions" aria-labelledby="iteration-actions-title">
        <div>
          <span className="experience-eyebrow">Acciones de iteracion</span>
          <h3 id="iteration-actions-title">Elige una accion concreta para cada hallazgo</h3>
        </div>
        <div>
          {ITERATION_ACTIONS.map((action) => (
            <article key={action}>
              <strong>{action}</strong>
              <span>{actionDescriptions[action]}</span>
            </article>
          ))}
        </div>
      </section>

      <section className="dt-test-results" aria-labelledby="test-results-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Resultados de prueba</span>
            <h3 id="test-results-title">Agrega hallazgos al plan de siguiente iteracion</h3>
          </div>
        </div>
        <p className="dt-test-selection-count" aria-live="polite">Has agregado {selectedCards.length} de {effectiveMax} hallazgo{effectiveMax === 1 ? "" : "s"} al plan.</p>
        {testCards.length > 0 ? (
          <div className="dt-test-card-grid">
            {testCards.map((card, index) => (
              <FindingCard
                key={card.id}
                card={card}
                index={index}
                action={actions[card.id] || "Volver a probar"}
                isSelected={model.selection.selectedOptionIds.includes(card.id)}
                onActionChange={updateAction}
                onToggle={toggleFinding}
              />
            ))}
          </div>
        ) : (
          <p className="dt-trace-empty">No se encontraron hallazgos configurados para esta fase. Se usara la experiencia generica para que puedas finalizar la simulacion.</p>
        )}
      </section>

      <section className="dt-iteration-plan" aria-labelledby="iteration-plan-title" aria-live="polite">
        <div className="dt-iteration-plan-heading">
          <div>
            <span className="experience-eyebrow">Plan de siguiente iteracion</span>
            <h3 id="iteration-plan-title">Acciones que llevaras a la siguiente prueba</h3>
          </div>
          <button type="button" className="dt-secondary-action" onClick={() => onTextAnswerChange(testPlan)} disabled={selectedCards.length === 0}>
            Usar plan como borrador
          </button>
        </div>
        {selectedCards.length > 0 ? (
          <div className="dt-iteration-plan-grid">
            {ITERATION_ACTIONS.map((action) => (
              <article key={action} className={groupedPlan[action].length > 0 ? "has-items" : ""}>
                <h4>{action}</h4>
                {groupedPlan[action].length > 0 ? (
                  <ul>{groupedPlan[action].map((card) => <li key={card.id}>{card.text}</li>)}</ul>
                ) : <p>Sin hallazgos en esta accion.</p>}
              </article>
            ))}
          </div>
        ) : (
          <p className="dt-iteration-plan-empty">Aun no has agregado hallazgos al plan. Elige una accion para los resultados de prueba mas importantes y agregalos a la siguiente iteracion.</p>
        )}
      </section>

      <div className="experience-text-answer">
        <label htmlFor="evaluate-text-answer">Justificacion de la siguiente iteracion</label>
        <p id="evaluate-text-answer-help" className="dt-text-answer-help">Explica que cambios haras despues de evaluar el MVP. Usa las metricas y hallazgos para justificar que mantendras, modificaras, eliminaras o volveras a probar.</p>
        <textarea
          id="evaluate-text-answer"
          aria-describedby="evaluate-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Mantendria los elementos que aumentaron la confianza del usuario, modificaria la forma en que se muestran los costos y volveria a probar el flujo con menos pasos para verificar si disminuye el abandono."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dt-interaction-message" role="status">{interactionMessage}</p>}
      <button type="button" className="experience-submit" onClick={submitIteration} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar aprendizaje y ver consecuencias"}
      </button>
    </section>
  );
}

export default EvaluateExperience;
