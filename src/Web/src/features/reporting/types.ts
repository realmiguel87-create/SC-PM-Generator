export type CommitteeReportType =
  | "StatusReport"
  | "CommitteeReport"
  | "CabinetReport"
  | "BoardReport"
  | "CapitalProgrammeReport"
  | "DecisionPaper";
export type CommitteeReportStatus = "Draft" | "Approved" | "Submitted";
export type ReportExportFormat = "Pdf" | "Xlsx" | "Csv" | "Json" | "Docx" | "Pptx";

export interface CommitteeReportListItem {
  id: string;
  projectId: string;
  projectName: string;
  projectRef: string;
  reportType: CommitteeReportType;
  title: string;
  meetingDate: string | null;
  status: CommitteeReportStatus;
}

/** One section of a report: a stable key, the heading to show, and what it says. */
export interface ReportSection {
  key: string;
  heading: string;
  content: string | null;
}

export interface CommitteeReport extends CommitteeReportListItem {
  projectId: string;
  createdDate: string;

  /** The date the position is reported as at, as distinct from a committee meeting date. */
  reportDate: string | null;

  /** Header-block facts, read from the project rather than typed into the report. */
  sponsorName: string | null;
  projectManagerName: string | null;
  approvedBudget: number;

  /**
   * The report's narrative, in the order its type defines, headings included.
   *
   * Sent by the server rather than hardcoded here. A status report and a committee paper share no
   * sections at all, and a list in the client would have to be kept in step with the server's by
   * hand — which is exactly the kind of duplication that goes stale and then renders a report with
   * the wrong headings on it.
   */
  sections: ReportSection[];
}

export interface SnapshotComparison {
  fromSnapshotId: string;
  fromLabel: string;
  fromCapturedAt: string;
  toSnapshotId: string;
  toLabel: string;
  toCapturedAt: string;
  fromRibaStage: number;
  toRibaStage: number;
  fromApprovedBudget: number;
  toApprovedBudget: number;
  budgetDelta: number;
  fromForecastCost: number;
  toForecastCost: number;
  forecastDelta: number;

  // Register aggregates (see the API's SnapshotComparisonDto). Every delta is To minus From
  // without exception, including where "up" is bad — so a positive number always means the
  // figure increased, never "improved". Presentation decides how to colour that.
  fromOpenRiskCount: number;
  toOpenRiskCount: number;
  openRiskCountDelta: number;
  fromHighRiskCount: number;
  toHighRiskCount: number;
  highRiskCountDelta: number;
  fromTotalOpenRiskScore: number;
  toTotalOpenRiskScore: number;
  totalOpenRiskScoreDelta: number;

  fromOpenIssueCount: number;
  toOpenIssueCount: number;
  openIssueCountDelta: number;
  fromSevereOpenIssueCount: number;
  toSevereOpenIssueCount: number;
  severeOpenIssueCountDelta: number;

  fromMilestonesDelayedCount: number;
  toMilestonesDelayedCount: number;
  milestonesDelayedCountDelta: number;
  fromWorstMilestoneDelayDays: number;
  toWorstMilestoneDelayDays: number;
  worstMilestoneDelayDaysDelta: number;

  fromOpenCompensationEventCount: number;
  toOpenCompensationEventCount: number;
  openCompensationEventCountDelta: number;
  fromCompensationEventValue: number;
  toCompensationEventValue: number;
  compensationEventValueDelta: number;

  fromOpenVariationCount: number;
  toOpenVariationCount: number;
  openVariationCountDelta: number;
  fromVariationValue: number;
  toVariationValue: number;
  variationValueDelta: number;
}

export type ItemChangeType = "Added" | "Removed" | "Modified";

export interface RiskChange {
  riskId: string;
  title: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
  fromProbability: number | null;
  toProbability: number | null;
  fromImpact: number | null;
  toImpact: number | null;
  fromScore: number | null;
  toScore: number | null;
  /** Null when the risk did not exist at both points — "appeared at 15" is not "rose by 15". */
  scoreDelta: number | null;
}

export interface MilestoneChange {
  milestoneId: string;
  name: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
  fromForecastDate: string | null;
  toForecastDate: string | null;
  fromActualDate: string | null;
  toActualDate: string | null;
  fromDelayDays: number | null;
  toDelayDays: number | null;
  delayDaysDelta: number | null;
}

export interface EarlyWarningChange {
  earlyWarningId: string;
  title: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
}

export interface CompensationEventChange {
  compensationEventId: string;
  reference: string;
  title: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
  fromEstimatedValue: number | null;
  toEstimatedValue: number | null;
  estimatedValueDelta: number | null;
}

export interface VariationChange {
  variationId: string;
  reference: string;
  description: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
  fromValueImpact: number | null;
  toValueImpact: number | null;
  valueImpactDelta: number | null;
}

export interface ExtensionOfTimeChange {
  extensionOfTimeId: string;
  reference: string;
  reason: string;
  changeType: ItemChangeType;
  fromStatus: string | null;
  toStatus: string | null;
  fromDaysClaimed: number | null;
  toDaysClaimed: number | null;
  /** Null while a claim is undetermined — different from an award of zero days. */
  fromDaysAwarded: number | null;
  toDaysAwarded: number | null;
  daysAwardedDelta: number | null;
}

export type IntervalActivityType = "RaisedAndRemoved" | "ChangedAndReverted";

export interface IntervalActivityItem {
  register: string;
  itemId: string;
  name: string;
  activityType: IntervalActivityType;
  versionCount: number;
}

/**
 * Activity between two snapshots that comparing their endpoints cannot reveal — items raised and
 * removed inside the window, or changed and changed back. A pointer rather than a second diff.
 */
export interface SnapshotIntervalActivity {
  fromSnapshotId: string;
  fromLabel: string;
  fromCapturedAt: string;
  toSnapshotId: string;
  toLabel: string;
  toCapturedAt: string;
  items: IntervalActivityItem[];
  hasActivity: boolean;
}

/** Which items changed between two snapshots, as opposed to how many. */
export interface SnapshotItemComparison {
  fromSnapshotId: string;
  fromLabel: string;
  fromCapturedAt: string;
  toSnapshotId: string;
  toLabel: string;
  toCapturedAt: string;
  riskChanges: RiskChange[];
  milestoneChanges: MilestoneChange[];
  earlyWarningChanges: EarlyWarningChange[];
  compensationEventChanges: CompensationEventChange[];
  variationChanges: VariationChange[];
  extensionOfTimeChanges: ExtensionOfTimeChange[];
  hasChanges: boolean;
}

