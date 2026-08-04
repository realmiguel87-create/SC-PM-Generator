import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";

export type ApprovalDecision = "Approved" | "Rejected" | "ApprovedWithConditions";

export function useCreateGateway(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { stageNumber: number; gatewayType: string; dueDate?: string }) =>
      apiClient.post<string>(`/projects/${projectId}/stages/${vars.stageNumber}/gateway`, {
        gatewayType: vars.gatewayType,
        dueDate: vars.dueDate ?? null,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects", "detail", projectId] }),
  });
}

export function useDecideGateway(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { gatewayId: string; decision: ApprovalDecision; comments?: string }) =>
      apiClient.post<void>(`/gateways/${vars.gatewayId}/decision`, {
        decision: vars.decision,
        comments: vars.comments ?? null,
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["projects", "detail", projectId] }),
  });
}
