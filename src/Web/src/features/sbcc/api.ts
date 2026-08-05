import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type { ArchitectsInstruction, ExtensionOfTime, InterimValuation, LossAndExpenseClaim, Variation } from "./types";

const key = (projectId: string, register: string) => ["projects", "detail", projectId, "sbcc", register] as const;

function useRegister<T>(projectId: string | undefined, register: string, path: string) {
  return useQuery({
    queryKey: key(projectId ?? "", register),
    queryFn: () => apiClient.get<T[]>(`/projects/${projectId}/sbcc/${path}`),
    enabled: !!projectId,
  });
}

function useCreate<TVars>(projectId: string, register: string, path: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: TVars) => apiClient.post<string>(`/projects/${projectId}/sbcc/${path}`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, register) }),
  });
}

export const useVariations = (projectId?: string) => useRegister<Variation>(projectId, "variations", "variations");
export const useCreateVariation = (projectId: string) =>
  useCreate<{ reference: string; description: string; valueImpact: number }>(projectId, "variations", "variations");
export function useUpdateVariationStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { variationId: string; status: Variation["status"] }) =>
      apiClient.put<void>(`/sbcc/variations/${vars.variationId}/status`, { status: vars.status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "variations") }),
  });
}

export const useExtensionsOfTime = (projectId?: string) => useRegister<ExtensionOfTime>(projectId, "extensions-of-time", "extensions-of-time");
export const useCreateExtensionOfTime = (projectId: string) =>
  useCreate<{ reference: string; reason: string; daysClaimed: number }>(projectId, "extensions-of-time", "extensions-of-time");
export function useUpdateExtensionOfTimeStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { extensionOfTimeId: string; status: ExtensionOfTime["status"]; daysAwarded?: number }) =>
      apiClient.put<void>(`/sbcc/extensions-of-time/${vars.extensionOfTimeId}/status`, { status: vars.status, daysAwarded: vars.daysAwarded ?? null }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "extensions-of-time") }),
  });
}

export const useLossAndExpenseClaims = (projectId?: string) => useRegister<LossAndExpenseClaim>(projectId, "loss-and-expense", "loss-and-expense");
export const useCreateLossAndExpenseClaim = (projectId: string) =>
  useCreate<{ reference: string; description: string; claimedAmount: number }>(projectId, "loss-and-expense", "loss-and-expense");

export const useArchitectsInstructions = (projectId?: string) => useRegister<ArchitectsInstruction>(projectId, "architects-instructions", "architects-instructions");
export const useCreateArchitectsInstruction = (projectId: string) =>
  useCreate<{ instructionNumber: number; description: string; issuedDate: string }>(projectId, "architects-instructions", "architects-instructions");

export const useInterimValuations = (projectId?: string) => useRegister<InterimValuation>(projectId, "interim-valuations", "interim-valuations");
export const useCreateInterimValuation = (projectId: string) =>
  useCreate<{ valuationNumber: number; valuationDate: string; grossValuation: number; netPayment: number }>(
    projectId, "interim-valuations", "interim-valuations",
  );
