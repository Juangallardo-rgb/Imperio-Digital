import "./bpmExperience.css";
import AnalyzeBottlenecksExperience from "./AnalyzeBottlenecksExperience";
import IdentifyProcessExperience from "./IdentifyProcessExperience";
import ModelCurrentProcessExperience from "./ModelCurrentProcessExperience";

const businessProcessManagementManifest = {
  methodologyCode: "BPM",
  phases: {
    "Identificar proceso": {
      interactionType: "critical-process-diagnosis",
      handlesEmptyOptions: true,
      title: "Identifica el proceso critico",
      objective: "Reconoce las senales que muestran cual proceso operativo requiere analisis.",
      component: IdentifyProcessExperience,
    },
    "Modelar proceso actual": {
      interactionType: "current-process-flow",
      handlesEmptyOptions: true,
      title: "Representa el flujo actual",
      objective: "Reconstruye como funciona el proceso antes de proponer mejoras.",
      component: ModelCurrentProcessExperience,
    },
    "Analizar cuellos de botella": {
      interactionType: "bottleneck-analysis",
      handlesEmptyOptions: true,
      title: "Detecta fricciones del proceso",
      objective: "Ubica las fricciones que acumulan trabajo, generan errores o reducen la trazabilidad.",
      component: AnalyzeBottlenecksExperience,
    },
  },
};

export default businessProcessManagementManifest;
