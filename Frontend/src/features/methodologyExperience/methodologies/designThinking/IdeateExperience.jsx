import { useMemo, useState } from "react";
import ideationMatrixIllustration from "../../../../assets/methodologyExperience/ideation-matrix.svg";
import {
  buildStrategySummary,
  getEffectiveIdeaLimit,
  getIdeaLevelLabel,
  getIdeaProfile,
  getPortfolioImpactLabel,
  getPortfolioTags,
  getPortfolioViabilityLabel,
  getTraceForPhase,
} from "./experienceHelpers";

const quadrants = [
  { key: "high-low", title: "Alto impacto / Bajo esfuerzo", description: "Ideas de alta prioridad. Suelen ser buenas candidatas para prototipar." },
  { key: "high-high", title: "Alto impacto / Alto esfuerzo", description: "Ideas valiosas, pero requieren mas recursos o tiempo." },
  { key: "low-low", title: "Bajo impacto / Bajo esfuerzo", description: "Ideas complementarias. Utiles si apoyan la solucion principal." },
  { key: "low-high", title: "Bajo impacto / Alto esfuerzo", description: "Ideas riesgosas o poco convenientes para un primer prototipo." },
];

const ratingOptions = {
  impact: [
    { value: "high", label: "Alto" },
    { value: "medium", label: "Medio" },
    { value: "low", label: "Bajo" },
  ],
  effort: [
    { value: "high", label: "Alto" },
    { value: "medium", label: "Medio" },
    { value: "low", label: "Bajo" },
  ],
  viability: [
    { value: "high", label: "Alta" },
    { value: "medium", label: "Media" },
    { value: "low", label: "Baja" },
  ],
};

