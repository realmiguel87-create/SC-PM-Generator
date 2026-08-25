export type CommitteeReportType = "CommitteeReport" | "CabinetReport" | "BoardReport" | "CapitalProgrammeReport" | "DecisionPaper";
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

export interface CommitteeReport extends CommitteeReportListItem {
  projectId: string;
  createdDate: string;
  executiveSummary: string;
  background: string | null;
  currentPosition: string | null;
  financeCommentary: string | null;
  programmeCommentary: string | null;
  riskCommentary: string | null;
  stakeholderCommentary: string | null;
  sustainabilityCommentary: string | null;
  equalityImpactCommentary: string | null;
  recommendations: string | null;
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

export const REPORT_SECTIONS: { key: keyof CommitteeReport; label: string }[] = [
  { key: "executiveSummary", label: "Executive Summary" },
  { key: "background", label: "Background" },
  { key: "currentPosition", label: "Current Position" },
  { key: "financeCommentary", label: "Finance" },
  { key: "programmeCommentary", label: "Programme" },
  { key: "riskCommentary", label: "Risk" },
  { key: "stakeholderCommentary", label: "Stakeholders" },
  { key: "sustainabilityCommentary", label: "Sustainability" },
  { key: "equalityImpactCommentary", label: "Equality Impact" },
  { key: "recommendations", label: "Recommendations" },
];
