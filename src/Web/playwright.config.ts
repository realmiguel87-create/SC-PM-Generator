import { defineConfig, devices } from "@playwright/test";

// Frontend-only smoke test: runs against `npm run dev` with no backend API running. That's
// deliberate, not an oversight — MSAL auth isn't wired into the frontend yet (see
// src/lib/api-client.ts), so even a live API would 401 every request from here. What this
// verifies is that the app shell boots and every top-level route renders and is navigable
// without an uncaught exception, including while every data fetch on the page is failing —
// exactly the condition the app is actually in today.
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
    // Builds first so the test runs against what actually ships, not the dev server's
    // unminified/HMR-instrumented output.
    command: "npm run build && npm run preview -- --port 4173 --strictPort",
    url: "http://127.0.0.1:4173",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
