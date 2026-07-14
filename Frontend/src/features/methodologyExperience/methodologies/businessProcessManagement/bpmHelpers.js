import { normalizeExperienceKey } from "../../engine/experienceContracts.js";

export const PROCESS_AREAS = Object.freeze([
  { key: "recepcion", label: "Recepcion" },
  { key: "registro", label: "Registro" },
  { key: "preparacion", label: "Preparacion" },
  { key: "entrega", label: "Entrega" },
  { key: "seguimiento", label: "Seguimiento" },
]);

export const PROCESS_RELATIONSHIPS = Object.freeze([
  { key: "alta", label: "Alta" },
  { key: "media", label: "Media" },
  { key: "baja", label: "Baja" },
]);

export const BOTTLENECK_EFFECTS = Object.freeze([
  { key: "retraso", label: "Retraso" },
  { key: "error", label: "Error" },
  { key: "acumulacion", label: "Acumulacion" },
  { key: "trazabilidad", label: "Falta de trazabilidad" },
]);

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase();
}

function uniqueTexts(values) {
  return [...new Set(
    (Array.isArray(values) ? values : [])
      .filter((value) => typeof value === "string" && value.trim())
      .map((value) => value.trim())
  )];
}

function getOptionTags(option) {
  return Array.isArray(option?.tags)
    ? option.tags.filter((tag) => typeof tag === "string" && tag.trim())
    : [];
}

function getSearchText(option) {
  return normalizeText(
    `${option?.text || ""} ${getOptionTags(option).join(" ")}`
  );
}

function hasAny(text, terms) {
  return terms.some((term) => text.includes(term));
}

function getChoiceLabel(value, choices, fallback) {
  return choices.find((choice) => choice.key === value)?.label || fallback;
}

function inferProcessArea(option) {
  const text = getSearchText(option);

  if (hasAny(text, ["seguimiento", "confirmacion", "estado", "notificacion"])) {
    return "seguimiento";
  }
  if (hasAny(text, ["entrega", "despacho", "salida", "respuesta final"])) {
    return "entrega";
  }
  if (hasAny(text, ["cocina", "preparacion", "produccion", "ejecucion"])) {
    return "preparacion";
  }
  if (hasAny(text, ["registro", "aprobacion", "revision manual", "trazabilidad", "validacion"])) {
    return "registro";
  }
  if (hasAny(text, ["solicitud", "whatsapp", "llamada", "recepcion", "entrada", "pedido recibido"])) {
    return "recepcion";
  }

  return "";
}

function inferRelationship(option) {
  const text = getSearchText(option);

  if (hasAny(text, ["demora", "delay", "ciclo", "trazabilidad", "aprobacion", "responsable", "manual", "handoff", "proceso"])) {
    return "alta";
  }
  if (hasAny(text, ["marca", "colores", "paleta", "publicidad", "redes sociales", "imagenes", "sitio web"])) {
    return "baja";
  }

  return "";
}

function inferBottleneckEffect(option) {
  const text = getSearchText(option);

  if (hasAny(text, ["trazabilidad", "visibilidad", "sin saber", "estado"])) {
    return "trazabilidad";
  }
  if (hasAny(text, ["error", "incompleta", "incorrect", "retrabajo"])) {
    return "error";
  }
  if (hasAny(text, ["demora", "retraso", "lento", "tarda", "depende de una sola persona", "delay"])) {
    return "retraso";
  }
  if (hasAny(text, ["acumulacion", "cola", "concentra", "espera", "saturacion"])) {
    return "acumulacion";
  }

  return "";
}

function splitFlowSegments(text) {
  const segments = String(text || "")
    .split(/\s*(?:\u2192|->)\s*/)
    .map((segment) => segment.trim())
    .filter(Boolean);

  return segments.length > 0 ? segments : [String(text || "").trim()].filter(Boolean);
}

export function getEffectiveSelectionLimit(cards, configuredMax) {
  const availableOptionsCount = (Array.isArray(cards) ? cards : [])
    .filter((card) => Number(card?.id) > 0)
    .length;

  if (availableOptionsCount === 0) return 0;

  const parsedMax = Number(configuredMax);
  const requestedMax = Number.isFinite(parsedMax) && parsedMax > 0
    ? Math.floor(parsedMax)
    : availableOptionsCount;

  return Math.min(requestedMax || availableOptionsCount, availableOptionsCount);
}

