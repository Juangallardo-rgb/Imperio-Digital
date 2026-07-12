import {
  normalizeExperienceKey,
  normalizeMethodologyCode,
  parseJsonRecord,
  parseStringList,
  toFiniteNumber,
} from "../engine/experienceContracts.js";

function readValue(record, camelName, pascalName) {
  return record?.[camelName] ?? record?.[pascalName];
}

function adaptOption(option) {
  const impactJson = option?.impactJson || "";
  const tagsJson = option?.tagsJson || "";

  return {
    id: Number(option?.id),
    phaseName: String(option?.phaseName || ""),
    optionType: String(option?.optionType || "General"),
    text: String(option?.text || ""),
    impacts: parseJsonRecord(impactJson),
    tags: parseStringList(tagsJson),
    cost: toFiniteNumber(option?.cost),
    timeCost: toFiniteNumber(option?.timeCost),
    riskImpact: toFiniteNumber(option?.riskImpact),
    maxSelections: toFiniteNumber(option?.maxSelections),
    expectedImpactLevel: String(option?.expectedImpactLevel || ""),
    expectedEffortLevel: String(option?.expectedEffortLevel || ""),
    expectedViabilityLevel: String(option?.expectedViabilityLevel || ""),
  };
}

function adaptDecisionTrace(value) {
  if (!value || typeof value !== "string") return [];

  try {
    const parsed = JSON.parse(value);

    if (!Array.isArray(parsed)) return [];

    return parsed.map((item) => {
      const selectedOptionIds = readValue(
        item,
        "selectedOptionIds",
        "SelectedOptionIds"
      );
      const selectedTexts = readValue(item, "selectedTexts", "SelectedTexts");
      const tags = readValue(item, "tags", "Tags");

      return {
        phaseName: String(readValue(item, "phaseName", "PhaseName") || ""),
        selectedOptionIds: (Array.isArray(selectedOptionIds) ? selectedOptionIds : [])
          .map((id) => Number(id))
          .filter((id) => Number.isInteger(id) && id > 0),
        selectedTexts: (Array.isArray(selectedTexts) ? selectedTexts : [])
          .filter((text) => typeof text === "string" && text.trim()),
        tags: (Array.isArray(tags) ? tags : [])
          .filter((tag) => typeof tag === "string" && tag.trim()),
        budgetUsed: toFiniteNumber(readValue(item, "budgetUsed", "BudgetUsed")),
        timeUsed: toFiniteNumber(readValue(item, "timeUsed", "TimeUsed")),
      };
    });
  } catch {
    return [];
  }
}

function isCurrentPhaseInOrder(phaseName, phaseOrder) {
  const currentKey = normalizeExperienceKey(phaseName);
  return phaseOrder.some(
    (phase) => normalizeExperienceKey(phase.name) === currentKey
  );
}

export function adaptCurrentSimulation({
  current,
  selectedOptionIds,
  textAnswer,
  phaseFeedback,
  maxSelections,
  totals,
  kpiItems,
  triggeredEvent,
}) {
  const phaseOrder = Array.isArray(current?.phaseOrder)
    ? current.phaseOrder.map((phase) => ({
        name: String(phase?.phaseName || ""),
        order: toFiniteNumber(phase?.phaseOrder),
        weight: toFiniteNumber(phase?.phaseWeight),
      }))
    : [];
  const currentPhaseName = String(current?.currentPhaseName || "");
  const options = Array.isArray(current?.currentPhaseOptions)
    ? current.currentPhaseOptions.map(adaptOption).filter((option) => option.id > 0)
    : [];

  return {
    attemptId: Number(current?.attemptId),
    methodology: {
      code: normalizeMethodologyCode(current?.methodologyCode),
      name: String(current?.methodologyName || current?.methodologyCode || ""),
    },
    scenario: {
      title: String(current?.scenarioTitle || ""),
      description: String(current?.scenarioDescription || ""),
      companyType: String(current?.scenarioCompanyType || ""),
      problem: String(current?.scenarioProblem || ""),
      targetUser: String(current?.scenarioTargetUser || ""),
      constraints: String(current?.scenarioConstraints || ""),
    },
    phase: {
      name: currentPhaseName,
      order: toFiniteNumber(current?.currentPhaseOrder),
      completed: Array.isArray(current?.completedPhases)
        ? current.completedPhases.map((phaseName) => String(phaseName))
        : [],
    },
    phaseOrder,
    options,
    hasOptions: options.length > 0,
    selection: {
      selectedOptionIds: Array.isArray(selectedOptionIds) ? selectedOptionIds : [],
      textAnswer: typeof textAnswer === "string" ? textAnswer : "",
      maxSelections: toFiniteNumber(maxSelections, 1),
      totals: {
        cost: toFiniteNumber(totals?.cost),
        time: toFiniteNumber(totals?.time),
        risk: toFiniteNumber(totals?.risk),
      },
    },
    resources: {
      initialBudget: toFiniteNumber(current?.initialBudget),
      remainingBudget: toFiniteNumber(
        phaseFeedback?.remainingBudget ?? current?.remainingBudget
      ),
      initialTimeWeeks: toFiniteNumber(current?.initialTimeWeeks),
      remainingTimeWeeks: toFiniteNumber(
        phaseFeedback?.remainingTimeWeeks ?? current?.remainingTimeWeeks
      ),
      riskLevel: toFiniteNumber(phaseFeedback?.riskLevel ?? current?.riskLevel),
    },
    kpis: Array.isArray(kpiItems) ? kpiItems : [],
    decisionTrace: adaptDecisionTrace(current?.decisionTraceJson),
    triggeredEvent,
    isCompatible:
      Number(current?.attemptId) > 0 &&
      Boolean(currentPhaseName) &&
      phaseOrder.length > 0 &&
      isCurrentPhaseInOrder(currentPhaseName, phaseOrder),
  };
}
