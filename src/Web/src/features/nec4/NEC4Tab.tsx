import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { StatusActions, type TransitionMap } from "@/components/StatusActions";
import { cn, formatCurrency, formatDate } from "@/lib/utils";
import {
  useAcceptedProgrammeEntries, useChangeRegisterItems, useCloseEarlyWarning, useCompensationEvents,
  useContractDataEntries, useCreateAcceptedProgrammeEntry, useCreateChangeRegisterItem,
  useCreateCompensationEvent, useCreateContractDataEntry, useCreateEarlyWarning, useCreatePaymentAssessment,
  useCreateRiskAllocationItem, useEarlyWarnings, usePaymentAssessments, useRiskAllocationItems,
  useUpdateChangeRegisterItemStatus, useUpdateCompensationEventStatus, useUpdatePaymentAssessmentStatus,
} from "./api";
import type { ChangeRegisterItem } from "./types";

const REGISTERS = [
  "Early Warnings", "Compensation Events", "Contract Data", "Risk Allocation", "Accepted Programme", "Payment Assessments", "Change Register",
] as const;
type Register = (typeof REGISTERS)[number];

function SubNav({ active, onChange }: { active: Register; onChange: (r: Register) => void }) {
  return (
    <div className="flex flex-wrap gap-1 border-b border-border pb-2">
      {REGISTERS.map((r) => (
        <button
          key={r}
          onClick={() => onChange(r)}
          className={cn(
            "rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
            active === r ? "bg-purple-soft text-stirling-purple" : "text-text-secondary hover:bg-purple-soft",
          )}
        >
          {r}
        </button>
      ))}
    </div>
  );
}

function EarlyWarningsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useEarlyWarnings(projectId);
  const create = useCreateEarlyWarning(projectId);
  const close = useCloseEarlyWarning(projectId);
  const [title, setTitle] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end gap-2">
        <input placeholder="Early warning title" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
        <Button size="sm" disabled={!title || create.isPending} onClick={() => create.mutate({ title, raisedDate: new Date().toISOString().slice(0, 10) }, { onSuccess: () => setTitle("") })}>Raise</Button>
      </div>
      {data.map((e) => (
        <div key={e.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{e.title}</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(e.status)}>{e.status}</Badge>
            {e.status === "Open" && <Button size="sm" variant="outline" onClick={() => close.mutate(e.id)}>Close</Button>}
          </div>
        </div>
      ))}
    </div>
  );
}

function CompensationEventsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useCompensationEvents(projectId);
  const create = useCreateCompensationEvent(projectId);
  const updateStatus = useUpdateCompensationEventStatus(projectId);
  const [reference, setReference] = useState("");
  const [title, setTitle] = useState("");
  const [value, setValue] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input placeholder="CE Ref" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={reference} onChange={(e) => setReference(e.target.value)} />
        <input placeholder="Title" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
        <input type="number" placeholder="Est. value" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={value} onChange={(e) => setValue(e.target.value)} />
        <Button
          size="sm"
          disabled={!reference || !title || !value || create.isPending}
          onClick={() => create.mutate(
            { reference, title, estimatedValue: Number(value), notifiedDate: new Date().toISOString().slice(0, 10) },
            { onSuccess: () => { setReference(""); setTitle(""); setValue(""); } },
          )}
        >
          Notify CE
        </Button>
      </div>
      {data.map((c) => (
        <div key={c.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{c.reference} — {c.title} ({formatCurrency(c.estimatedValue)})</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(c.status)}>{c.status}</Badge>
            {c.status === "Notified" && (
              <Button size="sm" variant="outline" onClick={() => updateStatus.mutate({ compensationEventId: c.id, status: "Quoted" })}>Mark Quoted</Button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function ContractDataSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useContractDataEntries(projectId);
  const create = useCreateContractDataEntry(projectId);
  const [clause, setClause] = useState("");
  const [description, setDescription] = useState("");
  const [value, setValue] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input placeholder="Clause" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={clause} onChange={(e) => setClause(e.target.value)} />
        <input placeholder="Description" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={description} onChange={(e) => setDescription(e.target.value)} />
        <input placeholder="Value" className="w-40 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={value} onChange={(e) => setValue(e.target.value)} />
        <Button
          size="sm"
          disabled={!clause || !description || !value || create.isPending}
          onClick={() => create.mutate(
            { part: "PartOne", clauseReference: clause, description, value },
            { onSuccess: () => { setClause(""); setDescription(""); setValue(""); } },
          )}
        >
          Add (Part One)
        </Button>
      </div>
      {data.map((c) => (
        <div key={c.id} className="rounded-md border border-border p-2 text-sm">
          <span className="font-medium">{c.part === "PartOne" ? "Part One" : "Part Two"} · {c.clauseReference}</span> — {c.description}: {c.value}
        </div>
      ))}
    </div>
  );
}

function RiskAllocationSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useRiskAllocationItems(projectId);
  const create = useCreateRiskAllocationItem(projectId);
  const [description, setDescription] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end gap-2">
        <input placeholder="Risk description" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={description} onChange={(e) => setDescription(e.target.value)} />
        <Button size="sm" disabled={!description || create.isPending} onClick={() => create.mutate({ description, allocatedTo: "Shared" }, { onSuccess: () => setDescription("") })}>Add</Button>
      </div>
      {data.map((r) => (
        <div key={r.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{r.description}</span>
          <Badge variant="information">{r.allocatedTo}</Badge>
        </div>
      ))}
    </div>
  );
}

function AcceptedProgrammeSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useAcceptedProgrammeEntries(projectId);
  const create = useCreateAcceptedProgrammeEntry(projectId);
  const [revision, setRevision] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end gap-2">
        <input type="number" placeholder="Revision #" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={revision} onChange={(e) => setRevision(e.target.value)} />
        <Button
          size="sm"
          disabled={!revision || create.isPending}
          onClick={() => create.mutate({ revisionNumber: Number(revision), acceptedDate: new Date().toISOString().slice(0, 10) }, { onSuccess: () => setRevision("") })}
        >
          Record Acceptance
        </Button>
      </div>
      {data.map((a) => (
        <div key={a.id} className="rounded-md border border-border p-2 text-sm">Revision {a.revisionNumber} — accepted {formatDate(a.acceptedDate)}</div>
      ))}
    </div>
  );
}

function PaymentAssessmentsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = usePaymentAssessments(projectId);
  const create = useCreatePaymentAssessment(projectId);
  const updateStatus = useUpdatePaymentAssessmentStatus(projectId);
  const [number, setNumber] = useState("");
  const [amount, setAmount] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input type="number" placeholder="Assessment #" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={number} onChange={(e) => setNumber(e.target.value)} />
        <input type="number" placeholder="Amount due" className="w-36 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={amount} onChange={(e) => setAmount(e.target.value)} />
        <Button
          size="sm"
          disabled={!number || !amount || create.isPending}
          onClick={() => create.mutate(
            { assessmentNumber: Number(number), assessmentDate: new Date().toISOString().slice(0, 10), amountDue: Number(amount) },
            { onSuccess: () => { setNumber(""); setAmount(""); } },
          )}
        >
          Add Assessment
        </Button>
      </div>
      {data.map((p) => (
        <div key={p.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>PA{p.assessmentNumber} — {formatCurrency(p.amountDue)}</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(p.status)}>{p.status}</Badge>
            {p.status === "Assessed" && <Button size="sm" variant="outline" onClick={() => updateStatus.mutate({ paymentAssessmentId: p.id, status: "Certified" })}>Certify</Button>}
          </div>
        </div>
      ))}
    </div>
  );
}

// Proposed changes are approved or rejected; an approved one is then implemented. Rejected and
// Implemented are terminal — a rejected change that needs revisiting is raised again, so that the
// register keeps the original decision rather than overwriting it.
const CHANGE_REGISTER_TRANSITIONS: TransitionMap<ChangeRegisterItem["status"]> = {
  Proposed: ["Approved", "Rejected"],
  Approved: ["Implemented"],
};

function ChangeRegisterSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useChangeRegisterItems(projectId);
  const create = useCreateChangeRegisterItem(projectId);
  const updateStatus = useUpdateChangeRegisterItemStatus(projectId);
  const [title, setTitle] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-end gap-2">
        <input placeholder="Change title" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
        <Button size="sm" disabled={!title || create.isPending} onClick={() => create.mutate({ title, valueImpact: 0, timeImpactDays: 0 }, { onSuccess: () => setTitle("") })}>Add</Button>
      </div>
      {data.map((c) => (
        <div key={c.id} className="flex items-center justify-between gap-2 rounded-md border border-border p-2 text-sm">
          <span>{c.title}</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(c.status)}>{c.status}</Badge>
            <StatusActions
              status={c.status}
              transitions={CHANGE_REGISTER_TRANSITIONS}
              pending={updateStatus.isPending}
              onSelect={(status) => updateStatus.mutate({ changeRegisterItemId: c.id, status })}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

export function NEC4Tab({ projectId }: { projectId: string }) {
  const [active, setActive] = useState<Register>("Early Warnings");

  return (
    <Card>
      <CardHeader><CardTitle>NEC4 Contract Administration</CardTitle></CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
        <SubNav active={active} onChange={setActive} />
        {active === "Early Warnings" && <EarlyWarningsSection projectId={projectId} />}
        {active === "Compensation Events" && <CompensationEventsSection projectId={projectId} />}
        {active === "Contract Data" && <ContractDataSection projectId={projectId} />}
        {active === "Risk Allocation" && <RiskAllocationSection projectId={projectId} />}
        {active === "Accepted Programme" && <AcceptedProgrammeSection projectId={projectId} />}
        {active === "Payment Assessments" && <PaymentAssessmentsSection projectId={projectId} />}
        {active === "Change Register" && <ChangeRegisterSection projectId={projectId} />}
      </CardContent>
    </Card>
  );
}
