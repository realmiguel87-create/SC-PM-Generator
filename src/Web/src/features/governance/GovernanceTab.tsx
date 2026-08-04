import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { useCreateDecision, useDecisions } from "./api";

export function GovernanceTab({ projectId }: { projectId: string }) {
  const { data: decisions, isLoading, isError } = useDecisions(projectId);
  const createDecision = useCreateDecision(projectId);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading decision register…</p>;
  if (isError || !decisions) return <p className="text-sm text-critical">Could not load the decision register.</p>;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Record a Decision</CardTitle></CardHeader>
        <CardContent className="flex flex-col gap-3 pt-0">
          <input
            placeholder="Title"
            className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
          />
          <textarea
            placeholder="Description"
            rows={2}
            className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
          <div>
            <Button
              size="sm"
              disabled={!title || !description || createDecision.isPending}
              onClick={() =>
                createDecision.mutate(
                  { title, description, decisionDate: new Date().toISOString().slice(0, 10) },
                  { onSuccess: () => { setTitle(""); setDescription(""); } },
                )
              }
            >
              {createDecision.isPending ? "Saving…" : "Record Decision"}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Decision Register</CardTitle></CardHeader>
        <CardContent className="flex flex-col gap-3 pt-0">
          {decisions.length === 0 ? (
            <p className="text-sm text-text-secondary">No decisions recorded yet.</p>
          ) : (
            decisions.map((d) => (
              <div key={d.id} className="rounded-md border border-border p-3">
                <div className="flex items-center justify-between">
                  <h4 className="text-sm font-semibold">{d.title}</h4>
                  <span className="text-xs text-text-secondary">{formatDate(d.decisionDate)}</span>
                </div>
                <p className="mt-1 text-sm text-text-secondary">{d.description}</p>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
