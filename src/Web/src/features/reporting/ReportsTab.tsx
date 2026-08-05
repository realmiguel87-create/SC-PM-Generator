import { useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import { exportReportUrl, useCommitteeReport, useCommitteeReports, useCreateCommitteeReport, useSubmitCommitteeReport, useUpdateCommitteeReport } from "./api";
import { REPORT_SECTIONS, type CommitteeReport, type CommitteeReportType, type ReportExportFormat } from "./types";

const REPORT_TYPES: CommitteeReportType[] = ["CommitteeReport", "CabinetReport", "BoardReport", "CapitalProgrammeReport", "DecisionPaper"];
const EXPORT_FORMATS: ReportExportFormat[] = ["Pdf", "Xlsx", "Csv", "Json"];

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
          <p className="text-xs text-text-secondary">{current.reportType} · {current.projectRef}{current.meetingDate ? ` · ${formatDate(current.meetingDate)}` : ""}</p>
        </div>
        <Badge variant={statusToBadgeVariant(current.status)}>{current.status}</Badge>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        {REPORT_SECTIONS.map(({ key, label }) => (
          <label key={key} className="flex flex-col gap-1 text-xs text-text-secondary">
            {label}
            <textarea
              rows={label === "Executive Summary" ? 3 : 2}
              disabled={!isEditable}
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm text-text-primary disabled:opacity-70"
              value={(current[key] as string) ?? ""}
              onChange={(e) => setDraft({ ...current, [key]: e.target.value })}
            />
          </label>
        ))}

        {isEditable && (
          <div className="flex gap-2">
            <Button
              size="sm"
              variant="secondary"
              disabled={!draft || update.isPending}
              onClick={() => draft && update.mutate(draft, { onSuccess: () => setDraft(null) })}
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

  if (isLoading) return <p className="text-sm text-text-secondary">Loading reports…</p>;
  if (isError || !reports) return <p className="text-sm text-critical">Could not load reports.</p>;

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="flex flex-col gap-4">
        <Card>
          <CardHeader><CardTitle>New Report</CardTitle></CardHeader>
          <CardContent className="flex flex-wrap items-end gap-2 pt-0">
            <select
              className="rounded-md border border-border bg-transparent px-2 py-1.5 text-sm"
              value={reportType}
              onChange={(e) => setReportType(e.target.value as CommitteeReportType)}
            >
              {REPORT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <input placeholder="Title" className="flex-1 min-w-[10rem] rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
            <Button
              size="sm"
              disabled={!title || createReport.isPending}
              onClick={() => createReport.mutate({ reportType, title }, { onSuccess: (id) => { setTitle(""); setSelectedId(id); } })}
            >
              {createReport.isPending ? "Generating…" : "Generate Draft"}
            </Button>
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
                    <div className="text-xs text-text-secondary">{r.reportType}</div>
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
