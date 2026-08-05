import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { CostPlanLine, CostSummary } from "./types";

const key = (projectId: string) => ["projects", "detail", projectId, "cost"] as const;

export function useCostSummary(projectId: string | undefined) {
  return useQuery({
    queryKey: key(projectId ?? ""),
    queryFn: () => apiClient.get<CostSummary>(`/projects/${projectId}/cost`),
    enabled: !!projectId,
  });
}

export function useCreateCostPlan(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { name: string; isBaseline: boolean; lines: CostPlanLine[] }) =>
      apiClient.post<string>(`/projects/${projectId}/cost/plans`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}

export function useRecordForecast(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { forecastDate: string; forecastCost: number; commentaryNotes?: string }) =>
      apiClient.post<string>(`/projects/${projectId}/cost/forecasts`, vars),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: key(projectId) });
      queryClient.invalidateQueries({ queryKey: ["projects", "detail", projectId] });
    },
  });
}
