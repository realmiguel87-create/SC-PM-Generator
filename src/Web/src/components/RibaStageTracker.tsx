import { cn } from "@/lib/utils";
import type { RibaStageInstance } from "@/features/projects/types";

const statusStyles: Record<RibaStageInstance["status"], string> = {
  NotStarted: "bg-purple-soft text-text-secondary border-border",
  InProgress: "bg-information text-white border-information",
  Complete: "bg-stirling-green text-white border-stirling-green",
  Gated: "bg-stirling-purple text-white border-stirling-purple",
};

export function RibaStageTracker({ stages }: { stages: RibaStageInstance[] }) {
  return (
    <ol className="flex flex-wrap gap-2">
      {stages
        .slice()
        .sort((a, b) => a.stageNumber - b.stageNumber)
        .map((stage) => (
          <li
            key={stage.id}
            className={cn(
              "flex min-w-[7.5rem] flex-col gap-0.5 rounded-md border px-3 py-2 text-xs",
              statusStyles[stage.status],
            )}
          >
            <span className="font-semibold">Stage {stage.stageNumber}</span>
            <span className="leading-tight">{stage.stageName}</span>
            <span className="mt-1 text-[10px] uppercase tracking-wide opacity-80">{stage.status}</span>
          </li>
        ))}
    </ol>
  );
}
