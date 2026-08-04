export type StakeholderInfluence = "Low" | "Medium" | "High";
export type StakeholderInterest = "Low" | "Medium" | "High";

export interface StakeholderEngagement {
  id: string;
  engagementDate: string;
  method: string;
  summary: string;
  outcome: string | null;
}

export interface Stakeholder {
  id: string;
  name: string;
  organisation: string | null;
  roleTitle: string | null;
  contactEmail: string | null;
  influence: StakeholderInfluence;
  interest: StakeholderInterest;
  engagements: StakeholderEngagement[];
}
