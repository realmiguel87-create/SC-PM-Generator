import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type {
  CommitteeReport,
  CommitteeReportListItem,
  CommitteeReportType,
  SnapshotComparison,
  SnapshotIntervalActivity,
  SnapshotItemComparison,
} from "./types";

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

/**
 * The item-level counterpart of useCompareSnapshots. A separate query rather than a flag on the
 * other one, because the API reads temporal history for this and it is materially more expensive
 * — a caller should ask for it deliberately.
 */
export function useCompareSnapshotItems(
  fromSnapshotId: string | undefined,
  toSnapshotId: string | undefined,
) {
  return useQuery({
    queryKey: ["snapshots", "compare", "items", fromSnapshotId, toSnapshotId],
    queryFn: () =>
      apiClient.get<SnapshotItemComparison>(
        `/snapshots/compare/items?fromSnapshotId=${fromSnapshotId}&toSnapshotId=${toSnapshotId}`,
      ),
    enabled: !!fromSnapshotId && !!toSnapshotId && fromSnapshotId !== toSnapshotId,
  });
}

/**
 * Activity an endpoint comparison cannot see. Reads every row version in the period rather than
 * the state at two instants, so it is the most expensive of the three comparison queries — hence
 * a separate call a caller opts into, not a field on the others.
 */
export function useSnapshotIntervalActivity(
  fromSnapshotId: string | undefined,
  toSnapshotId: string | undefined,
) {
  return useQuery({
    queryKey: ["snapshots", "compare", "interval", fromSnapshotId, toSnapshotId],
    queryFn: () =>
      apiClient.get<SnapshotIntervalActivity>(
        `/snapshots/compare/interval-activity?fromSnapshotId=${fromSnapshotId}&toSnapshotId=${toSnapshotId}`,
      ),
    enabled: !!fromSnapshotId && !!toSnapshotId && fromSnapshotId !== toSnapshotId,
  });
}

/** Download URL for the whole snapshot comparison — headline movements, item changes, and the
 *  activity in between — in any of the six export formats. */
export function exportComparisonUrl(
  fromSnapshotId: string,
  toSnapshotId: string,
  format: string,
) {
  const base = import.meta.env.VITE_API_BASE_URL ?? "/api";
  return `${base}/snapshots/compare/export/${format}?fromSnapshotId=${fromSnapshotId}&toSnapshotId=${toSnapshotId}`;
}

export function exportReportUrl(reportId: string, format: string) {
  const base = import.meta.env.VITE_API_BASE_URL ?? "/api";
  return `${base}/committee-reports/${reportId}/export/${format}`;
}
