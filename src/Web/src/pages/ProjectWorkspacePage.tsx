import { useParams } from "react-router-dom";
import * as Tabs from "@radix-ui/react-tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { RibaStageTracker } from "@/components/RibaStageTracker";
import { useAdvanceRibaStage, useProject } from "@/features/projects/api";
import { cn, formatCurrency, formatDate } from "@/lib/utils";

const WORKSPACE_TABS = [
  "Overview", "Governance", "Cost", "Programme", "Risks", "Issues", "Opportunities",
  "Stakeholders", "Documents", "Reports", "Approvals", "Snapshots", "NEC4", "SBCC",
  "Handover", "Lessons Learned", "Benefits Realisation",
];

export function ProjectWorkspacePage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { data: project, isLoading, isError } = useProject(projectId);
  const advanceStage = useAdvanceRibaStage();

  if (isLoading) return <p className="p-6 text-sm text-text-secondary">Loading project…</p>;
  if (isError || !project)
    return <p className="p-6 text-sm text-critical">Project not found, or the API is unreachable.</p>;

  return (
    <div className="flex flex-col gap-6 p-6">
      <header className="flex items-start justify-between">
        <div>
          <span className="text-xs font-medium text-text-secondary">{project.projectRef}</span>
          <h1 className="text-xl font-semibold">{project.name}</h1>
          <div className="mt-1 flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(project.status)}>{project.status}</Badge>
            <span className="text-sm text-text-secondary">
              Stage {project.currentRibaStage} — {project.currentRibaStageName}
            </span>
          </div>
        </div>
        <Button
          onClick={() => projectId && advanceStage.mutate(projectId)}
          disabled={advanceStage.isPending || project.currentRibaStage >= 7}
        >
          {advanceStage.isPending ? "Advancing…" : "Advance to Next Stage"}
        </Button>
      </header>

      <Tabs.Root defaultValue="Overview">
        <Tabs.List className="flex flex-wrap gap-1 border-b border-border pb-2">
          {WORKSPACE_TABS.map((tab) => (
            <Tabs.Trigger
              key={tab}
              value={tab}
              className={cn(
                "rounded-md px-3 py-1.5 text-sm font-medium text-text-secondary transition-colors",
                "data-[state=active]:bg-purple-soft data-[state=active]:text-stirling-purple",
                "hover:bg-purple-soft",
              )}
            >
              {tab}
            </Tabs.Trigger>
          ))}
        </Tabs.List>

        <Tabs.Content value="Overview" className="flex flex-col gap-4 pt-4">
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <Card>
              <CardHeader><CardTitle>Approved Budget</CardTitle></CardHeader>
              <CardContent className="pt-0 text-xl font-semibold">{formatCurrency(project.approvedBudget)}</CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle>Forecast Cost</CardTitle></CardHeader>
              <CardContent className="pt-0 text-xl font-semibold">{formatCurrency(project.forecastCost)}</CardContent>
            </Card>
            <Card>
              <CardHeader><CardTitle>Target Completion</CardTitle></CardHeader>
              <CardContent className="pt-0 text-xl font-semibold">{formatDate(project.targetCompletionDate)}</CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader><CardTitle>RIBA Stage Progression</CardTitle></CardHeader>
            <CardContent className="pt-0">
              <RibaStageTracker stages={project.ribaStages} />
            </CardContent>
          </Card>

          {project.description && (
            <Card>
              <CardHeader><CardTitle>Description</CardTitle></CardHeader>
              <CardContent className="pt-0 text-sm text-text-primary">{project.description}</CardContent>
            </Card>
          )}
        </Tabs.Content>

        {WORKSPACE_TABS.filter((t) => t !== "Overview").map((tab) => (
          <Tabs.Content key={tab} value={tab} className="pt-4">
            <Card>
              <CardContent className="pt-5 text-sm text-text-secondary">
                {tab} module is scheduled for a later delivery phase — see docs/roadmap.md.
              </CardContent>
            </Card>
          </Tabs.Content>
        ))}
      </Tabs.Root>
    </div>
  );
}
