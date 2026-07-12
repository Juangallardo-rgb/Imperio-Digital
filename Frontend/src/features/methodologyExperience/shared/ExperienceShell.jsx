function formatNumber(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? Math.round(parsed * 100) / 100 : 0;
}

function getPercent(value, total) {
  if (total <= 0) return 0;
  return Math.min(100, Math.max(0, (value / total) * 100));
}

function ResourceMeter({ label, value, total, suffix, tone = "primary" }) {
  const percent = getPercent(value, total);

  return (
    <section className="experience-resource" aria-label={label}>
      <div>
        <span>{label}</span>
        <strong>
          {formatNumber(value)}/{formatNumber(total)} {suffix}
        </strong>
      </div>
      <div className="experience-meter" aria-hidden="true">
        <span className={`experience-meter-fill ${tone}`} style={{ width: `${percent}%` }} />
      </div>
    </section>
  );
}

function getEventValue(event, camelName, pascalName) {
  return event?.[camelName] ?? event?.[pascalName];
}

function ExperienceShell({
  model,
  phaseFeedback,
  message,
  submitting,
  onContinue,
  children,
}) {
  const currentPhaseKey = normalizeExperienceKey(model.phase.name);
  const isEmpathizeResearchFlow =
    model.methodology.code === "DesignThinking" &&
    currentPhaseKey === "empatizar";
  const completedKeys = new Set(
    model.phase.completed.map((phase) => normalizeExperienceKey(phase))
  );
  const previousDecisions = model.decisionTrace.filter(
    (entry) => normalizeExperienceKey(entry.phaseName) !== currentPhaseKey
  );
  const riskTone =
    model.resources.riskLevel >= 70
      ? "danger"
      : model.resources.riskLevel >= 40
      ? "warning"
      : "success";

  return (
    <div className="experience-shell">
      <header className="experience-hero">
        <div>
          <span className="experience-eyebrow">
            Imperio Digital · {model.methodology.name}
          </span>
          <h1>{model.scenario.title}</h1>
          <p className="experience-consultant-intro">
            Has sido contratado como consultor para resolver este desafio de
            transformacion digital.
          </p>
          {!isEmpathizeResearchFlow && model.scenario.description && (
            <p>{model.scenario.description}</p>
          )}
        </div>
        <div className="experience-current-phase">
          <span>Fase actual</span>
          <strong>{model.phase.name}</strong>
          <small>Etapa {model.phase.order} de {model.phaseOrder.length}</small>
        </div>
      </header>

      {!isEmpathizeResearchFlow && (
        <section className="experience-briefing" aria-label="Briefing del escenario">
          <article>
            <span>Empresa</span>
            <strong>{model.scenario.companyType || "No especificada"}</strong>
          </article>
          <article>
            <span>Problema</span>
            <strong>{model.scenario.problem || "No especificado"}</strong>
          </article>
          <article>
            <span>Usuario objetivo</span>
            <strong>{model.scenario.targetUser || "No especificado"}</strong>
          </article>
          <article>
            <span>Restricciones</span>
            <strong>{model.scenario.constraints || "No especificadas"}</strong>
          </article>
        </section>
      )}

      <nav className="experience-phase-progress" aria-label="Progreso de fases">
        {model.phaseOrder.map((phase) => {
          const phaseKey = normalizeExperienceKey(phase.name);
          const isCurrent = phaseKey === currentPhaseKey;
          const isComplete = completedKeys.has(phaseKey);

          return (
            <span
              key={`${phase.order}-${phase.name}`}
              className={`experience-phase-step ${
                isCurrent ? "is-current" : isComplete ? "is-complete" : ""
              }`}
              aria-current={isCurrent ? "step" : undefined}
            >
              <b>{phase.order}</b>
              <span>{phase.name}</span>
            </span>
          );
        })}
      </nav>

      {message && <div className="experience-message" role="status">{message}</div>}

      <div className="experience-workspace">
        <aside className="experience-sidebar">
          <div className="experience-resource-stack">
            <ResourceMeter
              label="Presupuesto"
              value={model.resources.remainingBudget}
              total={model.resources.initialBudget}
              suffix="pts"
            />
            <ResourceMeter
              label="Tiempo"
              value={model.resources.remainingTimeWeeks}
              total={model.resources.initialTimeWeeks}
              suffix="sem"
              tone="secondary"
            />
            <ResourceMeter
              label="Riesgo"
              value={model.resources.riskLevel}
              total={100}
              suffix="/100"
              tone={riskTone}
            />
          </div>

          <section className="experience-kpis">
            <h2>KPIs actuales</h2>
            {model.kpis.map((kpi) => (
              <div key={kpi.key}>
                <span>{kpi.label}</span>
                <strong>{formatNumber(kpi.value)}{kpi.suffix}</strong>
              </div>
            ))}
          </section>

          {!phaseFeedback && (
            <section className="experience-decision-summary">
              <h2>Decision actual</h2>
              <dl>
                <div><dt>Seleccionadas</dt><dd>{model.selection.selectedOptionIds.length}/{model.selection.maxSelections}</dd></div>
                <div><dt>Costo</dt><dd>{formatNumber(model.selection.totals.cost)} pts</dd></div>
                <div><dt>Tiempo</dt><dd>{formatNumber(model.selection.totals.time)} sem</dd></div>
                <div><dt>Riesgo</dt><dd>{model.selection.totals.risk > 0 ? "+" : ""}{formatNumber(model.selection.totals.risk)}</dd></div>
              </dl>
            </section>
          )}
        </aside>

        <main className="experience-main">
          {phaseFeedback ? (
            <section className="experience-feedback" aria-live="polite">
              <span className="experience-eyebrow">Consecuencias de la fase</span>
              <h2>{phaseFeedback.phaseName}</h2>
              <div className="experience-feedback-score">
                <strong>{formatNumber(phaseFeedback.score)}</strong>
                <span>/100</span>
              </div>
              <p>{phaseFeedback.feedback}</p>

              {model.triggeredEvent && (
                <div className="experience-triggered-event">
                  <span>Actualizacion del caso</span>
                  <h3>{getEventValue(model.triggeredEvent, "title", "Title")}</h3>
                  <p>{getEventValue(model.triggeredEvent, "description", "Description")}</p>
                </div>
              )}

              <div className="experience-consequences">
                <div><span>Presupuesto restante</span><strong>{formatNumber(model.resources.remainingBudget)}</strong></div>
                <div><span>Tiempo restante</span><strong>{formatNumber(model.resources.remainingTimeWeeks)} sem</strong></div>
                <div><span>Riesgo actual</span><strong>{formatNumber(model.resources.riskLevel)}/100</strong></div>
              </div>

              <button type="button" className="experience-submit" onClick={onContinue} disabled={submitting}>
                {phaseFeedback.isLastPhase ? "Finalizar simulacion" : "Continuar a la siguiente fase"}
              </button>
            </section>
          ) : (
            <>
              {previousDecisions.length > 0 && (
                <section className="experience-continuity" aria-label="Decisiones anteriores">
                  <span className="experience-eyebrow">Continuidad del caso</span>
                  <h2>Decisiones que ya condicionan esta fase</h2>
                  <div>
                    {previousDecisions.map((entry) => (
                      <article key={entry.phaseName}>
                        <strong>{entry.phaseName}</strong>
                        <p>{entry.selectedTexts.join(" · ") || "Sin decisiones registradas"}</p>
                      </article>
                    ))}
                  </div>
                </section>
              )}
              {children}
            </>
          )}
        </main>
      </div>
    </div>
  );
}

export default ExperienceShell;
import { normalizeExperienceKey } from "../engine/experienceContracts";
