import { jwtDecode } from "jwt-decode";

const TOKEN_KEY = "token";

/**
 * Guarda el token en sessionStorage.
 * Cada pestaña tendrá su propia sesión.
 */
export function saveToken(token) {
  if (!token) {
    logout();
    return;
  }

  sessionStorage.setItem(TOKEN_KEY, token);

  // Elimina el token anterior compartido entre pestañas.
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Obtiene el token de la pestaña actual.
 * El localStorage queda como compatibilidad temporal.
 */
export function getToken() {
  return (
    sessionStorage.getItem(TOKEN_KEY) ||
    localStorage.getItem(TOKEN_KEY)
  );
}

/**
 * Elimina la sesión tanto nueva como antigua.
 */
export function logout() {
  sessionStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(TOKEN_KEY);
}

/**
 * Obtiene los datos del usuario almacenados en el JWT.
 */
export function getUserFromToken() {
  const token = getToken();

  if (!token) return null;

  try {
    const decoded = jwtDecode(token);

    return {
      id:
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        ] ||
        decoded.nameid ||
        decoded.sub ||
        null,

      name:
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
        ] ||
        decoded.name ||
        "",

      email:
        decoded[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ] ||
        decoded.email ||
        "",

      role:
        decoded[
          "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        ] ||
        decoded.role ||
        "",
    };
  } catch (error) {
    console.error("No se pudo leer el token:", error);
    return null;
  }
}