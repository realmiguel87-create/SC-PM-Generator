export type RiskStatus = "Open" | "Mitigated" | "Closed" | "Escalated";
export type IssueSeverity = "Low" | "Medium" | "High" | "Critical";
export type IssueStatus = "Open" | "InProgress" | "Resolved" | "Closed";
export type OpportunityStatus = "Identified" | "BeingPursued" | "Realised" | "NotPursued";
export type EscalationStatus = "Pending" | "Resolved" | "Withdrawn";

export interface RiskItem {
  id: string;
  title: string;
  description: string | null;
  category: string;
  probability: number;
  impact: number;
  score: number;
  status: RiskStatus;
  mitigationPlan: string | null;
}

export interface IssueItem {
  id: string;
  title: string;
  description: string | null;
  severity: IssueSeverity;
  status: IssueStatus;
  raisedDate: string;
  resolvedDate: string | null;
  resolutionNotes: string | null;
}

export interface OpportunityItem {
  id: string;
  title: string;
  description: string | null;
  potentialValue: number;
  probability: number;
  status: OpportunityStatus;
}
