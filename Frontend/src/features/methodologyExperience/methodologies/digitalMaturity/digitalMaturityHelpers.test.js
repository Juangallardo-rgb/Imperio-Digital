import assert from "node:assert/strict";
import test from "node:test";
import { createPhaseSubmission } from "../../engine/experienceContracts.js";
import {
  buildCapabilitiesDraft,
  buildCapabilityMatrix,
  buildDiagnosisDraft,
  buildDiagnosisMap,
  buildDiagnosisSnapshot,
  createCapabilityCard,
  createDiagnosisCard,
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
