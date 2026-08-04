import { useState } from "react";
import { CartesianGrid, ResponsiveContainer, Scatter, ScatterChart, Tooltip, XAxis, YAxis, ZAxis } from "recharts";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useCreateRisk, useRisks, useUpdateRiskStatus } from "./api";

export function RisksTab({ projectId }: { projectId: string }) {
  const { data: risks, isLoading, isError } = useRisks(projectId);
  const createRisk = useCreateRisk(projectId);
  const updateStatus = useUpdateRiskStatus(projectId);

  const [title, setTitle] = useState("");
  const [category, setCategory] = useState("");
  const [probability, setProbability] = useState(3);
  const [impact, setImpact] = useState(3);

  if (isLoading) return <p className="text-sm text-text-secondary">Loading risk register…</p>;
  if (isError || !risks) return <p className="text-sm text-critical">Could not load risks.</p>;

  const heatmapData = risks.map((r) => ({ ...r, x: r.probability, y: r.impact, z: r.score }));

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader><CardTitle>Risk Heatmap</CardTitle></CardHeader>
        <CardContent className="h-72 pt-0">
          {risks.length === 0 ? (
            <p className="text-sm text-text-secondary">No risks recorded yet.</p>
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <ScatterChart margin={{ top: 10, right: 20, bottom: 10, left: 0 }}>
                <CartesianGrid stroke="var(--border)" />
                <XAxis type="number" dataKey="x" name="Probability" domain={[0, 6]} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} />
                <YAxis type="number" dataKey="y" name="Impact" domain={[0, 6]} tick={{ fontSize: 11, fill: "var(--text-secondary)" }} />
                <ZAxis type="number" dataKey="z" range={[80, 400]} name="Score" />
                <Tooltip
                  cursor={{ strokeDasharray: "3 3" }}
                  contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", fontSize: 12 }}
                  formatter={(value, name) => [value, name]}
                  labelFormatter={() => ""}
                />
                <Scatter data={heatmapData} fill="var(--critical)" />
              </ScatterChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Add Risk</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-1 min-w-[10rem] flex-col gap-1 text-xs text-text-secondary">
            Title
            <input className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Category
            <input className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={category} onChange={(e) => setCategory(e.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Probability (1-5)
            <input type="number" min={1} max={5} className="w-20 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={probability} onChange={(e) => setProbability(Number(e.target.value))} />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Impact (1-5)
            <input type="number" min={1} max={5} className="w-20 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={impact} onChange={(e) => setImpact(Number(e.target.value))} />
          </label>
          <Button
            size="sm"
            disabled={!title || !category || createRisk.isPending}
            onClick={() =>
              createRisk.mutate(
                { title, category, probability, impact },
                { onSuccess: () => { setTitle(""); setCategory(""); setProbability(3); setImpact(3); } },
              )
            }
          >
            {createRisk.isPending ? "Adding…" : "Add Risk"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Risk Register</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {risks.length === 0 ? (
            <p className="text-sm text-text-secondary">No risks recorded yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Risk</th>
                  <th className="py-1.5 font-medium">Category</th>
                  <th className="py-1.5 font-medium">Score</th>
                  <th className="py-1.5 font-medium">Status</th>
                  <th className="py-1.5 font-medium" />
                </tr>
              </thead>
              <tbody>
                {risks.map((r) => (
                  <tr key={r.id} className="border-b border-border last:border-0">
                    <td className="py-1.5 font-medium">{r.title}</td>
                    <td className="py-1.5 text-text-secondary">{r.category}</td>
                    <td className="py-1.5">{r.score}</td>
                    <td className="py-1.5"><Badge variant={statusToBadgeVariant(r.status)}>{r.status}</Badge></td>
                    <td className="py-1.5 text-right">
                      {r.status !== "Closed" && (
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={updateStatus.isPending}
                          onClick={() => updateStatus.mutate({ riskId: r.id, status: "Mitigated" })}
                        >
                          Mark Mitigated
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
