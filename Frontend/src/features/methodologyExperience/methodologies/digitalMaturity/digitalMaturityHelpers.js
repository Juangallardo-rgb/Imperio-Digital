import { normalizeExperienceKey } from "../../engine/experienceContracts.js";

export const DIAGNOSIS_DIMENSIONS = Object.freeze([
  { key: "processes", label: "Procesos" },
  { key: "data", label: "Datos" },
  { key: "technology", label: "Tecnologia" },
  { key: "peopleCulture", label: "Personas y cultura" },
  { key: "customerExperience", label: "Experiencia del cliente" },
  { key: "digitalStrategy", label: "Estrategia digital" },
]);

export const RELEVANCE_LEVELS = Object.freeze([
  { key: "alta", label: "Alta" },
  { key: "media", label: "Media" },
  { key: "baja", label: "Baja" },
]);

export const OBSERVED_LEVELS = Object.freeze([
  { key: "bajo", label: "Bajo" },
  { key: "medio", label: "Medio" },
  { key: "alto", label: "Alto" },
]);

export const MATURITY_LEVELS = Object.freeze([
  { key: "inicial", label: "Inicial" },
  { key: "basico", label: "Basico" },
  { key: "enDesarrollo", label: "En desarrollo" },
  { key: "integrado", label: "Integrado" },
  { key: "optimizado", label: "Optimizado" },
]);

export const DIGITAL_CAPABILITIES = Object.freeze([
  { key: "processAutomation", label: "Automatizacion de procesos", dimension: "processes" },
  { key: "dataManagement", label: "Gestion de datos", dimension: "data" },
  { key: "technologyIntegration", label: "Integracion tecnologica", dimension: "technology" },
  { key: "decisionAnalytics", label: "Analitica para decisiones", dimension: "data" },
  { key: "digitalCulture", label: "Cultura digital", dimension: "peopleCulture" },
  { key: "customerExperience", label: "Experiencia digital del cliente", dimension: "customerExperience" },
  { key: "digitalLeadership", label: "Liderazgo y estrategia digital", dimension: "digitalStrategy" },
  { key: "digitalGovernance", label: "Seguridad y gobierno digital", dimension: "digitalStrategy" },
]);

export const ROADMAP_PERIODS = Object.freeze([
  { key: "short", label: "Corto plazo" },
  { key: "medium", label: "Mediano plazo" },
  { key: "long", label: "Largo plazo" },
]);

export const TRACKING_AREAS = Object.freeze([
  { key: "efficiency", label: "Eficiencia operativa" },
  { key: "data", label: "Uso de datos" },
  { key: "adoption", label: "Adopcion digital" },
  { key: "satisfaction", label: "Experiencia del cliente" },
  { key: "maturity", label: "Madurez digital" },
]);

const DIMENSION_MATCHERS = [
  { key: "data", terms: ["data", "datos", "analytics", "analitica", "kpi", "indicador", "decisionmaking", "informacion"] },
  { key: "technology", terms: ["technology", "tecnologia", "digitaltools", "tools", "herramienta", "integration", "integracion", "systems", "sistemas", "software", "platform"] },
  { key: "processes", terms: ["process", "proceso", "automation", "automatizacion", "manualprocess", "manualwork", "manual", "workflow", "efficiency", "eficiencia"] },
  { key: "peopleCulture", terms: ["culture", "cultura", "people", "personas", "talent", "talento", "adoption", "adopcion", "training", "capacitacion"] },
  { key: "customerExperience", terms: ["customer", "cliente", "client", "service", "servicio", "satisfaction", "satisfaccion", "experience", "experiencia", "user", "usuario"] },
  { key: "digitalStrategy", terms: ["strategy", "estrategia", "leadership", "liderazgo", "governance", "gobierno", "direction", "direccion", "roadmap"] },
];

