import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn, formatCurrency, formatDate } from "@/lib/utils";
import {
  useArchitectsInstructions, useCreateArchitectsInstruction, useCreateExtensionOfTime,
  useCreateInterimValuation, useCreateLossAndExpenseClaim, useCreateVariation, useExtensionsOfTime,
  useInterimValuations, useLossAndExpenseClaims, useUpdateExtensionOfTimeStatus, useUpdateVariationStatus,
  useVariations,
} from "./api";

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
            {v.status === "Instructed" && <Button size="sm" variant="outline" onClick={() => updateStatus.mutate({ variationId: v.id, status: "Priced" })}>Mark Priced</Button>}
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
        <div key={e.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{e.reference} — {e.reason} ({e.daysClaimed}d claimed{e.daysAwarded != null ? `, ${e.daysAwarded}d awarded` : ""})</span>
          <div className="flex items-center gap-2">
            <Badge variant={statusToBadgeVariant(e.status)}>{e.status}</Badge>
            {e.status === "Claimed" && (
              <Button size="sm" variant="outline" onClick={() => updateStatus.mutate({ extensionOfTimeId: e.id, status: "Awarded", daysAwarded: e.daysClaimed })}>Award in Full</Button>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}

function LossAndExpenseSection({ projectId }: { projectId: string }) {
  const { data, isLoading } = useLossAndExpenseClaims(projectId);
  const create = useCreateLossAndExpenseClaim(projectId);
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
        <div key={l.id} className="flex items-center justify-between rounded-md border border-border p-2 text-sm">
          <span>{l.reference} — {l.description} ({formatCurrency(l.claimedAmount)} claimed)</span>
          <Badge variant={statusToBadgeVariant(l.status)}>{l.status}</Badge>
        </div>
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
