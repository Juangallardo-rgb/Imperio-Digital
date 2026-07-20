import "./bpmExperience.css";
import AnalyzeBottlenecksExperience from "./AnalyzeBottlenecksExperience";
import IdentifyProcessExperience from "./IdentifyProcessExperience";
import MonitorIndicatorsExperience from "./MonitorIndicatorsExperience";
import ModelCurrentProcessExperience from "./ModelCurrentProcessExperience";
import RedesignProcessExperience from "./RedesignProcessExperience";

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
    "Rediseñar proceso": {
      interactionType: "process-redesign-board",
      handlesEmptyOptions: true,
      title: "Rediseña el proceso",
      objective: "Convierte las fricciones detectadas en cambios claros para el flujo operativo.",
      component: RedesignProcessExperience,
    },
    "Monitorear indicadores": {
      interactionType: "process-kpi-dashboard",
      handlesEmptyOptions: true,
      title: "Monitorea indicadores",
      objective: "Define las evidencias que permitiran seguir la mejora del proceso.",
      component: MonitorIndicatorsExperience,
    },
  },
};

export default businessProcessManagementManifest;
