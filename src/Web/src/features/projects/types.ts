export interface ProjectListItem {
  id: string;
  projectRef: string;
  name: string;
  status: string;
  currentRibaStage: number;
  currentRibaStageName: string;
  approvedBudget: number;
  forecastCost: number;
  programmeName: string | null;
}

export interface RibaStageInstance {
  id: string;
  stageNumber: number;
  stageName: string;
  status: "NotStarted" | "InProgress" | "Complete" | "Gated";
  plannedStartDate: string | null;
  plannedEndDate: string | null;
  actualStartDate: string | null;
  actualEndDate: string | null;
  /** The latest gateway whatever its status — what a gate report is written against. */
  gatewayId: string | null;
  /** The latest gateway only while it awaits a decision — what the "decide" action uses. */
  pendingGatewayId: string | null;
  gatewayStatus: "Pending" | "Approved" | "Rejected" | "Withdrawn" | null;
}

export interface ProjectDetail extends ProjectListItem {
  description: string | null;
  startDate: string | null;
  targetCompletionDate: string | null;
  ribaStages: RibaStageInstance[];
}

export interface CreateProjectRequest {
  projectRef: string;
  name: string;
  description?: string;
  programmeId?: string;
  approvedBudget: number;
  startDate?: string;
  targetCompletionDate?: string;
  sponsorUserId?: string;
  projectManagerUserId?: string;
}
