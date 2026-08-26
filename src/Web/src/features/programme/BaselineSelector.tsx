import { Card, CardContent } from "@/components/ui/card";
import { describeBaseline, orderBaselines, type ScopeChange } from "./baseline";
import type { ProgrammeBaseline } from "./types";

/**
 * Chooses which sanctioned programme the timeline is measured against.
 *
 * The default is the live programme — the same picture as before this existed. Selecting an
 * earlier revision answers the question a rebaseline otherwise makes unanswerable: not "are we on
 * programme?" but "how far are we from the programme that was actually approved?"
 */
export function BaselineSelector({
  baselines,
  selectedId,
  onSelect,
  scopeChange,
  reason,
}: {
  baselines: ProgrammeBaseline[];
  selectedId: string | undefined;
  onSelect: (id: string | undefined) => void;
  scopeChange: ScopeChange | undefined;
  reason: string | undefined;
}) {
  // Nothing to choose between until a project has been rebaselined at least once. Rendering an
  // empty dropdown would present a control that cannot do anything as one that has not been used.
  if (baselines.length === 0) return null;

  const ordered = orderBaselines(baselines);

  return (
    <Card>
      <CardContent className="flex flex-col gap-3 py-3">
        <label className="flex flex-wrap items-center gap-2 text-xs text-text-secondary">
          Measure against
          <select
            aria-label="Measure against"
            className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm text-text-primary"
            value={selectedId ?? ""}
            onChange={(e) => onSelect(e.target.value || undefined)}
          >
            <option value="">Live programme</option>
            {ordered.map((baseline) => (
              <option key={baseline.id} value={baseline.id}>
                {describeBaseline(baseline)}
              </option>
            ))}
          </select>
        </label>

        {reason && (
          // The reason for the rebaseline shown alongside the comparison, not buried in a
          // separate register. A reader looking at 92 days of slip against a superseded programme
          // needs to know what was said at the time about why it was replaced.
          <p className="text-xs text-text-secondary">
            <span className="font-medium">Why this baseline was replaced: </span>
            {reason}
          </p>
        )}

        {scopeChange?.hasChanges && (
          <div className="rounded-md border border-border p-2 text-xs text-text-secondary">
            {/* Named, not counted. A milestone appearing in or vanishing from an approved
                programme is not slip and cannot be drawn as a bar — the only honest report is
                which ones. */}
            {scopeChange.added.length > 0 && (
              <p>
                <span className="font-medium">Added since this baseline: </span>
                {scopeChange.added.join(", ")} — not shown on the chart, as there is no sanctioned
                date to measure them against.
              </p>
            )}
            {scopeChange.removed.length > 0 && (
              <p>
                <span className="font-medium">Removed since this baseline: </span>
                {scopeChange.removed.join(", ")}
              </p>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
