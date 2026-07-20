const DEFAULT_BACKEND_BASE_URL = "http://127.0.0.1:5148";

export function normalizeBackendBaseUrl(configuredUrl) {
  const candidate =
    typeof configuredUrl === "string" && configuredUrl.trim()
      ? configuredUrl.trim()
      : DEFAULT_BACKEND_BASE_URL;

  const withoutTrailingSlashes = candidate.replace(/\/+$/, "");

  return withoutTrailingSlashes.replace(/\/api$/i, "");
}

export function resolveBackendUrls(configuredUrl) {
  const backendBaseUrl = normalizeBackendBaseUrl(configuredUrl);

  return {
    backendBaseUrl,
    apiBaseUrl: `${backendBaseUrl}/api`,
  };
}

const runtimeUrls = resolveBackendUrls(import.meta.env?.VITE_API_URL);

export const BACKEND_BASE_URL = runtimeUrls.backendBaseUrl;
export const API_BASE_URL = runtimeUrls.apiBaseUrl;

export function buildBackendUrl(path = "") {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${BACKEND_BASE_URL}/${String(path).replace(/^\/+/, "")}`;
}

export function buildApiUrl(path = "") {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${API_BASE_URL}/${String(path).replace(/^\/+/, "")}`;
}
