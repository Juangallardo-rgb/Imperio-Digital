function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase();
}

const phaseCatalog = {
  empatizar: { interaction: "Analisis de evidencia", requiredTypes: ["evidence", "painpoint"] },
  definir: { interaction: "Constructor de problema", requiredTypes: ["problemstatement"] },
  idear: { interaction: "Matriz impacto-esfuerzo", requiredTypes: ["solution"] },
  prototipar: { interaction: "Constructor de MVP", requiredTypes: ["prototypefeature", "userflowstep"] },
  evaluar: { interaction: "Laboratorio de pruebas", requiredTypes: ["kpi", "test"] },
};

function getOptionPhaseName(option) {
  return normalizeText(option?.phaseName);
}

function hasRichMetadata(option, phaseKey) {
  const hasTags = Array.isArray(option?.tags)
    ? option.tags.length > 0
    : typeof option?.tagsJson === "string" && option.tagsJson !== "[]";

  if (phaseKey === "idear") {
    return Boolean(
      option?.expectedImpactLevel &&
      option?.expectedEffortLevel &&
      option?.expectedViabilityLevel
    );
  }

  if (phaseKey === "prototipar") {
    return Boolean(
      hasTags || option?.impactJson || option?.impacts
    );
  }

  return Boolean(hasTags || option?.optionType);
}

export function getPhaseExperienceDescriptor(phaseName) {
  const phaseKey = normalizeText(phaseName);
  return phaseCatalog[phaseKey] || {
    interaction: "Actividad generica",
    requiredTypes: [],
  };
}

export function getScenarioExperienceStatus(scenario) {
  const methodology = String(
    scenario?.methodology || scenario?.methodologyCode || ""
  );
  const isDesignThinking = normalizeText(methodology) === "designthinking";
  const options = Array.isArray(scenario?.options) ? scenario.options : [];
  const phases = Array.isArray(scenario?.phaseSettings) ? scenario.phaseSettings : [];

  const phaseStatuses = phases.map((phase) => {
    const phaseKey = normalizeText(phase?.phaseName);
    const phaseOptions = options.filter(
      (option) => getOptionPhaseName(option) === phaseKey
    );
    const descriptor = getPhaseExperienceDescriptor(phase?.phaseName);
    const types = new Set(
      phaseOptions.map((option) => normalizeText(option?.optionType))
    );
    const hasExpectedType =
      descriptor.requiredTypes.length === 0 ||
      descriptor.requiredTypes.some((type) => types.has(type));
    const richOptions = phaseOptions.filter((option) =>
      hasRichMetadata(option, phaseKey)
    );
    const status =
      phaseOptions.length === 0
        ? "fallback"
        : hasExpectedType && richOptions.length > 0
        ? "complete"
        : "adapted";

    return {
      phaseName: phase?.phaseName || "",
      interaction: descriptor.interaction,
      optionCount: phaseOptions.length,
      richOptionCount: richOptions.length,
      status,
    };
  });

  const completeCount = phaseStatuses.filter((phase) => phase.status === "complete").length;
  const fallbackCount = phaseStatuses.filter((phase) => phase.status === "fallback").length;

  return {
    isDesignThinking,
    phaseStatuses,
    status:
      !isDesignThinking || fallbackCount > 0
        ? "fallback"
        : completeCount === phaseStatuses.length && phaseStatuses.length > 0
        ? "complete"
        : "adapted",
  };
}

export function resolveLegacyScenarioExperience(model) {
  const phaseSettings = (model?.phaseOrder || []).map((phase) => ({
    phaseName: phase.name,
  }));
  const status = getScenarioExperienceStatus({
    methodology: model?.methodology?.code,
    phaseSettings,
    options: model?.options,
  });
  const activePhase = status.phaseStatuses.find(
    (phase) => normalizeText(phase.phaseName) === normalizeText(model?.phase?.name)
  );

  return {
    ...status,
    activePhase,
    useGenericActivity: activePhase?.status === "fallback",
  };
}
