import { useState, type FormEvent } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Field, Input, Textarea } from "@/components/ui/input";
import { ApiError } from "@/lib/api-client";
import { useCreateProject } from "./api";
import type { CreateProjectRequest } from "./types";

/**
 * Client-side rules deliberately mirror CreateProjectCommandValidator on the server
 * (SCPM.Application/Projects/Commands/CreateProject). The server remains the authority — this is
 * about telling someone their project reference is too long before a round trip, not about
 * trusting the browser. Keep the two in step: a limit tightened server-side and not reflected
 * here degrades to an opaque 400.
 */
const PROJECT_REF_MAX = 20;
const NAME_MAX = 200;

type Errors = Partial<Record<keyof CreateProjectRequest, string>>;

function validate(values: CreateProjectRequest): Errors {
  const errors: Errors = {};

  if (!values.projectRef.trim()) errors.projectRef = "Project reference is required.";
  else if (values.projectRef.length > PROJECT_REF_MAX)
    errors.projectRef = `Must be ${PROJECT_REF_MAX} characters or fewer.`;

  if (!values.name.trim()) errors.name = "Project name is required.";
  else if (values.name.length > NAME_MAX)
    errors.name = `Must be ${NAME_MAX} characters or fewer.`;

  if (Number.isNaN(values.approvedBudget)) errors.approvedBudget = "Enter a number.";
  else if (values.approvedBudget < 0) errors.approvedBudget = "Cannot be negative.";

  if (
    values.startDate &&
    values.targetCompletionDate &&
    values.targetCompletionDate < values.startDate
  ) {
    errors.targetCompletionDate = "Must be on or after the start date.";
  }

  return errors;
}

const EMPTY: CreateProjectRequest = {
  projectRef: "",
  name: "",
  description: "",
  approvedBudget: 0,
  startDate: "",
  targetCompletionDate: "",
};

export function NewProjectForm({ onClose }: { onClose: () => void }) {
  const [values, setValues] = useState<CreateProjectRequest>(EMPTY);
  const [errors, setErrors] = useState<Errors>({});
  const createProject = useCreateProject();

  const set = <K extends keyof CreateProjectRequest>(key: K, value: CreateProjectRequest[K]) =>
    setValues((current) => ({ ...current, [key]: value }));

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const found = validate(values);
    setErrors(found);
    if (Object.keys(found).length > 0) return;

    // Optional fields are sent as undefined rather than "" — the API binds empty strings for
    // dates to a validation failure rather than to null.
    createProject.mutate(
      {
        ...values,
        description: values.description?.trim() || undefined,
        startDate: values.startDate || undefined,
        targetCompletionDate: values.targetCompletionDate || undefined,
      },
      { onSuccess: onClose },
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>New project</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
            <Field
              label="Project reference"
              htmlFor="projectRef"
              required
              error={errors.projectRef}
              hint={`Unique identifier, up to ${PROJECT_REF_MAX} characters.`}
            >
              <Input
                id="projectRef"
                value={values.projectRef}
                onChange={(e) => set("projectRef", e.target.value)}
                aria-invalid={!!errors.projectRef}
                aria-describedby={errors.projectRef ? "projectRef-error" : undefined}
                placeholder="CP-2026-001"
              />
            </Field>

            <Field label="Project name" htmlFor="name" required error={errors.name}>
              <Input
                id="name"
                value={values.name}
                onChange={(e) => set("name", e.target.value)}
                aria-invalid={!!errors.name}
                aria-describedby={errors.name ? "name-error" : undefined}
                placeholder="Stirling Community Campus"
              />
            </Field>
          </div>

          <Field label="Description" htmlFor="description">
            <Textarea
              id="description"
              value={values.description ?? ""}
              onChange={(e) => set("description", e.target.value)}
              placeholder="Scope and purpose of the project."
            />
          </Field>

          <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
            <Field
              label="Approved budget (£)"
              htmlFor="approvedBudget"
              required
              error={errors.approvedBudget}
            >
              <Input
                id="approvedBudget"
                type="number"
                min="0"
                step="1000"
                value={Number.isNaN(values.approvedBudget) ? "" : values.approvedBudget}
                onChange={(e) => set("approvedBudget", e.target.valueAsNumber)}
                aria-invalid={!!errors.approvedBudget}
                aria-describedby={errors.approvedBudget ? "approvedBudget-error" : undefined}
              />
            </Field>

            <Field label="Start date" htmlFor="startDate">
              <Input
                id="startDate"
                type="date"
                value={values.startDate ?? ""}
                onChange={(e) => set("startDate", e.target.value)}
              />
            </Field>

            <Field
              label="Target completion"
              htmlFor="targetCompletionDate"
              error={errors.targetCompletionDate}
            >
              <Input
                id="targetCompletionDate"
                type="date"
                value={values.targetCompletionDate ?? ""}
                onChange={(e) => set("targetCompletionDate", e.target.value)}
                aria-invalid={!!errors.targetCompletionDate}
                aria-describedby={
                  errors.targetCompletionDate ? "targetCompletionDate-error" : undefined
                }
              />
            </Field>
          </div>

          {createProject.isError && (
            <p role="alert" className="text-xs text-critical">
              {/* Status, not a regex over the message — see ApiError in lib/api-client.ts. */}
              {createProject.error instanceof ApiError && createProject.error.status === 403
                ? "Your account does not have permission to create projects. This requires a role in the CanWrite policy."
                : `Could not create the project: ${
                    createProject.error instanceof Error
                      ? createProject.error.message
                      : String(createProject.error)
                  }`}
            </p>
          )}

          <div className="flex items-center gap-2">
            <Button type="submit" disabled={createProject.isPending}>
              {createProject.isPending ? "Creating…" : "Create project"}
            </Button>
            <Button type="button" variant="ghost" onClick={onClose}>
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
