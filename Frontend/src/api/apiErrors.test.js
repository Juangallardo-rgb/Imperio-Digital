import assert from "node:assert/strict";
import test from "node:test";
import { getApiErrorMessage } from "./apiErrors.js";

test("distingue un error de red real", () => {
  assert.equal(
    getApiErrorMessage({ request: {} }),
    "No se pudo conectar con el servidor."
  );
});

test("muestra el mensaje validado del backend para 400", () => {
  assert.equal(
    getApiErrorMessage({
      response: { status: 400, data: { message: "Dato inválido." } },
    }),
    "Dato inválido."
  );
});

test("no revela el detalle del backend para 401", () => {
  assert.equal(
    getApiErrorMessage({
      response: { status: 401, data: "Usuario no encontrado" },
    }),
    "Correo o contraseña incorrectos."
  );
});

test("conserva un mensaje autorizado para 403", () => {
  assert.equal(
    getApiErrorMessage({
      response: { status: 403, data: "Debes cambiar tu contraseña." },
    }),
    "Debes cambiar tu contraseña."
  );
});

test("usa un mensaje seguro para errores 500", () => {
  assert.equal(
    getApiErrorMessage({
      response: { status: 500, data: "detalle interno" },
    }),
    "El servidor encontró un error al procesar la solicitud."
  );
});
