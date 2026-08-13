import { defineConfig, devices } from "@playwright/test";

// Frontend-only smoke test: runs against the built app (`npm run build` + `vite preview`), with
// no backend API running. That's deliberate, not an oversight — MSAL auth isn't wired into the
// frontend yet (see src/lib/api-client.ts), so even a live API would 401 every request from
// here. What this verifies is that the app shell boots and every top-level route renders and is
// navigable without an uncaught exception, including while every data fetch on the page is
// failing — exactly the condition the app is actually in today. The build itself is expected to
// have already run (see package.json's test:e2e script, or the CI job) — this config only
// serves the result.
export default defineConfig({
  testDir: "./e2e",
  timeout: 30_000,
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? "dot" : "list",
  use: {
    baseURL: "http://127.0.0.1:4173",
    trace: "retain-on-failure",
  },
  projects: [
    {
      name: "chromium",
      use: {
        ...devices["Desktop Chrome"],
        launchOptions: {
          executablePath: process.env.PLAYWRIGHT_CHROMIUM_PATH || undefined,
        },
      },
    },
  ],
  webServer: {
    // The build runs as its own separate step (npm run test:e2e / the CI job), not chained in
    // here — a chained `build && preview` inside webServer.command hung silently in CI with no
    // way to tell whether the build or the server was stuck, since both share one timeout and
    // one opaque "Timed out waiting for server" failure. Splitting them gives the build its own
    // pass/fail signal and leaves this command doing only the fast, near-instant part.
    command: "npm run preview -- --port 4173 --strictPort",
    url: "http://127.0.0.1:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 30_000,
  },
});
