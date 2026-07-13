function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();
}

function containsAny(value, keywords) {
  return keywords.some((keyword) => value.includes(keyword));
}

export const EMPATHY_CATEGORIES = Object.freeze([
  {
    key: "pain",
    label: "Dolor",
    description: "Frustracion, obstaculo o problema que afecta al usuario.",
  },
  {
    key: "need",
    label: "Necesidad",
    description: "Algo que el usuario requiere para lograr su objetivo o sentirse seguro.",
  },
  {
    key: "behavior",
    label: "Comportamiento",
    description: "Accion observable que muestra como actua el usuario.",
  },
  {
    key: "evidence",
    label: "Evidencia",
    description: "Dato, metrica, testimonio u observacion que respalda el analisis.",
  },
]);

export function getEffectiveEvidenceLimit(evidenceCards, configuredMax) {
  const availableCount = Array.isArray(evidenceCards)
    ? evidenceCards.filter((card) => Number(card?.id) > 0).length
    : 0;

  if (availableCount === 0) return 0;

  const parsedMax = Number(configuredMax);
  const requestedMax = Number.isFinite(parsedMax) && parsedMax > 0
    ? Math.floor(parsedMax)
    : availableCount;

  return Math.min(requestedMax, availableCount);
}

export function buildEmpathyCounts(selectedCards, classifications) {
  return (Array.isArray(selectedCards) ? selectedCards : []).reduce(
    (counts, card) => {
      const category = classifications?.[card.id] || card.category;

      if (Object.hasOwn(counts, category)) {
        counts[category] += 1;
      }

      return counts;
    },
    { pain: 0, need: 0, behavior: 0, evidence: 0 }
  );
}

export function getEvidenceGuidance(card, category) {
  const tags = Array.isArray(card?.tags) ? card.tags : [];
  const searchableText = normalizeText(
    `${card?.text || ""} ${tags.join(" ")}`
  );

  if (containsAny(searchableText, ["color", "logo", "estetica", "branding"])) {
    return "Este hallazgo puede ser util solo si se conecta con un problema real del usuario y no con una preferencia estetica aislada.";
  }

  const categoryLabel = EMPATHY_CATEGORIES.find(
    (item) => item.key === category
  )?.label.toLowerCase() || "hallazgo";

  return `Evalua si este hallazgo representa un ${categoryLabel}, una necesidad, un comportamiento observable o una evidencia de apoyo. No todas las observaciones deben priorizarse.`;
}

export function createEvidenceCard(option) {
  const tags = Array.isArray(option?.tags) ? option.tags : [];
  const searchableText = normalizeText(`${option?.text || ""} ${tags.join(" ")}`);
  let source = "Observacion";

  if (containsAny(searchableText, ["entrevista", "testimonio", "usuario dice"])) {
    source = "Entrevista";
  } else if (containsAny(searchableText, ["metrica", "dato", "%", "conversion", "abandono"])) {
    source = "Metrica";
  } else if (containsAny(searchableText, ["queja", "reclamo", "frustracion"])) {
    source = "Queja";
  } else if (containsAny(searchableText, ["comportamiento", "abandona", "usa", "recorrido"])) {
    source = "Comportamiento";
  }

  let category = "evidence";

  if (containsAny(searchableText, ["dolor", "friccion", "problema", "abandono", "abandona", "confianza"])) {
    category = "pain";
  } else if (containsAny(searchableText, ["necesita", "necesidad", "quiere", "espera"])) {
    category = "need";
  } else if (containsAny(searchableText, ["usa", "hace", "comportamiento", "recorrido"])) {
    category = "behavior";
  }

  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    source,
    category,
    tags: tags.filter((tag) => typeof tag === "string" && tag.trim()),
  };
}

export function getTraceForPhase(decisionTrace, phaseName) {
  const expectedPhase = normalizeText(phaseName);

  return (Array.isArray(decisionTrace) ? decisionTrace : []).find(
    (entry) => normalizeText(entry?.phaseName) === expectedPhase
  );
}

export function buildEmpathySummary(selectedCards, classifications) {
  const selected = Array.isArray(selectedCards) ? selectedCards : [];
  const counts = buildEmpathyCounts(selected, classifications);

  if (selected.length === 0) {
    return "Aun no has priorizado hallazgos para el resumen de empatia.";
  }

  return `${selected.length} hallazgo(s) priorizado(s): ${counts.pain} dolor(es), ${counts.need} necesidad(es), ${counts.behavior} comportamiento(s) y ${counts.evidence} evidencia(s).`;
}

export function cleanSentencePart(value, { removeNeed = false, removeBecause = false } = {}) {
  let text = String(value || "")
    .replace(/\s+/g, " ")
    .trim()
    .replace(/[.]+$/g, "")
    .trim();

  if (removeNeed) {
    text = text.replace(/^(necesita|necesitan)\s+/i, "");
  }

  if (removeBecause) {
    text = text.replace(/^porque\s+/i, "");
  }

  return text.replace(/\s+/g, " ").trim();
}

