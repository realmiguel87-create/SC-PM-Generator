import type {
  Milestone,
  MilestoneAgainstBaseline,
  MilestoneStatus,
  ProgrammeAgainstBaseline,
  ProgrammeBaseline,
} from "./types";

/**
 * Turning a baseline comparison back into something the timeline can draw.
 *
 * The chart already knows how to render milestones: baseline date, current date, slip between
 * them. A comparison against a superseded baseline is the same shape with different numbers, so
 * rather than teach the chart a second data source — and get two drawing paths that can disagree
 * about what a bar means — the comparison is adapted into the shape the chart already takes.
 *
 * This is the part that can be wrong, so it lives here as a pure function with tests rather than
 * inside a component nobody can verify by looking at it.
 */

/**
 * Milestones added since the baseline are dropped rather than drawn.
 *
 * They have no baseline date, so there is nothing to measure them against. Substituting their
 * current date would give them a zero-length bar identical to a milestone that is bang on
 * programme — asserting "this is on time" about something that was never in the programme being
 * examined. They are surfaced in the comparison note instead, where they can be described rather
 * than scored.
 */
export function comparisonToMilestones(rows: MilestoneAgainstBaseline[]): Milestone[] {
  return rows
    .filter((row): row is MilestoneAgainstBaseline & { baselineDate: string } =>
      !row.addedSinceBaseline && row.baselineDate !== null,
    )
    .map((row) => ({
      id: row.milestoneId,
      // The name as it was when sanctioned. A chart of the March programme labelled with today's
      // names is a chart of neither.
      name: row.baselineName,
      description: null,
      // Derived rather than carried: the comparison endpoint reports dates and slip, not lifecycle
      // status. Only `Complete` changes how the timeline reads a row — it is what makes the
      // current date an actual rather than a forecast — and that is exactly what
      // currentDateIsActual tells us.
      status: (row.currentDateIsActual ? "Complete" : "InProgress") satisfies MilestoneStatus,
      baselineDate: row.baselineDate,
      forecastDate: row.currentDate,
      actualDate: row.currentDateIsActual ? row.currentDate : null,
      isKeyMilestone: row.isKeyMilestone,
      // Slip against the chosen baseline, not against the milestone's live baseline date. These
      // are the same number only when the chosen baseline is the current one — which is the whole
      // reason this screen exists.
      delayDays: row.slipDays,
    }));
}

/**
 * What changed about the *shape* of the programme since a baseline, as opposed to its dates.
 *
 * Kept separate from the chart because it cannot be drawn as slip. A milestone added or removed
 * has not moved by some number of days; it has appeared or disappeared, and the only honest way to
 * report that is to name it.
 */
export interface ScopeChange {
  added: string[];
  removed: string[];
  hasChanges: boolean;
}

export function summariseScopeChange(comparison: ProgrammeAgainstBaseline): ScopeChange {
  const added = comparison.milestones
    .filter((m) => m.addedSinceBaseline)
    .map((m) => m.name);

  return {
    added,
    removed: comparison.removedSinceBaseline,
    hasChanges: added.length > 0 || comparison.removedSinceBaseline.length > 0,
  };
}

/**
 * How a baseline is labelled in the selector.
 *
 * Revision leads because it is the stable identifier — names are free text and two baselines can
 * easily both be called "Revised programme". The current one is marked, because "which one are we
 * measured against right now?" is the question a reader brings to the list.
 */
export function describeBaseline(baseline: ProgrammeBaseline): string {
  const suffix = baseline.isCurrent ? " — current" : "";
  return `Rev ${baseline.revision}: ${baseline.name}${suffix}`;
}

/**
 * What a rebaseline would actually do, worked out before it is done.
 *
 * Rebaselining is not an edit — it moves the measure the project is judged against, and its most
 * visible effect is that slip everyone has been watching drops to zero. That is correct, and it is
 * also exactly the kind of change that should not happen to someone who did not realise it would.
 * So the effect is shown first, in the terms a reader cares about: how many dates move, and what
 * the slip figure stops saying.
 */
export interface RebaselineEffect {
  /** Milestones whose sanctioned date would change. */
  moving: number;
  total: number;
  /** The largest slip that would be re-sanctioned to zero. */
  worstSlipCleared: number;
  worstSlipName: string | null;
}

export function describeRebaselineEffect(milestones: Milestone[]): RebaselineEffect {
  // A milestone moves if the date it would be re-sanctioned to differs from the one it holds.
  // Same precedence as everywhere else: an actual supersedes a forecast it has overtaken.
  const moving = milestones.filter(
    (m) => (m.actualDate ?? m.forecastDate) !== m.baselineDate,
  );

  const worst = moving.reduce<Milestone | null>(
    (found, m) => (m.delayDays > 0 && (found === null || m.delayDays > found.delayDays) ? m : found),
    null,
  );

  return {
    moving: moving.length,
    total: milestones.length,
    worstSlipCleared: worst?.delayDays ?? 0,
    worstSlipName: worst?.name ?? null,
  };
}

/** Mirrors RebaselineProgrammeCommandValidator, the same way NewProjectForm mirrors its own. */
export function validateRebaseline(name: string, reason: string): string | null {
  if (name.trim().length === 0) return "Give the baseline a name.";
  if (name.trim().length > 200) return "The name is limited to 200 characters.";
  // Ten characters does not make an explanation good, but it stops the reflexive one-word entry
  // in the field that is the whole record of why the sanctioned programme changed.
  if (reason.trim().length < 10) {
    return "Give a reason for the rebaseline — this is the record of why the sanctioned programme changed.";
  }
  if (reason.trim().length > 2000) return "The reason is limited to 2000 characters.";
  return null;
}

/**
 * Orders baselines for display: newest revision first.
 *
 * Sorted here rather than trusted from the server. The endpoint does order them, but a list whose
 * meaning depends on arrival order is a list that silently reorders the day someone adds paging or
 * a filter.
 */
export function orderBaselines(baselines: ProgrammeBaseline[]): ProgrammeBaseline[] {
  return [...baselines].sort((a, b) => b.revision - a.revision);
}
