import { normalizeExperienceKey } from "../engine/experienceContracts";
import designThinkingManifest from "../methodologies/designThinking/manifest";

const legacyFallbackManifest = (methodologyCode) => ({
  methodologyCode,
  usesLegacyExperience: true,
  phases: {},
});

export const methodologyExperienceRegistry = Object.freeze({
  DesignThinking: designThinkingManifest,
  BPM: legacyFallbackManifest("BPM"),
  DigitalMaturity: legacyFallbackManifest("DigitalMaturity"),
  LeanStartup: legacyFallbackManifest("LeanStartup"),
});

function findPhaseDefinition(manifest, phaseName) {
  const phaseKey = normalizeExperienceKey(phaseName);

  return Object.entries(manifest.phases || {}).find(
    ([name]) => normalizeExperienceKey(name) === phaseKey
  )?.[1];
}

export function resolveMethodologyExperience(model) {
  if (!model?.isCompatible) {
    return { enabled: false, reason: "invalid-current-simulation" };
  }

  const manifest = methodologyExperienceRegistry[model.methodology.code];

  if (!manifest || manifest.usesLegacyExperience) {
    return { enabled: false, reason: "legacy-methodology" };
  }

  const phase = findPhaseDefinition(manifest, model.phase.name);

  if (!phase?.component) {
    return { enabled: false, reason: "phase-not-configured" };
  }

  if (!model.hasOptions && phase.handlesEmptyOptions !== true) {
    return { enabled: false, reason: "empty-options" };
  }

  return { enabled: true, manifest, phase };
}
