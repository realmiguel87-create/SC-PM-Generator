import { EventType, PublicClientApplication, type AuthenticationResult } from "@azure/msal-browser";
import { msalConfig } from "@/lib/msal-config";

// A single shared instance — imported both by main.tsx (to drive MsalProvider) and by
// api-client.ts (to acquire tokens outside of any React component, since the fetch wrapper is
// plain code, not a hook). Keeping it in its own module avoids a circular import between the two.
export const msalInstance = new PublicClientApplication(msalConfig);

// Without this, a user who signs in successfully still has no "active account" — MSAL tracks
// multiple accounts per browser and won't guess which one to use for acquireTokenSilent unless
// told. This is the standard MSAL.js pattern for a single-tenant, single-account app like this
// one (no "switch account" UI exists here).
msalInstance.addEventCallback((event) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const account = (event.payload as AuthenticationResult).account;
    msalInstance.setActiveAccount(account);
  }
});
