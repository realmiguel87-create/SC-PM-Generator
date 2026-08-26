import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { ApiError } from "@/lib/api-client";
import { describeRebaselineEffect, validateRebaseline } from "./baseline";
import { useRebaselineProgramme } from "./api";
import type { Milestone } from "./types";

/**
 * Rebaselining the programme.
 *
 * Deliberately two-stage. Everything else on this tab is an edit; this is not. It moves the
 * measure the project is judged against, and its most visible effect is that the slip everyone has
 * been watching drops to zero — which is correct, and is also exactly the kind of change that
 * should not happen to someone who had not realised it would. So the effect is stated in the terms
 * a reader cares about, and confirmed, before anything is written.
 *
 * The server requires `CanApprove` for this. The form is not hidden from users who lack it: no
 * screen in this app gates on role client-side, because the token's roles are a claim about what
 * the server will allow, not the decision itself — and a UI that guesses wrong either hides
 * something permitted or offers something that will be refused. A 403 is reported when it comes.
 */
export function RebaselineForm({
  projectId,
  milestones,
}: {
  projectId: string;
  milestones: Milestone[];
}) {
  const rebaseline = useRebaselineProgramme(projectId);

  const [name, setName] = useState("");
  const [reason, setReason] = useState("");
  const [approvedDate, setApprovedDate] = useState("");
  const [confirming, setConfirming] = useState(false);

  // Nothing to rebaseline onto. Offering the control would present a governance act that cannot
  // be performed as one that simply has not been.
  if (milestones.length === 0) return null;

  const effect = describeRebaselineEffect(milestones);
  const problem = validateRebaseline(name, reason);

  const submit = () => {
    rebaseline.mutate(
      { name: name.trim(), reason: reason.trim(), approvedDate: approvedDate || undefined },
      {
        onSuccess: () => {
          setName("");
          setReason("");
          setApprovedDate("");
          setConfirming(false);
        },
      },
    );
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Rebaseline Programme</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        <p className="text-xs text-text-secondary">
          Re-sanctions the current forecast as the programme this project is measured against. The
          programme being replaced is kept as a numbered revision and stays available in
          &ldquo;Measure against&rdquo; above.
        </p>

        <div className="flex flex-wrap items-end gap-3">
          <label className="flex flex-1 min-w-[12rem] flex-col gap-1 text-xs text-text-secondary">
            Baseline name
            <input
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              placeholder="Post-tender programme"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                // Any edit invalidates the figures the reader confirmed against.
                setConfirming(false);
              }}
            />
          </label>
          <label className="flex flex-col gap-1 text-xs text-text-secondary">
            Approved on <span className="text-[10px]">(optional)</span>
            <input
              type="date"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={approvedDate}
              onChange={(e) => {
                setApprovedDate(e.target.value);
                setConfirming(false);
              }}
            />
          </label>
        </div>

        <label className="flex flex-col gap-1 text-xs text-text-secondary">
          Reason
          <textarea
            className="min-h-[4rem] rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
            placeholder="Tender returns came in three months later than programmed."
            value={reason}
            onChange={(e) => {
              setReason(e.target.value);
              setConfirming(false);
            }}
          />
        </label>

        {approvedDate && (
          // Said plainly rather than left implied. The record will name the signed-in user as the
          // approver, and someone recording a committee's decision should know that is what it
          // will say before they enter its date.
          <p className="text-[11px] text-text-secondary">
            This will be recorded as approved by you on that date.
          </p>
        )}

        {confirming ? (
          <div
            role="group"
            aria-label="Confirm rebaseline"
            className="flex flex-col gap-2 rounded-md border border-critical p-3 text-sm"
          >
            <p className="font-medium">
              Re-sanction {effect.moving} of {effect.total} milestone
              {effect.total === 1 ? "" : "s"}?
            </p>
            {effect.worstSlipCleared > 0 ? (
              <p className="text-xs text-text-secondary">
                {/* The number that is about to change meaning, named. "Worst slip: 92d" becoming
                    "Nothing has slipped" is the whole effect of this action, and it is the thing a
                    reader would otherwise discover afterwards. */}
                The worst slip of {effect.worstSlipCleared} days
                {effect.worstSlipName ? ` (${effect.worstSlipName})` : ""} will read as zero against
                the new programme. It stays measurable against this one.
              </p>
            ) : (
              <p className="text-xs text-text-secondary">
                Nothing has slipped, so no slip figure changes.
              </p>
            )}
            <div className="flex gap-2">
              <Button size="sm" disabled={rebaseline.isPending} onClick={submit}>
                {rebaseline.isPending ? "Rebaselining…" : "Confirm rebaseline"}
              </Button>
              <Button
                size="sm"
                variant="outline"
                disabled={rebaseline.isPending}
                onClick={() => setConfirming(false)}
              >
                Cancel
              </Button>
            </div>
          </div>
        ) : (
          <div className="flex items-center gap-3">
            <Button size="sm" disabled={problem !== null} onClick={() => setConfirming(true)}>
              Review rebaseline
            </Button>
            {problem && name.length + reason.length > 0 && (
              <span className="text-xs text-text-secondary">{problem}</span>
            )}
          </div>
        )}

        {rebaseline.isError && <RebaselineError error={rebaseline.error} />}
      </CardContent>
    </Card>
  );
}

/**
 * Reports why the rebaseline was refused, rather than asserting a cause it has not checked — the
 * same rule ApiErrorNotice follows, and worth repeating here because 403 is a likely answer: this
 * endpoint needs approval rights, not write rights.
 */
function RebaselineError({ error }: { error: unknown }) {
  const status = error instanceof ApiError ? error.status : undefined;
  const message = error instanceof Error ? error.message : String(error);

  return (
    <p className="text-sm text-critical" role="alert">
      {status === 403
        ? "Rebaselining needs approval rights, which your account does not have. An administrator needs to grant them."
        : status === 401
          ? "Not signed in, or your session has expired. Use Sign in on the left to continue."
          : `The rebaseline was not saved: ${message}`}
    </p>
  );
}
