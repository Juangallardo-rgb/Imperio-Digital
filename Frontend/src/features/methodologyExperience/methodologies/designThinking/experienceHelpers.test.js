import assert from "node:assert/strict";
import test from "node:test";
import {
  buildEmpathyCounts,
  buildEmpathySummary,
  buildDefinitionDraft,
  buildMvpSummary,
  buildProblemPreview,
  buildStrategySummary,
  cleanSentencePart,
  createEvidenceCard,
  createPrototypeModule,
  createTestCard,
  getEffectiveEvidenceLimit,
  getEffectiveDefinitionLimit,
  getEffectiveIdeaLimit,
  getEffectiveMvpLimit,
  getEvidenceGuidance,
  getDefinitionCue,
  getIdeaLevelLabel,
  getIdeaProfile,
  getIdeaQuadrant,
  getMvpLearningLabel,
  getMvpResourceSummary,
  getMvpScope,
  getPortfolioImpactLabel,
  getPortfolioTags,
  getPortfolioViabilityLabel,
  getPrototypeModuleTypeLabel,
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

test("limita los hallazgos al menor valor entre configuracion y evidencias reales", () => {
  const cards = [{ id: 1 }, { id: 2 }, { id: 3 }];

  assert.equal(getEffectiveEvidenceLimit(cards, 5), 3);
  assert.equal(getEffectiveEvidenceLimit(cards, 2), 2);
  assert.equal(getEffectiveEvidenceLimit(cards, 0), 3);
  assert.equal(getEffectiveEvidenceLimit([], 5), 0);
});

test("actualiza el mapa de empatia cuando cambia la clasificacion", () => {
  const cards = [
    { id: 1, category: "pain" },
    { id: 2, category: "evidence" },
  ];

  assert.deepEqual(
    buildEmpathyCounts(cards, { 1: "need" }),
    { pain: 0, need: 1, behavior: 0, evidence: 1 }
  );
});

test("la orientacion de evidencia es neutral y no revela correccion", () => {
  const guidance = getEvidenceGuidance(
    { text: "Cambiar el color del logo", tags: ["branding"], isCorrect: false },
    "evidence"
  );

  assert.match(guidance, /problema real del usuario/);
  assert.equal(guidance.includes("incorrect"), false);
  assert.equal(guidance.includes("correct"), false);
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
    "Clientes moviles necesitan entender el proceso porque abandonan ante costos inesperados."
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

test("normaliza niveles de Idear y deriva una ubicacion sin inventar extremos", () => {
  assert.deepEqual(
    getIdeaProfile({
      expectedImpactLevel: "High",
      expectedEffortLevel: "bajo",
      expectedViabilityLevel: "Media",
    }),
    {
      impact: "high",
      effort: "low",
      viability: "medium",
      quadrant: "high-low",
      hasImpactAndEffort: true,
      needsManualEvaluation: false,
      isIntermediate: false,
      needsReview: false,
    }
  );
  assert.equal(
    getIdeaQuadrant({ expectedImpactLevel: "Medio", expectedEffortLevel: "Alto" }),
    "unclassified"
  );
  assert.equal(getIdeaLevelLabel("", "viability"), "Por revisar");
  assert.equal(getIdeaLevelLabel("medium", "viability"), "Media");
});

test("mantiene ideas antiguas pendientes hasta que se valoran manualmente", () => {
  const legacyIdea = {
    id: 8,
    expectedImpactLevel: "",
    expectedEffortLevel: null,
    expectedViabilityLevel: undefined,
  };

  assert.deepEqual(
    getIdeaProfile(legacyIdea),
    {
      impact: "",
      effort: "",
      viability: "",
      quadrant: "unclassified",
      hasImpactAndEffort: false,
      needsManualEvaluation: true,
      isIntermediate: false,
      needsReview: true,
    }
  );
  assert.equal(
    getIdeaProfile(legacyIdea, {
      impact: "ALTO",
      effort: "low",
      viability: "Media",
    }).quadrant,
    "high-low"
  );
});

test("limita los votos de Idear a las ideas disponibles", () => {
  assert.equal(
    getEffectiveIdeaLimit([
      { id: 1, maxSelections: 5 },
      { id: 2, maxSelections: 5 },
    ]),
    2
  );
  assert.equal(getEffectiveIdeaLimit([{ id: 1 }]), 1);
  assert.equal(getEffectiveIdeaLimit([], 3), 0);
});

test("resume la cartera de Idear como una estrategia y solo usa tags visibles", () => {
  const ideas = [
    { id: 1, text: "Simplificar el flujo", expectedImpactLevel: "Alto", tags: ["claridad"] },
    { id: 2, text: "Confirmaciones automaticas", expectedImpactLevel: "Bajo", tags: ["claridad", "confianza"] },
  ];
  const summary = buildStrategySummary(ideas);

  assert.match(summary, /Simplificar el flujo y Confirmaciones automaticas/);
  assert.match(summary, /equilibrar impacto/);
  assert.equal(summary.includes(" | "), false);
  assert.equal(getPortfolioImpactLabel(ideas), "Mixto");
  assert.deepEqual(getPortfolioTags(ideas), ["claridad", "confianza"]);
  assert.equal(
    getPortfolioViabilityLabel(ideas, {
      1: { viability: "high" },
      2: { viability: "medium" },
    }),
    "Mixta"
  );
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

test("presenta modulos antiguos con lenguaje pedagogico sin inventar estimaciones", () => {
  const legacyModule = createPrototypeModule({
    id: 5,
    text: "Confirmar la compra",
    optionType: "ConfirmationStep",
    cost: 0,
    timeCost: null,
    riskImpact: 0,
  });

  assert.equal(legacyModule.typeLabel, "Confirmacion");
  assert.equal(legacyModule.hasCostEstimate, false);
  assert.equal(legacyModule.hasTimeEstimate, false);
  assert.equal(legacyModule.effortLevel, "");
  assert.equal(getMvpResourceSummary([legacyModule]).cost, null);
  assert.equal(getMvpResourceSummary([legacyModule]).time, null);
  assert.equal(getMvpLearningLabel([legacyModule]), "Aprendizaje limitado: enfoca la prueba en una sola senal.");
  assert.match(buildMvpSummary([legacyModule]), /sin construir el producto completo/);
  assert.equal(Object.hasOwn(legacyModule, "score"), false);
  assert.equal(Object.hasOwn(legacyModule, "isCorrect"), false);
});

test("resume un MVP con metadata completa y limita el alcance a modulos reales", () => {
  const modules = [
    createPrototypeModule({
      id: 1,
      text: "Mostrar costos claros",
      optionType: "PrototypeFeature",
      tags: ["clarity", "trust"],
      cost: 12,
      timeCost: 2,
      riskImpact: 3,
      maxSelections: 7,
      expectedEffortLevel: "Bajo",
    }),
    createPrototypeModule({
      id: 2,
      text: "Confirmar la accion",
      optionType: "UserFlowStep",
      tags: ["confirmation"],
      cost: 8,
      timeCost: 1,
      riskImpact: 1,
      maxSelections: 7,
      expectedEffortLevel: "Medio",
    }),
  ];

  assert.equal(getPrototypeModuleTypeLabel("PrototypeFeature"), "Funcionalidad del MVP");
  assert.equal(getPrototypeModuleTypeLabel("unknown-value"), "Modulo del MVP");
  assert.equal(getEffectiveMvpLimit(modules), 2);
  assert.equal(getEffectiveMvpLimit([modules[0]]), 1);
  assert.equal(getEffectiveMvpLimit([]), 0);
  assert.deepEqual(getMvpResourceSummary(modules), { cost: 20, time: 3, risk: 4 });
  assert.equal(getMvpLearningLabel(modules), "Aprendizaje esperado: claridad, confianza y confirmacion.");
  assert.match(buildMvpSummary(modules), /2 modulo\(s\) enfocados en validar claridad, confianza y confirmacion/);
});

test("orienta el alcance del MVP sin afectar la evaluacion", () => {
  assert.equal(getMvpScope(0).label, "Sin construir");
  assert.equal(getMvpScope(3).label, "Enfocado");
  assert.equal(getMvpScope(5).label, "Amplio");
  assert.equal(getMvpScope(6).label, "Riesgo de sobreconstruccion");
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

test("limpia conectores repetidos y conserva concordancia en el constructor", () => {
  const preview = buildProblemPreview({
    userSegment: "Los estudiantes universitarios.",
    need: "Necesitan un proceso claro y confiable.",
    insight: "Porque existen demoras y errores al completar el tramite.",
  });

  assert.equal(
    preview,
    "Los estudiantes universitarios necesitan un proceso claro y confiable porque existen demoras y errores al completar el tramite."
  );
  assert.equal(
    cleanSentencePart(" porque existe friccion. ", { removeBecause: true }),
    "existe friccion"
  );
});

test("genera un borrador centrado en el problema sin duplicar el enunciado", () => {
  const draft = buildDefinitionDraft({
    userSegment: "Cliente movil",
    need: "necesita entender el costo final",
    insight: "porque abandona ante cobros inesperados",
  });

  assert.match(draft, /Cliente movil/);
  assert.equal(draft.includes("necesita necesita"), false);
  assert.equal(draft.includes("porque porque"), false);
  assert.equal(buildDefinitionDraft({ userSegment: "", need: "", insight: "" }), "");
});

test("limita las formulaciones de Definir a las opciones disponibles", () => {
  assert.equal(
    getEffectiveDefinitionLimit([
      { id: 1, maxSelections: 2 },
      { id: 2, maxSelections: 2 },
      { id: 3, maxSelections: 2 },
    ]),
    2
  );
  assert.equal(getEffectiveDefinitionLimit([{ id: 1 }]), 1);
  assert.equal(getEffectiveDefinitionLimit([]), 0);
});

test("mantiene una fase sin evidencias compatible con el estado vacio de Empatizar", () => {
  const model = adaptCurrentSimulation({
    current: {
      attemptId: 31,
      methodologyCode: "DesignThinking",
      methodologyName: "Design Thinking",
      currentPhaseName: "Empatizar",
      currentPhaseOrder: 1,
      phaseOrder: [{ phaseName: "Empatizar", phaseOrder: 1, phaseWeight: 20 }],
      currentPhaseOptions: [],
    },
  });

  assert.equal(model.isCompatible, true);
  assert.deepEqual(model.options, []);
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
