import { jwtDecode } from "jwt-decode";

export function getToken() {
  return localStorage.getItem("token");
}

export function logout() {
  localStorage.removeItem("token");
}

export function getUserFromToken() {
  const token = getToken();

  if (!token) return null;

  try {
    const decoded = jwtDecode(token);

    return {
      name:
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "",
      email:
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "",
      role:
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || "",
    };
  } catch {
    return null;
  }
}