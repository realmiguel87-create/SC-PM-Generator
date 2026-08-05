import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { CommitteeReport, CommitteeReportListItem, CommitteeReportType, SnapshotComparison } from "./types";

const listKey = (projectId?: string) => ["committee-reports", projectId ?? "all"] as const;
const detailKey = (reportId: string) => ["committee-reports", "detail", reportId] as const;

export function useCommitteeReports(projectId?: string) {
  return useQuery({
    queryKey: listKey(projectId),
    queryFn: () => apiClient.get<CommitteeReportListItem[]>(`/committee-reports${projectId ? `?projectId=${projectId}` : ""}`),
  });
}

export function useCommitteeReport(reportId: string | undefined) {
  return useQuery({
    queryKey: detailKey(reportId ?? ""),
    queryFn: () => apiClient.get<CommitteeReport>(`/committee-reports/${reportId}`),
    enabled: !!reportId,
  });
}

export function useCreateCommitteeReport(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { reportType: CommitteeReportType; title: string; meetingDate?: string; snapshotId?: string }) =>
      apiClient.post<string>(`/projects/${projectId}/committee-reports`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: listKey(projectId) }),
  });
}

export function useUpdateCommitteeReport(projectId: string, reportId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: Omit<CommitteeReport,
      "id" | "projectId" | "projectName" | "projectRef" | "reportType" | "title" | "meetingDate" | "status" | "createdDate">) =>
      apiClient.put<void>(`/committee-reports/${reportId}`, vars),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: listKey(projectId) });
      queryClient.invalidateQueries({ queryKey: detailKey(reportId) });
    },
  });
}

export function useSubmitCommitteeReport(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (reportId: string) => apiClient.put<void>(`/committee-reports/${reportId}/submit`),
    onSuccess: (_data, reportId) => {
      queryClient.invalidateQueries({ queryKey: listKey(projectId) });
      queryClient.invalidateQueries({ queryKey: detailKey(reportId) });
    },
  });
}

export function useCompareSnapshots(fromSnapshotId: string | undefined, toSnapshotId: string | undefined) {
  return useQuery({
    queryKey: ["snapshots", "compare", fromSnapshotId, toSnapshotId],
    queryFn: () => apiClient.get<SnapshotComparison>(`/snapshots/compare?fromSnapshotId=${fromSnapshotId}&toSnapshotId=${toSnapshotId}`),
    enabled: !!fromSnapshotId && !!toSnapshotId && fromSnapshotId !== toSnapshotId,
  });
}

export function exportReportUrl(reportId: string, format: string) {
  const base = import.meta.env.VITE_API_BASE_URL ?? "/api";
  return `${base}/committee-reports/${reportId}/export/${format}`;
}
