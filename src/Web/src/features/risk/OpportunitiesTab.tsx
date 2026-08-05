import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/utils";
import { useCreateOpportunity, useOpportunities, useUpdateOpportunityStatus } from "./api";

export function OpportunitiesTab({ projectId }: { projectId: string }) {
  const { data: opportunities, isLoading, isError } = useOpportunities(projectId);
  const createOpportunity = useCreateOpportunity(projectId);
  const updateStatus = useUpdateOpportunityStatus(projectId);

  const [title, setTitle] = useState("");
  const [potentialValue, setPotentialValue] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading opportunities…</p>;
  if (isError || !opportunities) return <p className="text-sm text-critical">Could not load opportunities.</p>;

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Log Opportunity</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Title
            <input className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Potential Value (£)
            <input type="number" min={0} className="w-36 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={potentialValue} onChange={(e) => setPotentialValue(e.target.value)} />
          </label>
          <Button
            size="sm"
            disabled={!title || !potentialValue || createOpportunity.isPending}
            onClick={() =>
              createOpportunity.mutate(
                { title, potentialValue: Number(potentialValue), probability: 3 },
                { onSuccess: () => { setTitle(""); setPotentialValue(""); } },
              )
            }
          >
            {createOpportunity.isPending ? "Saving…" : "Add Opportunity"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Opportunity Register</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {opportunities.length === 0 ? (
            <p className="text-sm text-text-secondary">No opportunities logged yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Opportunity</th>
                  <th className="py-1.5 font-medium">Potential Value</th>
                  <th className="py-1.5 font-medium">Status</th>
                  <th className="py-1.5 font-medium" />
                </tr>
              </thead>
              <tbody>
                {opportunities.map((o) => (
                  <tr key={o.id} className="border-b border-border last:border-0">
                    <td className="py-1.5 font-medium">{o.title}</td>
                    <td className="py-1.5">{formatCurrency(o.potentialValue)}</td>
                    <td className="py-1.5"><Badge variant={statusToBadgeVariant(o.status === "Realised" ? "Approved" : o.status)}>{o.status}</Badge></td>
                    <td className="py-1.5 text-right">
                      {o.status === "Identified" && (
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={updateStatus.isPending}
                          onClick={() => updateStatus.mutate({ opportunityId: o.id, status: "BeingPursued" })}
                        >
                          Pursue
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
