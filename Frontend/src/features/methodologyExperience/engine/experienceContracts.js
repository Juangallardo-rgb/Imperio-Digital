const methodologyAliases = {
  designthinking: "DesignThinking",
  bpm: "BPM",
  digitalmaturity: "DigitalMaturity",
  leanstartup: "LeanStartup",
};

export function normalizeExperienceKey(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "");
}

export function normalizeMethodologyCode(value) {
  const normalized = normalizeExperienceKey(value);
  return methodologyAliases[normalized] || String(value || "").trim();
}

export function parseJsonRecord(value) {
  if (!value || typeof value !== "string") return {};

  try {
    const parsed = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? parsed
      : {};
  } catch {
    return {};
  }
}

export function parseStringList(value) {
  if (Array.isArray(value)) {
    return value.filter((item) => typeof item === "string" && item.trim());
  }

  if (!value || typeof value !== "string") return [];

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed)
      ? parsed.filter((item) => typeof item === "string" && item.trim())
      : [];
  } catch {
    return [];
  }
}

export function toFiniteNumber(value, fallback = 0) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

export function createPhaseSubmission({ selectedOptionIds, textAnswer }) {
  const normalizedIds = (selectedOptionIds || [])
    .map((id) => Number(id))
    .filter((id) => Number.isInteger(id) && id > 0);
  const uniqueIds = [...new Set(normalizedIds)];

  return {
    selectedOptionIds: uniqueIds,
    textAnswer: typeof textAnswer === "string" ? textAnswer : "",
  };
}
