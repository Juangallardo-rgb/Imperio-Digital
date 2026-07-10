function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase();
}

function parseTags(value) {
  if (!value || typeof value !== "string") return [];

  try {
    const tags = JSON.parse(value);
    return Array.isArray(tags) ? tags.filter((tag) => typeof tag === "string") : [];
  } catch {
    return [];
  }
}

function findReview(reviews, phaseName) {
  return (reviews || []).find(
    (review) => normalizeText(review.phaseName) === normalizeText(phaseName)
  ) || { options: [] };
}

function findScore(scores, phaseName) {
  return (scores || []).find(
    (score) => normalizeText(score.phaseName) === normalizeText(phaseName)
  ) || { score: 0, feedback: "No disponible" };
}

function selectedOptions(review) {
  return (review.options || []).filter((option) => option.wasSelected);
}

function missedOptions(review) {
  return (review.options || []).filter(
    (option) => option.isCorrect && !option.wasSelected
  );
}

function createPhaseJourney(phaseName, results) {
  const review = findReview(results.phaseReviews, phaseName);
  const phaseScore = findScore(results.phaseScores, phaseName);
  const selected = selectedOptions(review);
  const missed = missedOptions(review);
  const common = {
    phaseName,
    score: Number(phaseScore.score || 0),
    feedback: phaseScore.feedback || "No disponible",
    selected,
    missed,
    textAnswer: review.textAnswer || "",
    textFeedback: review.textAnswerFeedback || "",
    highlights: [],
    metrics: [],
  };

  if (phaseName === "Empatizar") {
    common.highlights = selected.map((option) => option.text);
    common.metrics = [
      ["Hallazgos seleccionados", selected.length],
      ["Evidencias correctas omitidas", missed.length],
    ];
  }

  if (phaseName === "Definir") {
    common.highlights = selected.map((option) => option.text);
    common.metrics = [["Formulaciones seleccionadas", selected.length]];
  }

  if (phaseName === "Idear") {
    common.highlights = selected.map((option) => option.text);
    common.metrics = selected.flatMap((option) => [
      option.expectedImpactLevel ? ["Impacto", option.expectedImpactLevel] : null,
      option.expectedEffortLevel ? ["Esfuerzo", option.expectedEffortLevel] : null,
      option.expectedViabilityLevel ? ["Viabilidad", option.expectedViabilityLevel] : null,
    ].filter(Boolean));
  }

  if (phaseName === "Prototipar") {
    common.highlights = selected.map((option) => option.text);
    common.metrics = [
      ["Presupuesto usado", `${selected.reduce((sum, option) => sum + Number(option.cost || 0), 0)} pts`],
      ["Tiempo usado", `${selected.reduce((sum, option) => sum + Number(option.timeCost || 0), 0)} sem`],
      ["Riesgo de modulos", selected.reduce((sum, option) => sum + Number(option.riskImpact || 0), 0)],
      ["Etiquetas de aprendizaje", [...new Set(selected.flatMap((option) => parseTags(option.tagsJson)))].join(", ") || "No disponible"],
    ];
  }

  if (phaseName === "Evaluar") {
    common.highlights = selected.map((option) => option.text);
    common.metrics = [
      ["Hallazgos priorizados", selected.length],
      ["Acciones correctas omitidas", missed.length],
    ];
  }

  return common;
}

function buildRecognitions(journey, finalScore) {
  const recognitions = [];
  const phaseRecognition = [
    ["Empatizar", "Analista de evidencias"],
    ["Definir", "Disenador centrado en el usuario"],
    ["Idear", "Estratega de ideacion"],
    ["Prototipar", "Constructor de experimentos"],
    ["Evaluar", "Especialista en validacion"],
  ];

  phaseRecognition.forEach(([phaseName, label]) => {
    const phase = journey.find((item) => item.phaseName === phaseName);
    if (phase && phase.score >= 70 && phase.selected.length > 0) {
      recognitions.push(label);
    }
  });

  if (Number(finalScore || 0) >= 80 && journey.length === 5) {
    recognitions.push("Consultor de transformacion");
  }

  return recognitions;
}

export function adaptDesignThinkingResults(results) {
  const phases = ["Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"]
    .filter((phaseName) => findReview(results.phaseReviews, phaseName).phaseName || findScore(results.phaseScores, phaseName).phaseName)
    .map((phaseName) => createPhaseJourney(phaseName, results));

  return {
    title: "Recorrido metodologico",
    description: "Una lectura de las decisiones tomadas en cada fase del proceso.",
    phases,
    recognitions: buildRecognitions(phases, results.finalScore),
  };
}

export function adaptGenericMethodologyResults(results) {
  return {
    title: "Recorrido metodologico",
    description: "Resumen de las fases finalizadas en esta simulacion.",
    phases: (results.phaseScores || []).map((phase) => ({
      phaseName: phase.phaseName,
      score: Number(phase.score || 0),
      feedback: phase.feedback || "No disponible",
      selected: selectedOptions(findReview(results.phaseReviews, phase.phaseName)),
      missed: [],
      textAnswer: findReview(results.phaseReviews, phase.phaseName).textAnswer || "",
      textFeedback: "",
      highlights: selectedOptions(findReview(results.phaseReviews, phase.phaseName)).map((option) => option.text),
      metrics: [],
    })),
    recognitions: [],
  };
}
