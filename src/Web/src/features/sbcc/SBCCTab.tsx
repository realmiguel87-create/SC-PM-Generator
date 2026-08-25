import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { StatusActions, type TransitionMap } from "@/components/StatusActions";
import { cn, formatCurrency, formatDate } from "@/lib/utils";
import {
  useArchitectsInstructions, useCreateArchitectsInstruction, useCreateExtensionOfTime,
  useCreateInterimValuation, useCreateLossAndExpenseClaim, useCreateVariation, useExtensionsOfTime,
  useInterimValuations, useLossAndExpenseClaims, useUpdateExtensionOfTimeStatus,
  useUpdateLossAndExpenseStatus, useUpdateVariationStatus, useVariations,
} from "./api";
import type { ExtensionOfTime, LossAndExpenseClaim, Variation } from "./types";

const REGISTERS = ["Variations", "Extensions of Time", "Loss & Expense", "Architect's Instructions", "Interim Valuations"] as const;
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

function VariationsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useVariations(projectId);
  const create = useCreateVariation(projectId);
  const updateStatus = useUpdateVariationStatus(projectId);
  const [reference, setReference] = useState("");
  const [description, setDescription] = useState("");
  const [value, setValue] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input placeholder="Ref" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={reference} onChange={(e) => setReference(e.target.value)} />
        <input placeholder="Description" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={description} onChange={(e) => setDescription(e.target.value)} />
        <input type="number" placeholder="Value impact" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={value} onChange={(e) => setValue(e.target.value)} />
        <Button
          size="sm"
          disabled={!reference || !description || !value || create.isPending}
          onClick={() => create.mutate({ reference, description, valueImpact: Number(value) }, { onSuccess: () => { setReference(""); setDescription(""); setValue(""); } })}
        >
          Instruct Variation
        </Button>
      </div>
      {data.map((v) => (
        <div key={v.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{v.reference} — {v.description} ({formatCurrency(v.valueImpact)})</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(v.status)}>{v.status}</Badge>
            <StatusActions
              status={v.status}
              transitions={VARIATION_TRANSITIONS}
              labels={{ Priced: "Mark Priced", Agreed: "Agree" }}
              pending={updateStatus.isPending}
              onSelect={(status) => updateStatus.mutate({ variationId: v.id, status })}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function ExtensionsOfTimeSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useExtensionsOfTime(projectId);
  const create = useCreateExtensionOfTime(projectId);
  const updateStatus = useUpdateExtensionOfTimeStatus(projectId);
  const [reference, setReference] = useState("");
  const [reason, setReason] = useState("");
  const [days, setDays] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input placeholder="Ref" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={reference} onChange={(e) => setReference(e.target.value)} />
        <input placeholder="Reason" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={reason} onChange={(e) => setReason(e.target.value)} />
        <input type="number" placeholder="Days claimed" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={days} onChange={(e) => setDays(e.target.value)} />
        <Button
          size="sm"
          disabled={!reference || !reason || !days || create.isPending}
          onClick={() => create.mutate({ reference, reason, daysClaimed: Number(days) }, { onSuccess: () => { setReference(""); setReason(""); setDays(""); } })}
        >
          Claim EOT
        </Button>
      </div>
      {data.map((e) => (
        <ExtensionOfTimeRow
          key={e.id}
          extension={e}
          pending={updateStatus.isPending}
          onUpdate={(status, daysAwarded) =>
            updateStatus.mutate({ extensionOfTimeId: e.id, status, daysAwarded })
          }
        />
      ))}
    </div>
  );
}

// Mirrors StatusTransitions.Variation in the Domain. SBCC variations have no rejected state —
// an instruction has been issued, and the only question is what it is worth.
const VARIATION_TRANSITIONS: TransitionMap<Variation["status"]> = {
  Instructed: ["Priced"],
  Priced: ["Agreed"],
};

// Claimed and under-review claims can still be determined; agreed and rejected are terminal.
// Reopening a determination is not a UI action — it is a contractual event that should leave a
// record, and quietly flipping the status back would leave none.
const EOT_TRANSITIONS: TransitionMap<ExtensionOfTime["status"]> = {
  Claimed: ["UnderReview", "Awarded", "Rejected"],
  UnderReview: ["Awarded", "Rejected"],
};

const EOT_LABELS: Partial<Record<ExtensionOfTime["status"], string>> = {
  UnderReview: "Under review",
  Awarded: "Award",
};

function ExtensionOfTimeRow({
  extension,
  pending,
  onUpdate,
}: {
  extension: ExtensionOfTime;
  pending: boolean;
  onUpdate: (status: ExtensionOfTime["status"], daysAwarded?: number) => void;
}) {
  // Pre-filled with the days claimed, which is the common case — awarding in full is one click,
  // and a partial award is one edit. Leaving it blank would make the frequent case the fiddly one.
  const [days, setDays] = useState(String(extension.daysAwarded ?? extension.daysClaimed));

  const parsed = Number(days);
  const daysValid = days !== "" && Number.isFinite(parsed) && parsed >= 0;

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border p-2 text-sm">
      <span>
        {extension.reference} — {extension.reason} ({extension.daysClaimed}d claimed
        {extension.daysAwarded != null ? `, ${extension.daysAwarded}d awarded` : ""})
      </span>
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant={statusToBadgeVariant(extension.status)}>{extension.status}</Badge>
        {(EOT_TRANSITIONS[extension.status] ?? []).includes("Awarded") && (
          <label className="flex items-center gap-1 text-xs text-text-secondary">
            Days
            <input
              type="number"
              min="0"
              aria-label={`Days awarded for ${extension.reference}`}
              className="w-20 rounded-md border border-border bg-transparent px-2 py-1 text-sm"
              value={days}
              onChange={(event) => setDays(event.target.value)}
            />
          </label>
        )}
        <StatusActions
          status={extension.status}
          transitions={EOT_TRANSITIONS}
          labels={EOT_LABELS}
          pending={pending || !daysValid}
          onSelect={(status) =>
            // Days are only sent with an award. Attaching them to a rejection would record an
            // award figure against a claim that was refused.
            onUpdate(status, status === "Awarded" ? parsed : undefined)
          }
        />
      </div>
    </div>
  );
}

