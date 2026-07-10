export function isMethodologyExperienceV2Enabled() {
  const value = String(
    import.meta.env.VITE_METHODOLOGY_EXPERIENCE_V2 || ""
  )
    .trim()
    .toLowerCase();

  return value === "true" || value === "1" || value === "yes";
}
