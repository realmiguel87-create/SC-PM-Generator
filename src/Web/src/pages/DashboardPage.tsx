import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Banknote, ClipboardCheck, FolderKanban, LayoutGrid, ShieldAlert } from "lucide-react";
import { StatTile } from "@/components/StatTile";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiErrorNotice } from "@/components/ApiErrorNotice";
import { RIBA_STAGES } from "@/lib/riba";
import { useProjects } from "@/features/projects/api";
import { formatCurrency } from "@/lib/utils";

export function DashboardPage() {
  const { data: projects, isLoading, isError, error } = useProjects();

  const totalCapitalValue = projects?.reduce((sum, p) => sum + p.approvedBudget, 0) ?? 0;
  const totalForecastCost = projects?.reduce((sum, p) => sum + p.forecastCost, 0) ?? 0;

  const stageDistribution = RIBA_STAGES.map((stage) => ({
    stage: `Stage ${stage.number}`,
    projects: projects?.filter((p) => p.currentRibaStage === stage.number).length ?? 0,
  }));

  return (
    <div className="flex flex-col gap-6 p-6">
      <header>
        <h1 className="text-xl font-semibold">Executive Dashboard</h1>
        <p className="text-sm text-text-secondary">Portfolio-wide view of the capital programme.</p>
      </header>

      {isError && <ApiErrorNotice error={error} />}

      <section className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatTile label="Total Projects" value={isLoading ? "…" : String(projects?.length ?? 0)} icon={<FolderKanban size={18} />} accent="purple" />
        <StatTile label="Capital Value" value={isLoading ? "…" : formatCurrency(totalCapitalValue)} icon={<Banknote size={18} />} accent="green" />
        <StatTile label="Forecast Cost" value={isLoading ? "…" : formatCurrency(totalForecastCost)} icon={<Banknote size={18} />} accent="warning" />
        <StatTile label="Open Approvals" value={isLoading ? "…" : "0"} icon={<ClipboardCheck size={18} />} accent="purple" />
        <StatTile label="Open Risks" value="0" icon={<ShieldAlert size={18} />} accent="critical" />
        <StatTile label="Open Actions" value="0" icon={<ClipboardCheck size={18} />} accent="information" />
        <StatTile label="Total Programmes" value="—" icon={<LayoutGrid size={18} />} accent="purple" />
        <StatTile label="Upcoming Gateways" value="0" icon={<ClipboardCheck size={18} />} accent="information" />
      </section>

      <section className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Projects by RIBA Stage</CardTitle>
          </CardHeader>
          <CardContent className="h-72">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={stageDistribution}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="stage" tick={{ fontSize: 11, fill: "var(--text-secondary)" }} />
                <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} />
                <Tooltip
                  contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", fontSize: 12 }}
                />
                <Bar dataKey="projects" fill="var(--stirling-purple)" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Upcoming Committee Reports</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-text-secondary">
              No committee reports scheduled. The Committee Reporting module (Phase 6) will populate this panel.
            </p>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