function lowercaseSentenceStart(value) {
  const text = String(value || "");
  const first = text.charAt(0);
  const second = text.charAt(1);

  if (
    first &&
    first !== first.toLowerCase() &&
    (!second || second === second.toLowerCase())
  ) {
    return `${first.toLowerCase()}${text.slice(1)}`;
  }

  return text;
}

function capitalizeSentenceStart(value) {
  const text = String(value || "");
  const first = text.charAt(0);

  return first ? `${first.toUpperCase()}${text.slice(1)}` : text;
}

function usesPluralNeedVerb(value) {
  return /^(los|las|personas|usuarios|usuarias|clientes|estudiantes|equipos|profesionales|empleados|trabajadores)\b/i.test(
    String(value || "").trim()
  );
}

export function buildProblemPreview({ userSegment, need, insight }) {
  const resolvedUser = cleanSentencePart(userSegment);
  const resolvedNeed = cleanSentencePart(need, { removeNeed: true });
  const resolvedInsight = cleanSentencePart(insight, { removeBecause: true });

  if (!resolvedUser || !resolvedNeed || !resolvedInsight) {
    return "Completa el usuario, la necesidad y la evidencia para construir el enunciado.";
  }

  const needVerb = usesPluralNeedVerb(resolvedUser) ? "necesitan" : "necesita";

  return `${capitalizeSentenceStart(resolvedUser)} ${needVerb} ${lowercaseSentenceStart(resolvedNeed)} porque ${lowercaseSentenceStart(resolvedInsight)}.`;
}

export function buildDefinitionDraft({ userSegment, need, insight }) {
  const preview = buildProblemPreview({ userSegment, need, insight });

  if (preview.startsWith("Completa el usuario")) {
    return "";
  }

  const resolvedUser = cleanSentencePart(userSegment);
  const resolvedNeed = cleanSentencePart(need, { removeNeed: true });
  const resolvedInsight = cleanSentencePart(insight, { removeBecause: true });

  return `La definicion se centra en ${resolvedUser}. Su necesidad principal es ${lowercaseSentenceStart(resolvedNeed)} y la evidencia indica que ${lowercaseSentenceStart(resolvedInsight)}. Por ello, conviene comprender este problema antes de proponer una solucion.`;
}

export function getEffectiveDefinitionLimit(options) {
  const availableCount = Array.isArray(options) ? options.length : 0;

  if (availableCount === 0) return 0;

  const configuredLimits = options
    .map((option) => Number(option?.maxSelections))
    .filter((limit) => Number.isFinite(limit) && limit > 0);
  const configuredMax = configuredLimits.length > 0
    ? Math.max(...configuredLimits)
    : 1;

  return Math.min(configuredMax, availableCount);
}

export function getDefinitionCue(value) {
  const text = normalizeText(value);

  if (containsAny(text, ["plataforma", "aplicacion", "implementar", "crear una", "desarrollar"])) {
    return "Revisa si la formulacion anticipa una solucion antes de delimitar el problema.";
  }

  if (containsAny(text, ["aumentar", "mejorar", "baja", "alta", "falta de"])) {
    return "Revisa si describe un sintoma y completa la causa o necesidad del usuario.";
  }

  return "Relaciona esta formulacion con un usuario, una necesidad y evidencia concreta.";
}

export function normalizeLevel(value) {
  const level = normalizeText(value);

  if (containsAny(level, ["alto", "alta", "high"])) return "high";
  if (containsAny(level, ["bajo", "baja", "low"])) return "low";
  if (containsAny(level, ["medio", "media", "medium"])) return "medium";

  return "";
}

export function getIdeaProfile(option, manualRatings = {}) {
  const impact = normalizeLevel(
    manualRatings?.impact ?? option?.expectedImpactLevel
  );
  const effort = normalizeLevel(
    manualRatings?.effort ?? option?.expectedEffortLevel
  );
  const viability = normalizeLevel(
    manualRatings?.viability ?? option?.expectedViabilityLevel
  );
  const hasImpactAndEffort = Boolean(impact && effort);
  const canUsePriorityMatrix =
    ["high", "low"].includes(impact) &&
    ["high", "low"].includes(effort);

  return {
    impact,
    effort,
    viability,
    quadrant: canUsePriorityMatrix ? `${impact}-${effort}` : "unclassified",
    hasImpactAndEffort,
    needsManualEvaluation: !hasImpactAndEffort,
    isIntermediate: hasImpactAndEffort && !canUsePriorityMatrix,
    needsReview: !canUsePriorityMatrix,
  };
}

export function getIdeaQuadrant(option) {
  return getIdeaProfile(option).quadrant;
}

export function getIdeaLevelLabel(level, kind) {
  const labels = kind === "viability"
    ? { high: "Alta", medium: "Media", low: "Baja" }
    : { high: "Alto", medium: "Medio", low: "Bajo" };

  return labels[level] || "Por revisar";
}

