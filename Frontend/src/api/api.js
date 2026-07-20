import axios from "axios";
import { API_BASE_URL, buildApiUrl } from "./apiConfig";
import { getApiErrorMessage } from "./apiErrors";

const api = axios.create({
  baseURL: API_BASE_URL,
});

function getRequestMetadata(config, status) {
  return {
    method: String(config?.method || "get").toUpperCase(),
    url: buildApiUrl(config?.url || ""),
    ...(status ? { status } : {}),
  };
}

if (import.meta.env.DEV) {
  api.interceptors.request.use((config) => {
    console.info("Solicitud API", getRequestMetadata(config));
    return config;
  });

  api.interceptors.response.use(
    (response) => {
      console.info(
        "Respuesta API",
        getRequestMetadata(response.config, response.status)
      );
      return response;
    },
    (error) => {
      console.warn("Error API", {
        ...getRequestMetadata(error.config, error.response?.status),
        message: getApiErrorMessage(error),
      });
      return Promise.reject(error);
    }
  );
}

export default api;
