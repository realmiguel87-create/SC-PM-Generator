import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { useProject } from "@/features/projects/api";
import { exportReportUrl, useCommitteeReport, useCommitteeReports, useCreateCommitteeReport, useSubmitCommitteeReport, useUpdateCommitteeReport } from "./api";
import type { CommitteeReport, CommitteeReportType, ReportExportFormat } from "./types";

/**
 * The report types, grouped as a reader meets them, with the names they are called by.
 *
 * Labelled rather than listed raw: "StageGateApproval" in a dropdown is a leaked identifier, the
 * same defect as printing "ApprovedWithConditions" on a council document. The value sent to the
 * server is still the enum name.
 */
const REPORT_GROUPS: { group: string; types: { value: CommitteeReportType; label: string }[] }[] = [
  {
    group: "Reporting",
    types: [
      { value: "StatusReport", label: "Programme/Project Status Report" },
      { value: "CommitteeReport", label: "Committee Paper" },
      { value: "CabinetReport", label: "Cabinet Report" },
      { value: "BoardReport", label: "Board Report" },
      { value: "CapitalProgrammeReport", label: "Capital Programme Report" },
      { value: "DecisionPaper", label: "Decision Paper" },
    ],
  },
  {
    group: "RIBA Stage 0 — Strategic Definition",
    types: [
      { value: "ProjectMandate", label: "Project Mandate" },
      { value: "StrategicBusinessCase", label: "Strategic Business Case" },
    ],
  },
  {
    group: "RIBA Stage 1 — Preparation and Briefing",
    types: [
      { value: "ProjectBrief", label: "Project Brief" },
      { value: "FeasibilityStudy", label: "Feasibility Study Report" },
      { value: "ProjectExecutionPlan", label: "Project Execution Plan" },
    ],
  },
  {
    group: "Governance",
    types: [{ value: "StageGateApproval", label: "Stage Gate Approval Report" }],
  },
];

const REPORT_LABELS = new Map(
  REPORT_GROUPS.flatMap((g) => g.types).map((t) => [t.value, t.label] as const),
);

export function reportTypeLabel(type: CommitteeReportType): string {
  // Falls back to the raw value rather than throwing: a type added to the API before this list
  // should still render, badly named rather than not at all.
  return REPORT_LABELS.get(type) ?? type;
}
// Order is deliberate: the two formats a committee officer actually edits and presents come
// first, then the data formats. Must stay in step with ReportExportFormat on the API.
const EXPORT_FORMATS: ReportExportFormat[] = ["Pdf", "Docx", "Pptx", "Xlsx", "Csv", "Json"];