const CAPABILITY_MATCHERS = [
  { key: "dataManagement", terms: ["data", "datos", "datamanagement", "quality", "calidad", "information", "informacion"] },
  { key: "decisionAnalytics", terms: ["analytics", "analitica", "kpi", "indicador", "decisionmaking", "reporting", "tablero"] },
  { key: "technologyIntegration", terms: ["integration", "integracion", "technology", "tecnologia", "systems", "sistemas", "tools", "herramienta", "platform"] },
  { key: "processAutomation", terms: ["process", "proceso", "automation", "automatizacion", "manual", "workflow", "eficiencia"] },
  { key: "digitalCulture", terms: ["culture", "cultura", "people", "personas", "talent", "talento", "adoption", "adopcion", "training", "capacitacion"] },
  { key: "customerExperience", terms: ["customer", "cliente", "client", "satisfaction", "satisfaccion", "service", "servicio", "experience", "experiencia"] },
  { key: "digitalLeadership", terms: ["strategy", "estrategia", "leadership", "liderazgo", "direction", "direccion", "roadmap"] },
  { key: "digitalGovernance", terms: ["security", "seguridad", "governance", "gobierno", "compliance", "riesgo"] },
];

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .trim();
}

function normalizeCompact(value) {
  return normalizeText(value).replace(/\s+/g, "");
}

function getOptionTags(option) {
  return Array.isArray(option?.tags)
    ? option.tags.filter((tag) => typeof tag === "string" && tag.trim())
    : [];
}

function getSearchText(option) {
  const impacts = option?.impacts && typeof option.impacts === "object"
    ? Object.keys(option.impacts)
    : [];

  return normalizeCompact([
    option?.text,
    option?.optionType,
    ...getOptionTags(option),
    ...impacts,
  ].join(" "));
}

function findMatch(searchText, matchers) {
  return matchers.find((matcher) => matcher.terms.some((term) =>
    searchText.includes(normalizeCompact(term))
  ))?.key || "";
}

function getMetadataValue(option, fieldNames) {
  const impacts = option?.impacts && typeof option.impacts === "object"
    ? option.impacts
    : {};
  const normalizedFields = fieldNames.map(normalizeCompact);
  const entry = Object.entries(impacts).find(([key]) =>
    normalizedFields.includes(normalizeCompact(key))
  );

  return typeof entry?.[1] === "string" ? entry[1] : "";
}

function resolveChoiceKey(value, choices) {
  const normalized = normalizeCompact(value);

  return choices.find((choice) =>
    normalizeCompact(choice.key) === normalized ||
    normalizeCompact(choice.label) === normalized
  )?.key || "";
}

function getDimensionLabel(key) {
  return DIAGNOSIS_DIMENSIONS.find((dimension) => dimension.key === key)?.label || "";
}

function getCapabilityLabel(key) {
  return DIGITAL_CAPABILITIES.find((capability) => capability.key === key)?.label || "";
}

function getLabel(key, choices) {
  return choices.find((choice) => choice.key === key)?.label || "";
}

function toReadableList(values) {
  if (values.length === 0) return "";
  if (values.length === 1) return values[0];
  if (values.length === 2) return `${values[0]} y ${values[1]}`;

  return `${values.slice(0, -1).join(", ")} y ${values[values.length - 1]}`;
}

export function getEffectiveSelectionLimit(cards, configuredMax) {
  const availableCount = (Array.isArray(cards) ? cards : [])
    .filter((card) => Number(card?.id) > 0)
    .length;

  if (availableCount === 0) return 0;

  const configured = Number(configuredMax);
  const requested = Number.isFinite(configured) && configured > 0
    ? Math.floor(configured)
    : availableCount;

  return Math.min(requested, availableCount);
}

export function inferDiagnosisDimension(option) {
  const metadata = resolveChoiceKey(
    getMetadataValue(option, ["dimension", "maturityDimension"]),
    DIAGNOSIS_DIMENSIONS
  );

  return metadata || findMatch(getSearchText(option), DIMENSION_MATCHERS);
}

export function inferCapability(option) {
  const metadata = resolveChoiceKey(
    getMetadataValue(option, ["capability", "digitalCapability"]),
    DIGITAL_CAPABILITIES
  );

  return metadata || findMatch(getSearchText(option), CAPABILITY_MATCHERS);
}

