import { useMemo, useState } from "react";
import problemDefinitionIllustration from "../../../../assets/methodologyExperience/problem-definition.svg";
import {
  buildDefinitionDraft,
  buildEmpathyCounts,
  buildProblemPreview,
  createEvidenceCard,
  getDefinitionCue,
  getEffectiveDefinitionLimit,
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
  const evidenceTexts = useMemo(
    () => Array.isArray(empathyTrace?.selectedTexts) ? empathyTrace.selectedTexts : [],
    [empathyTrace]
  );
  const inheritedCards = evidenceTexts.map((text, index) => createEvidenceCard({
      id: index + 1,
      text,
      tags: empathyTrace?.tags || [],
    }));
  const inheritedCounts = buildEmpathyCounts(inheritedCards, {});
  const [userSegment, setUserSegment] = useState(
    model.scenario.targetUser || ""
  );
  const [need, setNeed] = useState("");
  const [insight, setInsight] = useState(evidenceTexts[0] || "");
  const [interactionMessage, setInteractionMessage] = useState("");
  const selectedDefinitions = model.options.filter((option) =>
    model.selection.selectedOptionIds.includes(option.id)
  );
  const effectiveMax = getEffectiveDefinitionLimit(model.options);
  const preview = buildProblemPreview({ userSegment, need, insight });
  const draft = buildDefinitionDraft({ userSegment, need, insight });
  const hasCompletePreview = Boolean(draft);
  const researchExample = evidenceTexts[0] || model.scenario.problem ||
    "El proceso actual genera friccion para las personas usuarias.";
  const selectionGuidance = effectiveMax === 1
    ? "Selecciona 1 definicion principal."
    : `Puedes comparar hasta ${effectiveMax} formulaciones, pero tu justificacion debe defender una como problema central.`;

  const usePreviewAsDraft = () => {
    if (!draft) {
      setInteractionMessage(
        "Completa el usuario, la necesidad y la evidencia antes de crear el borrador."
      );
      return;
    }

    onTextAnswerChange(draft);
    setInteractionMessage("La vista previa se copio como borrador de justificacion.");
  };

  const toggleDefinition = (option) => {
    const isSelected = model.selection.selectedOptionIds.includes(option.id);

    if (!isSelected && selectedDefinitions.length >= effectiveMax) {
      setInteractionMessage(
        effectiveMax === 1
          ? "Esta fase se enfoca en una definicion principal. Quita la seleccion actual para defender otra."
          : `Puedes comparar hasta ${effectiveMax} formulaciones en esta fase.`
      );
      return;
    }

    onToggleOption(option.id);
    setInteractionMessage(
      isSelected
        ? "La definicion se quito de tu seleccion."
        : "La definicion se agrego para que puedas defenderla."
    );
  };

  const submitDefinition = () => {
    if (selectedDefinitions.length === 0) {
      setInteractionMessage(
        "Selecciona la definicion de problema que defenderas antes de continuar."
      );
      return;
    }

    onSubmit();
  };

  return (
    <section className="dt-experience dt-define" aria-labelledby="define-title">
      <header className="dt-phase-intro dt-define-intro">
        <div>
          <span className="experience-eyebrow">Laboratorio de definicion del problema</span>
          <h2 id="define-title">Transforma evidencia en un problema claro</h2>
          <p>
            Debes conectar usuario, necesidad e insight verificable. Todavia no
            propongas soluciones: primero define correctamente el problema.
          </p>
        </div>
        <img
          className="dt-phase-illustration"
          src={problemDefinitionIllustration}
          alt="Sintesis de evidencia para definir un problema"
        />
      </header>

      <section className="dt-define-action-guide" aria-labelledby="define-action-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="define-action-guide-title">Convierte evidencia en enfoque</h3>
          <p>
            Identifica el usuario afectado, la necesidad principal y la evidencia
            que la respalda. No saltes todavia a una solucion.
          </p>
        </div>
        <ol>
          <li>Revisa los hallazgos de Empatizar.</li>
          <li>Identifica el usuario o segmento afectado.</li>
          <li>Define la necesidad principal.</li>
          <li>Conectala con una evidencia verificable.</li>
          <li>Defiende la formulacion mas clara.</li>
        </ol>
      </section>

      <section className="dt-define-continuity" aria-labelledby="define-evidence-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">De Empatizar a Definir</span>
            <h3 id="define-evidence-title">Hallazgos heredados de Empatizar</h3>
            <p>
              Estos hallazgos son insumos para definir el problema. Revisalos
              criticamente: no todos tienen el mismo valor.
            </p>
          </div>
          <span>{evidenceTexts.length} hallazgo{evidenceTexts.length === 1 ? "" : "s"} disponible{evidenceTexts.length === 1 ? "" : "s"}</span>
        </div>

        {evidenceTexts.length > 0 ? (
          <>
            <dl className="dt-inherited-counts">
              <div><dt>Dolores detectados</dt><dd>{inheritedCounts.pain}</dd></div>
              <div><dt>Necesidades identificadas</dt><dd>{inheritedCounts.need}</dd></div>
              <div><dt>Evidencias de apoyo</dt><dd>{inheritedCounts.evidence}</dd></div>
            </dl>
            <div className="dt-inherited-chip-list" aria-label="Hallazgos heredados">
              {evidenceTexts.map((text, index) => <span key={`${index}-${text}`}>{text}</span>)}
            </div>
          </>
        ) : (
          <p className="dt-trace-empty">
            No hay hallazgos previos suficientes. Usa el contexto del caso y las
            formulaciones disponibles para construir una definicion centrada en el usuario.
          </p>
        )}
      </section>

      <section className="dt-no-solution-warning" aria-label="Advertencia metodologica">
        <strong>Evita saltar directamente a soluciones.</strong>
        <span>Una buena definicion no dice que construir; explica que problema debe resolverse y por que.</span>
      </section>

      <div className="dt-define-layout">
        <section className="dt-problem-builder" aria-labelledby="problem-builder-title">
          <span className="experience-eyebrow">Constructor visual</span>
          <h3 id="problem-builder-title">Construye tu enunciado del problema</h3>
          <p className="dt-builder-intro">Usuario o segmento necesita una necesidad principal porque existe un insight o evidencia verificable.</p>

          <div className="dt-builder-fields">
            <label htmlFor="define-user-segment">
              Usuario o segmento
              <input
                id="define-user-segment"
                value={userSegment}
                onChange={(event) => setUserSegment(event.target.value)}
                placeholder="Describe el usuario afectado"
              />
            </label>
            <label htmlFor="define-need">
              Necesidad principal
              <input
                id="define-need"
                value={need}
                onChange={(event) => setNeed(event.target.value)}
                placeholder="Ejemplo: un proceso claro y confiable"
              />
            </label>
            <label htmlFor="define-insight">
              Insight o evidencia
              <input
                id="define-insight"
                value={insight}
                onChange={(event) => setInsight(event.target.value)}
                list="define-evidence-options"
                placeholder="Selecciona o escribe una evidencia"
              />
            </label>
          </div>
          <datalist id="define-evidence-options">
            {evidenceTexts.map((text) => <option key={text} value={text} />)}
          </datalist>

          <div className="dt-problem-preview" aria-live="polite">
            <span>Vista previa del problema</span>
            <p>{preview}</p>
            <button
              type="button"
              className="dt-secondary-action"
              onClick={usePreviewAsDraft}
              disabled={!hasCompletePreview}
            >
              Usar como borrador
            </button>
          </div>
        </section>

        <aside className="dt-definition-guide" aria-labelledby="definition-guide-title">
          <span className="experience-eyebrow">Guia de lectura</span>
          <h3 id="definition-guide-title">Del sintoma al problema</h3>
          <dl>
            <div>
              <dt>Sintoma</dt>
              <dd>Manifestacion visible del caso. Ejemplo: {researchExample}</dd>
            </div>
            <div>
              <dt>Solucion anticipada</dt>
              <dd>Crear una app o cambiar el diseno sin comprender primero la causa.</dd>
            </div>
            <div>
              <dt>Problema</dt>
              <dd>Conecta usuario, necesidad e insight verificable antes de idear soluciones.</dd>
            </div>
          </dl>
          <p>Esta guia orienta tu analisis; la evaluacion ocurre al enviar la fase.</p>
        </aside>
      </div>

      <section className="dt-candidate-section" aria-labelledby="candidate-title">
        <div className="dt-panel-heading">
          <div>
            <span className="experience-eyebrow">Formulaciones candidatas</span>
            <h3 id="candidate-title">Selecciona la definicion de problema que defenderas</h3>
            <p className="dt-candidate-instruction">
              Elige la formulacion que mejor conecte usuario, necesidad e insight.
              Evita sintomas aislados o soluciones anticipadas.
            </p>
          </div>
          <span className="dt-definition-limit">{selectionGuidance}</span>
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
                <div className="dt-candidate-actions">
                  <button
                    type="button"
                    aria-pressed={isSelected}
                    onClick={() => toggleDefinition(option)}
                  >
                    {isSelected ? "Quitar seleccion" : "Defender esta definicion"}
                  </button>
                  {isSelected && <strong>Definicion seleccionada</strong>}
                </div>
              </article>
            );
          })}
        </div>
      </section>

      <div className="experience-text-answer">
        <label htmlFor="define-text-answer">Justificacion de la definicion</label>
        <p className="dt-text-answer-help">
          Explica por que la definicion seleccionada representa el problema principal.
          Menciona el usuario afectado, la necesidad y la evidencia que respalda tu decision.
        </p>
        <textarea
          id="define-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Ejemplo: Esta definicion representa el problema principal porque afecta a..., la evidencia muestra que..., y permite enfocar la siguiente fase sin saltar directamente a una solucion."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && (
        <p className="dt-interaction-message" role="status">{interactionMessage}</p>
      )}

      <button
        type="button"
        className="experience-submit"
        onClick={submitDefinition}
        disabled={submitting}
      >
        {submitting ? "Evaluando fase..." : "Enviar definicion y ver consecuencias"}
      </button>
    </section>
  );
}

export default DefineExperience;
