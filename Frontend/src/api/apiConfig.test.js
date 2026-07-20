import assert from "node:assert/strict";
import test from "node:test";
import {
  normalizeBackendBaseUrl,
  resolveBackendUrls,
} from "./apiConfig.js";

test("usa el backend local estable cuando no hay configuración", () => {
  assert.equal(
    normalizeBackendBaseUrl(undefined),
    "http://127.0.0.1:5148"
  );
});

test("normaliza barras finales y un sufijo api heredado", () => {
  assert.deepEqual(
    resolveBackendUrls("http://127.0.0.1:5148/api///"),
    {
      backendBaseUrl: "http://127.0.0.1:5148",
      apiBaseUrl: "http://127.0.0.1:5148/api",
    }
  );
});

test("conserva el host de producción sin duplicar api", () => {
  assert.equal(
    resolveBackendUrls("https://api.example.test").apiBaseUrl,
    "https://api.example.test/api"
  );
});
