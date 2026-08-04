import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { IssueItem, IssueSeverity, IssueStatus, OpportunityItem, OpportunityStatus, RiskItem, RiskStatus } from "./types";

const risksKey = (projectId: string) => ["projects", "detail", projectId, "risks"] as const;
const issuesKey = (projectId: string) => ["projects", "detail", projectId, "issues"] as const;
const opportunitiesKey = (projectId: string) => ["projects", "detail", projectId, "opportunities"] as const;

// --- Risks ---

export function useRisks(projectId: string | undefined) {
  return useQuery({
    queryKey: risksKey(projectId ?? ""),
    queryFn: () => apiClient.get<RiskItem[]>(`/projects/${projectId}/risks`),
    enabled: !!projectId,
  });
}

export function useCreateRisk(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { title: string; description?: string; category: string; probability: number; impact: number; mitigationPlan?: string }) =>
      apiClient.post<string>(`/projects/${projectId}/risks`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: risksKey(projectId) }),
  });
}

export function useUpdateRiskStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { riskId: string; status: RiskStatus; mitigationPlan?: string }) =>
      apiClient.put<void>(`/risks/${vars.riskId}/status`, { status: vars.status, mitigationPlan: vars.mitigationPlan ?? null }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: risksKey(projectId) }),
  });
}

// --- Issues ---

export function useIssues(projectId: string | undefined) {
  return useQuery({
    queryKey: issuesKey(projectId ?? ""),
    queryFn: () => apiClient.get<IssueItem[]>(`/projects/${projectId}/issues`),
    enabled: !!projectId,
  });
}

export function useCreateIssue(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { title: string; description?: string; severity: IssueSeverity; raisedDate: string }) =>
      apiClient.post<string>(`/projects/${projectId}/issues`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: issuesKey(projectId) }),
  });
}

export function useUpdateIssueStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { issueId: string; status: IssueStatus; resolutionNotes?: string }) =>
      apiClient.put<void>(`/issues/${vars.issueId}/status`, { status: vars.status, resolutionNotes: vars.resolutionNotes ?? null }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: issuesKey(projectId) }),
  });
}

// --- Opportunities ---

export function useOpportunities(projectId: string | undefined) {
  return useQuery({
    queryKey: opportunitiesKey(projectId ?? ""),
    queryFn: () => apiClient.get<OpportunityItem[]>(`/projects/${projectId}/opportunities`),
    enabled: !!projectId,
  });
}

export function useCreateOpportunity(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { title: string; description?: string; potentialValue: number; probability: number }) =>
      apiClient.post<string>(`/projects/${projectId}/opportunities`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: opportunitiesKey(projectId) }),
  });
}

export function useUpdateOpportunityStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { opportunityId: string; status: OpportunityStatus }) =>
      apiClient.put<void>(`/opportunities/${vars.opportunityId}/status`, { status: vars.status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: opportunitiesKey(projectId) }),
  });
}
