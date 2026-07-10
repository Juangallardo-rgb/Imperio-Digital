import { useMemo, useState } from "react";
import prototypeBuilderIllustration from "../../../../assets/methodologyExperience/prototype-builder.svg";
import ConceptModal from "./ConceptModal";
import {
  buildMvpSummary,
  createPrototypeModule,
  getTraceForPhase,
} from "./experienceHelpers";

function PrototypeExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const [isConceptModalOpen, setIsConceptModalOpen] = useState(false);
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
  const mvpSummary = buildMvpSummary(selectedModules);
  const isOverBudget = model.selection.totals.cost > model.resources.remainingBudget;
  const isOverTime = model.selection.totals.time > model.resources.remainingTimeWeeks;

  return (
    <section className="dt-experience dt-prototype" aria-labelledby="prototype-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Constructor de prototipo minimo</span>
          <h2 id="prototype-title">Construye solo lo necesario para aprender</h2>
          <p>
            Agrega modulos reales del escenario al MVP. El presupuesto, el tiempo y
            el riesgo se calculan con los valores recibidos por la simulacion.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={prototypeBuilderIllustration}
          alt="Constructor visual de un prototipo minimo"
        />
      </header>

      <section className="dt-mvp-explainer">
        <div>
          <span className="experience-eyebrow">Concepto clave</span>
          <p>
            Un MVP no es un producto incompleto o de mala calidad. Es la solucion
            minima necesaria para validar una hipotesis y obtener aprendizaje.
          </p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={() => setIsConceptModalOpen(true)}>
          Ver conceptos
        </button>
      </section>

      <section className="dt-continuity-brief">
        <span className="experience-eyebrow">Cartera que llega de Idear</span>
        <p>{ideationTrace?.selectedTexts?.join(" | ") || "No hay ideas registradas en el trazado."}</p>
      </section>

      <div className="dt-prototype-layout">
        <section className="dt-module-catalog" aria-labelledby="module-title">
          <div className="dt-panel-heading">
            <div>
              <span className="experience-eyebrow">Catalogo de modulos</span>
              <h3 id="module-title">Funcionalidades disponibles</h3>
            </div>
            <span>{selectedModules.length}/{model.selection.maxSelections} seleccionadas</span>
          </div>
          <div className="dt-module-grid">
            {modules.map((module, index) => {
              const isSelected = model.selection.selectedOptionIds.includes(module.id);

              return (
                <article
                  key={module.id}
                  className={`dt-module-card ${isSelected ? "is-selected" : ""}`}
                  style={{ "--card-index": index }}
                >
                  <span>{module.optionType}</span>
                  <h4>{module.text}</h4>
                  <dl>
                    <div><dt>Costo</dt><dd>{module.cost} pts</dd></div>
                    <div><dt>Tiempo</dt><dd>{module.timeCost} sem</dd></div>
                    <div><dt>Riesgo</dt><dd>{module.riskImpact > 0 ? "+" : ""}{module.riskImpact}</dd></div>
                    <div><dt>Impacto</dt><dd>{module.expectedImpactLevel || "No configurado"}</dd></div>
                    <div><dt>Prioridad</dt><dd>{module.expectedViabilityLevel || "No configurada"}</dd></div>
                  </dl>
                  <p>
                    Aprendizaje: {module.tags.length > 0
                      ? module.tags.join(", ")
                      : module.impactKeys.length > 0
                      ? module.impactKeys.join(", ")
                      : "No especificado"}
                  </p>
                  <button type="button" aria-pressed={isSelected} onClick={() => onToggleOption(module.id)}>
                    {isSelected ? "Quitar del MVP" : "Agregar al MVP"}
                  </button>
                </article>
              );
            })}
          </div>
        </section>

        <aside className="dt-mvp-canvas" aria-label="Vista previa del MVP">
          <div className="dt-mvp-canvas-header">
            <span>Vista previa del MVP</span>
            <strong>{selectedModules.length} modulo(s)</strong>
          </div>
          <div className="dt-mvp-screen">
            <span className="dt-mvp-screen-title">Prototipo en construccion</span>
            {selectedModules.length > 0 ? (
              selectedModules.map((module) => (
                <div key={module.id} className="dt-mvp-block">{module.text}</div>
              ))
            ) : (
              <p>Selecciona modulos del catalogo para componer el MVP.</p>
            )}
          </div>
          <div className="dt-mvp-canvas-summary">
            <span>Presupuesto de la seleccion</span>
            <strong>{model.selection.totals.cost} pts</strong>
            <span>Tiempo de la seleccion</span>
            <strong>{model.selection.totals.time} sem</strong>
          </div>
        </aside>
      </div>

      {(isOverBudget || isOverTime) && (
        <div className="dt-scope-alert" role="status">
          {isOverBudget && "La seleccion excede el presupuesto disponible. "}
          {isOverTime && "La seleccion excede el tiempo disponible. "}
          Retira modulos antes de enviar la fase.
        </div>
      )}

      <section className="dt-mvp-summary">
        <div>
          <span className="experience-eyebrow">Resumen del MVP</span>
          <p>{mvpSummary}</p>
        </div>
        <button type="button" className="dt-secondary-action" onClick={() => onTextAnswerChange(mvpSummary)}>
          Usar resumen como borrador
        </button>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="prototype-text-answer">Justificacion del MVP</label>
        <textarea
          id="prototype-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica que hipotesis permite validar este MVP y por que los modulos seleccionados son suficientes."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button type="button" className="experience-submit" onClick={onSubmit} disabled={submitting}>
        {submitting ? "Evaluando fase..." : "Enviar MVP y ver consecuencias"}
      </button>

      <ConceptModal isOpen={isConceptModalOpen} onClose={() => setIsConceptModalOpen(false)} />
    </section>
  );
}

export default PrototypeExperience;
