-- =============================================================================
-- Projects schema — portfolio programmes, projects, RIBA stage progression
-- Temporal + soft-delete on governance-critical tables (Programme, Project, RibaStageInstance)
-- =============================================================================

CREATE TABLE Projects.Programme (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Programme_Id DEFAULT NEWSEQUENTIALID(),
    Name            NVARCHAR(200)    NOT NULL,
    Description     NVARCHAR(MAX)    NULL,
    CapitalValue    DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Programme_CapitalValue DEFAULT (0),
    SponsorUserId   UNIQUEIDENTIFIER NULL,
    IsDeleted       BIT              NOT NULL CONSTRAINT DF_Programme_IsDeleted DEFAULT (0),
    DeletedDate     DATETIME2        NULL,
    DeletedBy       UNIQUEIDENTIFIER NULL,
    CreatedBy       UNIQUEIDENTIFIER NOT NULL,
    CreatedDate     DATETIME2        NOT NULL CONSTRAINT DF_Programme_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy      UNIQUEIDENTIFIER NULL,
    ModifiedDate    DATETIME2        NULL,
    SysStartTime    DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime      DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime),
    CONSTRAINT PK_Programme PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_Programme_SponsorUser FOREIGN KEY (SponsorUserId) REFERENCES Security.[User] (Id)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = Projects.Programme_History));
CREATE CLUSTERED INDEX CIX_Programme_Id ON Projects.Programme (Id);
GO

CREATE TABLE Projects.RibaStageDefinition (
    StageNumber TINYINT       NOT NULL,
    StageName   NVARCHAR(100) NOT NULL,
    Description NVARCHAR(1000) NULL,
    CONSTRAINT PK_RibaStageDefinition PRIMARY KEY CLUSTERED (StageNumber)
);
GO

INSERT INTO Projects.RibaStageDefinition (StageNumber, StageName, Description) VALUES
    (0, N'Strategic Definition',        N'Identify the need, establish the business case and project brief.'),
    (1, N'Preparation and Brief',       N'Develop project objectives, quality objectives and project outcomes.'),
    (2, N'Concept Design',              N'Prepare concept design aligned to project brief.'),
    (3, N'Spatial Coordination',        N'Undertake technical design coordination and spatial coordination.'),
    (4, N'Technical Design',            N'Develop technical design in accordance with design responsibility matrix.'),
    (5, N'Manufacturing and Construction', N'Manufacture building systems and construct on site.'),
    (6, N'Handover',                    N'Conclude building contract, handover of building and asset information.'),
    (7, N'Use',                         N'Undertake in-use services in accordance with schedule of services.');
GO

CREATE TABLE Projects.Project (
    Id                      UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Project_Id DEFAULT NEWSEQUENTIALID(),
    ProgrammeId             UNIQUEIDENTIFIER NULL,
    ProjectRef              NVARCHAR(20)     NOT NULL,
    Name                    NVARCHAR(200)    NOT NULL,
    Description             NVARCHAR(MAX)    NULL,
    CurrentRibaStage        TINYINT          NOT NULL CONSTRAINT DF_Project_CurrentRibaStage DEFAULT (0),
    Status                  NVARCHAR(30)     NOT NULL CONSTRAINT DF_Project_Status DEFAULT (N'Active'),
    ApprovedBudget          DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Project_ApprovedBudget DEFAULT (0),
    ForecastCost            DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Project_ForecastCost DEFAULT (0),
    StartDate               DATE             NULL,
    TargetCompletionDate    DATE             NULL,
    SponsorUserId           UNIQUEIDENTIFIER NULL,
    ProjectManagerUserId    UNIQUEIDENTIFIER NULL,
    IsDeleted               BIT              NOT NULL CONSTRAINT DF_Project_IsDeleted DEFAULT (0),
    DeletedDate             DATETIME2        NULL,
    DeletedBy               UNIQUEIDENTIFIER NULL,
    CreatedBy               UNIQUEIDENTIFIER NOT NULL,
    CreatedDate              DATETIME2        NOT NULL CONSTRAINT DF_Project_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy              UNIQUEIDENTIFIER NULL,
    ModifiedDate            DATETIME2        NULL,
    SysStartTime    DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime      DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime),
    CONSTRAINT PK_Project PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT UQ_Project_ProjectRef UNIQUE (ProjectRef),
    CONSTRAINT FK_Project_Programme FOREIGN KEY (ProgrammeId) REFERENCES Projects.Programme (Id),
    CONSTRAINT FK_Project_RibaStage FOREIGN KEY (CurrentRibaStage) REFERENCES Projects.RibaStageDefinition (StageNumber),
    CONSTRAINT FK_Project_SponsorUser FOREIGN KEY (SponsorUserId) REFERENCES Security.[User] (Id),
    CONSTRAINT FK_Project_ProjectManagerUser FOREIGN KEY (ProjectManagerUserId) REFERENCES Security.[User] (Id),
    CONSTRAINT CK_Project_Status CHECK (Status IN (N'Active', N'OnHold', N'Closed', N'Cancelled'))
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = Projects.Project_History));
CREATE CLUSTERED INDEX CIX_Project_Id ON Projects.Project (Id);
CREATE NONCLUSTERED INDEX IX_Project_ProgrammeId ON Projects.Project (ProgrammeId) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_Project_Status ON Projects.Project (Status) WHERE IsDeleted = 0;
GO

