import { useMemo, useState } from "react";
import prototypeBuilderIllustration from "../../../../assets/methodologyExperience/prototype-builder.svg";
import ConceptModal from "./ConceptModal";
import {
  buildMvpSummary,
  createPrototypeModule,
  getEffectiveMvpLimit,
  getIdeaLevelLabel,
  getMvpLearningLabel,
  getMvpResourceSummary,
  getMvpScope,
  getTraceForPhase,
} from "./experienceHelpers";

function getPrioritizedIdeas(trace) {
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

function ModuleCard({ module, isSelected, index, onToggle }) {
  const contribution = module.learningItems.length > 0
    ? module.learningItems.join(", ")
    : "Por evaluar";
  const learningLabel = module.learningItems.length > 0
    ? module.learningItems.join(", ")
    : "Por evaluar";
  const impactLabel = module.impactLevel
    ? getIdeaLevelLabel(module.impactLevel, "impact")
    : "Por evaluar";
  const priorityLabel = module.viabilityLevel
    ? getIdeaLevelLabel(module.viabilityLevel, "viability")
    : "Por evaluar";

  return (
    <article
      className={`dt-module-card ${isSelected ? "is-selected" : ""}`}
      style={{ "--card-index": index }}
    >
      <div className="dt-module-card-heading">
        <span>{module.typeLabel}</span>
        {isSelected && <strong>En MVP</strong>}
      </div>
      <h4>{module.text}</h4>
      <dl>
        <div className="dt-module-detail-wide"><dt>Que valida</dt><dd>{module.validationFocus}</dd></div>
        <div><dt>Aporta</dt><dd>{contribution}</dd></div>
        <div><dt>Esfuerzo</dt><dd>{getIdeaLevelLabel(module.effortLevel, "effort")}</dd></div>
        <div><dt>Costo estimado</dt><dd>{module.hasCostEstimate ? `${module.cost} pts` : "Por definir"}</dd></div>
        <div><dt>Tiempo estimado</dt><dd>{module.hasTimeEstimate ? `${module.timeCost} sem` : "Por definir"}</dd></div>
      </dl>
      <div className="dt-module-evaluation">
        <span>Impacto esperado: <strong>{impactLabel}</strong></span>
        <span>Prioridad: <strong>{priorityLabel}</strong></span>
        <span>Aprendizaje esperado: <strong>{learningLabel}</strong></span>
      </div>
      <button type="button" aria-pressed={isSelected} onClick={() => onToggle(module)}>
        {isSelected ? "Quitar del MVP" : "Agregar al MVP"}
      </button>
    </article>
  );
}

function PrototypeExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const [isConceptModalOpen, setIsConceptModalOpen] = useState(false);
  const [interactionMessage, setInteractionMessage] = useState("");
  const ideationTrace = useMemo(
    () => getTraceForPhase(model.decisionTrace, "Idear"),
    [model.decisionTrace]
  );
  const modules = useMemo(
    () => model.options.map(createPrototypeModule),
    [model.options]
  );
  const selectedModules = modules.filter((module) =>
    model.selection.selectedOptionIds.includes(module.id)
  );
  const effectiveMax = getEffectiveMvpLimit(modules);
  const prioritizedIdeas = getPrioritizedIdeas(ideationTrace);
  const mvpSummary = buildMvpSummary(selectedModules);
  const scope = getMvpScope(selectedModules.length);
  const learningLabel = getMvpLearningLabel(selectedModules);
  const resourceSummary = getMvpResourceSummary(selectedModules);
  const isOverBudget = resourceSummary.cost !== null &&
    model.resources.remainingBudget > 0 &&
    resourceSummary.cost > model.resources.remainingBudget;
  const isOverTime = resourceSummary.time !== null &&
    model.resources.remainingTimeWeeks > 0 &&
    resourceSummary.time > model.resources.remainingTimeWeeks;
  const isOverbuilt = scope.tone === "overbuilt";

  const toggleModule = (module) => {
    const isSelected = model.selection.selectedOptionIds.includes(module.id);

    if (!isSelected && selectedModules.length >= effectiveMax) {
      setInteractionMessage(
        "Tu MVP supera el numero maximo de modulos permitidos. Quita un modulo para mantenerlo enfocado."
      );
      return;
    }

    setInteractionMessage("");
    onToggleOption(module.id);
  };

  const submitMvp = () => {
    if (selectedModules.length === 0) {
      setInteractionMessage("Agrega al menos un modulo al MVP antes de continuar.");
      return;
    }
    if (selectedModules.length > effectiveMax) {
      setInteractionMessage(
        "Tu MVP supera el numero maximo de modulos permitidos. Quita un modulo para mantenerlo enfocado."
      );
      return;
    }

    onSubmit();
  };

  return (
    <section className="dt-experience dt-prototype" aria-labelledby="prototype-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Constructor visual de MVP</span>
          <h2 id="prototype-title">Construye un MVP pequeno y comprobable</h2>
          <p>Tu objetivo es construir el MVP mas pequeno posible para aprender si la solucion propuesta tiene sentido.</p>
        </div>
        <img
          className="dt-phase-illustration"
          src={prototypeBuilderIllustration}
          alt="Constructor visual de un prototipo minimo"
        />
      </header>

      <section className="dt-prototype-action-guide" aria-labelledby="prototype-action-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="prototype-action-guide-title">Construye solo lo necesario para aprender</h3>
          <p>En esta fase debes construir una version minima de la solucion. Selecciona solo los modulos necesarios para validar la idea priorizada en Idear. No intentes construir todo: el objetivo del MVP es aprender rapido con el menor esfuerzo posible.</p>
        </div>
        <ol>
          <li>Revisa las ideas que llegan de Idear.</li>
          <li>Selecciona pocos modulos para tu MVP.</li>
          <li>Evita funcionalidades innecesarias.</li>
          <li>Revisa presupuesto, tiempo y riesgo.</li>
          <li>Observa como se arma la vista previa del MVP.</li>
          <li>Justifica que hipotesis valida tu prototipo.</li>
        </ol>
      </section>

      <section className="dt-mvp-explainer">
        <div>
          <span className="experience-eyebrow">Concepto clave</span>
          <p>Un MVP no es un producto incompleto o de mala calidad. Es la version minima necesaria para comprobar una hipotesis y aprender antes de invertir mas recursos.</p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={() => setIsConceptModalOpen(true)}>
          Ver conceptos
        </button>
      </section>

      <section className="dt-prototype-continuity" aria-labelledby="prototype-continuity-title">
        <div>
          <span className="experience-eyebrow">Cartera seleccionada para prototipar</span>
          <h3 id="prototype-continuity-title">Ideas priorizadas en la fase anterior</h3>
          <p>Estas ideas orientan el MVP. No necesitas construirlas completas; selecciona los modulos minimos para probarlas.</p>
        </div>
        {prioritizedIdeas.length > 0 ? (
          <div className="dt-prioritized-idea-list" aria-label="Ideas priorizadas en Idear">
            {prioritizedIdeas.map((idea) => <span key={idea}>{idea}</span>)}
          </div>
        ) : (
          <p className="dt-continuity-empty">Usa el problema del caso como punto de partida para construir una primera prueba.</p>
        )}
        {model.scenario.problem && (
          <p className="dt-prototype-problem"><strong>Problema a resolver:</strong> {model.scenario.problem}</p>
        )}
      </section>

      <div className="dt-prototype-layout">
        <section className="dt-module-catalog" aria-labelledby="module-title">
          <div className="dt-panel-heading">
            <div>
              <span className="experience-eyebrow">Catalogo de modulos</span>
              <h3 id="module-title">Elige las piezas minimas de tu MVP</h3>
            </div>
          </div>
          <div className="dt-module-selection-status" aria-live="polite">
            <strong>Has seleccionado {selectedModules.length} de {effectiveMax} modulos para tu MVP.</strong>
            <span>Selecciona pocos modulos. Un MVP debe ser suficiente para aprender, no una version completa del producto.</span>
          </div>
          {modules.length > 0 ? (
            <div className="dt-module-grid">
              {modules.map((module, index) => (
                <ModuleCard
                  key={module.id}
                  module={module}
                  index={index}
                  isSelected={model.selection.selectedOptionIds.includes(module.id)}
                  onToggle={toggleModule}
                />
              ))}
            </div>
          ) : (
            <p className="dt-module-empty">No se encontraron modulos configurados para esta fase. Se usara la experiencia generica para que puedas continuar.</p>
          )}
        </section>

        <aside className="dt-mvp-canvas" aria-labelledby="mvp-canvas-title">
          <div className="dt-mvp-canvas-header">
            <span id="mvp-canvas-title">Vista previa del MVP</span>
            <strong>{selectedModules.length} modulo{selectedModules.length === 1 ? "" : "s"}</strong>
          </div>
          <div className="dt-mvp-screen" aria-live="polite">
            {selectedModules.length > 0 ? (
              <div className="dt-mvp-flow">
                <div className="dt-mvp-flow-start">Inicio</div>
                {selectedModules.map((module) => (
                  <div key={module.id} className="dt-mvp-flow-step">
                    <span className="dt-mvp-flow-connector" aria-hidden="true" />
                    <div className="dt-mvp-block">
                      <small>{module.typeLabel}</small>
                      <strong>{module.text}</strong>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="dt-mvp-empty-state">
                <strong>Tu MVP aun esta vacio.</strong>
                <p>Agrega modulos desde el catalogo para construir una primera version comprobable.</p>
              </div>
            )}
          </div>
          <dl className="dt-mvp-canvas-summary">
            <div><dt>Costo estimado</dt><dd>{resourceSummary.cost === null ? "Por definir" : `${resourceSummary.cost} pts`}</dd></div>
            <div><dt>Tiempo estimado</dt><dd>{resourceSummary.time === null ? "Por definir" : `${resourceSummary.time} sem`}</dd></div>
            <div><dt>Riesgo acumulado</dt><dd>{resourceSummary.risk > 0 ? "+" : ""}{resourceSummary.risk}</dd></div>
          </dl>
        </aside>
      </div>

      <section className={`dt-mvp-scope is-${scope.tone}`} aria-live="polite">
        <div>
          <span className="experience-eyebrow">Alcance del MVP</span>
          <strong>{scope.label}</strong>
        </div>
        <p>{scope.description}</p>
      </section>

      {(isOverBudget || isOverTime || isOverbuilt) && (
        <div className="dt-scope-alert" role="status">
          {isOverBudget && "La seleccion excede el presupuesto disponible. "}
          {isOverTime && "La seleccion excede el tiempo disponible. "}
          {isOverbuilt && "Cuidado: este MVP puede volverse demasiado amplio para una primera prueba. Recuerda que el objetivo es aprender, no construir todo. "}
          {!isOverbuilt && "Quita modulos antes de enviar la fase."}
        </div>
      )}

      <section className="dt-mvp-summary" aria-labelledby="mvp-summary-title" aria-live="polite">
        <div>
          <span className="experience-eyebrow">Resumen del MVP</span>
          <h3 id="mvp-summary-title">Una version pequena para aprender</h3>
          <p>{mvpSummary}</p>
          <small>{learningLabel}</small>
          <dl className="dt-mvp-summary-metrics">
            <div><dt>Modulos</dt><dd>{selectedModules.length}</dd></div>
            <div><dt>Costo estimado</dt><dd>{resourceSummary.cost === null ? "Por definir" : `${resourceSummary.cost} pts`}</dd></div>
            <div><dt>Tiempo estimado</dt><dd>{resourceSummary.time === null ? "Por definir" : `${resourceSummary.time} sem`}</dd></div>
            <div><dt>Riesgo acumulado</dt><dd>{resourceSummary.risk > 0 ? "+" : ""}{resourceSummary.risk}</dd></div>
            <div><dt>Alcance</dt><dd>{scope.label}</dd></div>
          </dl>
        </div>
        <button type="button" className="dt-secondary-action" onClick={() => onTextAnswerChange(mvpSummary)} disabled={selectedModules.length === 0}>
          Usar resumen como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="prototype-text-answer">Justificacion del MVP</label>
        <p id="prototype-text-answer-help" className="dt-text-answer-help">Explica que quieres comprobar con este MVP. Menciona por que los modulos seleccionados son suficientes para validar la idea sin construir el producto completo.</p>
        <textarea
          id="prototype-text-answer"
          aria-describedby="prototype-text-answer-help"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Este MVP permite comprobar si mostrar costos claros, reducir pasos y confirmar la accion aumenta la confianza del usuario y disminuye el abandono antes de completar el proceso."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dt-interaction-message" role="status">{interactionMessage}</p>}
      <button type="button" className="experience-submit" onClick={submitMvp} disabled={submitting || modules.length === 0}>
        {submitting ? "Evaluando fase..." : "Enviar MVP y ver consecuencias"}
      </button>

      <ConceptModal isOpen={isConceptModalOpen} onClose={() => setIsConceptModalOpen(false)} />
    </section>
  );
}

export default PrototypeExperience;
