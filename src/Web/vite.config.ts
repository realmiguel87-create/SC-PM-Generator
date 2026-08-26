/// <reference types="vitest" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  test: {
    // Vitest and Playwright both look like "tests" to a bare glob, and Playwright's specs throw
    // a confusing "did not expect test.describe() to be called here" when vitest collects them.
    // Scoping vitest to src/ keeps the two runners to their own halves: unit tests here, browser
    // tests under e2e/ via `npm run test:e2e`.
    include: ["src/**/*.test.{ts,tsx}"],
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "https://localhost:5001",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
