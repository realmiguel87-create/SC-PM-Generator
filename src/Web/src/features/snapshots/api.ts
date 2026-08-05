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
