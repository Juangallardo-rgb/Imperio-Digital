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
