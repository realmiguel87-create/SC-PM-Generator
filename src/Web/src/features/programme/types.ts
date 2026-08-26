export type MilestoneStatus = "NotStarted" | "InProgress" | "Complete" | "Delayed";

export interface Milestone {
  id: string;
  name: string;
  description: string | null;
  status: MilestoneStatus;
  baselineDate: string;
  forecastDate: string;
  actualDate: string | null;
  isKeyMilestone: boolean;
  delayDays: number;
}

/** One sanctioned programme. Mirrors ProgrammeBaselineDto. */
export interface ProgrammeBaseline {
  id: string;
  revision: number;
  name: string;
  reason: string;
  approvedBy: string | null;
  approvedDate: string | null;
  isCurrent: boolean;
  createdDate: string;
  milestoneCount: number;
}

/** One milestone measured against a chosen baseline. Mirrors MilestoneAgainstBaselineDto. */
export interface MilestoneAgainstBaseline {
  milestoneId: string;
  name: string;
  /** The name as it stood when the baseline was captured; may differ from `name`. */
  baselineName: string;
  /** Null when the milestone was added after this baseline was sanctioned. */
  baselineDate: string | null;
  currentDate: string;
  currentDateIsActual: boolean;
  slipDays: number;
  isKeyMilestone: boolean;
  addedSinceBaseline: boolean;
}

/** Mirrors ProgrammeAgainstBaselineDto. */
export interface ProgrammeAgainstBaseline {
  baseline: ProgrammeBaseline;
  milestones: MilestoneAgainstBaseline[];
  worstSlipDays: number;
  worstSlipMilestone: string | null;
  removedSinceBaseline: string[];
}
