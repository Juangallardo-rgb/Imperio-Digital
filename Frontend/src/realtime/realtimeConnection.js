import * as signalR from "@microsoft/signalr";
import { getToken } from "../utils/auth";
import { buildBackendUrl } from "../api/apiConfig";

let connection = null;
let startPromise = null;
let retryTimer = null;

function getBackendBaseUrl() {
  const configuredUrl = buildBackendUrl("");

  if (!configuredUrl) {
    throw new Error(
      "No está configurada la variable VITE_API_URL."
    );
  }

  const cleanUrl = configuredUrl.replace(/\/+$/, "");

  return cleanUrl.endsWith("/api")
    ? cleanUrl.slice(0, -4)
    : cleanUrl;
}

function getHubUrl() {
  return `${getBackendBaseUrl()}/hubs/realtime`;
}

export function getRealtimeConnection() {
  if (connection) {
    return connection;
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(getHubUrl(), {
      accessTokenFactory: () => getToken() || "",
      withCredentials: true,
    })
    .withAutomaticReconnect([
      0,
      2000,
      5000,
      10000,
      30000,
    ])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  connection.onreconnecting((error) => {
    console.warn(
      "SignalR intentando reconectar:",
      error?.message || error
    );
  });

  connection.onreconnected(() => {
    console.info("SignalR reconectado.");

    window.dispatchEvent(
      new Event("imperio:realtime-reconnected")
    );
  });

  connection.onclose((error) => {
    console.warn(
      "SignalR desconectado:",
      error?.message || error
    );

    scheduleReconnect();
  });

  return connection;
}

function scheduleReconnect() {
  if (retryTimer) {
    return;
  }

  retryTimer = window.setTimeout(() => {
    retryTimer = null;
    void startRealtimeConnection();
  }, 5000);
}

export async function startRealtimeConnection() {
  const currentConnection = getRealtimeConnection();

  if (
    currentConnection.state ===
      signalR.HubConnectionState.Connected ||
    currentConnection.state ===
      signalR.HubConnectionState.Connecting ||
    currentConnection.state ===
      signalR.HubConnectionState.Reconnecting
  ) {
    return currentConnection;
  }

  if (startPromise) {
    return startPromise;
  }

  startPromise = currentConnection
    .start()
    .then(() => {
      console.info("SignalR conectado.");
      return currentConnection;
    })
    .catch((error) => {
      console.error(
        "No se pudo iniciar SignalR:",
        error
      );

      scheduleReconnect();

      return currentConnection;
    })
    .finally(() => {
      startPromise = null;
    });

  return startPromise;
}
