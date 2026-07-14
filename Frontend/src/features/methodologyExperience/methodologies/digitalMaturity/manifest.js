import "./digitalMaturityExperience.css";
import CapabilityAssessmentExperience from "./CapabilityAssessmentExperience";
import InitialDiagnosisExperience from "./InitialDiagnosisExperience";

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
  },
};

export default digitalMaturityManifest;
