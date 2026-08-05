import { NavLink, Outlet } from "react-router-dom";
import { cn } from "@/lib/utils";
import { LayoutDashboard, FolderKanban, ShieldCheck, FileBarChart2 } from "lucide-react";

const navItems = [
  { to: "/", label: "Executive Dashboard", icon: LayoutDashboard, end: true },
  { to: "/projects", label: "Projects", icon: FolderKanban },
  { to: "/governance", label: "Governance", icon: ShieldCheck },
  { to: "/reporting", label: "Reporting Centre", icon: FileBarChart2 },
];

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
      </aside>
      <main className="flex-1 overflow-y-auto">
        <Outlet />
      </main>
    </div>
  );
}
