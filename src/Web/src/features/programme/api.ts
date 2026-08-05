import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { Milestone, MilestoneStatus } from "./types";

const key = (projectId: string) => ["projects", "detail", projectId, "milestones"] as const;

export function useMilestones(projectId: string | undefined) {
  return useQuery({
    queryKey: key(projectId ?? ""),
    queryFn: () => apiClient.get<Milestone[]>(`/projects/${projectId}/milestones`),
    enabled: !!projectId,
  });
}

export function useCreateMilestone(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: {
      name: string;
      description?: string;
      baselineDate: string;
      forecastDate: string;
      isKeyMilestone: boolean;
    }) => apiClient.post<string>(`/projects/${projectId}/milestones`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}

export function useUpdateMilestoneStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { milestoneId: string; status: MilestoneStatus; actualDate?: string }) =>
      apiClient.put<void>(`/milestones/${vars.milestoneId}/status`, {
        status: vars.status,
        actualDate: vars.actualDate ?? null,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}
