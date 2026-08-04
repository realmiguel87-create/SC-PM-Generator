import { useRef, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge, statusToBadgeVariant } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/utils";
import {
  useApproveVersion, useArchiveVersion, useCreateDocument, useCreateDraftRevision,
  useAddDocumentFile, useDocument, useDocuments, useRejectVersion,
} from "./api";

function DocumentDetailPanel({ projectId, documentId }: { projectId: string; documentId: string }) {
  const { data: document, isLoading } = useDocument(documentId);
  const createRevision = useCreateDraftRevision(projectId);
  const approve = useApproveVersion(projectId, documentId);
  const reject = useRejectVersion(projectId, documentId);
  const archive = useArchiveVersion(projectId, documentId);
  const addFile = useAddDocumentFile(projectId, documentId);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadTargetVersionId, setUploadTargetVersionId] = useState<string | null>(null);

  if (isLoading || !document) return <p className="text-sm text-text-secondary">Loading document…</p>;

  return (
    <Card>
      <CardHeader>
        <CardTitle>{document.title}</CardTitle>
        <p className="text-xs text-text-secondary">{document.category}{document.ribaStageNumber != null ? ` · Stage ${document.ribaStageNumber}` : ""}</p>
      </CardHeader>
      <CardContent className="flex flex-col gap-3 pt-0">
        <Button size="sm" variant="secondary" disabled={createRevision.isPending} onClick={() => createRevision.mutate(documentId)}>
          {createRevision.isPending ? "Adding…" : "New Draft Revision"}
        </Button>

        <input
          ref={fileInputRef}
          type="file"
          className="hidden"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file && uploadTargetVersionId) {
              addFile.mutate({ documentVersionId: uploadTargetVersionId, fileType: file.name.split(".").pop() ?? "bin", category: document.category, file });
            }
            e.target.value = "";
          }}
        />

        {document.versions.map((v) => (
          <div key={v.id} className="rounded-md border border-border p-3">
            <div className="flex items-center justify-between">
              <span className="font-semibold">v{v.versionLabel}</span>
              <div className="flex items-center gap-2">
                <Badge variant={statusToBadgeVariant(v.status)}>{v.status}</Badge>
                <span className="text-xs text-text-secondary">{formatDate(v.createdDate)}</span>
              </div>
            </div>

            {v.files.length > 0 && (
              <ul className="mt-2 flex flex-col gap-1 text-xs text-text-secondary">
                {v.files.map((f) => (
                  <li key={f.id}>
                    {f.fileName} ({f.fileType}){f.blobArchiveUrl ? " — archived" : ""}
                  </li>
                ))}
              </ul>
            )}

            <div className="mt-2 flex flex-wrap gap-2">
              <Button
                size="sm"
                variant="outline"
                disabled={addFile.isPending}
                onClick={() => { setUploadTargetVersionId(v.id); fileInputRef.current?.click(); }}
              >
                Upload File
              </Button>
              {(v.status === "Draft" || v.status === "Review") && (
                <>
                  <Button size="sm" disabled={approve.isPending} onClick={() => approve.mutate(v.id)}>Approve</Button>
                  <Button size="sm" variant="outline" disabled={reject.isPending} onClick={() => reject.mutate(v.id)}>Reject</Button>
                </>
              )}
              {(v.status === "Superseded" || v.status === "Rejected") && (
                <Button size="sm" variant="outline" disabled={archive.isPending} onClick={() => archive.mutate(v.id)}>Archive</Button>
              )}
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

export function DocumentsTab({ projectId }: { projectId: string }) {
  const { data: documents, isLoading, isError } = useDocuments(projectId);
  const createDocument = useCreateDocument(projectId);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [title, setTitle] = useState("");
  const [category, setCategory] = useState("");

  if (isLoading) return <p className="text-sm text-text-secondary">Loading documents…</p>;
  if (isError || !documents) return <p className="text-sm text-critical">Could not load documents.</p>;

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <div className="flex flex-col gap-4">
        <Card>
          <CardHeader><CardTitle>New Document</CardTitle></CardHeader>
          <CardContent className="flex flex-wrap items-end gap-2 pt-0">
            <input placeholder="Title" className="flex-1 min-w-[10rem] rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={title} onChange={(e) => setTitle(e.target.value)} />
            <input placeholder="Category" className="w-32 rounded-md border border-border bg-transparent px-2 py-1.5 text-sm" value={category} onChange={(e) => setCategory(e.target.value)} />
            <Button
              size="sm"
              disabled={!title || !category || createDocument.isPending}
              onClick={() => createDocument.mutate({ title, category }, { onSuccess: (id) => { setTitle(""); setCategory(""); setSelectedId(id); } })}
            >
              {createDocument.isPending ? "Creating…" : "Create"}
            </Button>
          </CardContent>
        </Card>

        <Card>
          <CardHeader><CardTitle>Documents</CardTitle></CardHeader>
          <CardContent className="flex flex-col gap-2 pt-0">
            {documents.length === 0 ? (
              <p className="text-sm text-text-secondary">No documents yet.</p>
            ) : (
              documents.map((d) => (
                <button
                  key={d.id}
                  onClick={() => setSelectedId(d.id)}
                  className={`flex items-center justify-between rounded-md border px-3 py-2 text-left text-sm transition-colors ${
                    selectedId === d.id ? "border-stirling-purple bg-purple-soft" : "border-border hover:bg-purple-soft"
                  }`}
                >
                  <div>
                    <div className="font-medium">{d.title}</div>
                    <div className="text-xs text-text-secondary">{d.category}</div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-text-secondary">v{d.latestVersionLabel}</span>
                    <Badge variant={statusToBadgeVariant(d.latestVersionStatus)}>{d.latestVersionStatus}</Badge>
                  </div>
                </button>
              ))
            )}
          </CardContent>
        </Card>
      </div>

      <div>
        {selectedId ? (
          <DocumentDetailPanel projectId={projectId} documentId={selectedId} />
        ) : (
          <Card><CardContent className="pt-5 text-sm text-text-secondary">Select a document to see its version history.</CardContent></Card>
        )}
      </div>
    </div>
  );
}
