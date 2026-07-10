function getOptionTypeLabel(type) {
  return String(type || "Decision")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[-_]/g, " ");
}

function groupOptions(options) {
  return options.reduce((groups, option) => {
    const group = option.optionType || "General";
    groups[group] = groups[group] || [];
    groups[group].push(option);
    return groups;
  }, {});
}

function GenericPhaseExperience({
  model,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  submitting,
}) {
  const groupedOptions = groupOptions(model.options);

  return (
    <section className="experience-activity" aria-labelledby="experience-activity-title">
      <header className="experience-activity-header">
        <div>
          <span className="experience-eyebrow">Actividad guiada</span>
          <h2 id="experience-activity-title">{model.phase.name}</h2>
          <p>
            Selecciona las decisiones mas relevantes para este momento del caso y
            fundamenta tu criterio estrategico.
          </p>
        </div>
        <span className="experience-selection-limit">
          Maximo {model.selection.maxSelections} selecciones
        </span>
      </header>

      <div className="experience-option-groups">
        {Object.entries(groupedOptions).map(([type, options]) => (
          <section key={type} className="experience-option-group" aria-label={type}>
            <h3>{getOptionTypeLabel(type)}</h3>
            <div className="experience-option-grid">
              {options.map((option) => {
                const isSelected = model.selection.selectedOptionIds.includes(option.id);

                return (
                  <button
                    key={option.id}
                    type="button"
                    className={`experience-option ${isSelected ? "is-selected" : ""}`}
                    aria-pressed={isSelected}
                    onClick={() => onToggleOption(option.id)}
                  >
                    <span className="experience-option-status" aria-hidden="true">
                      {isSelected ? "Seleccionada" : "Disponible"}
                    </span>
                    <span className="experience-option-text">{option.text}</span>
                    <span className="experience-option-meta">
                      <span>Costo {option.cost}</span>
                      <span>Tiempo {option.timeCost} sem</span>
                      <span>
                        Riesgo {option.riskImpact > 0 ? "+" : ""}
                        {option.riskImpact}
                      </span>
                    </span>
                    {(option.expectedImpactLevel ||
                      option.expectedEffortLevel ||
                      option.expectedViabilityLevel) && (
                      <span className="experience-option-levels">
                        {option.expectedImpactLevel && (
                          <span>Impacto: {option.expectedImpactLevel}</span>
                        )}
                        {option.expectedEffortLevel && (
                          <span>Esfuerzo: {option.expectedEffortLevel}</span>
                        )}
                        {option.expectedViabilityLevel && (
                          <span>Viabilidad: {option.expectedViabilityLevel}</span>
                        )}
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          </section>
        ))}
      </div>

      <div className="experience-text-answer">
        <label htmlFor="experience-text-answer">Justificacion estrategica</label>
        <textarea
          id="experience-text-answer"
          value={model.selection.textAnswer}
          onChange={(event) => onTextAnswerChange(event.target.value)}
          placeholder="Explica como las decisiones seleccionadas responden al problema, al usuario y a las restricciones del caso."
        />
        <small>{model.selection.textAnswer.length} caracteres</small>
      </div>

      <button
        type="button"
        className="experience-submit"
        onClick={onSubmit}
        disabled={submitting}
      >
        {submitting ? "Evaluando fase..." : "Enviar fase y ver consecuencias"}
      </button>
    </section>
  );
}

export default GenericPhaseExperience;
