import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export interface Snapshot {
  id: string;
  type: "Daily" | "Weekly" | "Monthly" | "Gateway" | "Committee" | "Audit" | "Manual";
  label: string;
  capturedAt: string;
  ribaStageAtCapture: number;
  approvedBudgetAtCapture: number;
  forecastCostAtCapture: number;

  // Register aggregates. Snapshots captured before these existed read 0 rather than being
  // back-filled — the API cannot know what a register looked like on a date it never recorded.
  openRiskCount: number;
  highRiskCount: number;
  totalOpenRiskScore: number;

  openIssueCount: number;
  severeOpenIssueCount: number;

  milestoneCount: number;
  milestonesCompleteCount: number;
  milestonesDelayedCount: number;
  worstMilestoneDelayDays: number;

  openEarlyWarningCount: number;
  openCompensationEventCount: number;
  compensationEventValue: number;

  openVariationCount: number;
  variationValue: number;
  extensionOfTimeDaysAwarded: number;
}

const key = (projectId: string) => ["projects", "detail", projectId, "snapshots"] as const;

export function useSnapshots(projectId: string | undefined) {
  return useQuery({
    queryKey: key(projectId ?? ""),
    queryFn: () => apiClient.get<Snapshot[]>(`/projects/${projectId}/snapshots`),
    enabled: !!projectId,
  });
}

export function useCreateManualSnapshot(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { label: string }) => apiClient.post<string>(`/projects/${projectId}/snapshots`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}