export function createDiagnosisCard(option) {
  const dimension = inferDiagnosisDimension(option);
  const relevance = resolveChoiceKey(
    getMetadataValue(option, ["relevance", "importance"]),
    RELEVANCE_LEVELS
  );
  const observedLevel = resolveChoiceKey(
    getMetadataValue(option, ["observedLevel", "maturityLevel", "currentLevel"]),
    OBSERVED_LEVELS
  );

  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    signalType: dimension === "data"
      ? "Evidencia de datos"
      : dimension === "technology"
        ? "Evidencia tecnologica"
        : dimension === "processes"
          ? "Evidencia operativa"
          : "Senal de madurez digital",
    dimension,
    relevance,
    observedLevel,
    tags: getOptionTags(option),
  };
}

export function buildDiagnosisMap(cards, classifications) {
  const map = Object.fromEntries(
    DIAGNOSIS_DIMENSIONS.map((dimension) => [dimension.key, []])
  );

  (Array.isArray(cards) ? cards : []).forEach((card) => {
    const dimension = classifications?.[card.id]?.dimension || card.dimension;

    if (map[dimension]) map[dimension].push(card);
  });

  return map;
}

export function buildDiagnosisDraft(cards, classifications, maturityLevel) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no hay senales agregadas al diagnostico digital.";
  }

  const dimensionLabels = [...new Set(selectedCards
    .map((card) => classifications?.[card.id]?.dimension || card.dimension)
    .map(getDimensionLabel)
    .filter(Boolean))];
  const maturityLabel = getLabel(maturityLevel, MATURITY_LEVELS);
  const evidence = selectedCards.map((card) => card.text).join(" ");
  const maturitySentence = maturityLabel
    ? `El nivel de madurez estimado es ${maturityLabel}.`
    : "El nivel de madurez debe seguir afinandose con las evidencias disponibles.";

  return `El diagnostico inicial muestra senales relevantes en ${toReadableList(dimensionLabels) || "las dimensiones analizadas"}. ${maturitySentence} Las evidencias que sustentan este analisis son: ${evidence}`;
}

export function getDiagnosisTrace(decisionTrace) {
  return (Array.isArray(decisionTrace) ? decisionTrace : []).find((entry) =>
    normalizeExperienceKey(entry?.phaseName) === "diagnosticoinicial"
  ) || null;
}

export function buildDiagnosisSnapshot(decisionTrace) {
  const trace = getDiagnosisTrace(decisionTrace);
  const texts = Array.isArray(trace?.selectedTexts) ? trace.selectedTexts : [];
  const tags = Array.isArray(trace?.tags) ? trace.tags : [];
  const dimensions = [...new Set(texts
    .map((text, index) => inferDiagnosisDimension({ id: index + 1, text, tags: [] }))
    .filter(Boolean))];

  return {
    hasDiagnosis: Boolean(trace),
    texts,
    tags,
    dimensions,
  };
}

export function createCapabilityCard(option) {
  const capability = inferCapability(option);
  const level = resolveChoiceKey(
    getMetadataValue(option, ["capabilityLevel", "currentLevel", "maturityLevel"]),
    OBSERVED_LEVELS
  );
  const priority = resolveChoiceKey(
    getMetadataValue(option, ["priority", "relevance", "importance"]),
    RELEVANCE_LEVELS
  );

  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    capability,
    level,
    priority,
    tags: getOptionTags(option),
  };
}

export function getCapabilityRelation(card, diagnosisSnapshot) {
  const capability = DIGITAL_CAPABILITIES.find((item) => item.key === card?.capability);

  if (capability && diagnosisSnapshot?.dimensions?.includes(capability.dimension)) {
    return "Se relaciona con una dimension ya detectada en el diagnostico inicial.";
  }

  if (diagnosisSnapshot?.texts?.length > 0) {
    return "Relaciona esta capacidad con las senales recuperadas del diagnostico inicial.";
  }

  return "Usa el contexto del escenario para valorar como esta capacidad habilita la transformacion.";
}

export function buildCapabilityMatrix(cards, classifications) {
  const matrix = Object.fromEntries(
    RELEVANCE_LEVELS.map((priority) => [priority.key, []])
  );

  (Array.isArray(cards) ? cards : []).forEach((card) => {
    const priority = classifications?.[card.id]?.priority || card.priority;
    const capability = classifications?.[card.id]?.capability || card.capability;
    const level = classifications?.[card.id]?.level || card.level;

    if (matrix[priority]) {
      matrix[priority].push({ ...card, capability, level, priority });
    }
  });

  return matrix;
}

