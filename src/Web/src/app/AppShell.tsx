import { useState } from "react";
import { NavLink, Outlet } from "react-router-dom";
import { useMsal, AuthenticatedTemplate, UnauthenticatedTemplate } from "@azure/msal-react";
import { cn } from "@/lib/utils";
import { loginRequest } from "@/lib/msal-config";
import { Button } from "@/components/ui/button";
import { LayoutDashboard, FolderKanban, ShieldCheck, FileBarChart2, LogIn, LogOut } from "lucide-react";

/// Pulls a human-readable message out of whatever MSAL throws. MSAL errors carry the useful
/// detail (the AADSTS code and its explanation) on errorMessage rather than the standard Error
/// message property, so a plain String(error) loses exactly the part worth reading.
function describeAuthError(error: unknown): string {
  if (error && typeof error === "object") {
    const msalError = error as { errorCode?: string; errorMessage?: string; message?: string };
    const detail = msalError.errorMessage ?? msalError.message;
    if (msalError.errorCode && detail) return `${msalError.errorCode}: ${detail}`;
    if (detail) return detail;
  }
  return String(error);
}

const navItems = [
  { to: "/", label: "Executive Dashboard", icon: LayoutDashboard, end: true },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  { to: "/governance", label: "Governance", icon: ShieldCheck },
  { to: "/reporting", label: "Reporting Centre", icon: FileBarChart2 },
];

function AuthControl() {
  const { instance, accounts } = useMsal();
  // Shown in the UI, not just logged. A sign-in failure is something the person trying to sign
  // in needs to see — expecting them to open DevTools to find out why the button did nothing is
  // not a reasonable ask, and it is precisely what made this hard to diagnose in practice.
  const [signInError, setSignInError] = useState<string | null>(null);

  return (
    <div className="border-t border-border p-3">
      <AuthenticatedTemplate>
        <div className="flex items-center justify-between gap-2 px-1">
          <span className="truncate text-xs text-text-secondary" title={accounts[0]?.username}>
            {accounts[0]?.name ?? accounts[0]?.username}
          </span>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              instance.logoutPopup().catch((error: unknown) => {
                console.error("[SCPM auth] Sign-out failed:", error);
              });
            }}
            aria-label="Sign out"
          >
            <LogOut size={14} />
          </Button>
        </div>
      </AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <Button
          variant="outline"
          size="sm"
          className="w-full"
          onClick={() => {
            // Errors are surfaced, not swallowed: an unhandled rejection here (consent not
            // granted, popup blocked, account not in the tenant, wrong scope name) otherwise
            // leaves the UI sitting on "Sign in" with nothing explaining why — which is exactly
            // how a real first-time setup of this app failed, undiagnosably.
            setSignInError(null);
            instance.loginPopup(loginRequest).catch((error: unknown) => {
              console.error("[SCPM auth] Sign-in failed:", error);
              setSignInError(describeAuthError(error));
            });
          }}
        >
          <LogIn size={14} />
          Sign in
        </Button>
        {signInError && (
          <p className="mt-2 break-words text-[11px] leading-snug text-red-600" role="alert">
            {signInError}
          </p>
        )}
      </UnauthenticatedTemplate>
    </div>
  );
}

export function AppShell() {
  return (
    <div className="flex min-h-screen bg-background text-text-primary">
      <aside className="flex w-64 flex-col border-r border-border bg-card">
        <div className="flex flex-col gap-0.5 border-b border-border px-5 py-5">
          <span className="text-xs font-semibold uppercase tracking-wide text-stirling-purple">
            Stirling Council
          </span>
          <span className="text-sm font-medium text-text-secondary">Capital Projects Platform</span>
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3">
          {navItems.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                  isActive
                    ? "bg-purple-soft text-stirling-purple"
                    : "text-text-secondary hover:bg-purple-soft hover:text-stirling-purple",
                )
              }
            >
              <Icon size={16} />
              {label}
            </NavLink>
          ))}
        </nav>
        <AuthControl />
      </aside>
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}
