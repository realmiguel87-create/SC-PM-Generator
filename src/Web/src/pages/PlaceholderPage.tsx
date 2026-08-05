import { Card, CardContent } from "@/components/ui/card";

export function PlaceholderPage({ title, phase }: { title: string; phase: string }) {
  return (
    <div className="flex flex-col gap-4 p-6">
      <h1 className="text-xl font-semibold">{title}</h1>
      <Card>
        <CardContent className="pt-5 text-sm text-text-secondary">
          {title} is scheduled for {phase}. See <code>docs/roadmap.md</code> for the delivery sequence.
        </CardContent>
      </Card>
    </div>
  );
}
