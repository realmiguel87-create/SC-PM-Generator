import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type {
  Milestone,
  MilestoneStatus,
  ProgrammeAgainstBaseline,
  ProgrammeBaseline,
} from "./types";

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

/** The project's sanctioned programmes, newest revision first. */
export function useProgrammeBaselines(projectId: string | undefined) {
  return useQuery({
    queryKey: ["projects", "detail", projectId ?? "", "baselines"] as const,
    queryFn: () => apiClient.get<ProgrammeBaseline[]>(`/projects/${projectId}/baselines`),
    enabled: !!projectId,
  });
}

/**
 * The programme measured against one baseline.
 *
 * Only runs when a baseline is actually selected. Passing no id would make the endpoint answer for
 * the current baseline, which is the same picture the milestone list already gives — a request
 * whose result the screen would discard.
 */
export function useProgrammeAgainstBaseline(
  projectId: string | undefined,
  baselineId: string | undefined,
) {
  return useQuery({
    queryKey: ["projects", "detail", projectId ?? "", "baseline-comparison", baselineId ?? ""] as const,
    queryFn: () =>
      apiClient.get<ProgrammeAgainstBaseline>(
        `/projects/${projectId}/baseline-comparison?baselineId=${baselineId}`,
      ),
    enabled: !!projectId && !!baselineId,
  });
}

/**
 * Rebaselines the programme.
 *
 * Invalidates the milestones as well as the baselines: rebaselining rewrites every milestone's
 * baseline date, so a cache holding the old ones would leave the table below the chart showing
 * slip against a programme that has just been superseded.
 *
 * No approver is sent. It is taken from the caller's identity server-side — a browser cannot know
 * an SCPM user id. See RebaselineProgrammeCommand.
 */
export function useRebaselineProgramme(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { name: string; reason: string; approvedDate?: string }) =>
      apiClient.post<string>(`/projects/${projectId}/baselines`, {
        name: vars.name,
        reason: vars.reason,
        approvedDate: vars.approvedDate || null,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: key(projectId) });
      await queryClient.invalidateQueries({
        queryKey: ["projects", "detail", projectId, "baselines"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["projects", "detail", projectId, "baseline-comparison"],
      });
    },
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