function ManualRatingControls({ idea, profile, onRatingChange }) {
  return (
    <fieldset className="dt-manual-ratings">
      <legend>Evalua esta idea antes de priorizarla</legend>
      {Object.entries(ratingOptions).map(([field, options]) => {
        const fieldLabel = field === "impact"
          ? "Impacto"
          : field === "effort"
            ? "Esfuerzo"
            : "Viabilidad";

        return (
          <div key={field} className="dt-rating-row">
            <span>{fieldLabel}</span>
            <div role="group" aria-label={`${fieldLabel} de ${idea.text}`}>
              {options.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  aria-pressed={profile[field] === option.value}
                  onClick={() => onRatingChange(idea, field, option.value)}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </div>
        );
      })}
    </fieldset>
  );
}

function IdeaCard({
  idea,
  profile,
  isSelected,
  needsManualEvaluation,
  onRatingChange,
  onToggle,
  index,
}) {
  const tags = Array.isArray(idea.tags) ? idea.tags : [];
  const relation = tags.length > 0
    ? `Enfoques asociados: ${tags.join(", ")}.`
    : "Relacion con el problema: explicala en tu estrategia de priorizacion.";

  return (
    <article
      className={`dt-idea-card ${isSelected ? "is-selected" : ""} ${needsManualEvaluation ? "is-manual" : ""}`}
      style={{ "--card-index": index }}
    >
      <div className="dt-idea-card-heading">
        <span>{needsManualEvaluation ? "Valoracion manual" : "Idea candidata"}</span>
        {isSelected && <strong>En cartera</strong>}
      </div>
      <h4>{idea.text}</h4>
      <dl className="dt-idea-metadata">
        <div><dt>Impacto</dt><dd>{getIdeaLevelLabel(profile.impact, "impact")}</dd></div>
        <div><dt>Esfuerzo</dt><dd>{getIdeaLevelLabel(profile.effort, "effort")}</dd></div>
        <div><dt>Viabilidad</dt><dd>{getIdeaLevelLabel(profile.viability, "viability")}</dd></div>
        <div><dt>Costo</dt><dd>{idea.cost} pts</dd></div>
        <div><dt>Tiempo</dt><dd>{idea.timeCost} sem</dd></div>
        <div><dt>Riesgo</dt><dd>{idea.riskImpact > 0 ? "+" : ""}{idea.riskImpact}</dd></div>
      </dl>
      {needsManualEvaluation && (
        <ManualRatingControls
          idea={idea}
          profile={profile}
          onRatingChange={onRatingChange}
        />
      )}
      <p className="dt-idea-connection">{relation}</p>
      {tags.length > 0 && (
        <div className="dt-idea-tags" aria-label="Enfoques asociados">
          {tags.map((tag, tagIndex) => <span key={`${tag}-${tagIndex}`}>{tag}</span>)}
        </div>
      )}
      <div className="dt-idea-action">
        <button type="button" aria-pressed={isSelected} onClick={() => onToggle(idea)}>
          {isSelected ? "Quitar de cartera" : "Agregar a cartera"}
        </button>
      </div>
    </article>
  );
}

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
  const empathyTrace = useMemo(
    () => getTraceForPhase(model.decisionTrace, "Empatizar"),
    [model.decisionTrace]
  );
  const [manualRatings, setManualRatings] = useState({});
  const [interactionMessage, setInteractionMessage] = useState("");
  const automaticProfiles = useMemo(
    () => Object.fromEntries(model.options.map((idea) => [idea.id, getIdeaProfile(idea)])),
    [model.options]
  );
  const ideaProfiles = useMemo(
    () => Object.fromEntries(model.options.map((idea) => [
      idea.id,
      getIdeaProfile(idea, automaticProfiles[idea.id].needsManualEvaluation ? manualRatings[idea.id] : undefined),
    ])),
    [automaticProfiles, manualRatings, model.options]
  );
  const manualIdeas = model.options.filter(
    (idea) => automaticProfiles[idea.id].needsManualEvaluation
  );
  const pendingIdeas = manualIdeas.filter(
    (idea) => ideaProfiles[idea.id].needsManualEvaluation
  );
  const intermediateIdeas = model.options.filter((idea) =>
    ideaProfiles[idea.id].isIntermediate
  );
  const selectedIdeas = model.options.filter((option) =>
    model.selection.selectedOptionIds.includes(option.id)
  );
  const effectiveMax = getEffectiveIdeaLimit(model.options);
  const ideasByQuadrant = quadrants.reduce((groups, { key }) => {
    groups[key] = model.options.filter((idea) => ideaProfiles[idea.id].quadrant === key);
    return groups;
  }, {});
  const hasMatrixIdeas = Object.values(ideasByQuadrant).some((ideas) => ideas.length > 0);
  const showManualFirst = pendingIdeas.length > 0 && !hasMatrixIdeas;
  const problemStatement = definitionTrace?.selectedTexts?.join(" | ") ||
    model.scenario.problem ||
    "Usa el contexto del caso para filtrar las ideas disponibles.";
  const evidence = empathyTrace?.selectedTexts?.slice(0, 2).join(" | ") ||
    "Usa el contexto y las restricciones del caso como evidencia de trabajo.";
  const strategySummary = buildStrategySummary(selectedIdeas);
  const portfolioTags = getPortfolioTags(selectedIdeas);
  const portfolioImpact = getPortfolioImpactLabel(selectedIdeas, ideaProfiles);
  const portfolioViability = getPortfolioViabilityLabel(selectedIdeas, ideaProfiles);
  const projectedRisk = model.resources.riskLevel + Math.max(0, model.selection.totals.risk);
  const warnings = [];

  if (model.resources.remainingBudget > 0 && model.selection.totals.cost > model.resources.remainingBudget) {
    warnings.push("El costo estimado supera el presupuesto disponible para esta fase.");
  }
  if (model.resources.remainingTimeWeeks > 0 && model.selection.totals.time > model.resources.remainingTimeWeeks) {
    warnings.push("El tiempo estimado supera el tiempo disponible para esta fase.");
  }
  if (projectedRisk >= 70) {
    warnings.push("La cartera eleva el riesgo previsto. Revisa su viabilidad antes de enviarla.");
  } else if (model.selection.totals.risk > 0) {
    warnings.push(`La cartera agrega +${model.selection.totals.risk} de riesgo. Revisa si se justifica.`);
  }

  const updateManualRating = (idea, field, value) => {
    setManualRatings((previous) => ({
      ...previous,
      [idea.id]: { ...previous[idea.id], [field]: value },
    }));
    setInteractionMessage("La valoracion se actualizo y la matriz se ajustara con tus criterios.");
  };

  const toggleIdea = (idea) => {
    const isSelected = model.selection.selectedOptionIds.includes(idea.id);

    if (!isSelected && selectedIdeas.length >= effectiveMax) {
      setInteractionMessage(`Ya usaste los ${effectiveMax} votos disponibles. Quita una idea antes de agregar otra.`);
      return;
    }

    onToggleOption(idea.id);
    setInteractionMessage(isSelected ? "La idea se quito de tu cartera." : "La idea se agrego a tu cartera para prototipar.");
  };

  const useSummaryAsDraft = () => {
    if (selectedIdeas.length === 0) {
      setInteractionMessage("Agrega al menos una idea a tu cartera antes de crear el borrador.");
      return;
    }

    onTextAnswerChange(strategySummary);
    setInteractionMessage("El resumen se copio como borrador de tu estrategia.");
  };

  const submitIdeas = () => {
    if (selectedIdeas.length === 0) {
      setInteractionMessage("Agrega al menos una idea a tu cartera antes de continuar.");
      return;
    }
    if (selectedIdeas.length > effectiveMax) {
      setInteractionMessage("Tu cartera supera el numero maximo de ideas permitidas. Quita una idea para continuar.");
      return;
    }
    if (selectedIdeas.some((idea) => ideaProfiles[idea.id].needsManualEvaluation)) {
      setInteractionMessage("Completa impacto y esfuerzo de las ideas seleccionadas antes de continuar.");
      return;
    }

    onSubmit();
  };

  const pendingEvaluation = pendingIdeas.length > 0 && (
    <section className="dt-manual-evaluation" aria-labelledby="manual-evaluation-title">
      <div>
        <span className="experience-eyebrow">Priorizacion estrategica</span>
        <h3 id="manual-evaluation-title">Evalua las ideas antes de priorizar</h3>
        <p>Antes de construir tu cartera, asigna una valoracion rapida a cada idea. Esto te ayudara a ubicarlas en la matriz y priorizar con criterio.</p>
      </div>
      <div className="dt-manual-idea-grid">
        {pendingIdeas.map((idea, index) => (
          <IdeaCard
            key={idea.id}
            idea={idea}
            profile={ideaProfiles[idea.id]}
            index={index}
            isSelected={model.selection.selectedOptionIds.includes(idea.id)}
            needsManualEvaluation
            onRatingChange={updateManualRating}
            onToggle={toggleIdea}
          />
        ))}
      </div>
    </section>
  );

  const priorityMatrix = hasMatrixIdeas ? (
    <section className="dt-matrix-section" aria-labelledby="matrix-title">
      <div className="dt-panel-heading">
        <div>
          <span className="experience-eyebrow">Matriz de priorizacion</span>
          <h3 id="matrix-title">Compara impacto y esfuerzo antes de elegir</h3>
          <p className="dt-matrix-intro">Las ideas se organizan segun el impacto esperado para el usuario y el esfuerzo necesario para implementarlas.</p>
        </div>
        <span className="dt-selection-guidance">Selecciona una cartera limitada para prototipar.</span>
      </div>
      <div className="dt-idea-matrix">
        {quadrants.map(({ key, title, description }) => (
          <section key={key} className={`dt-matrix-quadrant ${key}`}>
            <h4>{title}</h4>
            <p>{description}</p>
            <div className="dt-matrix-card-list">
              {ideasByQuadrant[key].length > 0 ? ideasByQuadrant[key].map((idea, index) => (
                <IdeaCard
                  key={idea.id}
                  idea={idea}
                  profile={ideaProfiles[idea.id]}
                  index={index}
                  isSelected={model.selection.selectedOptionIds.includes(idea.id)}
                  needsManualEvaluation={automaticProfiles[idea.id].needsManualEvaluation}
                  onRatingChange={updateManualRating}
                  onToggle={toggleIdea}
                />
              )) : <small>No hay ideas en este cuadrante.</small>}
            </div>
          </section>
        ))}
      </div>
    </section>
  ) : (
    <section className="dt-matrix-building" aria-live="polite">
      <strong>Matriz en construccion</strong>
      <span>{pendingIdeas.length > 0
        ? "Cuando asignes impacto y esfuerzo a las ideas, apareceran organizadas aqui."
        : "Las ideas intermedias se muestran para compararlas con cuidado antes de priorizar."}</span>
    </section>
  );

  return (
    <section className="dt-experience dt-ideate" aria-labelledby="ideate-title">
      <header className="dt-phase-intro">
        <div>
          <span className="experience-eyebrow">Estudio de ideacion estrategica</span>
          <h2 id="ideate-title">Construye una cartera de ideas con criterio</h2>
          <p>Tu objetivo es seleccionar una cartera limitada de ideas que responda al problema definido, priorizando impacto, viabilidad y uso responsable de recursos.</p>
        </div>
        <img className="dt-phase-illustration" src={ideationMatrixIllustration} alt="Matriz de impacto y esfuerzo para comparar ideas" />
      </header>

      <section className="dt-ideate-action-guide" aria-labelledby="ideate-action-guide-title">
        <div>
          <span className="experience-eyebrow">Que debes hacer en esta fase</span>
          <h3 id="ideate-action-guide-title">Convierte el problema en alternativas priorizadas</h3>
          <p>Revisa las ideas disponibles, comparalas por impacto, esfuerzo y viabilidad, y selecciona una cartera limitada para llevar a prototipo.</p>
        </div>
        <ol>
          <li>Revisa el problema definido.</li><li>Analiza las ideas disponibles.</li><li>Compara impacto, esfuerzo y viabilidad.</li><li>Usa tus votos para elegir las mejores ideas.</li><li>Revisa costo, tiempo y riesgo acumulados.</li><li>Justifica por que tu cartera debe pasar a prototipo.</li>
        </ol>
      </section>

      <section className="dt-ideate-context" aria-labelledby="ideate-context-title">
        <div className="dt-panel-heading"><div><span className="experience-eyebrow">De Definir a Idear</span><h3 id="ideate-context-title">Problema definido que debes resolver</h3><p>Usa este problema como filtro. Una buena idea responde a esta necesidad, no solo suena interesante.</p></div></div>
        <dl className="dt-ideate-context-grid">
          <div><dt>Problema definido</dt><dd>{problemStatement}</dd></div><div><dt>Usuario afectado</dt><dd>{model.scenario.targetUser || "Usa el usuario descrito en el caso."}</dd></div><div><dt>Necesidad o contexto</dt><dd>{model.scenario.problem || "Revisa la definicion seleccionada para identificar la necesidad principal."}</dd></div><div><dt>Evidencia de apoyo</dt><dd>{evidence}</dd></div>
        </dl>
      </section>

      <section className="dt-portfolio-explainer" aria-label="Explicacion de la cartera de ideas"><strong>Que es una cartera de ideas</strong><span>Es el conjunto limitado de alternativas que decides llevar a la siguiente fase. No elijas todas: prioriza las que combinan impacto, viabilidad y foco.</span></section>

      <section className="dt-resource-strip" aria-label="Recursos y votos de priorizacion">
        <div><span>Presupuesto disponible</span><strong>{model.resources.remainingBudget} pts</strong></div><div><span>Tiempo disponible</span><strong>{model.resources.remainingTimeWeeks} sem</strong></div><div><span>Riesgo actual</span><strong>{model.resources.riskLevel}/100</strong></div><div className="dt-vote-status" aria-live="polite"><span>Votos usados</span><strong>{selectedIdeas.length} de {effectiveMax}</strong><small>Has seleccionado {selectedIdeas.length} idea{selectedIdeas.length === 1 ? "" : "s"} para tu cartera. Cada voto representa una idea que llevas a prototipo.</small></div>
      </section>

      {showManualFirst && pendingEvaluation}
      {priorityMatrix}
      {!showManualFirst && pendingEvaluation}

      {intermediateIdeas.length > 0 && (
        <section className="dt-intermediate-ideas" aria-labelledby="intermediate-ideas-title">
          <div><span className="experience-eyebrow">Ideas intermedias</span><h3 id="intermediate-ideas-title">Compara estas ideas con cuidado</h3><p>Estas ideas tienen impacto o esfuerzo intermedio. Revisa su relacion con el problema antes de agregarlas a tu cartera.</p></div>
          <div className="dt-unclassified-grid">
            {intermediateIdeas.map((idea, index) => <IdeaCard key={idea.id} idea={idea} profile={ideaProfiles[idea.id]} index={index} isSelected={model.selection.selectedOptionIds.includes(idea.id)} needsManualEvaluation={automaticProfiles[idea.id].needsManualEvaluation} onRatingChange={updateManualRating} onToggle={toggleIdea} />)}
          </div>
        </section>
      )}

      <section className="dt-idea-portfolio" aria-labelledby="portfolio-title">
        <div className="dt-portfolio-heading"><div><span className="experience-eyebrow">Cartera seleccionada</span><h3 id="portfolio-title">Tu cartera para prototipar</h3><p>{selectedIdeas.length > 0 ? "Estas son las ideas que llevaras a la siguiente fase. Revisa si equilibran impacto, esfuerzo, viabilidad y recursos." : "Aun no has agregado ideas a tu cartera."}</p></div><button type="button" className="dt-secondary-action" onClick={useSummaryAsDraft} disabled={selectedIdeas.length === 0}>Usar resumen como borrador</button></div>
        {selectedIdeas.length > 0 && <ol className="dt-portfolio-list">{selectedIdeas.map((idea) => <li key={idea.id}>{idea.text}</li>)}</ol>}
        <dl className="dt-portfolio-metrics"><div><dt>Ideas seleccionadas</dt><dd>{selectedIdeas.length} de {effectiveMax}</dd></div><div><dt>Impacto esperado</dt><dd>{portfolioImpact}</dd></div><div><dt>Viabilidad estimada</dt><dd>{portfolioViability}</dd></div><div><dt>Costo acumulado</dt><dd>{model.selection.totals.cost} pts</dd></div><div><dt>Tiempo acumulado</dt><dd>{model.selection.totals.time} sem</dd></div><div><dt>Riesgo agregado</dt><dd>{model.selection.totals.risk > 0 ? "+" : ""}{model.selection.totals.risk}</dd></div></dl>
        <p className="dt-portfolio-coverage">{portfolioTags.length > 0 ? `Enfoques asociados a la cartera: ${portfolioTags.join(", ")}.` : "Explica en tu estrategia como esta cartera cubre el problema definido."}</p>
        {warnings.length > 0 && <ul className="dt-portfolio-warnings" aria-live="polite">{warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>}
      </section>

      <div className="experience-text-answer">
        <label htmlFor="ideate-text-answer">Estrategia de priorizacion</label>
        <p id="ideate-text-answer-help" className="dt-text-answer-help">Explica por que las ideas seleccionadas forman una cartera adecuada para pasar a prototipo. Menciona como responden al problema definido y como equilibran impacto, esfuerzo, viabilidad y recursos.{manualIdeas.length > 0 && " Tambien puedes explicar por que asignaste esos niveles de impacto, esfuerzo y viabilidad."}</p>
        <textarea id="ideate-text-answer" aria-describedby="ideate-text-answer-help" value={model.selection.textAnswer} onChange={(event) => onTextAnswerChange(event.target.value)} placeholder="Ejemplo: Priorizo estas ideas porque responden directamente al problema definido, tienen impacto alto para el usuario y pueden probarse dentro del tiempo y presupuesto disponibles." />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      {interactionMessage && <p className="dt-interaction-message" role="status">{interactionMessage}</p>}
      <button type="button" className="experience-submit" onClick={submitIdeas} disabled={submitting}>{submitting ? "Evaluando fase..." : "Enviar cartera y ver consecuencias"}</button>
    </section>
  );
}

export default IdeateExperience;
