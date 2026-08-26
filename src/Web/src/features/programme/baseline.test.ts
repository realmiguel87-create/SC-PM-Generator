import { describe, expect, it } from "vitest";
import {
  comparisonToMilestones,
  describeBaseline,
  describeRebaselineEffect,
  orderBaselines,
  summariseScopeChange,
  validateRebaseline,
} from "./baseline";
import { buildTimeline, summariseDelay } from "./timeline";
import type {
  Milestone,
  MilestoneAgainstBaseline,
  ProgrammeAgainstBaseline,
  ProgrammeBaseline,
} from "./types";

/**
 * The adapter between a baseline comparison and the chart.
 *
 * Worth stating what these are protecting. The whole point of measuring against a superseded
 * baseline is that it gives a *different* answer from the live one — bigger slip, against dates
 * that were sanctioned rather than dates that have since been moved. An adapter that quietly
 * dropped back to the live baseline would still render a plausible chart with plausible numbers,
 * and nobody looking at it would be able to tell.
 */

function row(overrides: Partial<MilestoneAgainstBaseline> = {}): MilestoneAgainstBaseline {
  return {
    milestoneId: crypto.randomUUID(),
    name: "Start on site",
    baselineName: "Start on site",
    baselineDate: "2026-08-01",
    currentDate: "2026-11-01",
    currentDateIsActual: false,
    slipDays: 92,
    isKeyMilestone: false,
    addedSinceBaseline: false,
    ...overrides,
  };
}

function baseline(overrides: Partial<ProgrammeBaseline> = {}): ProgrammeBaseline {
  return {
    id: crypto.randomUUID(),
    revision: 1,
    name: "Original baseline",
    reason: "Captured when the programme was first rebaselined.",
    approvedBy: null,
    approvedDate: null,
    isCurrent: false,
    createdDate: "2026-08-26T00:00:00Z",
    milestoneCount: 1,
    ...overrides,
  };
}

describe("comparisonToMilestones", () => {
  it("carries the slip measured against the chosen baseline", () => {
    const [milestone] = comparisonToMilestones([row({ slipDays: 92 })]);

    // Not the milestone's live delayDays, which after a rebaseline reads zero. If this ever
    // silently fell back to the live figure, the chart would show a project bang on programme
    // against a baseline it is three months adrift of.
    expect(milestone.delayDays).toBe(92);
    expect(milestone.baselineDate).toBe("2026-08-01");
    expect(milestone.forecastDate).toBe("2026-11-01");
  });

  it("uses the name the milestone had when the baseline was sanctioned", () => {
    const [milestone] = comparisonToMilestones([
      row({ name: "Construction commencement", baselineName: "Start on site" }),
    ]);

    // A chart of the March programme labelled with today's names is a chart of neither.
    expect(milestone.name).toBe("Start on site");
  });

  it("drops milestones added since the baseline", () => {
    const rows = [
      row({ name: "Start on site" }),
      row({ name: "Handover", baselineDate: null, slipDays: 0, addedSinceBaseline: true }),
    ];

    // Drawing it with its current date as its baseline would give it a zero-length bar identical
    // to a milestone bang on programme — asserting "on time" about something that was never in
    // the programme being examined.
    expect(comparisonToMilestones(rows).map((m) => m.name)).toEqual(["Start on site"]);
  });

  it("drops a row with no baseline date even when the flag disagrees", () => {
    // Belt and braces against the two fields contradicting each other. A null baseline date with
    // addedSinceBaseline false should not happen, but if it did, the date is the thing the chart
    // actually needs — and a null there produces NaN offsets and an empty plot rather than an
    // error anyone would notice.
    const rows = [row({ baselineDate: null, addedSinceBaseline: false })];

    expect(comparisonToMilestones(rows)).toHaveLength(0);
  });

  it("marks a completed milestone so the chart reads its date as an actual", () => {
    const [milestone] = comparisonToMilestones([
      row({ currentDate: "2026-06-15", currentDateIsActual: true, slipDays: 14 }),
    ]);

    expect(milestone.actualDate).toBe("2026-06-15");
    expect(milestone.status).toBe("Complete");
  });

  it("leaves a forecast date as a forecast", () => {
    const [milestone] = comparisonToMilestones([row({ currentDateIsActual: false })]);

    expect(milestone.actualDate).toBeNull();
    expect(milestone.status).toBe("InProgress");
  });

  it("feeds the timeline a chart that measures against the superseded baseline", () => {
    // The end-to-end shape of the thing: comparison rows in, a timeline whose worst slip is the
    // slip against the sanctioned programme out. This is the assertion that would fail if any
    // link in the chain reverted to live dates.
    const timeline = buildTimeline(
      comparisonToMilestones([
        row({ name: "Start on site", slipDays: 92 }),
        row({
          name: "Practical completion",
          baselineDate: "2027-06-01",
          currentDate: "2027-06-01",
          slipDays: 0,
        }),
      ]),
    );

    const summary = summariseDelay(timeline.rows);
    expect(summary.worstSlipDays).toBe(92);
    expect(summary.worstSlipName).toBe("Start on site");
    expect(summary.total).toBe(2);
  });
});