CREATE TABLE Projects.RibaStageInstance (
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RibaStageInstance_Id DEFAULT NEWSEQUENTIALID(),
    ProjectId         UNIQUEIDENTIFIER NOT NULL,
    StageNumber       TINYINT          NOT NULL,
    Status            NVARCHAR(30)     NOT NULL CONSTRAINT DF_RibaStageInstance_Status DEFAULT (N'NotStarted'),
    PlannedStartDate  DATE             NULL,
    PlannedEndDate    DATE             NULL,
    ActualStartDate   DATE             NULL,
    ActualEndDate     DATE             NULL,
    IsDeleted         BIT              NOT NULL CONSTRAINT DF_RibaStageInstance_IsDeleted DEFAULT (0),
    DeletedDate       DATETIME2        NULL,
    DeletedBy         UNIQUEIDENTIFIER NULL,
    CreatedBy         UNIQUEIDENTIFIER NOT NULL,
    CreatedDate       DATETIME2        NOT NULL CONSTRAINT DF_RibaStageInstance_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy        UNIQUEIDENTIFIER NULL,
    ModifiedDate      DATETIME2        NULL,
    SysStartTime    DATETIME2 GENERATED ALWAYS AS ROW START NOT NULL,
    SysEndTime      DATETIME2 GENERATED ALWAYS AS ROW END NOT NULL,
    PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime),
    CONSTRAINT PK_RibaStageInstance PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_RibaStageInstance_Project FOREIGN KEY (ProjectId) REFERENCES Projects.Project (Id),
    CONSTRAINT FK_RibaStageInstance_StageDef FOREIGN KEY (StageNumber) REFERENCES Projects.RibaStageDefinition (StageNumber),
    CONSTRAINT UQ_RibaStageInstance_Project_Stage UNIQUE (ProjectId, StageNumber),
    CONSTRAINT CK_RibaStageInstance_Status CHECK (Status IN (N'NotStarted', N'InProgress', N'Complete', N'Gated'))
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = Projects.RibaStageInstance_History));
CREATE CLUSTERED INDEX CIX_RibaStageInstance_Id ON Projects.RibaStageInstance (Id);
CREATE NONCLUSTERED INDEX IX_RibaStageInstance_ProjectId ON Projects.RibaStageInstance (ProjectId) WHERE IsDeleted = 0;
GO

CREATE TABLE Projects.ProjectMember (
    Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProjectMember_Id DEFAULT NEWSEQUENTIALID(),
    ProjectId  UNIQUEIDENTIFIER NOT NULL,
    UserId     UNIQUEIDENTIFIER NOT NULL,
    RoleId     UNIQUEIDENTIFIER NOT NULL,
    AddedDate  DATETIME2        NOT NULL CONSTRAINT DF_ProjectMember_AddedDate DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_ProjectMember PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_ProjectMember_Project FOREIGN KEY (ProjectId) REFERENCES Projects.Project (Id),
    CONSTRAINT FK_ProjectMember_User FOREIGN KEY (UserId) REFERENCES Security.[User] (Id),
    CONSTRAINT FK_ProjectMember_Role FOREIGN KEY (RoleId) REFERENCES Security.Role (Id),
    CONSTRAINT UQ_ProjectMember_Project_User_Role UNIQUE (ProjectId, UserId, RoleId)
);
CREATE CLUSTERED INDEX CIX_ProjectMember_Id ON Projects.ProjectMember (Id);
CREATE NONCLUSTERED INDEX IX_ProjectMember_ProjectId ON Projects.ProjectMember (ProjectId);
GO
