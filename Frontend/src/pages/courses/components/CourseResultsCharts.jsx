const PERFORMANCE_LEVELS = [
  {
    key: "reinforcement",
    label: "Requiere refuerzo",
    shortLabel: "Refuerzo",
    min: 0,
    max: 59,
  },
  {
    key: "developing",
    label: "En desarrollo",
    shortLabel: "En desarrollo",
    min: 60,
    max: 79,
  },
  {
    key: "good",
    label: "Buen desempeño",
    shortLabel: "Buen desempeño",
    min: 80,
    max: 100,
  },
];

function getPerformanceLevel(score) {
  if (score === null || score === undefined || Number.isNaN(Number(score))) {
    return {
      key: "empty",
      label: "Sin datos",
      shortLabel: "Sin datos",
    };
  }

  const numericScore = Number(score);

  return PERFORMANCE_LEVELS.find(
    (level) => numericScore >= level.min && numericScore <= level.max
  ) || PERFORMANCE_LEVELS[PERFORMANCE_LEVELS.length - 1];
}

export function PerformanceLegend() {
  return (
    <div className="course-results-legend" aria-label="Niveles de desempeño">
      {PERFORMANCE_LEVELS.map((level) => (
        <span key={level.key}>
          <i className={`performance-swatch ${level.key}`} aria-hidden="true" />
          {level.label} ({level.min}-{level.max})
        </span>
      ))}
      <span>
        <i className="performance-swatch empty" aria-hidden="true" />
        Sin datos
      </span>
    </div>
  );
}

export function AnimatedMetric({ label, value, detail, tone = "neutral" }) {
  return (
    <article className={`course-results-metric ${tone}`}>
      <span>{label}</span>
      <strong>{value}</strong>
      {detail && <p>{detail}</p>}
    </article>
  );
}

