import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { useCreateEngagement, useCreateStakeholder, useStakeholders } from "./api";

export function StakeholdersTab({ projectId }: { projectId: string }) {
  const { data: stakeholders, isLoading, isError } = useStakeholders(projectId);
  const createStakeholder = useCreateStakeholder(projectId);
  const createEngagement = useCreateEngagement(projectId);

  const [name, setName] = useState("");
  const [organisation, setOrganisation] = useState("");
  const [engagementNotes, setEngagementNotes] = useState<Record<string, string>>({});

  if (isLoading) return <p className="text-sm text-text-secondary">Loading stakeholder register…</p>;
  if (isError || !stakeholders) return <p className="text-sm text-critical">Could not load stakeholders.</p>;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Add Stakeholder</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Name
            <input className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={name} onChange={(e) => setName(e.target.value)} />
          </label>
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Organisation
            <input className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={organisation} onChange={(e) => setOrganisation(e.target.value)} />
          </label>
          <Button
            size="sm"
            disabled={!name || createStakeholder.isPending}
            onClick={() =>
              createStakeholder.mutate(
                { name, organisation: organisation || undefined, influence: "Medium", interest: "Medium" },
                { onSuccess: () => { setName(""); setOrganisation(""); } },
              )
            }
          >
            {createStakeholder.isPending ? "Adding…" : "Add Stakeholder"}
          </Button>
        </CardContent>
      </Card>

      {stakeholders.length === 0 ? (
        <Card><CardContent className="pt-5 text-sm text-text-secondary">No stakeholders registered yet.</CardContent></Card>
      ) : (
        stakeholders.map((s) => (
          <Card key={s.id}>
            <CardHeader className="flex-row items-center justify-between">
              <div>
                <CardTitle className="text-sm font-semibold text-text-primary">{s.name}</CardTitle>
                <p className="text-xs text-text-secondary">{s.organisation ?? "—"}{s.roleTitle ? ` · ${s.roleTitle}` : ""}</p>
              </div>
              <div className="flex gap-2">
                <Badge variant="information">Influence: {s.influence}</Badge>
                <Badge variant="neutral">Interest: {s.interest}</Badge>
              </div>
            </CardHeader>
            <CardContent className="flex flex-col gap-3 pt-0">
              <div className="flex items-end gap-2">
                <input
                  placeholder="Log an engagement…"
                  className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
                  value={engagementNotes[s.id] ?? ""}
                  onChange={(e) => setEngagementNotes((prev) => ({ ...prev, [s.id]: e.target.value }))}
                />
                <Button
                  size="sm"
                  variant="secondary"
                  disabled={!engagementNotes[s.id] || createEngagement.isPending}
                  onClick={() =>
                    createEngagement.mutate(
                      {
                        stakeholderId: s.id,
                        engagementDate: new Date().toISOString().slice(0, 10),
                        method: "Note",
                        summary: engagementNotes[s.id],
                      },
                      { onSuccess: () => setEngagementNotes((prev) => ({ ...prev, [s.id]: "" })) },
                    )
                  }
                >
                  Log
                </Button>
              </div>
              {s.engagements.length > 0 && (
                <ul className="flex flex-col gap-1 text-xs text-text-secondary">
                  {s.engagements.map((e) => (
                    <li key={e.id}>
                      <span className="font-medium text-text-primary">{formatDate(e.engagementDate)}</span> — {e.summary}
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
        ))
      )}
    </div>
  );
}
