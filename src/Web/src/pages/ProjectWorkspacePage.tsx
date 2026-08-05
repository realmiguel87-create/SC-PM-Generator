import { useParams } from "react-router-dom";
import * as Tabs from "@radix-ui/react-tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { RibaStageTracker } from "@/components/RibaStageTracker";
import { useAdvanceRibaStage, useProject } from "@/features/projects/api";
import { useCreateGateway, useDecideGateway } from "@/features/governance/api";
import { GovernanceTab } from "@/features/governance/GovernanceTab";
import { CostTab } from "@/features/cost/CostTab";
import { ProgrammeTab } from "@/features/programme/ProgrammeTab";
import { SnapshotsTab } from "@/features/snapshots/SnapshotsTab";
import { RisksTab } from "@/features/risk/RisksTab";
import { IssuesTab } from "@/features/risk/IssuesTab";
import { OpportunitiesTab } from "@/features/risk/OpportunitiesTab";
import { StakeholdersTab } from "@/features/stakeholders/StakeholdersTab";
import { NEC4Tab } from "@/features/nec4/NEC4Tab";
import { SBCCTab } from "@/features/sbcc/SBCCTab";
import { DocumentsTab } from "@/features/documents/DocumentsTab";
import { ReportsTab } from "@/features/reporting/ReportsTab";
import { cn, formatCurrency, formatDate } from "@/lib/utils";

const WORKSPACE_TABS = [
  "Overview", "Governance", "Cost", "Programme", "Risks", "Issues", "Opportunities",
  "Stakeholders", "Documents", "Reports", "Approvals", "Snapshots", "NEC4", "SBCC",
  "Handover", "Lessons Learned", "Benefits Realisation",
];

const FUNCTIONAL_TABS = new Set([
  "Overview", "Governance", "Cost", "Programme", "Snapshots", "Risks", "Issues", "Opportunities",
  "Stakeholders", "NEC4", "SBCC", "Documents", "Reports",
]);

export function ProjectWorkspacePage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { data: project, isLoading, isError } = useProject(projectId);
  const advanceStage = useAdvanceRibaStage();
  const createGateway = useCreateGateway(projectId ?? "");
  const decideGateway = useDecideGateway(projectId ?? "");

  if (isLoading) return <p className="p-6 text-sm text-text-secondary">Loading project…</p>;
  if (isError || !project)
    return <p className="p-6 text-sm text-critical">Project not found, or the API is unreachable.</p>;

  const currentStage = project.ribaStages.find((s) => s.stageNumber === project.currentRibaStage);
  const canAdvance = currentStage?.status === "Gated" || currentStage?.status === "Complete";

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
          disabled={advanceStage.isPending || project.currentRibaStage >= 7 || !canAdvance}
          title={canAdvance ? undefined : "This stage's gateway must be approved first"}
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

          <Card>
            <CardHeader><CardTitle>Stage Gate — Stage {currentStage?.stageNumber} ({currentStage?.stageName})</CardTitle></CardHeader>
            <CardContent className="flex items-center justify-between gap-3 pt-0">
              {!currentStage?.pendingGatewayId && currentStage?.gatewayStatus !== "Approved" && (
                <>
                  <p className="text-sm text-text-secondary">
                    {currentStage?.gatewayStatus === "Rejected"
                      ? "The previous gateway request was rejected. Submit a new request when ready."
                      : "No gateway approval has been requested for this stage yet."}
                  </p>
                  <Button
                    variant="secondary"
                    size="sm"
                    disabled={createGateway.isPending}
                    onClick={() =>
                      currentStage &&
                      createGateway.mutate({ stageNumber: currentStage.stageNumber, gatewayType: "StageGate" })
                    }
                  >
                    {createGateway.isPending ? "Submitting…" : "Request Gateway Approval"}
                  </Button>
                </>
              )}

              {currentStage?.pendingGatewayId && (
                <>
                  <p className="text-sm text-text-secondary">
                    A gateway approval decision is pending for this stage.
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      disabled={decideGateway.isPending}
                      onClick={() =>
                        currentStage.pendingGatewayId &&
                        decideGateway.mutate({ gatewayId: currentStage.pendingGatewayId, decision: "Approved" })
                      }
                    >
                      Approve
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={decideGateway.isPending}
                      onClick={() =>
                        currentStage.pendingGatewayId &&
                        decideGateway.mutate({ gatewayId: currentStage.pendingGatewayId, decision: "Rejected" })
                      }
                    >
                      Reject
                    </Button>
                  </div>
                </>
              )}

              {currentStage?.gatewayStatus === "Approved" && !currentStage.pendingGatewayId && (
                <p className="text-sm text-success">Gateway approved — this stage can advance.</p>
              )}
            </CardContent>
          </Card>

          {project.description && (
            <Card>
              <CardHeader><CardTitle>Description</CardTitle></CardHeader>
              <CardContent className="pt-0 text-sm text-text-primary">{project.description}</CardContent>
            </Card>
          )}
        </Tabs.Content>

        <Tabs.Content value="Governance" className="pt-4">
          {projectId && <GovernanceTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Cost" className="pt-4">
          {projectId && <CostTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Programme" className="pt-4">
          {projectId && <ProgrammeTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Snapshots" className="pt-4">
          {projectId && <SnapshotsTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Documents" className="pt-4">
          {projectId && <DocumentsTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Reports" className="pt-4">
          {projectId && <ReportsTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Risks" className="pt-4">
          {projectId && <RisksTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Issues" className="pt-4">
          {projectId && <IssuesTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Opportunities" className="pt-4">
          {projectId && <OpportunitiesTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="Stakeholders" className="pt-4">
          {projectId && <StakeholdersTab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="NEC4" className="pt-4">
          {projectId && <NEC4Tab projectId={projectId} />}
        </Tabs.Content>
        <Tabs.Content value="SBCC" className="pt-4">
          {projectId && <SBCCTab projectId={projectId} />}
        </Tabs.Content>

        {WORKSPACE_TABS.filter((t) => !FUNCTIONAL_TABS.has(t)).map((tab) => (
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
