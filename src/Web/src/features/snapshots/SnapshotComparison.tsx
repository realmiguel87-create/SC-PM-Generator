import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ApiErrorNotice } from "@/components/ApiErrorNotice";
import { useCompareSnapshotItems, useCompareSnapshots } from "@/features/reporting/api";
import type { MilestoneChange, RiskChange } from "@/features/reporting/types";
import { formatCurrency } from "@/lib/utils";
import type { Snapshot } from "./api";

/**
 * Compares two snapshots: the headline movements, then which individual risks and milestones
 * moved.
 *
 * Direction is deliberately not enforced. Comparing a later snapshot against an earlier one is a
 * legitimate question, and every delta is To minus From either way — so a positive number always
 * means the figure increased, never that it improved. That distinction is why the colouring here
 * is driven by an explicit `higherIsWorse` flag per row rather than by the sign alone: a budget
 * rising and a risk score rising are not the same news.
 */
export function SnapshotComparison({ snapshots }: { snapshots: Snapshot[] }) {
  // Newest is the natural "to", the one before it the natural "from" — the comparison people
  // want most of the time, available without touching the selects.
  const [fromId, setFromId] = useState(() => snapshots[1]?.id ?? "");
  const [toId, setToId] = useState(() => snapshots[0]?.id ?? "");

  const summary = useCompareSnapshots(fromId, toId);
  const items = useCompareSnapshotItems(fromId, toId);

  if (snapshots.length < 2) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Compare Snapshots</CardTitle>
        </CardHeader>
        <CardContent className="pt-0 text-sm text-text-secondary">
          Two snapshots are needed before anything can be compared. Capture another, or wait for
          the next scheduled one.
        </CardContent>
      </Card>
    );
  }

  const sameSnapshot = fromId === toId;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Compare Snapshots</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
        <div className="flex flex-wrap items-end gap-3">
          <SnapshotSelect label="From" value={fromId} onChange={setFromId} snapshots={snapshots} />
          <SnapshotSelect label="To" value={toId} onChange={setToId} snapshots={snapshots} />
        </div>

        {sameSnapshot && (
          <p className="text-sm text-text-secondary">
            Pick two different snapshots to see what changed between them.
          </p>
        )}

        {summary.isError && <ApiErrorNotice error={summary.error} />}

        {summary.data && (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[36rem] text-sm">
              <thead>
                <tr className="border-b border-border text-left text-xs uppercase text-text-secondary">
                  <th className="py-1.5 font-medium">Measure</th>
                  <th className="py-1.5 font-medium">{summary.data.fromLabel}</th>
                  <th className="py-1.5 font-medium">{summary.data.toLabel}</th>
                  <th className="py-1.5 font-medium">Movement</th>
                </tr>
              </thead>
              <tbody>
                <DeltaRow
                  label="Approved budget"
                  from={formatCurrency(summary.data.fromApprovedBudget)}
                  to={formatCurrency(summary.data.toApprovedBudget)}
                  delta={summary.data.budgetDelta}
                  format={formatCurrency}
                  higherIsWorse
                />
                <DeltaRow
                  label="Forecast cost"
                  from={formatCurrency(summary.data.fromForecastCost)}
                  to={formatCurrency(summary.data.toForecastCost)}
                  delta={summary.data.forecastDelta}
                  format={formatCurrency}
                  higherIsWorse
                />
                <DeltaRow
                  label="Open risks"
                  from={summary.data.fromOpenRiskCount}
                  to={summary.data.toOpenRiskCount}
                  delta={summary.data.openRiskCountDelta}
                  higherIsWorse
                />
                <DeltaRow
                  label="High risks (15+)"
                  from={summary.data.fromHighRiskCount}
                  to={summary.data.toHighRiskCount}
                  delta={summary.data.highRiskCountDelta}
                  higherIsWorse
                />
                <DeltaRow
                  label="Open issues"
                  from={summary.data.fromOpenIssueCount}
                  to={summary.data.toOpenIssueCount}
                  delta={summary.data.openIssueCountDelta}
                  higherIsWorse
                />
                <DeltaRow
                  label="Milestones delayed"
                  from={summary.data.fromMilestonesDelayedCount}
                  to={summary.data.toMilestonesDelayedCount}
                  delta={summary.data.milestonesDelayedCountDelta}
                  higherIsWorse
                />
                <DeltaRow
                  label="Worst milestone slip (days)"
                  from={summary.data.fromWorstMilestoneDelayDays}
                  to={summary.data.toWorstMilestoneDelayDays}
                  delta={summary.data.worstMilestoneDelayDaysDelta}
                  higherIsWorse
                />
                <DeltaRow
                  label="Compensation event value"
                  from={formatCurrency(summary.data.fromCompensationEventValue)}
                  to={formatCurrency(summary.data.toCompensationEventValue)}
                  delta={summary.data.compensationEventValueDelta}
                  format={formatCurrency}
                  higherIsWorse
                />
              </tbody>
            </table>
          </div>
        )}

        {items.isLoading && !sameSnapshot && (
          <p className="text-sm text-text-secondary">Reading the register history…</p>
        )}
        {items.isError && <ApiErrorNotice error={items.error} />}

        {items.data && !items.data.hasChanges && (
          <p className="text-sm text-text-secondary">
            No individual risks or milestones changed between these two points.
          </p>
        )}

        {items.data && items.data.riskChanges.length > 0 && (
          <section className="flex flex-col gap-2">
            <h4 className="text-xs font-medium uppercase text-text-secondary">Risk changes</h4>
            <ul className="flex flex-col gap-1.5">
              {items.data.riskChanges.map((change) => (
                <RiskChangeRow key={change.riskId} change={change} />
              ))}
            </ul>
          </section>
        )}

        {items.data && items.data.milestoneChanges.length > 0 && (
          <section className="flex flex-col gap-2">
            <h4 className="text-xs font-medium uppercase text-text-secondary">Milestone changes</h4>
            <ul className="flex flex-col gap-1.5">
              {items.data.milestoneChanges.map((change) => (
                <MilestoneChangeRow key={change.milestoneId} change={change} />
              ))}
            </ul>
          </section>
        )}
      </CardContent>
    </Card>
  );
}

