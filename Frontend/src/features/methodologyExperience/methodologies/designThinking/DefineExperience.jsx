import { useMemo, useState } from "react";
import problemDefinitionIllustration from "../../../../assets/methodologyExperience/problem-definition.svg";
import {
  buildProblemPreview,
  getDefinitionCue,
  getTraceForPhase,
} from "./experienceHelpers";

function DefineExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const empathyTrace = useMemo(
    () => getTraceForPhase(model.decisionTrace, "Empatizar"),
    [model.decisionTrace]
  );
  const evidenceTexts = empathyTrace?.selectedTexts || [];
  const [userSegment, setUserSegment] = useState(model.scenario.targetUser || "");
  const [need, setNeed] = useState(evidenceTexts[0] || model.scenario.problem || "");
  const [insight, setInsight] = useState(evidenceTexts[1] || evidenceTexts[0] || "");
  const preview = buildProblemPreview({ userSegment, need, insight });

  const usePreviewAsDraft = () => onTextAnswerChange(preview);

  return (
    <section className="dt-experience dt-define" aria-labelledby="define-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Laboratorio de definicion del problema</span>
          <h2 id="define-title">Transforma evidencia en enfoque</h2>
          <p>
            Conecta el usuario, su necesidad y un insight verificable antes de
            discutir una solucion.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={problemDefinitionIllustration}
          alt="Sintesis de evidencia para definir un problema"
        />
      </header>

      <section className="dt-define-evidence" aria-labelledby="define-evidence-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Continuidad de Empatizar</span>
            <h3 id="define-evidence-title">Hallazgos que respaldan la definicion</h3>
          </div>
          <span>{evidenceTexts.length} evidencia(s) disponible(s)</span>
        </div>
        {evidenceTexts.length > 0 ? (
          <div className="dt-trace-list">
            {evidenceTexts.map((text, index) => <p key={`${index}-${text}`}>{text}</p>)}
          </div>
        ) : (
          <p className="dt-trace-empty">
            No hay evidencia registrada en el trazado. Usa el contexto del caso y
            documenta una definicion que pueda validarse.
          </p>
        )}
      </section>

      <div className="dt-define-layout">
        <section className="dt-problem-builder" aria-labelledby="problem-builder-title">
          <span className="experience-eyebrow">Constructor visual</span>
          <h3 id="problem-builder-title">Enunciado de problema</h3>
          <div className="dt-problem-formula" aria-live="polite">
            <span>{userSegment || "Usuario"}</span>
            <b>necesita</b>
            <span>{need || "Necesidad"}</span>
            <b>porque</b>
            <span>{insight || "Insight respaldado por evidencia"}</span>
          </div>

          <div className="dt-builder-fields">
            <label>
              Usuario o segmento
              <input
                value={userSegment}
                onChange={(event) => setUserSegment(event.target.value)}
                placeholder="Describe el usuario afectado"
              />
            </label>
            <label>
              Necesidad a resolver
              <input
                value={need}
                onChange={(event) => setNeed(event.target.value)}
                list="define-evidence-options"
                placeholder="Selecciona o escribe una necesidad"
              />
            </label>
            <label>
              Insight o evidencia
              <input
                value={insight}
                onChange={(event) => setInsight(event.target.value)}
                list="define-evidence-options"
                placeholder="Selecciona o escribe un insight"
              />
            </label>
          </div>
          <datalist id="define-evidence-options">
            {evidenceTexts.map((text) => <option key={text} value={text} />)}
          </datalist>

          <div className="dt-problem-preview">
            <span>Vista previa</span>
            <p>{preview}</p>
            <button type="button" className="dt-secondary-action" onClick={usePreviewAsDraft}>
              Usar como borrador
            </button>
          </div>
        </section>

        <aside className="dt-definition-guide" aria-label="Guia pedagogica">
          <span className="experience-eyebrow">Guia de lectura</span>
          <h3>Del sintoma al problema</h3>
          <dl>
            <div><dt>Sintoma</dt><dd>Describe una manifestacion visible del caso.</dd></div>
            <div><dt>Solucion anticipada</dt><dd>Propone una respuesta antes de comprender la causa.</dd></div>
            <div><dt>Problema</dt><dd>Conecta usuario, necesidad e insight verificable.</dd></div>
          </dl>
          <p>Las pistas son orientacion pedagogica y no revelan la evaluacion de la fase.</p>
        </aside>
      </div>

      <section className="dt-candidate-section" aria-labelledby="candidate-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Formulaciones candidatas</span>
            <h3 id="candidate-title">Selecciona la formulacion que defenderas</h3>
          </div>
          <span>Maximo {model.selection.maxSelections} selecciones</span>
        </div>
        <div className="dt-candidate-grid">
          {model.options.map((option, index) => {
            const isSelected = model.selection.selectedOptionIds.includes(option.id);

            return (
              <article
                key={option.id}
                className={`dt-candidate-card ${isSelected ? "is-selected" : ""}`}
                style={{ "--card-index": index }}
              >
                <span>{option.optionType || "Formulacion"}</span>
                <p>{option.text}</p>
                <small>{getDefinitionCue(option.text)}</small>
                <button
                  type="button"
                  aria-pressed={isSelected}
                  onClick={() => onToggleOption(option.id)}
                >
                  {isSelected ? "Retirar formulacion" : "Seleccionar formulacion"}
                </button>
              </article>
            );
          })}
        </div>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="define-text-answer">Justificacion de la definicion</label>
        <textarea
          id="define-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica como la formulacion elegida se respalda en evidencia y evita anticipar una solucion."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button
        type="button"
        className="experience-submit"
        onClick={onSubmit}
        disabled={submitting}
      >
        {submitting ? "Evaluando fase..." : "Enviar definicion y ver consecuencias"}
      </button>
    </section>
  );
}

export default DefineExperience;
