import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/utils";
import { useCreateManualSnapshot, useSnapshots } from "./api";

export function SnapshotsTab({ projectId }: { projectId: string }) {
  const { data: snapshots, isLoading, isError } = useSnapshots(projectId);
  const createSnapshot = useCreateManualSnapshot(projectId);
  const [label, setLabel] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading snapshots…</p>;
  if (isError || !snapshots) return <p className="text-sm text-critical">Could not load snapshots.</p>;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Capture a Manual Snapshot</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[12rem] flex-col gap-1 text-xs text-text-secondary">
            Label
            <input
              placeholder="e.g. Pre-committee baseline"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={label}
              onChange={(e) => setLabel(e.target.value)}
            />
          </label>
          <Button
            size="sm"
            disabled={!label || createSnapshot.isPending}
            onClick={() => createSnapshot.mutate({ label }, { onSuccess: () => setLabel("") })}
          >
            {createSnapshot.isPending ? "Capturing…" : "Capture Snapshot"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Snapshot History</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {snapshots.length === 0 ? (
            <p className="text-sm text-text-secondary">
              No snapshots yet. Scheduled snapshots run automatically (daily/weekly/monthly); you can also capture one manually above.
            </p>
          ) : (
            // Wide enough to need its own scroll container: the register columns matter, and
            // squeezing them into the page width would either truncate figures or force the
            // whole workspace to scroll sideways.
            <div className="overflow-x-auto">
              <table className="w-full min-w-[56rem] text-sm">
                <thead>
                  <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                    <th className="py-1.5 font-medium">Captured</th>
                    <th className="py-1.5 font-medium">Type</th>
                    <th className="py-1.5 font-medium">Label</th>
                    <th className="py-1.5 font-medium">Stage</th>
                    <th className="py-1.5 font-medium">Budget</th>
                    <th className="py-1.5 font-medium">Forecast</th>
                    <th className="py-1.5 font-medium" title="Open risks, with those scoring 15+ in brackets">
                      Risks
                    </th>
                    <th className="py-1.5 font-medium" title="Open issues, with High/Critical in brackets">
                      Issues
                    </th>
                    <th className="py-1.5 font-medium" title="Delayed milestones, with the worst single slip in brackets">
                      Programme
                    </th>
                    <th className="py-1.5 font-medium" title="Compensation event value carried (NEC4)">
                      CE value
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {snapshots.map((s) => (
                    <tr key={s.id} className="border-b border-border last:border-0">
                      <td className="py-1.5">{new Date(s.capturedAt).toLocaleString("en-GB")}</td>
                      <td className="py-1.5"><Badge variant="information">{s.type}</Badge></td>
                      <td className="py-1.5">{s.label}</td>
                      <td className="py-1.5">Stage {s.ribaStageAtCapture}</td>
                      <td className="py-1.5">{formatCurrency(s.approvedBudgetAtCapture)}</td>
                      <td className="py-1.5">{formatCurrency(s.forecastCostAtCapture)}</td>
                      <td className="py-1.5">
                        {s.openRiskCount}
                        {s.highRiskCount > 0 && (
                          <span className="text-critical"> ({s.highRiskCount} high)</span>
                        )}
                      </td>
                      <td className="py-1.5">
                        {s.openIssueCount}
                        {s.severeOpenIssueCount > 0 && (
                          <span className="text-critical"> ({s.severeOpenIssueCount} severe)</span>
                        )}
                      </td>
                      <td className="py-1.5">
                        {s.milestonesDelayedCount === 0
                          ? "On baseline"
                          : `${s.milestonesDelayedCount} late (worst ${s.worstMilestoneDelayDays}d)`}
                      </td>
                      <td className="py-1.5">{formatCurrency(s.compensationEventValue)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
