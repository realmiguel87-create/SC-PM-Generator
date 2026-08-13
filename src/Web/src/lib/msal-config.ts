import type { Configuration } from "@azure/msal-browser";

// A SPA's client ID and tenant ID are not secrets — they're visible in every login redirect URL
// and in the ID token itself, so shipping real defaults here (Stirling's actual Entra ID tenant,
// wired up once real Azure access became available — see docs/roadmap.md) is safe in a way a
// client secret or connection string would not be. VITE_MSAL_* env vars override these for
// anyone pointing the app at a different tenant/app registration.
const clientId = import.meta.env.VITE_MSAL_CLIENT_ID ?? "5ee8daf1-b7bb-43f8-9d78-b9741de0657e";
const tenantId = import.meta.env.VITE_MSAL_TENANT_ID ?? "fa391950-9355-4a7e-ab7f-6d3dc47380c3";

// Single app registration used for both the SPA and the API (self-referencing scope) — see
// docs/roadmap.md for why, and note the scope name assumes "access_as_user" as configured during
// setup; if the real app registration used a different scope name, override via
// VITE_MSAL_API_SCOPE rather than editing this default.
export const apiScope =
  import.meta.env.VITE_MSAL_API_SCOPE ?? `api://${clientId}/access_as_user`;

export const msalConfig: Configuration = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri: window.location.origin,
  },
  cache: {
    // sessionStorage, not localStorage — tokens shouldn't outlive the browser tab for a
    // council system handling governance/commercial data, and this avoids silently persisting
    // a stale session across unrelated tabs/windows. (storeAuthStateInCookie was an IE11-era
    // option removed in msal-browser v5 — not needed, not available.)
    cacheLocation: "sessionStorage",
  },
};

export const loginRequest = {
  scopes: [apiScope],
};
