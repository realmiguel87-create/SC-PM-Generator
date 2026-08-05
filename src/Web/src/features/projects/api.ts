import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { CreateProjectRequest, ProjectDetail, ProjectListItem } from "./types";

const keys = {
  all: ["projects"] as const,
  list: (status?: string) => [...keys.all, "list", status ?? "all"] as const,
  detail: (id: string) => [...keys.all, "detail", id] as const,
};

export function useProjects(status?: string) {
  return useQuery({
    queryKey: keys.list(status),
    queryFn: () => apiClient.get<ProjectListItem[]>(`/projects${status ? `?status=${status}` : ""}`),
  });
}

export function useProject(id: string | undefined) {
  return useQuery({
    queryKey: keys.detail(id ?? ""),
    queryFn: () => apiClient.get<ProjectDetail>(`/projects/${id}`),
    enabled: !!id,
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateProjectRequest) => apiClient.post<string>("/projects", request),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: keys.all }),
  });
}

export function useAdvanceRibaStage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (projectId: string) => apiClient.post<void>(`/projects/${projectId}/advance-stage`),
    onSuccess: (_data, projectId) => {
      queryClient.invalidateQueries({ queryKey: keys.detail(projectId) });
      queryClient.invalidateQueries({ queryKey: keys.all });
    },
  });
}
