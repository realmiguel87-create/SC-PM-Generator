import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { ApiErrorNotice } from "@/components/ApiErrorNotice";
import {
  useCompareSnapshotItems,
  useCompareSnapshots,
  useSnapshotIntervalActivity,
} from "@/features/reporting/api";
import type {
  CompensationEventChange,
  EarlyWarningChange,
  ExtensionOfTimeChange,
  IntervalActivityItem,
  MilestoneChange,
  RiskChange,
  VariationChange,
} from "@/features/reporting/types";
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
  const interval = useSnapshotIntervalActivity(fromId, toId);

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
            Nothing on the risk, programme, NEC4 or SBCC registers changed between these two
            points.
          </p>
        )}

        {items.data && items.data.riskChanges.length > 0 && (
          <ChangeSection title="Risk changes">
            {items.data.riskChanges.map((change) => (
              <RiskChangeRow key={change.riskId} change={change} />
            ))}
          </ChangeSection>
        )}

        {items.data && items.data.milestoneChanges.length > 0 && (
          <ChangeSection title="Milestone changes">
            {items.data.milestoneChanges.map((change) => (
              <MilestoneChangeRow key={change.milestoneId} change={change} />
            ))}
          </ChangeSection>
        )}

        {items.data && items.data.earlyWarningChanges.length > 0 && (
          <ChangeSection title="Early warning changes (NEC4)">
            {items.data.earlyWarningChanges.map((change) => (
              <EarlyWarningChangeRow key={change.earlyWarningId} change={change} />
            ))}
          </ChangeSection>
        )}

        {items.data && items.data.compensationEventChanges.length > 0 && (
          <ChangeSection title="Compensation event changes (NEC4)">
            {items.data.compensationEventChanges.map((change) => (
              <CompensationEventChangeRow key={change.compensationEventId} change={change} />
            ))}
          </ChangeSection>
        )}

        {items.data && items.data.variationChanges.length > 0 && (
          <ChangeSection title="Variation changes (SBCC)">
            {items.data.variationChanges.map((change) => (
              <VariationChangeRow key={change.variationId} change={change} />
            ))}
          </ChangeSection>
        )}

        {items.data && items.data.extensionOfTimeChanges.length > 0 && (
          <ChangeSection title="Extension of time changes (SBCC)">
            {items.data.extensionOfTimeChanges.map((change) => (
              <ExtensionOfTimeChangeRow key={change.extensionOfTimeId} change={change} />
            ))}
          </ChangeSection>
        )}

        {interval.isError && <ApiErrorNotice error={interval.error} />}

        {interval.data && interval.data.hasActivity && (
          <section className="flex flex-col gap-2 rounded-md border border-warning/40 bg-warning/5 p-3">
            <h4 className="text-xs font-medium uppercase text-text-secondary">
              Also happened in between
            </h4>
            <p className="text-xs text-text-secondary">
              These left no trace at either end of the period, so nothing above can show them.
            </p>
            <ul className="flex flex-col gap-1.5">
              {interval.data.items.map((item) => (
                <IntervalActivityRow key={`${item.register}-${item.itemId}`} item={item} />
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

function IntervalActivityRow({ item }: { item: IntervalActivityItem }) {
  return (
    <li className="flex flex-wrap items-center gap-2 text-sm">
      <Badge variant="warning">
        {item.activityType === "RaisedAndRemoved" ? "Raised & removed" : "Changed & reverted"}
      </Badge>
      <span className="text-xs uppercase text-text-secondary">{item.register}</span>
      <span className="font-medium">{item.name}</span>
      {item.versionCount > 2 && (
        <span className="text-text-secondary">({item.versionCount} revisions)</span>
      )}
    </li>
  );
}

/** Every register's changes render the same way — heading, then a list of one-line movements. */
function ChangeSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-2">
      <h4 className="text-xs font-medium uppercase text-text-secondary">{title}</h4>
      <ul className="flex flex-col gap-1.5">{children}</ul>
    </section>
  );
}

function ChangeRow({
  changeType,
  name,
  children,
  trailing,
}: {
  changeType: string;
  name: string;
  children: React.ReactNode;
  trailing?: React.ReactNode;
}) {
  return (
    <li className="flex flex-wrap items-center gap-2 border-b border-border pb-1.5 text-sm last:border-0">
      {changeBadge(changeType)}
      <span className="font-medium">{name}</span>
      <span className="text-text-secondary">{children}</span>
      {trailing}
    </li>
  );
}

/** Signed money, coloured on the assumption that a commercial figure rising is bad news. */
function ValueDelta({ delta }: { delta: number | null }) {
  if (delta === null || delta === 0) return null;
  return (
    <span className={delta > 0 ? "text-critical" : "text-stirling-green"}>
      {delta > 0 ? "+" : "−"}
      {formatCurrency(Math.abs(delta))}
    </span>
  );
}

function statusTransition(from: string | null, to: string | null) {
  return from !== to ? `${from} → ${to}` : null;
}

function EarlyWarningChangeRow({ change }: { change: EarlyWarningChange }) {
  return (
    <ChangeRow changeType={change.changeType} name={change.title}>
      {change.changeType === "Added" && `raised (${change.toStatus})`}
      {change.changeType === "Removed" && "removed from the register"}
      {change.changeType === "Modified" && statusTransition(change.fromStatus, change.toStatus)}
    </ChangeRow>
  );
}

function CompensationEventChangeRow({ change }: { change: CompensationEventChange }) {
  const transition = statusTransition(change.fromStatus, change.toStatus);
  return (
    <ChangeRow
      changeType={change.changeType}
      name={`${change.reference} — ${change.title}`}
      trailing={<ValueDelta delta={change.estimatedValueDelta} />}
    >
      {change.changeType === "Added" &&
        `notified at ${formatCurrency(change.toEstimatedValue ?? 0)}`}
      {change.changeType === "Removed" &&
        `was ${formatCurrency(change.fromEstimatedValue ?? 0)}`}
      {change.changeType === "Modified" && (
        <>
          {transition}
          {transition && change.estimatedValueDelta !== 0 && ", "}
          {change.estimatedValueDelta !== 0 &&
            `${formatCurrency(change.fromEstimatedValue ?? 0)} → ${formatCurrency(change.toEstimatedValue ?? 0)}`}
        </>
      )}
    </ChangeRow>
  );
}

function VariationChangeRow({ change }: { change: VariationChange }) {
  const transition = statusTransition(change.fromStatus, change.toStatus);
  return (
    <ChangeRow
      changeType={change.changeType}
      name={`${change.reference} — ${change.description}`}
      trailing={<ValueDelta delta={change.valueImpactDelta} />}
    >
      {change.changeType === "Added" &&
        `instructed at ${formatCurrency(change.toValueImpact ?? 0)}`}
      {change.changeType === "Removed" && "removed from the register"}
      {change.changeType === "Modified" && (
        <>
          {transition}
          {transition && change.valueImpactDelta !== 0 && ", "}
          {change.valueImpactDelta !== 0 &&
            `${formatCurrency(change.fromValueImpact ?? 0)} → ${formatCurrency(change.toValueImpact ?? 0)}`}
        </>
      )}
    </ChangeRow>
  );
}

function ExtensionOfTimeChangeRow({ change }: { change: ExtensionOfTimeChange }) {
  // Claimed and awarded are shown separately and labelled: a claim is the contractor's position,
  // an award is the programme actually moving, and the two must not read as one figure.
  const claimMoved = change.fromDaysClaimed !== change.toDaysClaimed;
  const awardMoved = change.fromDaysAwarded !== change.toDaysAwarded;

  return (
    <ChangeRow changeType={change.changeType} name={`${change.reference} — ${change.reason}`}>
      {change.changeType === "Added" && `claimed ${change.toDaysClaimed} days`}
      {change.changeType === "Removed" && "withdrawn from the register"}
      {change.changeType === "Modified" && (
        <>
          {claimMoved && `claim ${change.fromDaysClaimed} → ${change.toDaysClaimed} days`}
          {claimMoved && awardMoved && ", "}
          {awardMoved &&
            `awarded ${change.fromDaysAwarded ?? "undetermined"} → ${change.toDaysAwarded ?? "undetermined"} days`}
          {!claimMoved && !awardMoved && statusTransition(change.fromStatus, change.toStatus)}
        </>
      )}
    </ChangeRow>
  );
}

function changeBadge(changeType: string) {
  if (changeType === "Added") return <Badge variant="warning">New</Badge>;
  if (changeType === "Removed") return <Badge variant="information">Removed</Badge>;
  return <Badge variant="information">Changed</Badge>;
}

function RiskChangeRow({ change }: { change: RiskChange }) {
  const transition = statusTransition(change.fromStatus, change.toStatus);
  return (
    <ChangeRow
      changeType={change.changeType}
      name={change.title}
      trailing={
        change.scoreDelta !== null && change.scoreDelta !== 0 ? (
          <span className={change.scoreDelta > 0 ? "text-critical" : "text-stirling-green"}>
            {change.scoreDelta > 0 ? "+" : "−"}
            {Math.abs(change.scoreDelta)}
          </span>
        ) : undefined
      }
    >
      {change.changeType === "Added" && `raised at score ${change.toScore} (${change.toStatus})`}
      {change.changeType === "Removed" && `was score ${change.fromScore} (${change.fromStatus})`}
      {change.changeType === "Modified" && (
        <>
          {transition}
          {transition && change.scoreDelta !== 0 && ", "}
          {change.scoreDelta !== 0 && `score ${change.fromScore} → ${change.toScore}`}
        </>
      )}
    </ChangeRow>
  );
}

function MilestoneChangeRow({ change }: { change: MilestoneChange }) {
  const transition = statusTransition(change.fromStatus, change.toStatus);
  return (
    <ChangeRow changeType={change.changeType} name={change.name}>
      {change.changeType === "Added" && `added, ${change.toDelayDays} days against baseline`}
      {change.changeType === "Removed" && "removed from the programme"}
      {change.changeType === "Modified" && (
        <>
          {transition}
          {transition && change.delayDaysDelta !== 0 && ", "}
          {change.delayDaysDelta !== null && change.delayDaysDelta !== 0 &&
            (change.delayDaysDelta > 0
              ? `slipped a further ${change.delayDaysDelta} days`
              : `recovered ${Math.abs(change.delayDaysDelta)} days`)}
        </>
      )}
    </ChangeRow>
  );
}