export function getEffectiveIdeaLimit(options, fallback = 3) {
  const availableCount = Array.isArray(options) ? options.length : 0;

  if (availableCount === 0) return 0;

  const configuredLimits = options
    .map((option) => Number(option?.maxSelections))
    .filter((limit) => Number.isFinite(limit) && limit > 0);
  const fallbackLimit = Number.isFinite(Number(fallback)) && Number(fallback) > 0
    ? Math.floor(Number(fallback))
    : 3;
  const configuredMax = configuredLimits.length > 0
    ? Math.max(...configuredLimits)
    : fallbackLimit;

  return Math.min(configuredMax, availableCount);
}

function getProfileForIdea(idea, profiles) {
  return profiles?.[idea?.id] || getIdeaProfile(idea);
}

export function getPortfolioImpactLabel(ideas, profiles) {
  const impacts = (Array.isArray(ideas) ? ideas : [])
    .map((idea) => getProfileForIdea(idea, profiles).impact)
    .filter(Boolean);

  if (impacts.length === 0) return "Por revisar";
  if (impacts.every((impact) => impact === "high")) return "Alto";
  if (impacts.every((impact) => impact === "low")) return "Bajo";

  return "Mixto";
}

export function getPortfolioViabilityLabel(ideas, profiles) {
  const viability = (Array.isArray(ideas) ? ideas : [])
    .map((idea) => getProfileForIdea(idea, profiles).viability)
    .filter(Boolean);

  if (viability.length === 0) return "Por revisar";
  if (viability.every((level) => level === "high")) return "Alta";
  if (viability.every((level) => level === "low")) return "Baja";

  return "Mixta";
}

export function getPortfolioTags(ideas) {
  return [...new Set(
    (Array.isArray(ideas) ? ideas : []).flatMap((idea) =>
      Array.isArray(idea?.tags) ? idea.tags : []
    )
  )]
    .filter((tag) => typeof tag === "string" && tag.trim())
    .slice(0, 6);
}

export function buildStrategySummary(ideas) {
  const selectedIdeas = Array.isArray(ideas) ? ideas : [];

  if (selectedIdeas.length === 0) {
    return "Aun no hay ideas priorizadas para la cartera estrategica.";
  }

  const names = selectedIdeas
    .map((idea) => String(idea?.text || "").trim())
    .filter(Boolean);
  const list = names.length === 0
    ? "las ideas seleccionadas"
    : names.length === 1
    ? names[0]
    : `${names.slice(0, -1).join(", ")} y ${names.at(-1)}`;

  return `La cartera priorizada combina ${list}. Estas ideas se seleccionan para responder al problema definido y equilibrar impacto, esfuerzo, viabilidad y uso responsable de recursos antes de pasar a prototipo.`;
}

export function createPrototypeModule(option) {
  const tags = Array.isArray(option?.tags) ? option.tags : [];
  const impacts = option?.impacts && typeof option.impacts === "object"
    ? Object.keys(option.impacts)
    : [];

  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    optionType: String(option?.optionType || "Modulo"),
    tags: tags.filter((tag) => typeof tag === "string" && tag.trim()),
    impactKeys: impacts.filter((key) => typeof key === "string" && key.trim()),
    cost: Number(option?.cost) || 0,
    timeCost: Number(option?.timeCost) || 0,
    riskImpact: Number(option?.riskImpact) || 0,
    expectedImpactLevel: String(option?.expectedImpactLevel || ""),
    expectedViabilityLevel: String(option?.expectedViabilityLevel || ""),
  };
}

export function buildMvpSummary(modules) {
  const selectedModules = Array.isArray(modules) ? modules : [];

  if (selectedModules.length === 0) {
    return "El lienzo del MVP aun no contiene modulos seleccionados.";
  }

  return `MVP propuesto con ${selectedModules.length} modulo(s): ${selectedModules.map((module) => module.text).join(" | ")}`;
}

export function createTestCard(option) {
  const tags = Array.isArray(option?.tags) ? option.tags : [];
  const text = String(option?.text || "");
  const searchableText = normalizeText(`${text} ${tags.join(" ")}`);
  let lens = "Hallazgo a validar";

  if (containsAny(searchableText, ["error", "abandono", "friccion", "problema", "queja"])) {
    lens = "Problema observado";
  } else if (containsAny(searchableText, ["satisfaccion", "mejora", "adopcion", "confianza"])) {
    lens = "Hallazgo positivo";
  } else if (containsAny(searchableText, ["kpi", "metrica", "conversion", "tiempo"])) {
    lens = "Indicador de prueba";
  }

  return {
    id: Number(option?.id),
    text,
    lens,
    tags: tags.filter((tag) => typeof tag === "string" && tag.trim()),
  };
}

export function buildTestPlan(cards, actions) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no has definido acciones para la siguiente iteracion.";
  }

  return selectedCards
    .map((card) => `${actions?.[card.id] || "Volver a probar"}: ${card.text}`)
    .join(" | ");
}
