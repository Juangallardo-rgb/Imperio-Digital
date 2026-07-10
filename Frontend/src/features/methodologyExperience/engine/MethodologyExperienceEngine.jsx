import "../shared/methodologyExperience.css";
import { adaptCurrentSimulation } from "../adapters/currentSimulationAdapter";
import { resolveLegacyScenarioExperience } from "../adapters/legacyScenarioAdapter";
import { resolveMethodologyExperience } from "../registry/methodologyExperienceRegistry";
import LegacyExperienceFallback from "../fallback/LegacyExperienceFallback";
import ExperienceShell from "../shared/ExperienceShell";
import GenericPhaseExperience from "../shared/GenericPhaseExperience";
import ExperienceErrorBoundary from "./ExperienceErrorBoundary";
import { isMethodologyExperienceV2Enabled } from "./featureFlags";

function MethodologyExperienceEngine({
  current,
  selectedOptionIds,
  textAnswer,
  phaseFeedback,
  message,
  maxSelections,
  totals,
  kpiItems,
  triggeredEvent,
  submitting,
  onToggleOption,
  onTextAnswerChange,
  onSubmit,
  onContinue,
  fallback,
}) {
  if (!isMethodologyExperienceV2Enabled()) {
    return <LegacyExperienceFallback>{fallback}</LegacyExperienceFallback>;
  }

  const model = adaptCurrentSimulation({
    current,
    selectedOptionIds,
    textAnswer,
    phaseFeedback,
    maxSelections,
    totals,
    kpiItems,
    triggeredEvent,
  });
  const resolution = resolveMethodologyExperience(model);

  if (!resolution.enabled) {
    return <LegacyExperienceFallback>{fallback}</LegacyExperienceFallback>;
  }

  const scenarioCompatibility = resolveLegacyScenarioExperience(model);
  const Activity = scenarioCompatibility.useGenericActivity
    ? GenericPhaseExperience
    : resolution.phase.component;
  const resetKey = `${model.attemptId}-${model.phase.name}-${Boolean(phaseFeedback)}`;

  return (
    <ExperienceErrorBoundary
      resetKey={resetKey}
      fallback={<LegacyExperienceFallback>{fallback}</LegacyExperienceFallback>}
    >
      <ExperienceShell
        model={model}
        phaseFeedback={phaseFeedback}
        message={message}
        submitting={submitting}
        onContinue={onContinue}
      >
        <Activity
          model={model}
          onToggleOption={onToggleOption}
          onTextAnswerChange={onTextAnswerChange}
          onSubmit={onSubmit}
          submitting={submitting}
        />
      </ExperienceShell>
    </ExperienceErrorBoundary>
  );
}

export default MethodologyExperienceEngine;
