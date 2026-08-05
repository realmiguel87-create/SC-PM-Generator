import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { Stakeholder, StakeholderInfluence, StakeholderInterest } from "./types";

const key = (projectId: string) => ["projects", "detail", projectId, "stakeholders"] as const;

export function useStakeholders(projectId: string | undefined) {
  return useQuery({
    queryKey: key(projectId ?? ""),
    queryFn: () => apiClient.get<Stakeholder[]>(`/projects/${projectId}/stakeholders`),
    enabled: !!projectId,
  });
}

export function useCreateStakeholder(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: {
      name: string;
      organisation?: string;
      roleTitle?: string;
      contactEmail?: string;
      influence: StakeholderInfluence;
      interest: StakeholderInterest;
    }) => apiClient.post<string>(`/projects/${projectId}/stakeholders`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}

export function useCreateEngagement(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { stakeholderId: string; engagementDate: string; method: string; summary: string; outcome?: string }) =>
      apiClient.post<string>(`/stakeholders/${vars.stakeholderId}/engagements`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId) }),
  });
}
