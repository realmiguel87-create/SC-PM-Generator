import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { MilestoneTimeline } from "./MilestoneTimeline";
import { useCreateMilestone, useMilestones, useUpdateMilestoneStatus } from "./api";

export function ProgrammeTab({ projectId }: { projectId: string }) {
  const { data: milestones, isLoading, isError } = useMilestones(projectId);
  const createMilestone = useCreateMilestone(projectId);
  const updateStatus = useUpdateMilestoneStatus(projectId);

  const [name, setName] = useState("");
  const [baselineDate, setBaselineDate] = useState("");
  const [forecastDate, setForecastDate] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading programme…</p>;
  if (isError || !milestones) return <p className="text-sm text-critical">Could not load milestones.</p>;

  return (
    <div className="flex flex-col gap-4">
      <MilestoneTimeline milestones={milestones} />

      <Card>
        <CardHeader><CardTitle>Add Milestone</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Name
            <input
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Baseline Date
            <input
              type="date"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={baselineDate}
              onChange={(e) => setBaselineDate(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Forecast Date
            <input
              type="date"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={forecastDate}
              onChange={(e) => setForecastDate(e.target.value)}
            />
          </label>
          <Button
            size="sm"
            disabled={!name || !baselineDate || !forecastDate || createMilestone.isPending}
            onClick={() =>
              createMilestone.mutate(
                { name, baselineDate, forecastDate, isKeyMilestone: false },
                { onSuccess: () => { setName(""); setBaselineDate(""); setForecastDate(""); } },
              )
            }
          >
            {createMilestone.isPending ? "Adding…" : "Add"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Milestones</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {milestones.length === 0 ? (
            <p className="text-sm text-text-secondary">No milestones yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Milestone</th>
                  <th className="py-1.5 font-medium">Baseline</th>
                  <th className="py-1.5 font-medium">Forecast</th>
                  <th className="py-1.5 font-medium">Delay</th>
                  <th className="py-1.5 font-medium">Status</th>
                  <th className="py-1.5 font-medium" />
                </tr>
              </thead>
              <tbody>
                {milestones.map((m) => (
                  <tr key={m.id} className="border-b border-border last:border-0">
                    <td className="py-1.5 font-medium">{m.name}</td>
                    <td className="py-1.5">{formatDate(m.baselineDate)}</td>
                    <td className="py-1.5">{formatDate(m.forecastDate)}</td>
                    <td className={`py-1.5 ${m.delayDays > 0 ? "text-critical" : "text-success"}`}>
                      {m.delayDays > 0 ? `+${m.delayDays}d` : `${m.delayDays}d`}
                    </td>
                    <td className="py-1.5">
                      <Badge variant={statusToBadgeVariant(m.status)}>{m.status}</Badge>
                    </td>
                    <td className="py-1.5 text-right">
                      {m.status !== "Complete" && (
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={updateStatus.isPending}
                          onClick={() => updateStatus.mutate({ milestoneId: m.id, status: "Complete" })}
                        >
                          Mark Complete
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
