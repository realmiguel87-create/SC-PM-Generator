import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { formatCurrency, formatDate } from "@/lib/utils";
import { useCostSummary, useRecordForecast } from "./api";

export function CostTab({ projectId }: { projectId: string }) {
  const { data: summary, isLoading, isError } = useCostSummary(projectId);
  const recordForecast = useRecordForecast(projectId);

  const [forecastCost, setForecastCost] = useState("");
  const [notes, setNotes] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading cost data…</p>;
  if (isError || !summary) return <p className="text-sm text-critical">Could not load cost data.</p>;

  return (
    <div className="flex flex-col gap-4">
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <Card>
          <CardHeader><CardTitle>Approved Budget</CardTitle></CardHeader>
          <CardContent className="pt-0 text-xl font-semibold">{formatCurrency(summary.approvedBudget)}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>Current Forecast</CardTitle></CardHeader>
          <CardContent className="pt-0 text-xl font-semibold">{formatCurrency(summary.currentForecastCost)}</CardContent>
        </Card>
        <Card>
          <CardHeader><CardTitle>Variance</CardTitle></CardHeader>
          <CardContent
            className={`pt-0 text-xl font-semibold ${summary.currentVariance > 0 ? "text-critical" : "text-success"}`}
          >
            {formatCurrency(summary.currentVariance)}
          </CardContent>
        </Card>
      </div>

      {summary.baselineCostPlan && (
        <Card>
          <CardHeader><CardTitle>Baseline Cost Plan — {summary.baselineCostPlan.name}</CardTitle></CardHeader>
          <CardContent className="pt-0">
            <table className="w-full text-sm">
              <tbody>
                {summary.baselineCostPlan.lines.map((line, i) => (
                  <tr key={i} className="border-b border-border last:border-0">
                    <td className="py-1.5 text-text-secondary">{line.costCategory}</td>
                    <td className="py-1.5 text-right font-medium">{formatCurrency(line.amount)}</td>
                  </tr>
                ))}
                <tr>
                  <td className="pt-2 font-semibold">Total</td>
                  <td className="pt-2 text-right font-semibold">{formatCurrency(summary.baselineCostPlan.totalAmount)}</td>
                </tr>
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader><CardTitle>Record New Forecast</CardTitle></CardHeader>
        <CardContent className="flex flex-wrap items-end gap-3 pt-0">
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Forecast Cost (£)
            <input
              type="number"
              min={0}
              className="w-40 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={forecastCost}
              onChange={(e) => setForecastCost(e.target.value)}
            />
          </label>
          <label className="flex flex-1 min-w-[12rem] flex-col gap-1 text-xs text-text-secondary">
            Notes
            <input
              type="text"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
          <Button
            size="sm"
            disabled={!forecastCost || recordForecast.isPending}
            onClick={() => {
              recordForecast.mutate(
                {
                  forecastDate: new Date().toISOString().slice(0, 10),
                  forecastCost: Number(forecastCost),
                  commentaryNotes: notes || undefined,
                },
                { onSuccess: () => { setForecastCost(""); setNotes(""); } },
              );
            }}
          >
            {recordForecast.isPending ? "Saving…" : "Record Forecast"}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>Forecast History</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {summary.forecastHistory.length === 0 ? (
            <p className="text-sm text-text-secondary">No forecasts recorded yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Date</th>
                  <th className="py-1.5 font-medium">Forecast</th>
                  <th className="py-1.5 font-medium">Variance</th>
                  <th className="py-1.5 font-medium">Notes</th>
                </tr>
              </thead>
              <tbody>
                {summary.forecastHistory.map((f) => (
                  <tr key={f.id} className="border-b border-border last:border-0">
                    <td className="py-1.5">{formatDate(f.forecastDate)}</td>
                    <td className="py-1.5">{formatCurrency(f.forecastCost)}</td>
                    <td className={`py-1.5 ${f.variance > 0 ? "text-critical" : "text-success"}`}>
                      {formatCurrency(f.variance)}
                    </td>
                    <td className="py-1.5 text-text-secondary">{f.commentaryNotes ?? "—"}</td>
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
