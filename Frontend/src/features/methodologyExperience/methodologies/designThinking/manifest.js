import DefineExperience from "./DefineExperience";
import EmpathizeExperience from "./EmpathizeExperience";
import EvaluateExperience from "./EvaluateExperience";
import IdeateExperience from "./IdeateExperience";
import PrototypeExperience from "./PrototypeExperience";

const designThinkingManifest = {
  methodologyCode: "DesignThinking",
  phases: {
    Empatizar: {
      interactionType: "evidence-classifier",
      handlesEmptyOptions: true,
      title: "Explora evidencia del usuario",
      objective: "Prioriza las observaciones que mejor describen el desafio antes de proponer una solucion.",
      component: EmpathizeExperience,
    },
    Definir: {
      interactionType: "problem-statement-builder",
      title: "Delimita el problema",
      objective: "Relaciona la decision con el usuario, la necesidad y la evidencia disponible.",
      component: DefineExperience,
    },
    Idear: {
      interactionType: "impact-effort-matrix",
      title: "Prioriza alternativas",
      objective: "Compara las opciones de acuerdo con el contexto, los recursos y el impacto esperado.",
      component: IdeateExperience,
    },
    Prototipar: {
      interactionType: "mvp-builder",
      title: "Construye una propuesta minima",
      objective: "Selecciona los elementos que permiten validar una solucion de manera viable.",
      component: PrototypeExperience,
    },
    Evaluar: {
      interactionType: "test-lab",
      title: "Interpreta el aprendizaje",
      objective: "Define las decisiones que permiten medir el avance y ajustar la propuesta.",
      component: EvaluateExperience,
    },
  },
};

export default designThinkingManifest;
