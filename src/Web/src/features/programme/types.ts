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
