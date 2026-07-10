function MethodologyJourneyResults({ journey }) {
  if (!journey?.phases?.length) return null;

  return (
    <section className="journey-results" aria-labelledby="journey-results-title">
      <header className="journey-results-header">
        <div>
          <span className="experience-eyebrow">Resultados V2</span>
          <h2 id="journey-results-title">{journey.title}</h2>
          <p>{journey.description}</p>
        </div>
        {journey.recognitions?.length > 0 && (
          <div className="journey-recognitions" aria-label="Reconocimientos profesionales">
            {journey.recognitions.map((recognition) => <span key={recognition}>{recognition}</span>)}
          </div>
        )}
      </header>

      <div className="journey-timeline" aria-label="Linea de recorrido metodologico">
        {journey.phases.map((phase, index) => (
          <article key={phase.phaseName} className="journey-phase-card">
            <div className="journey-phase-marker" aria-hidden="true">{index + 1}</div>
            <div className="journey-phase-content">
              <div className="journey-phase-heading">
                <div>
                  <span>Fase {index + 1}</span>
                  <h3>{phase.phaseName}</h3>
                </div>
                <strong>{phase.score}/100</strong>
              </div>
              <p>{phase.feedback}</p>

              {phase.highlights?.length > 0 && (
                <div className="journey-highlight-list">
                  {phase.highlights.map((highlight, itemIndex) => (
                    <span key={`${phase.phaseName}-${itemIndex}`}>{highlight}</span>
                  ))}
                </div>
              )}

              {phase.metrics?.length > 0 && (
                <dl className="journey-metrics">
                  {phase.metrics.map(([label, value], itemIndex) => (
                    <div key={`${label}-${itemIndex}`}><dt>{label}</dt><dd>{value || "No disponible"}</dd></div>
                  ))}
                </dl>
              )}

              {phase.textAnswer && (
                <div className="journey-text-answer">
                  <strong>Justificacion registrada</strong>
                  <p>{phase.textAnswer}</p>
                </div>
              )}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}

export default MethodologyJourneyResults;