function ReportEditor({ projectId, reportId }: { projectId: string; reportId: string }) {
  const { data: report, isLoading } = useCommitteeReport(reportId);
  const update = useUpdateCommitteeReport(projectId, reportId);
  const submit = useSubmitCommitteeReport(projectId);
  const [draft, setDraft] = useState<CommitteeReport | null>(null);

  const current = draft ?? report;
  if (isLoading || !current) return <p className="text-sm text-text-secondary">Loading report…</p>;

  const isEditable = current.status === "Draft";

  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between">
        <div>
          <CardTitle className="text-base font-semibold text-text-primary">{current.title}</CardTitle>
          <p className="text-xs text-text-secondary">{reportTypeLabel(current.reportType)} · {current.projectRef}{current.meetingDate ? ` · ${formatDate(current.meetingDate)}` : ""}</p>
        </div>
        <Badge variant={statusToBadgeVariant(current.status)}>{current.status}</Badge>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        {/* Headings come from the server, so a new report type needs no change here. */}
        {current.sections.map((section) => (
          <label key={section.key} className="flex flex-col gap-1 text-xs text-text-secondary">
            {section.heading}
            <textarea
              rows={4}
              disabled={!isEditable}
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm text-text-primary disabled:opacity-70"
              value={section.content ?? ""}
              onChange={(e) =>
                setDraft({
                  ...current,
                  sections: current.sections.map((s) =>
                    s.key === section.key ? { ...s, content: e.target.value } : s,
                  ),
                })
              }
            />
          </label>
        ))}

        {isEditable && (
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="secondary"
              disabled={!draft || update.isPending}
              onClick={() =>
                draft &&
                update.mutate(
                  {
                    // Only the sections, not the whole document. The server treats an omitted
                    // section as unchanged rather than as cleared.
                    sections: draft.sections.map((s) => ({ key: s.key, content: s.content })),
                    reportDate: draft.reportDate,
                  },
                  { onSuccess: () => setDraft(null) },
                )
              }
            >
              {update.isPending ? "Saving…" : "Save Draft"}
            </Button>
            <Button
              size="sm"
              disabled={submit.isPending}
              onClick={() => submit.mutate(reportId)}
            >
              {submit.isPending ? "Submitting…" : "Submit to Committee"}
            </Button>
          </div>
        )}

        <div className="flex flex-wrap gap-2 border-t border-border pt-3">
          <span className="text-xs text-text-secondary">Export:</span>
          {EXPORT_FORMATS.map((format) => (
            <a key={format} href={exportReportUrl(reportId, format)} target="_blank" rel="noreferrer">
              <Button size="sm" variant="outline">{format}</Button>
            </a>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

export function ReportsTab({ projectId }: { projectId: string }) {
  const { data: reports, isLoading, isError } = useCommitteeReports(projectId);
  const createReport = useCreateCommitteeReport(projectId);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [reportType, setReportType] = useState<CommitteeReportType>("CommitteeReport");
  const [gatewayId, setGatewayId] = useState("");

  // Only a gate report needs this, so the project is fetched for its stages and nothing else.
  const { data: project } = useProject(projectId);

  const gateways = (project?.ribaStages ?? [])
    .filter((s) => s.gatewayId)
    .map((s) => ({
      id: s.gatewayId!,
      label: `Stage ${s.stageNumber} — ${s.stageName}${s.gatewayStatus ? ` (${s.gatewayStatus})` : ""}`,
    }));

  const needsGateway = reportType === "StageGateApproval";

  if (isLoading) return <p className="text-sm text-text-secondary">Loading reports…</p>;
  if (isError || !reports) return <p className="text-sm text-critical">Could not load reports.</p>;

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="flex flex-col gap-4">
        <Card>
          <CardHeader><CardTitle>New Report</CardTitle></CardHeader>
          <CardContent className="flex flex-wrap items-end gap-2 pt-0">
            <label className="sr-only" htmlFor="report-type">Report type</label>
            <select
              id="report-type"
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={reportType}
              onChange={(e) => {
                setReportType(e.target.value as CommitteeReportType);
                // A gateway chosen for a gate report must not survive a switch to a type the
                // server rejects one on.
                setGatewayId("");
              }}
            >
              {REPORT_GROUPS.map((g) => (
                <optgroup key={g.group} label={g.group}>
                  {g.types.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                </optgroup>
              ))}
            </select>
            <label className="sr-only" htmlFor="report-title">Title</label>
            <input id="report-title" placeholder="Title" className="flex-1 min-w-[10rem] rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />

            {needsGateway ? (
              <>
                <label className="sr-only" htmlFor="report-gateway">Gateway</label>
                <select
                  id="report-gateway"
                  className="min-w-[14rem] rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
                  value={gatewayId}
                  onChange={(e) => setGatewayId(e.target.value)}
                  disabled={gateways.length === 0}
                >
                  <option value="">
                    {gateways.length === 0 ? "No gateways on this project" : "Select a gateway…"}
                  </option>
                  {gateways.map((g) => <option key={g.id} value={g.id}>{g.label}</option>)}
                </select>
              </>
            ) : null}

            <Button
              size="sm"
              // A gate report without a gateway is rejected by the server, so the button is held
              // rather than letting the request fail: the reason is visible here and would be a
              // validation message there.
              disabled={!title || createReport.isPending || (needsGateway && !gatewayId)}
              onClick={() =>
                createReport.mutate(
                  { reportType, title, gatewayId: needsGateway ? gatewayId : undefined },
                  { onSuccess: (id) => { setTitle(""); setGatewayId(""); setSelectedId(id); } },
                )
              }
            >
              {createReport.isPending ? "Generating…" : "Generate Draft"}
            </Button>

            {needsGateway && gateways.length === 0 ? (
              <p className="w-full text-xs text-text-secondary">
                A stage gate approval report is written against a gateway. Raise one on the
                Governance tab first.
              </p>
            ) : null}
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Reports</CardTitle></CardHeader>
          <CardContent className="flex flex-col gap-2 pt-0">
            {reports.length === 0 ? (
              <p className="text-sm text-text-secondary">No reports yet.</p>
            ) : (
              reports.map((r) => (
                <button
                  key={r.id}
                  onClick={() => setSelectedId(r.id)}
                  className={`flex items-center justify-between rounded-md border px-3 py-2 text-left text-sm transition-colors ${
                    selectedId === r.id ? "border-stirling-purple bg-purple-soft" : "border-border hover:bg-purple-soft"
                  }`}
                >
                  <div>
                    <div className="font-medium">{r.title}</div>
                    <div className="text-xs text-text-secondary">{reportTypeLabel(r.reportType)}</div>
                  </div>
                  <Badge variant={statusToBadgeVariant(r.status)}>{r.status}</Badge>
                </button>
              ))
            )}
          </CardContent>
        </Card>
      </div>

      <div>
        {selectedId ? (
          <ReportEditor projectId={projectId} reportId={selectedId} />
        ) : (
          <Card><CardContent className="pt-5 text-sm text-text-secondary">Select a report to view and edit it.</CardContent></Card>
        )}
      </div>
    </div>
  );
}
