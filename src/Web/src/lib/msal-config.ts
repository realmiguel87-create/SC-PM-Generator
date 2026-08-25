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
    // The app's own origin: this app uses the redirect flow, not the popup flow, so the response
    // comes back to the main window and is processed by handleRedirectPromise() in main.tsx.
    //
    // The popup flow was tried first and abandoned. It authenticated correctly every time, but
    // the handoff of the auth code from the popup back to the parent window never completed —
    // first failing as "BrowserAuthError: timed_out" (the popup was loading the whole app before
    // it could hand over), then silently hanging on a blank page with a valid `#code=` sitting
    // unread in its URL. The redirect flow has no second window and therefore no handoff to
    // fail, which makes it the right choice here regardless of what was wrong with the popup.
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  system: {
    // Default is a few seconds, which is not enough on a slow or high-latency connection — the
    // hidden renewal iframe has to reach login.microsoftonline.com and come back within it, and
    // exceeding it surfaces as an opaque "BrowserAuthError: timed_out" with no indication that
    // the network was simply slow. Raising it costs nothing when the network is fast.
    iframeBridgeTimeout: 20000,
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

/**
 * Redirect URI for *silent* token renewal only.
 *
 * MSAL renews access tokens by navigating a hidden iframe to the redirect URI. Left on the app
 * origin, that boots the whole React app inside the iframe and MSAL aborts with
 * "BrowserAuthError: block_iframe_reload" — every acquireTokenSilent call then fails before any
 * HTTP request is made, so nothing appears in the network log to explain the failure.
 *
 * Kept separate from msalConfig.auth.redirectUri, which must stay on the app origin so the
 * interactive redirect flow can load the app and call handleRedirectPromise(). Both must be
 * registered as SPA redirect URIs on the Entra ID app registration.
 */
export const silentRedirectUri = `${window.location.origin}/blank.html`;