function SnapshotSelect({
  label,
  value,
  onChange,
  snapshots,
}: {
  label: string;
  value: string;
  onChange: (id: string) => void;
  snapshots: Snapshot[];
}) {
  const id = `snapshot-${label.toLowerCase()}`;
  return (
    <label htmlFor={id} className="flex flex-col gap-1 text-xs text-text-secondary">
      {label}
      <select
        id={id}
        className="rounded-md border border-border bg-card px-2 py-1.5 text-sm text-text-primary"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      >
        {snapshots.map((s) => (
          <option key={s.id} value={s.id}>
            {new Date(s.capturedAt).toLocaleDateString("en-GB")} — {s.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function DeltaRow({
  label,
  from,
  to,
  delta,
  format,
  higherIsWorse,
}: {
  label: string;
  from: string | number;
  to: string | number;
  delta: number;
  format?: (value: number) => string;
  higherIsWorse?: boolean;
}) {
  // Zero is neither good nor bad and is deliberately not coloured — colouring "no change" green
  // would imply a result where there is only an absence of one.
  const tone =
    delta === 0
      ? "text-text-secondary"
      : (delta > 0) === !!higherIsWorse
        ? "text-critical"
        : "text-stirling-green";

  const shown = format ? format(Math.abs(delta)) : Math.abs(delta);
  const sign = delta > 0 ? "+" : delta < 0 ? "−" : "";

  return (
    <tr className="border-b border-border last:border-0">
      <td className="py-1.5">{label}</td>
      <td className="py-1.5 text-text-secondary">{from}</td>
      <td className="py-1.5">{to}</td>
      <td className={`py-1.5 font-medium ${tone}`}>
        {delta === 0 ? "No change" : `${sign}${shown}`}
      </td>
    </tr>
  );
}

function changeBadge(changeType: string) {
  if (changeType === "Added") return <Badge variant="warning">New</Badge>;
  if (changeType === "Removed") return <Badge variant="information">Removed</Badge>;
  return <Badge variant="information">Changed</Badge>;
}

function RiskChangeRow({ change }: { change: RiskChange }) {
  return (
    <li className="flex flex-wrap items-center gap-2 border-b border-border pb-1.5 text-sm last:border-0">
      {changeBadge(change.changeType)}
      <span className="font-medium">{change.title}</span>
      <span className="text-text-secondary">
        {change.changeType === "Added" && `raised at score ${change.toScore} (${change.toStatus})`}
        {change.changeType === "Removed" && `was score ${change.fromScore} (${change.fromStatus})`}
        {change.changeType === "Modified" && (
          <>
            {change.fromStatus !== change.toStatus && `${change.fromStatus} → ${change.toStatus}`}
            {change.fromStatus !== change.toStatus && change.scoreDelta !== 0 && ", "}
            {change.scoreDelta !== 0 && `score ${change.fromScore} → ${change.toScore}`}
          </>
        )}
      </span>
      {change.scoreDelta !== null && change.scoreDelta !== 0 && (
        <span className={change.scoreDelta > 0 ? "text-critical" : "text-stirling-green"}>
          {change.scoreDelta > 0 ? "+" : "−"}
          {Math.abs(change.scoreDelta)}
        </span>
      )}
    </li>
  );
}

function MilestoneChangeRow({ change }: { change: MilestoneChange }) {
  return (
    <li className="flex flex-wrap items-center gap-2 border-b border-border pb-1.5 text-sm last:border-0">
      {changeBadge(change.changeType)}
      <span className="font-medium">{change.name}</span>
      <span className="text-text-secondary">
        {change.changeType === "Modified" && change.fromStatus !== change.toStatus &&
          `${change.fromStatus} → ${change.toStatus}`}
        {change.delayDaysDelta !== null && change.delayDaysDelta !== 0 && (
          <>
            {change.fromStatus !== change.toStatus && ", "}
            {change.delayDaysDelta > 0
              ? `slipped a further ${change.delayDaysDelta} days`
              : `recovered ${Math.abs(change.delayDaysDelta)} days`}
          </>
        )}
        {change.changeType === "Added" && `added, ${change.toDelayDays} days against baseline`}
        {change.changeType === "Removed" && "removed from the programme"}
      </span>
    </li>
  );
}
