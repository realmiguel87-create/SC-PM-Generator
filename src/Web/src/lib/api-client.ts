/**
 * Thin fetch wrapper. Auth token attachment happens here so every feature
 * hook gets it "for free" once MSAL is wired in (Phase 2 — see docs/roadmap.md);
 * for now it's a pass-through so the UI can be developed against a mock/dev API.
 */
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api";

async function getAccessToken(): Promise<string | null> {
  // TODO(Phase 2): acquire token via @azure/msal-browser against the EntraId app registration.
  return null;
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

export const apiClient = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined }),
  del: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};
