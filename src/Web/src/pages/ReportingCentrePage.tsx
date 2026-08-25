import { Link } from "react-router-dom";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ApiErrorNotice } from "@/components/ApiErrorNotice";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { useCommitteeReports } from "@/features/reporting/api";
import { formatDate } from "@/lib/utils";

export function ReportingCentrePage() {
  const { data: reports, isLoading, isError, error } = useCommitteeReports();

  return (
    <div className="flex flex-col gap-6 p-6">
      <header>
        <h1 className="text-xl font-semibold">Reporting Centre</h1>
        <p className="text-sm text-text-secondary">Committee, cabinet and board reports across the portfolio.</p>
      </header>

      {isLoading && <p className="text-sm text-text-secondary">Loading reports…</p>}
      {isError && <ApiErrorNotice error={error} />}

      {reports && reports.length === 0 && (
        <Card><CardContent className="pt-5 text-sm text-text-secondary">
          No reports yet. Generate one from a project's Reports tab.
        </CardContent></Card>
      )}

      <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
        {reports?.map((r) => (
          <Card key={r.id}>
            <CardHeader className="flex-row items-start justify-between">
              <div>
                <CardTitle className="text-sm font-semibold text-text-primary">{r.title}</CardTitle>
                <p className="text-xs text-text-secondary">{r.reportType} · {r.projectRef} — {r.projectName}</p>
              </div>
              <Badge variant={statusToBadgeVariant(r.status)}>{r.status}</Badge>
            </CardHeader>
            <CardContent className="flex items-center justify-between pt-0">
              <span className="text-xs text-text-secondary">
                {r.meetingDate ? `Meeting: ${formatDate(r.meetingDate)}` : "No meeting date set"}
              </span>
              <Link to={`/projects/${r.projectId}`} className="text-xs font-medium text-stirling-purple hover:underline">
                Open in project →
              </Link>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
