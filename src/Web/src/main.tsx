import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { MsalProvider } from "@azure/msal-react";
import { queryClient } from "@/app/query-client";
import { msalInstance } from "@/lib/msal-instance";
import { App } from "@/App";
import "@/styles/globals.css";

// msal-browser v3+ requires initialize() to resolve before the instance is used anywhere
// (including by MsalProvider) — rendering before this settles would silently no-op auth calls.
// An async IIFE rather than top-level await: Vite's production build target (set for broad
// browser compatibility, not just evergreen Chrome/Edge) doesn't support top-level await.
void (async () => {
  await msalInstance.initialize();

  createRoot(document.getElementById("root")!).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <QueryClientProvider client={queryClient}>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </QueryClientProvider>
      </MsalProvider>
    </StrictMode>,
  );
})();
