-- =============================================================================
-- Governance schema — stage gates and approvals
-- =============================================================================

CREATE TABLE Governance.Gateway (
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Gateway_Id DEFAULT NEWSEQUENTIALID(),
    ProjectId             UNIQUEIDENTIFIER NOT NULL,
    RibaStageInstanceId   UNIQUEIDENTIFIER NOT NULL,
    GatewayType           NVARCHAR(50)     NOT NULL,
    Status                NVARCHAR(30)     NOT NULL CONSTRAINT DF_Gateway_Status DEFAULT (N'Pending'),
    DueDate               DATE             NULL,
    IsDeleted             BIT              NOT NULL CONSTRAINT DF_Gateway_IsDeleted DEFAULT (0),
    DeletedDate           DATETIME2        NULL,
    DeletedBy             UNIQUEIDENTIFIER NULL,
    CreatedBy             UNIQUEIDENTIFIER NOT NULL,
    CreatedDate           DATETIME2        NOT NULL CONSTRAINT DF_Gateway_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy            UNIQUEIDENTIFIER NULL,
    ModifiedDate          DATETIME2        NULL,
    SysStartTime    DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime      DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime),
    CONSTRAINT PK_Gateway PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_Gateway_Project FOREIGN KEY (ProjectId) REFERENCES Projects.Project (Id),
    CONSTRAINT FK_Gateway_RibaStageInstance FOREIGN KEY (RibaStageInstanceId) REFERENCES Projects.RibaStageInstance (Id),
    CONSTRAINT CK_Gateway_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected', N'Withdrawn'))
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = Governance.Gateway_History));
CREATE CLUSTERED INDEX CIX_Gateway_Id ON Governance.Gateway (Id);
CREATE NONCLUSTERED INDEX IX_Gateway_ProjectId ON Governance.Gateway (ProjectId) WHERE IsDeleted = 0;
GO

CREATE TABLE Governance.Approval (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Approval_Id DEFAULT NEWSEQUENTIALID(),
    GatewayId      UNIQUEIDENTIFIER NOT NULL,
    ApproverUserId UNIQUEIDENTIFIER NOT NULL,
    Decision       NVARCHAR(20)     NOT NULL,
    Comments       NVARCHAR(MAX)    NULL,
    DecisionDate   DATETIME2        NOT NULL CONSTRAINT DF_Approval_DecisionDate DEFAULT SYSUTCDATETIME(),
    IsDeleted      BIT              NOT NULL CONSTRAINT DF_Approval_IsDeleted DEFAULT (0),
    DeletedDate    DATETIME2        NULL,
    DeletedBy      UNIQUEIDENTIFIER NULL,
    CreatedBy      UNIQUEIDENTIFIER NOT NULL,
    CreatedDate    DATETIME2        NOT NULL CONSTRAINT DF_Approval_CreatedDate DEFAULT SYSUTCDATETIME(),
    SysStartTime    DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime      DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime),
    CONSTRAINT PK_Approval PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_Approval_Gateway FOREIGN KEY (GatewayId) REFERENCES Governance.Gateway (Id),
    CONSTRAINT FK_Approval_ApproverUser FOREIGN KEY (ApproverUserId) REFERENCES Security.[User] (Id),
    CONSTRAINT CK_Approval_Decision CHECK (Decision IN (N'Approved', N'Rejected', N'ApprovedWithConditions'))
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = Governance.Approval_History));
CREATE CLUSTERED INDEX CIX_Approval_Id ON Governance.Approval (Id);
CREATE NONCLUSTERED INDEX IX_Approval_GatewayId ON Governance.Approval (GatewayId) WHERE IsDeleted = 0;
GO
