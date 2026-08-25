import { describe, expect, it } from "vitest";
import { buildTimeline, daysBetween, offsetToDate, summariseDelay } from "./timeline";
import type { Milestone } from "./types";

/**
 * The timeline maths, tested without rendering anything.
 *
 * Worth being blunt about why these exist: a chart is the one part of this app that cannot be
 * checked by looking at the code, and the person writing it here cannot look at the result
 * either. Extracting the arithmetic makes the part that can be wrong — offsets, direction, which
 * date wins — verifiable, and leaves the chart component with nothing but rendering.
 */

function milestone(overrides: Partial<Milestone> = {}): Milestone {
  return {
    id: crypto.randomUUID(),
    name: "Practical completion",
    description: null,
    status: "InProgress",
    baselineDate: "2026-06-01",
    forecastDate: "2026-06-01",
    actualDate: null,
    isKeyMilestone: false,
    delayDays: 0,
    ...overrides,
  };
}

describe("daysBetween", () => {
  it("counts forward days as positive and backward as negative", () => {
    expect(daysBetween("2026-06-01", "2026-06-11")).toBe(10);
    expect(daysBetween("2026-06-11", "2026-06-01")).toBe(-10);
    expect(daysBetween("2026-06-01", "2026-06-01")).toBe(0);
  });

  it("is not thrown off by a daylight-saving boundary", () => {
    // The UK clocks go forward on 29 March 2026, making one day 23 hours long. Dividing
    // milliseconds without rounding would return 30.958… and truncate to 30.
    expect(daysBetween("2026-03-01", "2026-03-31")).toBe(30);
  });
});

describe("buildTimeline", () => {
  it("returns an empty timeline rather than throwing when there are no milestones", () => {
    const timeline = buildTimeline([]);

    expect(timeline.rows).toEqual([]);
    expect(timeline.todayOffset).toBeNull();
  });

  it("orders rows by baseline date, not entry order", () => {
    const timeline = buildTimeline([
      milestone({ name: "Second", baselineDate: "2026-09-01", forecastDate: "2026-09-01" }),
      milestone({ name: "First", baselineDate: "2026-06-01", forecastDate: "2026-06-01" }),
    ]);

    expect(timeline.rows.map((r) => r.name)).toEqual(["First", "Second"]);
  });

  it("gives a slipped milestone a bar starting at its baseline", () => {
    const timeline = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-07-01" }),
    ]);

    const row = timeline.rows[0];
    expect(row.delayDays).toBe(30);
    expect(row.barOffset).toBe(row.baselineOffset);
    expect(row.barSpan).toBe(30);
  });

  it("gives a recovered milestone a bar starting at its new, earlier date", () => {
    const timeline = buildTimeline([
      milestone({ baselineDate: "2026-07-01", forecastDate: "2026-06-01" }),
    ]);

    const row = timeline.rows[0];
    expect(row.delayDays).toBe(-30);
    // The bar always runs left to right; only its start and colour differ.
    expect(row.barOffset).toBe(row.currentOffset);
    expect(row.barSpan).toBe(30);
  });

  it("gives a milestone on programme no bar at all", () => {
    const timeline = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-06-01" }),
    ]);

    // Nothing has moved, so nothing is drawn. The baseline marker still places it on the chart —
    // a minimum bar width would look like a small slip where there is none.
    expect(timeline.rows[0].barSpan).toBe(0);
  });

  it("uses the actual date once a milestone completes, not its forecast", () => {
    const timeline = buildTimeline([
      milestone({
        baselineDate: "2026-06-01",
        forecastDate: "2026-09-01",
        actualDate: "2026-06-15",
        status: "Complete",
      }),
    ]);

    const row = timeline.rows[0];
    // Completed 14 days late, so the 92-day forecast is history. Reporting the forecast would
    // show a project three months late that in fact finished a fortnight late.
    expect(row.delayDays).toBe(14);
    expect(row.currentDateSource).toBe("actual");
  });

  it("takes its origin from the earliest date on the chart, baseline or not", () => {
    const timeline = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-06-01", actualDate: "2026-05-01" }),
    ]);

    // The actual beat every baseline, so the origin is the actual — otherwise the marker would
    // sit at a negative offset off the left of the plot.
    expect(timeline.originDate).toBe("2026-05-01");
    expect(timeline.rows[0].currentOffset).toBe(0);
  });

  it("places today on the chart when it falls inside the range", () => {
    const timeline = buildTimeline(
      [milestone({ baselineDate: "2026-06-01", forecastDate: "2026-08-01" })],
      new Date("2026-07-01T00:00:00Z"),
    );

    expect(timeline.todayOffset).toBe(30);
  });

  it("omits today when the whole programme is in the past", () => {
    const timeline = buildTimeline(
      [milestone({ baselineDate: "2020-06-01", forecastDate: "2020-06-02" })],
      new Date("2026-07-01T00:00:00Z"),
    );

    // Clamping it to the edge would draw a "today" line against a programme that finished years
    // ago, asserting something false rather than showing nothing.
    expect(timeline.todayOffset).toBeNull();
  });

  it("round-trips an offset back to its date", () => {
    const timeline = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-07-01" }),
    ]);

    const date = offsetToDate(timeline.originDate, timeline.rows[0].currentOffset);
    expect(date.toISOString().slice(0, 10)).toBe("2026-07-01");
  });
});

describe("summariseDelay", () => {
  it("reports the worst single slip rather than a total", () => {
    const { rows } = buildTimeline([
      milestone({ name: "A", baselineDate: "2026-06-01", forecastDate: "2026-06-02" }),
      milestone({ name: "B", baselineDate: "2026-06-01", forecastDate: "2026-06-02" }),
      milestone({ name: "C", baselineDate: "2026-06-01", forecastDate: "2026-12-01" }),
    ]);

    const summary = summariseDelay(rows);

    // Ten milestones one day late is a different programme from one six months late. A total or
    // a mean makes those look alike, which is precisely the thing a reader needs to tell apart.
    expect(summary.worstSlipDays).toBe(183);
    expect(summary.worstSlipName).toBe("C");
    expect(summary.late).toBe(3);
  });

  it("counts on-programme and recovered milestones separately from late ones", () => {
    const { rows } = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-06-01" }),
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-05-01" }),
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-07-01" }),
    ]);

    const summary = summariseDelay(rows);

    expect(summary.onProgramme).toBe(1);
    expect(summary.recovered).toBe(1);
    expect(summary.late).toBe(1);
    expect(summary.total).toBe(3);
  });

  it("totals slip on key milestones only", () => {
    const { rows } = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-06-11", isKeyMilestone: true }),
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-07-01", isKeyMilestone: false }),
    ]);

    // Key milestones are the ones a committee is told about, so their slip is worth its own
    // figure rather than being buried among internal ones.
    expect(summariseDelay(rows).keyMilestoneSlipDays).toBe(10);
  });

  it("reports no worst slip when nothing has slipped", () => {
    const { rows } = buildTimeline([
      milestone({ baselineDate: "2026-06-01", forecastDate: "2026-06-01" }),
    ]);

    const summary = summariseDelay(rows);
    expect(summary.worstSlipDays).toBe(0);
    expect(summary.worstSlipName).toBeNull();
  });
});
