import "./digitalMaturityExperience.css";
import CapabilityAssessmentExperience from "./CapabilityAssessmentExperience";
import InitialDiagnosisExperience from "./InitialDiagnosisExperience";
import MaturityTrackingExperience from "./MaturityTrackingExperience";
import PrioritizeGapsExperience from "./PrioritizeGapsExperience";
import TransformationPlanExperience from "./TransformationPlanExperience";

const digitalMaturityManifest = {
  methodologyCode: "DigitalMaturity",
  phases: {
    "Diagnostico inicial": {
      interactionType: "digital-diagnosis-map",
      handlesEmptyOptions: true,
      title: "Construye un diagnostico digital",
      objective: "Identifica las senales que describen el estado digital actual de la empresa.",
      component: InitialDiagnosisExperience,
    },
    "Evaluar capacidades": {
      interactionType: "digital-capability-matrix",
      handlesEmptyOptions: true,
      title: "Evalua capacidades digitales",
      objective: "Analiza las capacidades que la empresa necesita fortalecer para transformar su operacion.",
      component: CapabilityAssessmentExperience,
    },
    "Priorizar brechas": {
      interactionType: "digital-gap-prioritization",
      handlesEmptyOptions: true,
      title: "Prioriza brechas digitales",
      objective: "Valora las brechas que mas afectan la transformacion y define cuales atender primero.",
      component: PrioritizeGapsExperience,
    },
    "Plan de transformacion": {
      interactionType: "digital-transformation-roadmap",
      handlesEmptyOptions: true,
      title: "Construye un plan de transformacion",
      objective: "Organiza iniciativas de transformacion en un roadmap gradual y coherente.",
      component: TransformationPlanExperience,
    },
    "Seguimiento de madurez": {
      interactionType: "digital-maturity-tracking",
      handlesEmptyOptions: true,
      title: "Define el seguimiento de madurez",
      objective: "Selecciona indicadores para observar el progreso y ajustar el plan de transformacion.",
      component: MaturityTrackingExperience,
    },
  },
};

export default digitalMaturityManifest;
