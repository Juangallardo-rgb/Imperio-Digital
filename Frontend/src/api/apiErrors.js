function sanitizeMessage(value) {
  if (typeof value !== "string") {
    return "";
  }

  const message = value.replace(/\s+/g, " ").trim();

  if (!message || /<\/?(?:html|body|script)\b/i.test(message)) {
    return "";
  }

  return message.slice(0, 300);
}

export function getBackendErrorMessage(data) {
  const directMessage = sanitizeMessage(data);

  if (directMessage) {
    return directMessage;
  }

  if (!data || typeof data !== "object") {
    return "";
  }

  for (const candidate of [data.message, data.detail, data.error]) {
    const message = sanitizeMessage(candidate);

    if (message) {
      return message;
    }
  }

  if (data.errors && typeof data.errors === "object") {
    for (const messages of Object.values(data.errors)) {
      const message = sanitizeMessage(
        Array.isArray(messages) ? messages[0] : messages
      );

      if (message) {
        return message;
      }
    }
  }

  return sanitizeMessage(data.title);
}

export function getApiErrorMessage(error) {
  if (!error?.response) {
    return "No se pudo conectar con el servidor.";
  }

  const status = error.response.status;
  const backendMessage = getBackendErrorMessage(error.response.data);

  if (status === 400) {
    return backendMessage || "La solicitud contiene datos inválidos.";
  }

  if (status === 401) {
    return "Correo o contraseña incorrectos.";
  }

  if (status === 403) {
    return backendMessage || "No tienes autorización para realizar esta acción.";
  }

  if (status >= 500) {
    return "El servidor encontró un error al procesar la solicitud.";
  }

  return backendMessage || `La solicitud fue rechazada con estado ${status}.`;
}
