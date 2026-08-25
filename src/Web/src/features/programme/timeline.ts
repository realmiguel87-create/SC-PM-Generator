import type { Milestone } from "./types";

/**
 * Timeline maths, kept separate from the chart so it can be tested without rendering anything.
 *
 * A word on what this chart is, because it is deliberately not a Gantt in the usual sense. A
 * Gantt draws tasks: each has a start and an end, and the bar's length is its duration. This data
 * models *milestones* — single dates, with no duration at all. Drawing task bars would mean
 * inventing durations that nobody entered, which would look more familiar and mean less.
 *
 * So each bar spans baseline → current position, and its length is the slip. A milestone that is
 * on programme has no bar, only its baseline marker, which is the honest picture: nothing has
 * moved. Late runs one way and coloured red, recovered runs the other and green.
 */

/** Days between two ISO dates, positive when `to` is later. */
export function daysBetween(from: string, to: string): number {
  const ms = Date.parse(to) - Date.parse(from);
  return Math.round(ms / 86_400_000);
}

export interface TimelineRow {
  id: string;
  name: string;
  status: Milestone["status"];
  isKeyMilestone: boolean;

  /** Day offsets from the timeline origin, for positioning. */
  baselineOffset: number;
  currentOffset: number;

  /** Transparent spacer placing the visible bar at the right point on the axis. */
  barOffset: number;
  /** Length of the visible bar: the size of the slip, zero when on programme. */
  barSpan: number;

  /** Signed slip: positive is late. Mirrors Milestone.DelayDays on the server. */
  delayDays: number;
  /** Which date the current position came from — actual once complete, forecast otherwise. */
  currentDateSource: "actual" | "forecast";
  baselineDate: string;
  currentDate: string;
}

export interface Timeline {
  rows: TimelineRow[];
  /** Day offsets of the axis bounds, relative to `originDate`. */
  min: number;
  max: number;
  originDate: string;
  /** Offset of today, or null when today falls outside the charted range. */
  todayOffset: number | null;
}

/**
 * Builds the chart rows. Ordered by baseline date so the timeline reads chronologically — the
 * order milestones appear in the register is an entry order, not a programme.
 */
export function buildTimeline(milestones: Milestone[], today = new Date()): Timeline {
  if (milestones.length === 0) {
    return { rows: [], min: 0, max: 0, originDate: new Date().toISOString(), todayOffset: null };
  }

  const ordered = [...milestones].sort(
    (a, b) => Date.parse(a.baselineDate) - Date.parse(b.baselineDate),
  );

  // The origin is the earliest date on the chart, which may be an actual completion that beat
  // every baseline — not necessarily the earliest baseline.
  const originDate = ordered
    .flatMap((m) => [m.baselineDate, m.actualDate ?? m.forecastDate])
    .reduce((earliest, date) => (Date.parse(date) < Date.parse(earliest) ? date : earliest));

  const rows: TimelineRow[] = ordered.map((m) => {
    // Actual wins once it exists: a completed milestone's forecast is no longer meaningful. Same
    // rule as SnapshotMetrics.DelayDays on the server, which is what the register reports.
    const currentDate = m.actualDate ?? m.forecastDate;
    const baselineOffset = daysBetween(originDate, m.baselineDate);
    const currentOffset = daysBetween(originDate, currentDate);
    const delayDays = currentOffset - baselineOffset;

    return {
      id: m.id,
      name: m.name,
      status: m.status,
      isKeyMilestone: m.isKeyMilestone,
      baselineOffset,
      currentOffset,
      // The bar always runs left to right, so a recovery starts at the current (earlier) date.
      barOffset: Math.min(baselineOffset, currentOffset),
      barSpan: Math.abs(delayDays),
      delayDays,
      currentDateSource: m.actualDate ? "actual" : "forecast",
      baselineDate: m.baselineDate,
      currentDate,
    };
  });

  const offsets = rows.flatMap((r) => [r.baselineOffset, r.currentOffset]);
  const min = Math.min(...offsets);
  const max = Math.max(...offsets);

  // A little breathing room, so a marker sitting exactly on the axis bound is not clipped in
  // half by the plot edge.
  const padding = Math.max(1, Math.round((max - min) * 0.05));
  const paddedMin = min - padding;
  const paddedMax = max + padding;

  const todayRaw = daysBetween(originDate, today.toISOString());

  return {
    rows,
    min: paddedMin,
    max: paddedMax,
    originDate,
    // Omitted rather than clamped when today is off the chart: a "today" line pinned to the edge
    // of a programme that finished last year would assert something false.
    todayOffset: todayRaw >= paddedMin && todayRaw <= paddedMax ? todayRaw : null,
  };
}

/** Turns a day offset back into a date, for axis labels and tooltips. */
export function offsetToDate(originDate: string, offset: number): Date {
  return new Date(Date.parse(originDate) + offset * 86_400_000);
}

export interface DelaySummary {
  total: number;
  late: number;
  onProgramme: number;
  recovered: number;
  complete: number;
  /** Largest single slip in days; 0 when nothing has slipped. */
  worstSlipDays: number;
  worstSlipName: string | null;
  /** Slip on key milestones only — the ones a committee is told about. */
  keyMilestoneSlipDays: number;
}

/**
 * The numbers behind the chart. Deliberately reports the worst single slip rather than a total or
 * a mean: ten milestones one day late is a different programme from one milestone six months
 * late, and summing or averaging them makes those two look alike.
 */
export function summariseDelay(rows: TimelineRow[]): DelaySummary {
  const late = rows.filter((r) => r.delayDays > 0);
  const worst = late.reduce<TimelineRow | null>(
    (found, row) => (found === null || row.delayDays > found.delayDays ? row : found),
    null,
  );

  return {
    total: rows.length,
    late: late.length,
    onProgramme: rows.filter((r) => r.delayDays === 0).length,
    recovered: rows.filter((r) => r.delayDays < 0).length,
    complete: rows.filter((r) => r.status === "Complete").length,
    worstSlipDays: worst?.delayDays ?? 0,
    worstSlipName: worst?.name ?? null,
    keyMilestoneSlipDays: late
      .filter((r) => r.isKeyMilestone)
      .reduce((sum, r) => sum + r.delayDays, 0),
  };
}
