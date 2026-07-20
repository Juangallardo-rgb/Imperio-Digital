export const CREATION_MODE_ACTIONS = Object.freeze([
  { mode: "Manual", label: "Completar manualmente" },
  { mode: "AiAssisted", label: "Generar borrador con IA" },
]);

export function createScenarioRequestCoordinator() {
  let draftSequence = 0;
  let activeDraftRequest = null;
  let creationLocked = false;

  return {
    beginDraft() {
      if (activeDraftRequest !== null) return null;
      activeDraftRequest = ++draftSequence;
      return activeDraftRequest;
    },
    isCurrentDraft(requestId) {
      return requestId !== null &&
        activeDraftRequest === requestId &&
        draftSequence === requestId;
    },
    finishDraft(requestId) {
      if (this.isCurrentDraft(requestId)) activeDraftRequest = null;
    },
    invalidateDraft() {
      draftSequence += 1;
      activeDraftRequest = null;
    },
    beginCreation() {
      if (creationLocked) return false;
      creationLocked = true;
      return true;
    },
    finishCreation() {
      creationLocked = false;
    },
    isCreating() {
      return creationLocked;
    },
  };
}

export function resolveAiDraftGenerationId(creationMode, aiDraft) {
  return creationMode === "AiAssisted" ? aiDraft?.generationId ?? null : null;
}

export function retainValidDraftAfterFailure(previousDraft) {
  return previousDraft ?? null;
}

export function parseScenarioRequestError(error, fallbackMessage) {
  const responseData = error?.response?.data;

  if (typeof responseData === "string" && responseData.trim()) {
    return { message: responseData.trim() };
  }

  return {
    code: responseData?.code ?? null,
    message:
      typeof responseData?.message === "string"
        ? responseData.message
        : fallbackMessage,
    detail:
      typeof responseData?.detail === "string" ? responseData.detail : null,
    phaseName:
      typeof responseData?.phaseName === "string"
        ? responseData.phaseName
        : null,
    correlationId: responseData?.correlationId ?? null,
  };
}
