import { Button } from "@/components/ui/button";

/**
 * The transitions offered from a given status, per register.
 *
 * These maps are the only thing deciding which moves a user is offered — worth being explicit
 * about, because the API does not validate transitions. `UpdateExtensionOfTimeStatusCommand` and
 * its siblings set whatever status they are given, so anything constructing a request by hand can
 * move an item anywhere. That is a real gap, recorded in docs/roadmap.md rather than papered over
 * here: this component shapes what the UI offers, and shaping is not enforcing.
 */
export type TransitionMap<TStatus extends string> = Partial<Record<TStatus, TStatus[]>>;

/**
 * Renders one button per available transition, or nothing when an item has reached a terminal
 * status. A register whose item is finished shows its badge and no buttons, rather than a
 * disabled row of actions that invites clicking.
 */
export function StatusActions<TStatus extends string>({
  status,
  transitions,
  onSelect,
  pending,
  labels,
}: {
  status: TStatus;
  transitions: TransitionMap<TStatus>;
  onSelect: (next: TStatus) => void;
  pending?: boolean;
  /** Overrides the button text where the status name alone is not a sensible verb. */
  labels?: Partial<Record<TStatus, string>>;
}) {
  const available = transitions[status] ?? [];
  if (available.length === 0) return null;

  return (
    <>
      {available.map((next) => (
        <Button
          key={next}
          size="sm"
          variant="outline"
          disabled={pending}
          onClick={() => onSelect(next)}
        >
          {labels?.[next] ?? next}
        </Button>
      ))}
    </>
  );
}