describe("summariseScopeChange", () => {
  function comparison(overrides: Partial<ProgrammeAgainstBaseline> = {}): ProgrammeAgainstBaseline {
    return {
      baseline: baseline(),
      milestones: [],
      worstSlipDays: 0,
      worstSlipMilestone: null,
      removedSinceBaseline: [],
      ...overrides,
    };
  }

  it("names what has been added and removed rather than counting it", () => {
    const change = summariseScopeChange(comparison({
      milestones: [
        row({ name: "Start on site" }),
        row({ name: "Handover", baselineDate: null, addedSinceBaseline: true }),
      ],
      removedSinceBaseline: ["Enabling works"],
    }));

    // A milestone appearing in or vanishing from an approved programme is not slip and cannot be
    // drawn as a bar. "1 added, 1 removed" tells a reader nothing they can act on.
    expect(change.added).toEqual(["Handover"]);
    expect(change.removed).toEqual(["Enabling works"]);
    expect(change.hasChanges).toBe(true);
  });

  it("reports no change when the programme has the same milestones", () => {
    const change = summariseScopeChange(comparison({ milestones: [row()] }));

    expect(change.hasChanges).toBe(false);
  });
});

describe("describeRebaselineEffect", () => {
  function milestone(overrides: Partial<Milestone> = {}): Milestone {
    return {
      id: crypto.randomUUID(),
      name: "Start on site",
      description: null,
      status: "InProgress",
      baselineDate: "2026-08-01",
      forecastDate: "2026-11-01",
      actualDate: null,
      isKeyMilestone: false,
      delayDays: 92,
      ...overrides,
    };
  }

  it("names the slip that is about to be re-sanctioned to zero", () => {
    const effect = describeRebaselineEffect([
      milestone({ name: "Start on site", delayDays: 92 }),
      milestone({ name: "Enabling works", forecastDate: "2026-08-11", delayDays: 10 }),
    ]);

    // "Worst slip: 92d" becoming "Nothing has slipped" is the whole effect of rebaselining, and
    // it is what a reader would otherwise discover only afterwards.
    expect(effect.worstSlipCleared).toBe(92);
    expect(effect.worstSlipName).toBe("Start on site");
    expect(effect.moving).toBe(2);
    expect(effect.total).toBe(2);
  });

  it("does not count a milestone already sitting on its baseline as moving", () => {
    const effect = describeRebaselineEffect([
      milestone({ baselineDate: "2026-11-01", forecastDate: "2026-11-01", delayDays: 0 }),
      milestone({ name: "Late one", delayDays: 92 }),
    ]);

    expect(effect.moving).toBe(1);
    expect(effect.total).toBe(2);
  });

  it("measures a completed milestone against its actual date", () => {
    const effect = describeRebaselineEffect([
      milestone({
        baselineDate: "2026-06-15",
        forecastDate: "2026-09-01",
        actualDate: "2026-06-15",
        status: "Complete",
        delayDays: 0,
      }),
    ]);

    // Its forecast still says September, but it completed on its baseline date — so rebaselining
    // would change nothing about it. Reading the forecast would report a move that is not real.
    expect(effect.moving).toBe(0);
  });

  it("reports no slip cleared when a programme is only running early", () => {
    const effect = describeRebaselineEffect([
      milestone({ forecastDate: "2026-07-01", delayDays: -31 }),
    ]);

    // Recovery is a move, but it is not a slip being cleared — announcing one would describe the
    // opposite of what is happening.
    expect(effect.moving).toBe(1);
    expect(effect.worstSlipCleared).toBe(0);
    expect(effect.worstSlipName).toBeNull();
  });
});

describe("validateRebaseline", () => {
  it("accepts a name with a real reason", () => {
    expect(validateRebaseline("Post-tender", "Tender returns came in late.")).toBeNull();
  });

  it("rejects a one-word reason", () => {
    // The reason is the entire record of why the sanctioned programme changed. Ten characters
    // does not make an explanation good, but it stops the reflexive "update".
    expect(validateRebaseline("Post-tender", "update")).toMatch(/reason/i);
  });

  it("rejects whitespace padded out to look like a reason", () => {
    expect(validateRebaseline("Post-tender", "          ")).toMatch(/reason/i);
  });

  it("rejects a missing name", () => {
    expect(validateRebaseline("   ", "Tender returns came in late.")).toMatch(/name/i);
  });
});

describe("describeBaseline", () => {
  it("leads with the revision and marks the current one", () => {
    // Revision leads because it is the stable identifier: names are free text and two baselines
    // can easily both be called "Revised programme".
    expect(describeBaseline(baseline({ revision: 2, name: "Post-tender", isCurrent: true })))
      .toBe("Rev 2: Post-tender — current");

    expect(describeBaseline(baseline({ revision: 1, name: "Original baseline" })))
      .toBe("Rev 1: Original baseline");
  });
});

describe("orderBaselines", () => {
  it("puts the newest revision first without mutating the input", () => {
    const input = [baseline({ revision: 1 }), baseline({ revision: 3 }), baseline({ revision: 2 })];

    expect(orderBaselines(input).map((b) => b.revision)).toEqual([3, 2, 1]);
    // Sorting in place would reorder whatever react-query is caching, which is shared state.
    expect(input.map((b) => b.revision)).toEqual([1, 3, 2]);
  });
});