export function CourseResultsBarChart({
  phases,
  ariaLabel = "Desempeño promedio del grupo por fase, de cero a cien",
  showStudentCount = true,
}) {
  const evaluatedPhases = phases.filter((phase) => phase.averageScore !== null);

  if (evaluatedPhases.length === 0) {
    return (
      <div className="course-results-empty compact">
        <strong>Sin puntajes por fase</strong>
        <p>El gráfico se completará cuando existan intentos finalizados.</p>
      </div>
    );
  }

  return (
    <div className="course-phase-chart-scroll">
      <div
        className="course-phase-chart"
        style={{ "--phase-columns": Math.max(phases.length, 1) }}
        role="img"
        aria-label={ariaLabel}
      >
        <div className="course-phase-axis" aria-hidden="true">
          <span>100</span>
          <span>75</span>
          <span>50</span>
          <span>25</span>
          <span>0</span>
        </div>

        <div className="course-phase-bars">
          {phases.map((phase, index) => {
            const hasScore = phase.averageScore !== null;
            const score = hasScore ? Math.round(Number(phase.averageScore)) : 0;
            const level = getPerformanceLevel(phase.averageScore);
            const tooltip = hasScore
              ? showStudentCount
                ? `${phase.phaseName}: ${score}/100, ${phase.studentsEvaluated} estudiante(s) considerado(s)`
                : `${phase.phaseName}: ${score}/100`
              : `${phase.phaseName}: sin estudiantes evaluados`;

            return (
              <div
                key={`${phase.phaseName}-${phase.phaseOrder}`}
                className="course-phase-column"
                title={tooltip}
                aria-label={tooltip}
              >
                <strong>{hasScore ? score : "--"}</strong>
                <div className="course-phase-track">
                  {hasScore && (
                    <div
                      className={`course-phase-fill ${level.key}`}
                      style={{
                        "--bar-height": `${Math.min(100, Math.max(0, score))}%`,
                        "--animation-delay": `${index * 70}ms`,
                      }}
                    />
                  )}
                </div>
                <span>{phase.phaseName}</span>
                <small>
                  {hasScore
                    ? showStudentCount
                      ? `${phase.studentsEvaluated} evaluado(s)`
                      : level.label
                    : "Sin datos"}
                </small>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

export function PhaseDistributionChart({ phases }) {
  const hasData = phases.some((phase) => phase.studentsEvaluated > 0);

  if (!hasData) {
    return (
      <div className="course-results-empty compact">
        <strong>Sin distribución disponible</strong>
        <p>Se necesitan intentos finalizados para calcular los niveles.</p>
      </div>
    );
  }

  return (
    <div className="phase-distribution-list">
      {phases.map((phase, index) => {
        const total = Number(phase.studentsEvaluated || 0);
        const segments = [
          {
            key: "reinforcement",
            label: "Requiere refuerzo",
            value: Number(phase.reinforcementCount || 0),
          },
          {
            key: "developing",
            label: "En desarrollo",
            value: Number(phase.developingCount || 0),
          },
          {
            key: "good",
            label: "Buen desempeño",
            value: Number(phase.goodPerformanceCount || 0),
          },
        ];

        return (
          <div
            key={`${phase.phaseName}-${phase.phaseOrder}`}
            className="phase-distribution-row"
            style={{ "--animation-delay": `${index * 70}ms` }}
          >
            <div className="phase-distribution-label">
              <strong>{phase.phaseName}</strong>
              <span>{total > 0 ? `${total} evaluado(s)` : "Sin datos"}</span>
            </div>

            <div
              className="phase-distribution-track"
              aria-label={`Distribución de niveles en ${phase.phaseName}`}
            >
              {total === 0 ? (
                <span className="phase-distribution-empty">Sin datos</span>
              ) : (
                segments.map((segment) => (
                  <div
                    key={segment.key}
                    className={`phase-distribution-segment ${segment.key}`}
                    style={{ "--segment-width": `${(segment.value / total) * 100}%` }}
                    title={`${segment.label}: ${segment.value} estudiante(s)`}
                    aria-label={`${segment.label}: ${segment.value} estudiante(s)`}
                  >
                    {segment.value > 0 && <span>{segment.value}</span>}
                  </div>
                ))
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function StudentPhaseHeatmap({ students, phases }) {
  const scoreByStudentAndPhase = new Map();

  students.forEach((student) => {
    (student.phaseResults || []).forEach((phase) => {
      scoreByStudentAndPhase.set(
        `${student.studentId}:${phase.phaseOrder}`,
        Number(phase.score)
      );
    });
  });

  if (students.length === 0) {
    return (
      <div className="course-results-empty compact">
        <strong>No hay estudiantes inscritos</strong>
        <p>La matriz aparecerá cuando el curso tenga estudiantes.</p>
      </div>
    );
  }

  return (
    <div className="student-heatmap-scroll">
      <div
        className="student-heatmap"
        style={{ "--phase-count": Math.max(phases.length, 1) }}
      >
        <div className="student-heatmap-row header" role="row">
          <div className="student-heatmap-name" role="columnheader">
            Estudiante
          </div>
          {phases.map((phase) => (
            <div
              key={`${phase.phaseName}-${phase.phaseOrder}`}
              className="student-heatmap-heading"
              role="columnheader"
              title={phase.phaseName}
            >
              {phase.phaseName}
            </div>
          ))}
        </div>

        {students.map((student, studentIndex) => (
          <div
            key={student.studentId}
            className="student-heatmap-row"
            role="row"
          >
            <div className="student-heatmap-name" role="rowheader">
              <strong>{student.studentName}</strong>
              <span>{student.studentEmail}</span>
            </div>

            {phases.map((phase, phaseIndex) => {
              const score = scoreByStudentAndPhase.get(
                `${student.studentId}:${phase.phaseOrder}`
              );
              const level = getPerformanceLevel(score);
              const hasScore = score !== undefined;
              const tooltip = hasScore
                ? `${student.studentName}, ${phase.phaseName}: ${Math.round(score)}/100 (${level.label})`
                : `${student.studentName}, ${phase.phaseName}: sin datos finalizados`;

              return (
                <div
                  key={`${student.studentId}-${phase.phaseOrder}`}
                  className={`student-heatmap-cell ${level.key}`}
                  style={{
                    "--animation-delay": `${Math.min(
                      650,
                      studentIndex * 45 + phaseIndex * 30
                    )}ms`,
                  }}
                  role="cell"
                  title={tooltip}
                  aria-label={tooltip}
                >
                  <strong>{hasScore ? Math.round(score) : "--"}</strong>
                  <span>{level.shortLabel}</span>
                </div>
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
}
