import type { ReactNode } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";

interface StatTileProps {
  label: string;
  value: string;
  icon?: ReactNode;
  accent?: "purple" | "green" | "warning" | "critical" | "information";
}

const accentClass: Record<NonNullable<StatTileProps["accent"]>, string> = {
  purple: "text-stirling-purple",
  green: "text-stirling-green",
  warning: "text-warning",
  critical: "text-critical",
  information: "text-information",
};

export function StatTile({ label, value, icon, accent = "purple" }: StatTileProps) {
  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between pb-1">
        <CardTitle>{label}</CardTitle>
        {icon && <span className={cn("opacity-80", accentClass[accent])}>{icon}</span>}
      </CardHeader>
      <CardContent className="pt-0">
        <p className={cn("text-2xl font-semibold", accentClass[accent])}>{value}</p>
      </CardContent>
    </Card>
  );
}