export function buildCapabilitiesDraft(cards, classifications, diagnosisSnapshot) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no hay capacidades agregadas a la matriz digital.";
  }

  const matrix = buildCapabilityMatrix(selectedCards, classifications);
  const criticalCards = matrix.alta.length > 0 ? matrix.alta : selectedCards;
  const capabilityLabels = [...new Set(criticalCards
    .map((card) => card.capability)
    .map(getCapabilityLabel)
    .filter(Boolean))];
  const diagnosisReference = diagnosisSnapshot?.texts?.length > 0
    ? "El diagnostico inicial aporta evidencias para sustentar esta decision."
    : "La decision se fundamenta en el contexto actual de la empresa.";

  return `Las capacidades que deben fortalecerse primero son ${toReadableList(capabilityLabels) || "las capacidades seleccionadas"}. ${diagnosisReference} Esta priorizacion busca habilitar una transformacion digital sostenible.`;
}

export function getDimensionLabelForCard(card, classifications) {
  return getDimensionLabel(classifications?.[card?.id]?.dimension || card?.dimension) || "Por clasificar";
}

export function getCapabilityLabelForCard(card, classifications) {
  return getCapabilityLabel(classifications?.[card?.id]?.capability || card?.capability) || "Por clasificar";
}

export function getChoiceLabel(value, choices, fallback) {
  return getLabel(value, choices) || fallback;
}

function getTraceEntries(decisionTrace, phaseNames) {
  const expectedPhases = new Set((Array.isArray(phaseNames) ? phaseNames : [])
    .map(normalizeExperienceKey));

  return (Array.isArray(decisionTrace) ? decisionTrace : []).filter((entry) =>
    expectedPhases.has(normalizeExperienceKey(entry?.phaseName))
  );
}

function getTraceTexts(entries) {
  return [...new Set(entries.flatMap((entry) =>
    Array.isArray(entry?.selectedTexts) ? entry.selectedTexts : []
  ).filter((text) => typeof text === "string" && text.trim()))];
}

function getTraceTags(entries) {
  return [...new Set(entries.flatMap((entry) =>
    Array.isArray(entry?.tags) ? entry.tags : []
  ).filter((tag) => typeof tag === "string" && tag.trim()))];
}

function getGapPriority(card, classifications) {
  return classifications?.[card?.id]?.impact || card?.impact || "";
}

function getRoadmapPeriod(card, classifications) {
  return classifications?.[card?.id]?.period || card?.period || "";
}

function getTrackingArea(card, classifications) {
  return classifications?.[card?.id]?.area || card?.area || "";
}

function inferTrackingArea(option) {
  const searchText = getSearchText(option);

  if (searchText.includes("efficiency") || searchText.includes("eficiencia") || searchText.includes("process")) {
    return "efficiency";
  }

  if (searchText.includes("data") || searchText.includes("datos") || searchText.includes("analytics")) {
    return "data";
  }

  if (searchText.includes("adoption") || searchText.includes("adopcion") || searchText.includes("training")) {
    return "adoption";
  }

  if (searchText.includes("satisfaction") || searchText.includes("satisfaccion") || searchText.includes("customer")) {
    return "satisfaction";
  }

  return "";
}

function getContextReference(context) {
  if (context?.texts?.length > 0) {
    return "Se apoya en las decisiones registradas en las fases anteriores.";
  }

  return "Se fundamenta en el contexto disponible del escenario.";
}

export function buildMaturityContext(decisionTrace, phaseNames) {
  const entries = getTraceEntries(decisionTrace, phaseNames);
  const texts = getTraceTexts(entries);
  const tags = getTraceTags(entries);
  const dimensions = [...new Set(texts.map((text, index) =>
    inferDiagnosisDimension({ id: index + 1, text, tags })
  ).filter(Boolean))];

  return {
    hasContext: entries.length > 0,
    texts,
    tags,
    dimensions,
  };
}

