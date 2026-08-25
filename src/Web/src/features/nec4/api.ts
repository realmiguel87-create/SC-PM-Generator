import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import type {
  AcceptedProgrammeEntry, ChangeRegisterItem, CompensationEvent,
  ContractDataEntry, EarlyWarning, PaymentAssessment, RiskAllocationItem,
} from "./types";

const key = (projectId: string, register: string) => ["projects", "detail", projectId, "nec4", register] as const;

function useRegister<T>(projectId: string | undefined, register: string, path: string) {
  return useQuery({
    queryKey: key(projectId ?? "", register),
    queryFn: () => apiClient.get<T[]>(`/projects/${projectId}/nec4/${path}`),
    enabled: !!projectId,
  });
}

function useCreate<TVars>(projectId: string, register: string, path: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: TVars) => apiClient.post<string>(`/projects/${projectId}/nec4/${path}`, vars),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, register) }),
  });
}

export const useEarlyWarnings = (projectId?: string) => useRegister<EarlyWarning>(projectId, "early-warnings", "early-warnings");
export const useCreateEarlyWarning = (projectId: string) =>
  useCreate<{ title: string; raisedDate: string; mitigationAction?: string }>(projectId, "early-warnings", "early-warnings");
export function useCloseEarlyWarning(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (earlyWarningId: string) => apiClient.put<void>(`/nec4/early-warnings/${earlyWarningId}/close`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "early-warnings") }),
  });
}

export const useCompensationEvents = (projectId?: string) => useRegister<CompensationEvent>(projectId, "compensation-events", "compensation-events");
export const useCreateCompensationEvent = (projectId: string) =>
  useCreate<{ reference: string; title: string; clauseReference?: string; estimatedValue: number; notifiedDate: string }>(
    projectId, "compensation-events", "compensation-events",
  );
export function useUpdateCompensationEventStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { compensationEventId: string; status: CompensationEvent["status"] }) =>
      apiClient.put<void>(`/nec4/compensation-events/${vars.compensationEventId}/status`, { status: vars.status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "compensation-events") }),
  });
}

export const useContractDataEntries = (projectId?: string) => useRegister<ContractDataEntry>(projectId, "contract-data", "contract-data");
export const useCreateContractDataEntry = (projectId: string) =>
  useCreate<{ part: ContractDataEntry["part"]; clauseReference: string; description: string; value: string }>(
    projectId, "contract-data", "contract-data",
  );

export const useRiskAllocationItems = (projectId?: string) => useRegister<RiskAllocationItem>(projectId, "risk-allocation", "risk-allocation");
export const useCreateRiskAllocationItem = (projectId: string) =>
  useCreate<{ description: string; allocatedTo: RiskAllocationItem["allocatedTo"]; mitigationOwner?: string }>(
    projectId, "risk-allocation", "risk-allocation",
  );

export const useAcceptedProgrammeEntries = (projectId?: string) => useRegister<AcceptedProgrammeEntry>(projectId, "accepted-programme", "accepted-programme");
export const useCreateAcceptedProgrammeEntry = (projectId: string) =>
  useCreate<{ revisionNumber: number; acceptedDate: string; notes?: string }>(projectId, "accepted-programme", "accepted-programme");

export const usePaymentAssessments = (projectId?: string) => useRegister<PaymentAssessment>(projectId, "payment-assessments", "payment-assessments");
export const useCreatePaymentAssessment = (projectId: string) =>
  useCreate<{ assessmentNumber: number; assessmentDate: string; amountDue: number }>(projectId, "payment-assessments", "payment-assessments");
export function useUpdatePaymentAssessmentStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { paymentAssessmentId: string; status: PaymentAssessment["status"] }) =>
      apiClient.put<void>(`/nec4/payment-assessments/${vars.paymentAssessmentId}/status`, { status: vars.status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "payment-assessments") }),
  });
}

export const useChangeRegisterItems = (projectId?: string) => useRegister<ChangeRegisterItem>(projectId, "change-register", "change-register");
export const useCreateChangeRegisterItem = (projectId: string) =>
  useCreate<{ title: string; valueImpact: number; timeImpactDays: number }>(projectId, "change-register", "change-register");

export function useUpdateChangeRegisterItemStatus(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { changeRegisterItemId: string; status: ChangeRegisterItem["status"] }) =>
      apiClient.put<void>(`/nec4/change-register/${vars.changeRegisterItemId}/status`, { status: vars.status }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: key(projectId, "change-register") }),
  });
}
