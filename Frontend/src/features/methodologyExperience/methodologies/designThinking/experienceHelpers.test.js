import assert from "node:assert/strict";
import test from "node:test";
import {
  buildEmpathySummary,
  buildProblemPreview,
  createEvidenceCard,
  createPrototypeModule,
  createTestCard,
  getDefinitionCue,
  getIdeaQuadrant,
  getTraceForPhase,
} from "./experienceHelpers.js";
import {
  getScenarioExperienceStatus,
  resolveLegacyScenarioExperience,
} from "../../adapters/legacyScenarioAdapter.js";
import { createPhaseSubmission } from "../../engine/experienceContracts.js";
import { adaptCurrentSimulation } from "../../adapters/currentSimulationAdapter.js";
import { adaptDesignThinkingResults } from "./designThinkingResultsAdapter.js";

test("adapta evidencia sin exponer datos de correccion", () => {
  const card = createEvidenceCard({
    id: 7,
    text: "Los usuarios abandonan cuando no comprenden el costo final.",
    tags: ["friction", "abandonment"],
    isCorrect: true,
    score: 100,
  });

  assert.equal(card.id, 7);
  assert.equal(card.category, "pain");
  assert.equal(Object.hasOwn(card, "isCorrect"), false);
  assert.equal(Object.hasOwn(card, "score"), false);
});

test("resume solo los hallazgos seleccionados", () => {
  const summary = buildEmpathySummary(
    [
      { id: 1, category: "pain" },
      { id: 2, category: "need" },
    ],
    { 2: "behavior" }
  );

  assert.match(summary, /2 hallazgo/);
  assert.match(summary, /1 dolor/);
  assert.match(summary, /1 comportamiento/);
});

test("mantiene continuidad de Empatizar sin depender de tildes", () => {
  const trace = [{ phaseName: "Empatizar", selectedTexts: ["Hallazgo"] }];
  assert.deepEqual(getTraceForPhase(trace, "EMPATIZAR"), trace[0]);
});

test("construye un problema con usuario, necesidad e insight", () => {
  const preview = buildProblemPreview({
    userSegment: "Clientes moviles",
    need: "entender el proceso",
    insight: "abandonan ante costos inesperados",
  });

  assert.equal(
    preview,
    "Clientes moviles necesita entender el proceso porque abandonan ante costos inesperados."
  );
});

test("las pistas pedagogicas no declaran una respuesta correcta", () => {
  const cue = getDefinitionCue("Crear una plataforma para resolver el caso");
  assert.match(cue, /solucion/);
  assert.equal(cue.includes("correcta"), false);
});

test("ubica ideas solo con niveles configurados", () => {
  assert.equal(
    getIdeaQuadrant({ expectedImpactLevel: "Alto", expectedEffortLevel: "Bajo" }),
    "high-low"
  );
  assert.equal(getIdeaQuadrant({ expectedImpactLevel: "Alto" }), "unclassified");
});

test("adapta modulos y pruebas sin campos de correccion", () => {
  const module = createPrototypeModule({
    id: 4,
    text: "Confirmacion de pago",
    tags: ["trust"],
    impacts: { satisfaction: 5 },
    isCorrect: true,
    score: 100,
  });
  const testCard = createTestCard({
    id: 8,
    text: "La tasa de abandono sigue siendo alta.",
    tags: ["abandonment"],
    isCorrect: true,
  });

  assert.equal(module.tags[0], "trust");
  assert.equal(Object.hasOwn(module, "isCorrect"), false);
  assert.equal(testCard.lens, "Problema observado");
  assert.equal(Object.hasOwn(testCard, "isCorrect"), false);
});

test("distingue un escenario V2 completo de uno heredado sin metadata", () => {
  const phaseSettings = [
    "Empatizar",
    "Definir",
    "Idear",
    "Prototipar",
    "Evaluar",
  ].map((phaseName) => ({ phaseName }));
  const complete = getScenarioExperienceStatus({
    methodology: "DesignThinking",
    phaseSettings,
    options: [
      { phaseName: "Empatizar", optionType: "Evidence", tagsJson: '["research"]' },
      { phaseName: "Definir", optionType: "ProblemStatement" },
      {
        phaseName: "Idear",
        optionType: "Solution",
        expectedImpactLevel: "Alto",
        expectedEffortLevel: "Bajo",
        expectedViabilityLevel: "Alta",
      },
      { phaseName: "Prototipar", optionType: "PrototypeFeature", tagsJson: '["mvp"]' },
      { phaseName: "Evaluar", optionType: "Test" },
    ],
  });

  assert.equal(complete.status, "complete");

  const emptyTags = getScenarioExperienceStatus({
    methodology: "DesignThinking",
    phaseSettings: [{ phaseName: "Prototipar" }],
    options: [
      { phaseName: "Prototipar", optionType: "PrototypeFeature", tagsJson: "[]" },
    ],
  });
  assert.equal(emptyTags.status, "adapted");
});

test("usa la actividad generica cuando la fase actual del escenario antiguo no tiene opciones", () => {
  const compatibility = resolveLegacyScenarioExperience({
    methodology: { code: "DesignThinking" },
    phaseOrder: [{ name: "Empatizar" }],
    phase: { name: "Empatizar" },
    options: [],
  });

  assert.equal(compatibility.useGenericActivity, true);
});

test("normaliza el contrato de envio de una fase", () => {
  assert.deepEqual(
    createPhaseSubmission({ selectedOptionIds: [2, "2", 4, 0, "x"], textAnswer: "Justificacion" }),
    { selectedOptionIds: [2, 4], textAnswer: "Justificacion" }
  );
});

test("no expone correccion ni puntaje al adaptar una simulacion activa", () => {
  const model = adaptCurrentSimulation({
    current: {
      attemptId: 22,
      methodologyCode: "DesignThinking",
      methodologyName: "Design Thinking",
      currentPhaseName: "Empatizar",
      currentPhaseOrder: 1,
      phaseOrder: [{ phaseName: "Empatizar", phaseOrder: 1, phaseWeight: 20 }],
      currentPhaseOptions: [{
        id: 8,
        phaseName: "Empatizar",
        optionType: "Evidence",
        text: "Entrevista con usuarios",
        score: 100,
        isCorrect: true,
      }],
    },
  });

  assert.equal(Object.hasOwn(model.options[0], "score"), false);
  assert.equal(Object.hasOwn(model.options[0], "isCorrect"), false);
});

test("construye el recorrido V2 solo con datos de un resultado final", () => {
  const phaseNames = ["Empatizar", "Definir", "Idear", "Prototipar", "Evaluar"];
  const journey = adaptDesignThinkingResults({
    finalScore: 84,
    phaseScores: phaseNames.map((phaseName) => ({
      phaseName,
      score: 75,
      feedback: "Feedback final de la fase",
    })),
    phaseReviews: phaseNames.map((phaseName, index) => ({
      phaseName,
      textAnswer: `Justificacion ${phaseName}`,
      options: [{
        text: `Decision ${phaseName}`,
        wasSelected: true,
        isCorrect: true,
        cost: index,
        timeCost: 1,
        riskImpact: 0,
        tagsJson: '["aprendizaje"]',
      }],
    })),
  });

  assert.equal(journey.phases.length, 5);
  assert.match(journey.phases[0].highlights[0], /Empatizar/);
  assert.ok(journey.recognitions.includes("Consultor de transformacion"));
});
