import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { useCreateIssue, useIssues, useUpdateIssueStatus } from "./api";
import type { IssueSeverity } from "./types";

const SEVERITIES: IssueSeverity[] = ["Low", "Medium", "High", "Critical"];

export function IssuesTab({ projectId }: { projectId: string }) {
  const { data: issues, isLoading, isError } = useIssues(projectId);
  const createIssue = useCreateIssue(projectId);
  const updateStatus = useUpdateIssueStatus(projectId);

  const [title, setTitle] = useState("");
  const [severity, setSeverity] = useState<IssueSeverity>("Medium");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading issues…</p>;
  if (isError || !issues) return <p className="text-sm text-critical">Could not load issues.</p>;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Raise Issue</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Title
            <input className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Severity
            <select
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={severity}
              onChange={(e) => setSeverity(e.target.value as IssueSeverity)}
            >
              {SEVERITIES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </label>
          <Button
            size="sm"
            disabled={!title || createIssue.isPending}
            onClick={() =>
              createIssue.mutate(
                { title, severity, raisedDate: new Date().toISOString().slice(0, 10) },
                { onSuccess: () => setTitle("") },
              )
            }
          >
            {createIssue.isPending ? "Raising…" : "Raise Issue"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Issue Log</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {issues.length === 0 ? (
            <p className="text-sm text-text-secondary">No issues raised yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Issue</th>
                  <th className="py-1.5 font-medium">Raised</th>
                  <th className="py-1.5 font-medium">Severity</th>
                  <th className="py-1.5 font-medium">Status</th>
                  <th className="py-1.5 font-medium" />
                </tr>
              </thead>
              <tbody>
                {issues.map((i) => (
                  <tr key={i.id} className="border-b border-border last:border-0">
                    <td className="py-1.5 font-medium">{i.title}</td>
                    <td className="py-1.5">{formatDate(i.raisedDate)}</td>
                    <td className="py-1.5"><Badge variant={statusToBadgeVariant(i.severity === "Critical" ? "Rejected" : i.severity)}>{i.severity}</Badge></td>
                    <td className="py-1.5"><Badge variant={statusToBadgeVariant(i.status)}>{i.status}</Badge></td>
                    <td className="py-1.5 text-right">
                      {i.status !== "Closed" && i.status !== "Resolved" && (
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={updateStatus.isPending}
                          onClick={() => updateStatus.mutate({ issueId: i.id, status: "Resolved" })}
                        >
                          Mark Resolved
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