export function createGapCard(option) {
  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    dimension: inferDiagnosisDimension(option),
    impact: resolveChoiceKey(
      getMetadataValue(option, ["impact", "priority", "relevance", "importance"]),
      RELEVANCE_LEVELS
    ),
    urgency: resolveChoiceKey(
      getMetadataValue(option, ["urgency", "priority", "relevance"]),
      RELEVANCE_LEVELS
    ),
    tags: getOptionTags(option),
  };
}

export function buildGapPriorityMap(cards, classifications) {
  const map = Object.fromEntries(
    RELEVANCE_LEVELS.map((level) => [level.key, []])
  );

  (Array.isArray(cards) ? cards : []).forEach((card) => {
    const impact = getGapPriority(card, classifications) || "media";
    const urgency = classifications?.[card?.id]?.urgency || card?.urgency || "media";

    if (map[impact]) {
      map[impact].push({ ...card, impact, urgency });
    }
  });

  return map;
}

export function buildGapDraft(cards, classifications, context) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no hay brechas agregadas a la priorizacion.";
  }

  const priorityMap = buildGapPriorityMap(selectedCards, classifications);
  const criticalCards = priorityMap.alta.length > 0 ? priorityMap.alta : selectedCards;
  const dimensions = [...new Set(criticalCards
    .map((card) => card.dimension)
    .map(getDimensionLabel)
    .filter(Boolean))];

  return `Las brechas prioritarias se concentran en ${toReadableList(dimensions) || "las areas seleccionadas"}. ${getContextReference(context)} La priorizacion combina impacto y urgencia para orientar las siguientes iniciativas.`;
}

export function createInitiativeCard(option) {
  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    dimension: inferDiagnosisDimension(option),
    period: resolveChoiceKey(
      getMetadataValue(option, ["period", "timeframe", "roadmapPeriod", "horizon"]),
      ROADMAP_PERIODS
    ),
    effort: resolveChoiceKey(
      getMetadataValue(option, ["effort", "implementationEffort", "complexity"]),
      RELEVANCE_LEVELS
    ),
    tags: getOptionTags(option),
  };
}

export function buildRoadmap(cards, classifications) {
  const roadmap = Object.fromEntries(
    ROADMAP_PERIODS.map((period) => [period.key, []])
  );

  (Array.isArray(cards) ? cards : []).forEach((card) => {
    const period = getRoadmapPeriod(card, classifications) || "medium";

    if (roadmap[period]) roadmap[period].push({ ...card, period });
  });

  return roadmap;
}

export function buildTransformationDraft(cards, classifications, context) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no hay iniciativas agregadas al plan de transformacion.";
  }

  const roadmap = buildRoadmap(selectedCards, classifications);
  const immediate = roadmap.short.length > 0 ? roadmap.short : selectedCards;
  const initiativeNames = immediate.map((card) => card.text).filter(Boolean);

  return `El plan inicia con ${toReadableList(initiativeNames) || "las iniciativas seleccionadas"}. ${getContextReference(context)} La secuencia propuesta permite avanzar de forma gradual y revisar el progreso en cada horizonte.`;
}

export function createTrackingCard(option) {
  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    area: inferTrackingArea(option),
    tags: getOptionTags(option),
  };
}

export function buildTrackingPanel(cards, classifications) {
  const panel = Object.fromEntries(
    TRACKING_AREAS.map((area) => [area.key, []])
  );

  (Array.isArray(cards) ? cards : []).forEach((card) => {
    const area = getTrackingArea(card, classifications) || "maturity";

    if (panel[area]) panel[area].push({ ...card, area });
  });

  return panel;
}

export function buildTrackingDraft(cards, classifications, context) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no hay indicadores agregados al seguimiento de madurez.";
  }

  const panel = buildTrackingPanel(selectedCards, classifications);
  const activeAreas = TRACKING_AREAS
    .filter((area) => panel[area.key].length > 0)
    .map((area) => area.label);

  return `El seguimiento observara ${toReadableList(activeAreas) || "las areas seleccionadas"}. ${getContextReference(context)} Los indicadores se revisaran de forma periodica para ajustar el plan de transformacion.`;
}

export function getDimensionLabelFromKey(key, fallback = "Por clasificar") {
  return getDimensionLabel(key) || fallback;
}
