export interface EarlyWarning {
  id: string;
  title: string;
  description: string | null;
  raisedDate: string;
  mitigationAction: string | null;
  status: "Open" | "Closed";
}

export interface CompensationEvent {
  id: string;
  reference: string;
  title: string;
  clauseReference: string | null;
  estimatedValue: number;
  status: "Notified" | "Quoted" | "Accepted" | "Rejected" | "Implemented";
  notifiedDate: string;
}

export interface ContractDataEntry {
  id: string;
  part: "PartOne" | "PartTwo";
  clauseReference: string;
  description: string;
  value: string;
}

export interface RiskAllocationItem {
  id: string;
  description: string;
  allocatedTo: "Client" | "Contractor" | "Shared";
  mitigationOwner: string | null;
}

export interface AcceptedProgrammeEntry {
  id: string;
  revisionNumber: number;
  acceptedDate: string;
  notes: string | null;
}

export interface PaymentAssessment {
  id: string;
  assessmentNumber: number;
  assessmentDate: string;
  amountDue: number;
  status: "Assessed" | "Certified" | "Paid";
}

export interface ChangeRegisterItem {
  id: string;
  title: string;
  description: string | null;
  valueImpact: number;
  timeImpactDays: number;
  status: "Proposed" | "Approved" | "Rejected" | "Implemented";
}
