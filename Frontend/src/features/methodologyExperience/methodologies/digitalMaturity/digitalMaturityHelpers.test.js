import assert from "node:assert/strict";
import test from "node:test";
import { createPhaseSubmission } from "../../engine/experienceContracts.js";
import {
  buildCapabilitiesDraft,
  buildCapabilityMatrix,
  buildDiagnosisDraft,
  buildDiagnosisMap,
  buildDiagnosisSnapshot,
  buildGapDraft,
  buildGapPriorityMap,
  buildMaturityContext,
  buildRoadmap,
  buildTrackingPanel,
  createCapabilityCard,
  createDiagnosisCard,
  createGapCard,
  createInitiativeCard,
  createTrackingCard,
  getEffectiveSelectionLimit,
} from "./digitalMaturityHelpers.js";

test("adapta senales de diagnostico sin exponer datos de correccion", () => {
  const card = createDiagnosisCard({
    id: 4,
    text: "Los datos se registran manualmente y no se usan para tomar decisiones.",
    tags: ["data", "manual-process", "decision-making"],
    cost: 0,
    score: 100,
    isCorrect: true,
  });

  assert.equal(card.dimension, "data");
  assert.equal(card.signalType, "Evidencia de datos");
  assert.equal("score" in card, false);
  assert.equal("isCorrect" in card, false);
});

test("construye un mapa y borrador de diagnostico con el limite real", () => {
  const cards = [
    createDiagnosisCard({ id: 1, text: "Procesos manuales", tags: ["processes", "manual-work"] }),
    createDiagnosisCard({ id: 2, text: "Datos dispersos", tags: ["data"] }),
    createDiagnosisCard({ id: 3, text: "Sistemas sin integracion", tags: ["integration"] }),
  ];
  const classifications = {
    1: { dimension: "processes" },
    2: { dimension: "data" },
    3: { dimension: "technology" },
  };
  const map = buildDiagnosisMap(cards, classifications);
  const draft = buildDiagnosisDraft(cards, classifications, "inicial");

  assert.equal(getEffectiveSelectionLimit(cards, 5), 3);
  assert.equal(getEffectiveSelectionLimit([cards[0]], 4), 1);
  assert.equal(getEffectiveSelectionLimit([], 4), 0);
  assert.equal(map.processes.length, 1);
  assert.equal(map.data.length, 1);
  assert.match(draft, /nivel de madurez estimado es Inicial/);
});

test("recupera el diagnostico previo y construye una matriz de capacidades", () => {
  const diagnosis = buildDiagnosisSnapshot([
    {
      phaseName: "Diagnostico inicial",
      selectedTexts: ["Los datos se registran manualmente."],
      tags: ["data", "manual-process"],
    },
  ]);
  const card = createCapabilityCard({
    id: 7,
    text: "Ausencia de indicadores consolidados para medir desempeno.",
    tags: ["data", "analytics", "kpi"],
  });
  const classifications = {
    7: { capability: "dataManagement", level: "bajo", priority: "alta" },
  };
  const matrix = buildCapabilityMatrix([card], classifications);
  const draft = buildCapabilitiesDraft([card], classifications, diagnosis);

  assert.equal(diagnosis.hasDiagnosis, true);
  assert.equal(card.capability, "dataManagement");
  assert.equal(matrix.alta[0].level, "bajo");
  assert.match(draft, /Gestion de datos/);
});

test("mantiene opciones antiguas disponibles para clasificacion manual", () => {
  const diagnosisCard = createDiagnosisCard({
    id: 11,
    text: "La empresa usa practicas digitales aisladas.",
  });
  const capabilityCard = createCapabilityCard({
    id: 12,
    text: "La empresa necesita revisar sus capacidades internas.",
  });

  assert.equal(diagnosisCard.dimension, "");
  assert.equal(capabilityCard.capability, "");
  assert.equal(getEffectiveSelectionLimit([diagnosisCard], 5), 1);
});

test("prioriza brechas sin revelar datos internos y usa el contexto previo", () => {
  const context = buildMaturityContext([
    {
      phaseName: "Diagnostico inicial",
      selectedTexts: ["Los procesos tienen tareas manuales."],
      tags: ["processes", "manual-work"],
    },
    {
      phaseName: "Evaluar capacidades",
      selectedTexts: ["La automatizacion de procesos requiere fortalecimiento."],
      tags: ["automation", "processes"],
    },
  ], ["Diagnostico inicial", "Evaluar capacidades"]);
  const card = createGapCard({
    id: 21,
    text: "Priorizar brechas en la eficiencia operativa.",
    tags: ["processes", "process-efficiency"],
    score: 100,
    isCorrect: true,
  });
  const classifications = { 21: { impact: "alta", urgency: "alta" } };
  const priorityMap = buildGapPriorityMap([card], classifications);
  const draft = buildGapDraft([card], classifications, context);

  assert.equal(context.texts.length, 2);
  assert.equal(card.dimension, "processes");
  assert.equal(priorityMap.alta[0].urgency, "alta");
  assert.match(draft, /Procesos/);
  assert.equal("score" in card, false);
  assert.equal("isCorrect" in card, false);
});

test("permite clasificar manualmente escenarios antiguos en las fases finales", () => {
  const oldGap = createGapCard({ id: 31, text: "Revisar necesidades de transformacion." });
  const initiative = createInitiativeCard({
    id: 32,
    text: "Construir un plan de datos para las decisiones.",
    tags: ["data", "analytics"],
    impacts: { effort: "Alta" },
  });
  const indicator = createTrackingCard({
    id: 33,
    text: "Observar la adopcion de las nuevas practicas digitales.",
    tags: ["adoption"],
  });
  const manualGapMap = buildGapPriorityMap([oldGap], {});
  const roadmap = buildRoadmap([initiative], { 32: { period: "short" } });
  const tracking = buildTrackingPanel([indicator], { 33: { area: "adoption" } });

  assert.equal(oldGap.dimension, "");
  assert.equal(manualGapMap.media.length, 1);
  assert.equal(initiative.dimension, "data");
  assert.equal(initiative.effort, "alta");
  assert.equal(roadmap.short[0].id, 32);
  assert.equal(indicator.area, "adoption");
  assert.equal(tracking.adoption[0].id, 33);
  assert.equal(getEffectiveSelectionLimit([oldGap, initiative, indicator], 1), 1);
});

test("mantiene el contrato de envio sin clasificaciones locales", () => {
  assert.deepEqual(
    createPhaseSubmission({
      selectedOptionIds: [3, "3", 8, 0],
      textAnswer: "Diagnostico sustentado con evidencia.",
      maturityLevel: "inicial",
    }),
    {
      selectedOptionIds: [3, 8],
      textAnswer: "Diagnostico sustentado con evidencia.",
    }
  );
});
