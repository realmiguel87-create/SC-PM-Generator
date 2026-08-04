-- =============================================================================
-- Audit schema — activity log + field-level audit
-- Populated exclusively via the EF Core SaveChanges interceptor
-- (SCPM.Infrastructure/Persistence/AuditSaveChangesInterceptor.cs) — never hand-written.
-- =============================================================================

CREATE TABLE Audit.ActivityLog (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ActivityLog_Id DEFAULT NEWSEQUENTIALID(),
    UserId          UNIQUEIDENTIFIER NULL,
    Action          NVARCHAR(30)     NOT NULL,
    EntityType      NVARCHAR(100)    NOT NULL,
    EntityId        UNIQUEIDENTIFIER NULL,
    OccurredAt      DATETIME2        NOT NULL CONSTRAINT DF_ActivityLog_OccurredAt DEFAULT SYSUTCDATETIME(),
    CorrelationId   NVARCHAR(50)     NULL,
    IpAddress       NVARCHAR(50)     NULL,
    CONSTRAINT PK_ActivityLog PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_ActivityLog_User FOREIGN KEY (UserId) REFERENCES Security.[User] (Id),
    CONSTRAINT CK_ActivityLog_Action CHECK (Action IN
        (N'Create', N'Update', N'Delete', N'Approve', N'Reject', N'GenerateReport', N'ExportFile', N'Login', N'Logout'))
);
CREATE CLUSTERED INDEX CIX_ActivityLog_Id ON Audit.ActivityLog (Id);
CREATE NONCLUSTERED INDEX IX_ActivityLog_Entity ON Audit.ActivityLog (EntityType, EntityId);
CREATE NONCLUSTERED INDEX IX_ActivityLog_OccurredAt ON Audit.ActivityLog (OccurredAt DESC);
GO

CREATE TABLE Audit.FieldAudit (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_FieldAudit_Id DEFAULT NEWSEQUENTIALID(),
    ActivityLogId  UNIQUEIDENTIFIER NOT NULL,
    EntityName     NVARCHAR(100)    NOT NULL,
    FieldName      NVARCHAR(100)    NOT NULL,
    OldValue       NVARCHAR(MAX)    NULL,
    NewValue       NVARCHAR(MAX)    NULL,
    ChangedBy      UNIQUEIDENTIFIER NULL,
    ChangedDate    DATETIME2        NOT NULL CONSTRAINT DF_FieldAudit_ChangedDate DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_FieldAudit PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_FieldAudit_ActivityLog FOREIGN KEY (ActivityLogId) REFERENCES Audit.ActivityLog (Id),
    CONSTRAINT FK_FieldAudit_ChangedByUser FOREIGN KEY (ChangedBy) REFERENCES Security.[User] (Id)
);
CREATE CLUSTERED INDEX CIX_FieldAudit_Id ON Audit.FieldAudit (Id);
CREATE NONCLUSTERED INDEX IX_FieldAudit_ActivityLogId ON Audit.FieldAudit (ActivityLogId);
GO
