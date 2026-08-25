/**
 * Thin fetch wrapper. Auth token attachment happens here so every feature hook gets it "for
 * free" — every request goes through acquireAccessToken below.
 */
import { msalInstance } from "@/lib/msal-instance";
import { loginRequest, silentRedirectUri } from "@/lib/msal-config";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api";

async function getAccessToken(): Promise<string | null> {
  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
  if (!account) return null; // Not signed in — request goes out unauthenticated, API 401s it.

  try {
    const result = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account,
      // Silent renewal runs in a hidden iframe. Without this override the iframe navigates to
      // the app origin and boots the entire React app inside itself, which MSAL rejects with
      // "BrowserAuthError: block_iframe_reload" — failing before any HTTP request is made, so
      // nothing shows in the network log. See silentRedirectUri in msal-config.ts.
      redirectUri: silentRedirectUri,
    });
    return result.accessToken;
  } catch (error) {
    // Logged, not silently swallowed. Returning null without a trace makes a failed token
    // acquisition indistinguishable from "not signed in" — both produce a bare 401 from the
    // API with nothing in the console. That cost real debugging time during first setup.
    console.warn(
      "[SCPM auth] Silent token acquisition failed; request will be sent unauthenticated:",
      error,
    );
    // Every silent failure returns null rather than throwing, so the request still goes out and
    // the API's 401 becomes the visible symptom.
    //
    // Previously only InteractionRequiredAuthError returned null and everything else was
    // rethrown. That meant a failure like "timed_out" or "block_iframe_reload" aborted before
    // fetch was ever called — nothing appeared in the network log at all, and the UI reported a
    // generic failure with no request to inspect. Hard to diagnose, and it hides a recoverable
    // situation behind an unrecoverable-looking one.
    //
    // Still no interactive prompt from here: raising one inside an arbitrary background refetch
    // is a jarring UX. Re-authentication belongs to AppShell's sign-in control, which the 401
    // notice directs the user towards.
    return null;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getAccessToken();

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.title ?? `Request to ${path} failed with status ${response.status}`);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

async function requestForm<T>(path: string, formData: FormData): Promise<T> {
  const token = await getAccessToken();

  // No Content-Type header here — the browser sets multipart/form-data with the correct
  // boundary itself; forcing application/json (as `request` does for every other call) would
  // silently break the upload.
  const response = await fetch(`${API_BASE}${path}`, {
    method: "POST",
    body: formData,
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });

  if (!response.ok) {
    const problem = await response.json().catch(() => null);
    throw new Error(problem?.title ?? `Upload to ${path} failed with status ${response.status}`);
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: "DELETE" }),
  postForm: <T>(path: string, formData: FormData) => requestForm<T>(path, formData),
};
