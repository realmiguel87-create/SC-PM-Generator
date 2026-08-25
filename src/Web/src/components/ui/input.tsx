import { forwardRef, type InputHTMLAttributes, type TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

const fieldStyles =
  "w-full rounded-md border border-border bg-card px-3 py-2 text-sm text-text-primary " +
  "placeholder:text-text-secondary focus-visible:outline-none focus-visible:ring-2 " +
  "focus-visible:ring-stirling-purple disabled:cursor-not-allowed disabled:opacity-50 " +
  "aria-[invalid=true]:border-critical aria-[invalid=true]:focus-visible:ring-critical";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input ref={ref} className={cn(fieldStyles, className)} {...props} />
  ),
);
Input.displayName = "Input";

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...props }, ref) => (
    <textarea ref={ref} className={cn(fieldStyles, "min-h-20 resize-y", className)} {...props} />
  ),
);
Textarea.displayName = "Textarea";

/**
 * Label plus optional inline validation message. The message is tied to the field via
 * aria-describedby and marked role="alert" so it reaches screen readers, not just sighted users —
 * a governance system for a public authority should meet that bar by default.
 */
export function Field({
  label,
  htmlFor,
  error,
  hint,
  required,
  children,
}: {
  label: string;
  htmlFor: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={htmlFor} className="text-xs font-medium text-text-secondary">
        {label}
        {required && <span className="ml-0.5 text-critical">*</span>}
      </label>
      {children}
      {hint && !error && <p className="text-[11px] text-text-secondary">{hint}</p>}
      {error && (
        <p id={`${htmlFor}-error`} role="alert" className="text-[11px] text-critical">
          {error}
        </p>
      )}
    </div>
  );
}
