import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { DocumentDetail, DocumentListItem } from "./types";

const listKey = (projectId: string) => ["projects", "detail", projectId, "documents"] as const;
const detailKey = (documentId: string) => ["documents", "detail", documentId] as const;

export function useDocuments(projectId: string | undefined) {
  return useQuery({
    queryKey: listKey(projectId ?? ""),
    queryFn: () => apiClient.get<DocumentListItem[]>(`/projects/${projectId}/documents`),
    enabled: !!projectId,
  });
}

export function useDocument(documentId: string | undefined) {
  return useQuery({
    queryKey: detailKey(documentId ?? ""),
    queryFn: () => apiClient.get<DocumentDetail>(`/documents/${documentId}`),
    enabled: !!documentId,
  });
}

export function useCreateDocument(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { title: string; category: string; ribaStageNumber?: number }) =>
      apiClient.post<string>(`/projects/${projectId}/documents`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: listKey(projectId) }),
  });
}

export function useCreateDraftRevision(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => apiClient.post<string>(`/documents/${documentId}/revisions`),
    onSuccess: (_data, documentId) => {
      queryClient.invalidateQueries({ queryKey: listKey(projectId) });
      queryClient.invalidateQueries({ queryKey: detailKey(documentId) });
    },
  });
}

function useVersionTransition(projectId: string, documentId: string, action: "approve" | "reject" | "archive") {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (documentVersionId: string) => apiClient.put<void>(`/document-versions/${documentVersionId}/${action}`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: listKey(projectId) });
      queryClient.invalidateQueries({ queryKey: detailKey(documentId) });
    },
  });
}

export const useApproveVersion = (projectId: string, documentId: string) => useVersionTransition(projectId, documentId, "approve");
export const useRejectVersion = (projectId: string, documentId: string) => useVersionTransition(projectId, documentId, "reject");
export const useArchiveVersion = (projectId: string, documentId: string) => useVersionTransition(projectId, documentId, "archive");

export function useAddDocumentFile(projectId: string, documentId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { documentVersionId: string; fileType: string; category: string; file: File }) => {
      const formData = new FormData();
      formData.append("fileType", vars.fileType);
      formData.append("category", vars.category);
      formData.append("file", vars.file);
      return apiClient.postForm<string>(`/document-versions/${vars.documentVersionId}/files`, formData);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: detailKey(documentId) }),
  });
}
