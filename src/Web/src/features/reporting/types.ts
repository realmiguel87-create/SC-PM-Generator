export type CommitteeReportType = "CommitteeReport" | "CabinetReport" | "BoardReport" | "CapitalProgrammeReport" | "DecisionPaper";
export type CommitteeReportStatus = "Draft" | "Approved" | "Submitted";
export type ReportExportFormat = "Pdf" | "Xlsx" | "Csv" | "Json";

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
