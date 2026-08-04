import { cva, type VariantProps } from "class-variance-authority";
import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const badgeVariants = cva("inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium", {
  variants: {
    variant: {
      neutral: "bg-purple-soft text-stirling-purple",
      success: "bg-[color-mix(in_srgb,var(--success)_15%,transparent)] text-success",
      warning: "bg-[color-mix(in_srgb,var(--warning)_15%,transparent)] text-warning",
      critical: "bg-[color-mix(in_srgb,var(--critical)_15%,transparent)] text-critical",
      information: "bg-[color-mix(in_srgb,var(--information)_15%,transparent)] text-information",
    },
  },
  defaultVariants: { variant: "neutral" },
});

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />;
}

export function statusToBadgeVariant(status: string): BadgeProps["variant"] {
  switch (status) {
    case "Complete":
    case "Gated":
    case "Approved":
    case "Active":
      return "success";
    case "InProgress":
      return "information";
    case "OnHold":
    case "Pending":
      return "warning";
    case "Rejected":
    case "Cancelled":
      return "critical";
    default:
      return "neutral";
  }
}
