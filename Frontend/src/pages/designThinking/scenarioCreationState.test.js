import test from "node:test";
import assert from "node:assert/strict";
import {
  CREATION_MODE_ACTIONS,
  createScenarioRequestCoordinator,
  parseScenarioRequestError,
  retainValidDraftAfterFailure,
  resolveAiDraftGenerationId,
} from "./scenarioCreationState.js";

test("la interfaz define solo los dos modos requeridos", () => {
  assert.deepEqual(
    CREATION_MODE_ACTIONS.map(({ label }) => label),
    ["Completar manualmente", "Generar borrador con IA"]
  );
});

test("el modo manual limpia generationId", () => {
  assert.equal(
    resolveAiDraftGenerationId("Manual", { generationId: "anterior" }),
    null
  );
});

test("el modo IA utiliza el último generationId", () => {
  assert.equal(
    resolveAiDraftGenerationId("AiAssisted", { generationId: "ultimo" }),
    "ultimo"
  );
});

test("un fallo posterior conserva el borrador válido anterior", () => {
  const previous = { generationId: "vigente", title: "Borrador válido" };
  assert.equal(retainValidDraftAfterFailure(previous), previous);
});

test("una respuesta invalidada no se considera actual", () => {
  const coordinator = createScenarioRequestCoordinator();
  const requestId = coordinator.beginDraft();
  coordinator.invalidateDraft();
  assert.equal(coordinator.isCurrentDraft(requestId), false);
});

test("un segundo inicio no reemplaza una solicitud activa", () => {
  const coordinator = createScenarioRequestCoordinator();
  assert.equal(typeof coordinator.beginDraft(), "number");
  assert.equal(coordinator.beginDraft(), null);
});

test("el bloqueo de creación evita doble envío", () => {
  const coordinator = createScenarioRequestCoordinator();
  assert.equal(coordinator.beginCreation(), true);
  assert.equal(coordinator.beginCreation(), false);
  assert.equal(coordinator.isCreating(), true);
  coordinator.finishCreation();
  assert.equal(coordinator.isCreating(), false);
});

test("el error estructurado conserva fase y correlationId", () => {
  const parsed = parseScenarioRequestError(
    {
      response: {
        data: {
          code: "AI_PHASE_GENERATION_FAILED",
          message: "No fue posible generar la fase Hipótesis.",
          phaseName: "Hipótesis",
          correlationId: "corr-1",
          detail: "Estructura inválida.",
          validationErrors: [
            "Opción 2: el texto está duplicado.",
            "Opción 2: tags no debe contener duplicados.",
          ],
        },
      },
    },
    "Error"
  );

  assert.equal(parsed.phaseName, "Hipótesis");
  assert.equal(parsed.correlationId, "corr-1");
  assert.equal(parsed.detail, "Estructura inválida.");
  assert.deepEqual(parsed.validationErrors, [
    "Opción 2: el texto está duplicado.",
    "Opción 2: tags no debe contener duplicados.",
  ]);
});
