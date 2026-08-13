export type DocumentVersionStatus = "Draft" | "Review" | "Approved" | "Superseded" | "Archived" | "Rejected";

export interface DocumentListItem {
  id: string;
  title: string;
  category: string;
  ribaStageNumber: number | null;
  latestVersionLabel: string;
  latestVersionStatus: DocumentVersionStatus;
}

export interface DocumentFile {
  id: string;
  fileType: string;
  category: string;
  fileName: string;
  storageUrl: string | null;
  blobArchiveUrl: string | null;
  sizeBytes: number;
  createdDate: string;
}

export interface DocumentVersion {
  id: string;
  versionLabel: string;
  status: DocumentVersionStatus;
  createdDate: string;
  files: DocumentFile[];
}

export interface DocumentDetail extends DocumentListItem {
  versions: DocumentVersion[];
}
