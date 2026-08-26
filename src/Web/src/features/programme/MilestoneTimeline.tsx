import {
  Bar,
  BarChart,
  Cell,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { buildTimeline, offsetToDate, summariseDelay, type TimelineRow } from "./timeline";
import type { Milestone } from "./types";

/**
 * The programme as a picture: every milestone's baseline against where it now sits.
 *
 * Not a Gantt in the usual sense, and deliberately so. A Gantt draws tasks, each with a start, an
 * end and therefore a length. These are milestones — single dates with no duration — so the only
 * bar that means anything is the slip between baseline and current position. Drawing task bars
 * would mean inventing durations nobody entered: more familiar to look at, and less true.
 *
 * All the arithmetic lives in timeline.ts and is unit-tested there. A chart is the one part of an
 * app that cannot be checked by reading it, so the parts that can be wrong — offsets, direction,
 * which date wins — are kept out of the rendering.
 */
export function MilestoneTimeline({ milestones }: { milestones: Milestone[] }) {
  const timeline = buildTimeline(milestones);
  const summary = summariseDelay(timeline.rows);

  if (timeline.rows.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Programme Timeline</CardTitle>
        </CardHeader>
        <CardContent className="pt-0 text-sm text-text-secondary">
          Add milestones to see the programme against its baseline.
        </CardContent>
      </Card>
    );
  }

  const formatOffset = (offset: number) =>
    offsetToDate(timeline.originDate, offset).toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "short",
      year: "2-digit",
    });

  // Row height rather than a fixed chart height: twenty milestones in a 300px box is a smear.
  const chartHeight = Math.max(160, timeline.rows.length * 34 + 40);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Programme Timeline</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
        <DelayHeadline summary={summary} />

        <div style={{ height: chartHeight }}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart
              layout="vertical"
              data={timeline.rows}
              margin={{ top: 5, right: 24, bottom: 5, left: 8 }}
              barCategoryGap="25%"
            >
              <XAxis
                type="number"
                domain={[timeline.min, timeline.max]}
                tickFormatter={formatOffset}
                tick={{ fontSize: 11, fill: "var(--text-secondary)" }}
              />
              <YAxis
                type="category"
                dataKey="name"
                width={150}
                tick={{ fontSize: 11, fill: "var(--text-secondary)" }}
              />
              <Tooltip content={<TimelineTooltip />} cursor={{ fill: "var(--border)", opacity: 0.3 }} />

              {timeline.todayOffset !== null && (
                <ReferenceLine
                  x={timeline.todayOffset}
                  stroke="var(--text-secondary)"
                  strokeDasharray="3 3"
                  label={{ value: "Today", position: "top", fontSize: 10, fill: "var(--text-secondary)" }}
                />
              )}

              {/* Transparent spacer: positions the visible bar without drawing anything. */}
              <Bar dataKey="barOffset" stackId="slip" fill="transparent" isAnimationActive={false} />

              {/* Named so a browser test can assert one slip bar per milestone. Recharts emits a
                  rectangle for the transparent spacer above as well, and counting those together
                  would pass whatever the chart drew. */}
              <Bar
                dataKey="barSpan"
                stackId="slip"
                className="milestone-slip-bar"
                isAnimationActive={false}
                minPointSize={2}
              >
                {timeline.rows.map((row) => (
                  <Cell
                    key={row.id}
                    // minPointSize gives an on-programme milestone a 2px mark so it is visible at
                    // all; colouring it neutral stops that mark reading as a small slip.
                    fill={
                      row.delayDays > 0
                        ? "var(--critical)"
                        : row.delayDays < 0
                          ? "var(--stirling-green)"
                          : "var(--text-secondary)"
                    }
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>

        <p className="text-xs text-text-secondary">
          Each bar runs from a milestone&rsquo;s baseline to where it now sits — red for slipped,
          green for recovered, grey where nothing has moved. Completed milestones use their actual
          date; the rest use the current forecast.
        </p>
      </CardContent>
    </Card>
  );
}

function DelayHeadline({ summary }: { summary: ReturnType<typeof summariseDelay> }) {
  return (
    <div className="grid grid-cols-2 gap-3 text-sm lg:grid-cols-4">
      <Figure label="Milestones" value={String(summary.total)} note={`${summary.complete} complete`} />
      <Figure
        label="Late"
        value={String(summary.late)}
        note={`${summary.onProgramme} on programme, ${summary.recovered} recovered`}
        tone={summary.late > 0 ? "critical" : "neutral"}
      />
      <Figure
        label="Worst slip"
        // The worst single slip, not a total: ten milestones one day late is a different
        // programme from one six months late, and a sum makes those read alike.
        value={summary.worstSlipDays > 0 ? `${summary.worstSlipDays}d` : "—"}
        note={summary.worstSlipName ?? "Nothing has slipped"}
        tone={summary.worstSlipDays > 0 ? "critical" : "neutral"}
      />
      <Figure
        label="Key milestone slip"
        value={summary.keyMilestoneSlipDays > 0 ? `${summary.keyMilestoneSlipDays}d` : "—"}
        note="Total across key milestones"
        tone={summary.keyMilestoneSlipDays > 0 ? "critical" : "neutral"}
      />
    </div>
  );
}

function Figure({
  label,
  value,
  note,
  tone = "neutral",
}: {
  label: string;
  value: string;
  note: string;
  tone?: "neutral" | "critical";
}) {
  return (
    <div className="rounded-md border border-border p-2">
      <p className="text-xs text-text-secondary">{label}</p>
      <p className={`text-lg font-semibold ${tone === "critical" ? "text-critical" : ""}`}>{value}</p>
      <p className="text-[11px] text-text-secondary">{note}</p>
    </div>
  );
}

function TimelineTooltip({ active, payload }: { active?: boolean; payload?: { payload: TimelineRow }[] }) {
  if (!active || !payload?.length) return null;
  const row = payload[0].payload;

  const movement =
    row.delayDays === 0
      ? "On programme"
      : row.delayDays > 0
        ? `${row.delayDays} days late`
        : `${Math.abs(row.delayDays)} days ahead`;

  return (
    <div className="rounded-md border border-border bg-card p-2 text-xs shadow-card">
      <p className="font-medium">{row.name}</p>
      <p className="text-text-secondary">Baseline: {formatIso(row.baselineDate)}</p>
      <p className="text-text-secondary">
        {row.currentDateSource === "actual" ? "Actual" : "Forecast"}: {formatIso(row.currentDate)}
      </p>
      <p className={row.delayDays > 0 ? "text-critical" : "text-text-secondary"}>{movement}</p>
    </div>
  );
}

function formatIso(date: string) {
  return new Date(date).toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
}
