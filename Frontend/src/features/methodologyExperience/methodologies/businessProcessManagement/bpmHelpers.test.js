import assert from "node:assert/strict";
import test from "node:test";
import { createPhaseSubmission } from "../../engine/experienceContracts.js";
import {
  buildBottleneckDraft,
  buildBpmPreviousContext,
  buildCurrentProcessFlow,
  buildKpiDashboard,
  buildRedesignBoard,
  createBottleneckCard,
  createCurrentProcessStepCard,
  createKpiCard,
  createProcessEvidenceCard,
  createProcessImprovementCard,
  getEffectiveSelectionLimit,
} from "./bpmHelpers.js";

test("adapta evidencias BPM sin exponer datos internos de evaluacion", () => {
  const card = createProcessEvidenceCard({
    id: 1,
    text: "El proceso presenta demoras recurrentes y falta de trazabilidad.",
    tags: ["process-delay", "traceability"],
    score: 100,
    isCorrect: true,
  });

  assert.equal(card.relationship, "alta");
  assert.equal(card.area, "registro");
  assert.equal("score" in card, false);
  assert.equal("isCorrect" in card, false);
});

test("limita las selecciones BPM a las opciones realmente disponibles", () => {
  const cards = [{ id: 1 }, { id: 2 }, { id: 3 }];

  assert.equal(getEffectiveSelectionLimit(cards, 4), 3);
  assert.equal(getEffectiveSelectionLimit([cards[0]], 4), 1);
  assert.equal(getEffectiveSelectionLimit([], 4), 0);
});

test("construye el flujo actual y recupera contexto previo sin datos de correccion", () => {
  const card = createCurrentProcessStepCard({
    id: 2,
    text: "Solicitud recibida -> revision manual -> respuesta final.",
    tags: ["manual-review", "as-is"],
  });
  const flow = buildCurrentProcessFlow([card], {});
  const context = buildBpmPreviousContext([
    {
      phaseName: "Identificar proceso",
      selectedTexts: ["Demoras recurrentes"],
      score: 80,
    },
    {
      phaseName: "Modelar proceso actual",
      selectedTexts: ["Solicitud recibida"],
      isCorrect: true,
    },
  ]);

  assert.equal(flow.length, 3);
  assert.equal(flow[0].text, "Solicitud recibida");
  assert.deepEqual(context.processSignals, ["Demoras recurrentes"]);
  assert.deepEqual(context.flowSteps, ["Solicitud recibida"]);
});

test("conserva el contrato de envio al analizar cuellos de botella", () => {
  const card = createBottleneckCard({
    id: 3,
    text: "La aprobacion manual concentra retrasos porque depende de una persona.",
    tags: ["bottleneck", "approval", "delay"],
  });
  const draft = buildBottleneckDraft([card], {}, { flowSteps: ["Registro manual"] });

  assert.equal(card.effect, "retraso");
  assert.match(draft, /Retraso/);
  assert.deepEqual(
    createPhaseSubmission({
      selectedOptionIds: [3, "3", 0],
      textAnswer: "El registro manual concentra la demora.",
      localClassification: "registro",
    }),
    {
      selectedOptionIds: [3],
      textAnswer: "El registro manual concentra la demora.",
    }
  );
});

test("construye el tablero de rediseño sin exponer datos internos", () => {
  const improvement = createProcessImprovementCard({
    id: 4,
    text: "Automatizar estados y notificaciones para reducir consultas manuales.",
    tags: ["automation", "traceability"],
    score: 100,
    isCorrect: true,
  });
  const board = buildRedesignBoard(
    [improvement],
    {},
    {
      flowSteps: ["Registro manual"],
      bottlenecks: ["Aprobacion manual"],
    }
  );

  assert.equal(improvement.improvementType, "automatizar");
  assert.equal("score" in improvement, false);
  assert.equal("isCorrect" in improvement, false);
  assert.deepEqual(board.currentSteps, ["Registro manual"]);
  assert.equal(board.improvements[0].label, "Automatizar");
});

test("organiza indicadores BPM por enfoque sin alterar el payload", () => {
  const cycleTime = createKpiCard({
    id: 5,
    text: "Tiempo de ciclo del proceso.",
    tags: ["cycleTime", "process-efficiency"],
    isCorrect: true,
  });
  const errorRate = createKpiCard({
    id: 6,
    text: "Tasa de errores o reprocesos.",
    tags: ["errorRate", "quality"],
  });
  const dashboard = buildKpiDashboard([cycleTime, errorRate], {});

  assert.equal(cycleTime.focus, "eficiencia");
  assert.equal(errorRate.focus, "calidad");
  assert.equal("isCorrect" in cycleTime, false);
  assert.equal(dashboard.find((area) => area.key === "eficiencia").metrics.length, 1);
  assert.equal(dashboard.find((area) => area.key === "calidad").metrics.length, 1);
  assert.deepEqual(
    createPhaseSubmission({
      selectedOptionIds: [5, 6, 6],
      textAnswer: "Revisaremos los indicadores cada semana.",
      localFocus: "calidad",
    }),
    {
      selectedOptionIds: [5, 6],
      textAnswer: "Revisaremos los indicadores cada semana.",
    }
  );
});