const LOSS_AND_EXPENSE_TRANSITIONS: TransitionMap<LossAndExpenseClaim["status"]> = {
  Claimed: ["UnderReview", "Agreed", "Rejected"],
  UnderReview: ["Agreed", "Rejected"],
};

const LOSS_AND_EXPENSE_LABELS: Partial<Record<LossAndExpenseClaim["status"], string>> = {
  UnderReview: "Under review",
  Agreed: "Agree",
};

function LossAndExpenseRow({
  claim,
  pending,
  onUpdate,
}: {
  claim: LossAndExpenseClaim;
  pending: boolean;
  onUpdate: (status: LossAndExpenseClaim["status"], awardedAmount?: number) => void;
}) {
  const [amount, setAmount] = useState(String(claim.awardedAmount ?? claim.claimedAmount));

  const parsed = Number(amount);
  const amountValid = amount !== "" && Number.isFinite(parsed) && parsed >= 0;

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-border p-2 text-sm">
      <span>
        {claim.reference} — {claim.description} ({formatCurrency(claim.claimedAmount)} claimed
        {claim.awardedAmount != null ? `, ${formatCurrency(claim.awardedAmount)} agreed` : ""})
      </span>
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant={statusToBadgeVariant(claim.status)}>{claim.status}</Badge>
        {(LOSS_AND_EXPENSE_TRANSITIONS[claim.status] ?? []).includes("Agreed") && (
          <label className="flex items-center gap-1 text-xs text-text-secondary">
            £
            <input
              type="number"
              min="0"
              aria-label={`Amount agreed for ${claim.reference}`}
              className="w-28 rounded-md border border-border bg-transparent px-2 py-1 text-sm"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
            />
          </label>
        )}
        <StatusActions
          status={claim.status}
          transitions={LOSS_AND_EXPENSE_TRANSITIONS}
          labels={LOSS_AND_EXPENSE_LABELS}
          pending={pending || !amountValid}
          onSelect={(status) => onUpdate(status, status === "Agreed" ? parsed : undefined)}
        />
      </div>
    </div>
  );
}

function LossAndExpenseSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useLossAndExpenseClaims(projectId);
  const create = useCreateLossAndExpenseClaim(projectId);
  const updateStatus = useUpdateLossAndExpenseStatus(projectId);
  const [reference, setReference] = useState("");
  const [description, setDescription] = useState("");
  const [amount, setAmount] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input placeholder="Ref" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={reference} onChange={(e) => setReference(e.target.value)} />
        <input placeholder="Description" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={description} onChange={(e) => setDescription(e.target.value)} />
        <input type="number" placeholder="Claimed amount" className="w-36 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={amount} onChange={(e) => setAmount(e.target.value)} />
        <Button
          size="sm"
          disabled={!reference || !description || !amount || create.isPending}
          onClick={() => create.mutate({ reference, description, claimedAmount: Number(amount) }, { onSuccess: () => { setReference(""); setDescription(""); setAmount(""); } })}
        >
          Submit Claim
        </Button>
      </div>
      {data.map((l) => (
        <LossAndExpenseRow
          key={l.id}
          claim={l}
          pending={updateStatus.isPending}
          onUpdate={(status, awardedAmount) =>
            updateStatus.mutate({ lossAndExpenseClaimId: l.id, status, awardedAmount })
          }
        />
      ))}
    </div>
  );
}

function ArchitectsInstructionsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useArchitectsInstructions(projectId);
  const create = useCreateArchitectsInstruction(projectId);
  const [number, setNumber] = useState("");
  const [description, setDescription] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input type="number" placeholder="AI #" className="w-24 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={number} onChange={(e) => setNumber(e.target.value)} />
        <input placeholder="Description" className="flex-1 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={description} onChange={(e) => setDescription(e.target.value)} />
        <Button
          size="sm"
          disabled={!number || !description || create.isPending}
          onClick={() => create.mutate({ instructionNumber: Number(number), description, issuedDate: new Date().toISOString().slice(0, 10) }, { onSuccess: () => { setNumber(""); setDescription(""); } })}
        >
          Issue Instruction
        </Button>
      </div>
      {data.map((a) => (
        <div key={a.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>AI{a.instructionNumber} — {a.description}</span>
          <Badge variant={statusToBadgeVariant(a.status)}>{a.status}</Badge>
        </div>
      ))}
    </div>
  );
}

function InterimValuationsSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useInterimValuations(projectId);
  const create = useCreateInterimValuation(projectId);
  const [number, setNumber] = useState("");
  const [gross, setGross] = useState("");
  const [net, setNet] = useState("");

  if (isLoading || !data) return <p className="text-sm text-text-secondary">Loading…</p>;

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-end gap-2">
        <input type="number" placeholder="Valuation #" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={number} onChange={(e) => setNumber(e.target.value)} />
        <input type="number" placeholder="Gross valuation" className="w-36 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={gross} onChange={(e) => setGross(e.target.value)} />
        <input type="number" placeholder="Net payment" className="w-36 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={net} onChange={(e) => setNet(e.target.value)} />
        <Button
          size="sm"
          disabled={!number || !gross || !net || create.isPending}
          onClick={() => create.mutate(
            { valuationNumber: Number(number), valuationDate: new Date().toISOString().slice(0, 10), grossValuation: Number(gross), netPayment: Number(net) },
            { onSuccess: () => { setNumber(""); setGross(""); setNet(""); } },
          )}
        >
          Add Valuation
        </Button>
      </div>
      {data.map((v) => (
        <div key={v.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>IV{v.valuationNumber} — {formatDate(v.valuationDate)} — gross {formatCurrency(v.grossValuation)}, net {formatCurrency(v.netPayment)}</span>
          <Badge variant={statusToBadgeVariant(v.status)}>{v.status}</Badge>
        </div>
      ))}
    </div>
  );
}

export function SBCCTab({ projectId }: { projectId: string }) {
  const [active, setActive] = useState<Register>("Variations");

  return (
    <Card>
      <CardHeader><CardTitle>SBCC Contract Administration</CardTitle></CardHeader>
      <CardContent className="flex flex-col gap-4 pt-0">
        <SubNav active={active} onChange={setActive} />
        {active === "Variations" && <VariationsSection projectId={projectId} />}
        {active === "Extensions of Time" && <ExtensionsOfTimeSection projectId={projectId} />}
        {active === "Loss & Expense" && <LossAndExpenseSection projectId={projectId} />}
        {active === "Architect's Instructions" && <ArchitectsInstructionsSection projectId={projectId} />}
        {active === "Interim Valuations" && <InterimValuationsSection projectId={projectId} />}
      </CardContent>
    </Card>
  );
}