export function createProcessEvidenceCard(option) {
  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    type: "Evidencia del proceso",
    relationship: inferRelationship(option),
    area: inferProcessArea(option),
    tags: getOptionTags(option),
  };
}

export function createCurrentProcessStepCard(option) {
  const text = String(option?.text || "");
  const searchableText = getSearchText(option);

  return {
    id: Number(option?.id),
    text,
    type: "Paso del proceso actual",
    stage: inferProcessArea(option),
    flowSegments: splitFlowSegments(text),
    isLessOperational: hasAny(searchableText, [
      "marca",
      "colores",
      "publicidad",
      "redes sociales",
      "imagenes",
      "sitio web",
      "marketing",
    ]),
    tags: getOptionTags(option),
  };
}

export function createBottleneckCard(option) {
  return {
    id: Number(option?.id),
    text: String(option?.text || ""),
    type: "Friccion del proceso",
    location: inferProcessArea(option),
    effect: inferBottleneckEffect(option),
    tags: getOptionTags(option),
  };
}

export function getProcessAreaLabel(value, fallback = "Por clasificar") {
  return getChoiceLabel(value, PROCESS_AREAS, fallback);
}

export function getRelationshipLabel(value, fallback = "Por evaluar") {
  return getChoiceLabel(value, PROCESS_RELATIONSHIPS, fallback);
}

export function getBottleneckEffectLabel(value, fallback = "Por evaluar") {
  return getChoiceLabel(value, BOTTLENECK_EFFECTS, fallback);
}

export function buildCurrentProcessFlow(cards, classifications) {
  return (Array.isArray(cards) ? cards : []).flatMap((card) => {
    const stage = classifications?.[card.id]?.stage || card.stage;

    return (Array.isArray(card.flowSegments) ? card.flowSegments : [])
      .filter(Boolean)
      .map((text, index) => ({
        id: `${card.id}-${index}`,
        text,
        stage,
      }));
  });
}

export function buildBpmPreviousContext(decisionTrace) {
  const entries = Array.isArray(decisionTrace) ? decisionTrace : [];
  const identifyKey = normalizeExperienceKey("Identificar proceso");
  const modelKey = normalizeExperienceKey("Modelar proceso actual");

  const getTexts = (phaseKey) => uniqueTexts(
    entries
      .filter((entry) => normalizeExperienceKey(entry?.phaseName) === phaseKey)
      .flatMap((entry) => entry?.selectedTexts || [])
  );

  return {
    processSignals: getTexts(identifyKey),
    flowSteps: getTexts(modelKey),
  };
}

export function buildCriticalProcessDraft(cards, classifications) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no has agregado evidencias al diagnostico del proceso.";
  }

  const areas = uniqueTexts(selectedCards.map((card) =>
    getProcessAreaLabel(classifications?.[card.id]?.area || card.area, "")
  ));

  return `El proceso critico se evidencia en ${areas.join(", ") || "las etapas seleccionadas"}. Las senales priorizadas muestran demoras, errores o falta de trazabilidad que requieren analisis operativo.`;
}

export function buildCurrentFlowDraft(cards, classifications) {
  const flow = buildCurrentProcessFlow(cards, classifications);

  if (flow.length === 0) {
    return "Aun no has agregado pasos al flujo actual del proceso.";
  }

  return `El flujo actual incluye ${flow.map((step) => step.text).join(", ")}. Esta representacion permite identificar donde se concentran las transferencias manuales, los retrasos y las perdidas de trazabilidad.`;
}

export function buildBottleneckDraft(cards, classifications, previousContext) {
  const selectedCards = Array.isArray(cards) ? cards : [];

  if (selectedCards.length === 0) {
    return "Aun no has marcado fricciones como cuello de botella.";
  }

  const effects = uniqueTexts(selectedCards.map((card) =>
    getBottleneckEffectLabel(classifications?.[card.id]?.effect || card.effect, "")
  ));
  const contextReference = previousContext?.flowSteps?.length > 0
    ? "Se relaciona con el flujo actual que construiste en la fase anterior."
    : "Se fundamenta en las fricciones observadas en el proceso.";

  return `El cuello de botella se explica por ${effects.join(", ") || "las fricciones seleccionadas"}. ${contextReference}`;
}
