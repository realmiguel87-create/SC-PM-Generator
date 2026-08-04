-- =============================================================================
-- Security schema — users, roles, RBAC
-- =============================================================================

CREATE TABLE Security.[User] (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_User_Id DEFAULT NEWSEQUENTIALID(),
    EntraObjectId   NVARCHAR(100)    NOT NULL,
    DisplayName     NVARCHAR(200)    NOT NULL,
    Email           NVARCHAR(256)    NOT NULL,
    JobTitle        NVARCHAR(200)    NULL,
    IsActive        BIT              NOT NULL CONSTRAINT DF_User_IsActive DEFAULT (1),
    CreatedDate     DATETIME2        NOT NULL CONSTRAINT DF_User_CreatedDate DEFAULT SYSUTCDATETIME(),
    ModifiedDate    DATETIME2        NULL,
    CONSTRAINT PK_User PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT UQ_User_EntraObjectId UNIQUE (EntraObjectId),
    CONSTRAINT UQ_User_Email UNIQUE (Email)
);
CREATE CLUSTERED INDEX CIX_User_Id ON Security.[User] (Id);
GO

-- Roles: Administrator, Director, Project Sponsor, Programme Manager, Project Manager,
--        Commercial Manager, Quantity Surveyor, Governance Officer, Committee Officer, Read Only User
CREATE TABLE Security.Role (
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Role_Id DEFAULT NEWSEQUENTIALID(),
    Name        NVARCHAR(100)    NOT NULL,
    Description NVARCHAR(500)    NULL,
    CONSTRAINT PK_Role PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT UQ_Role_Name UNIQUE (Name)
);
CREATE CLUSTERED INDEX CIX_Role_Id ON Security.Role (Id);
GO

CREATE TABLE Security.UserRole (
    Id       UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UserRole_Id DEFAULT NEWSEQUENTIALID(),
    UserId   UNIQUEIDENTIFIER NOT NULL,
    RoleId   UNIQUEIDENTIFIER NOT NULL,
    GrantedDate DATETIME2     NOT NULL CONSTRAINT DF_UserRole_GrantedDate DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_UserRole PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT FK_UserRole_User FOREIGN KEY (UserId) REFERENCES Security.[User] (Id),
    CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleId) REFERENCES Security.Role (Id),
    CONSTRAINT UQ_UserRole_User_Role UNIQUE (UserId, RoleId)
);
CREATE CLUSTERED INDEX CIX_UserRole_Id ON Security.UserRole (Id);
CREATE NONCLUSTERED INDEX IX_UserRole_UserId ON Security.UserRole (UserId);
GO

INSERT INTO Security.Role (Name, Description) VALUES
    (N'Administrator',     N'Full platform administration'),
    (N'Director',          N'Portfolio-wide oversight and approval'),
    (N'Project Sponsor',   N'Accountable owner for assigned project(s)'),
    (N'Programme Manager', N'Manages a capital programme (group of projects)'),
    (N'Project Manager',   N'Day-to-day delivery of assigned project(s)'),
    (N'Commercial Manager',N'NEC4/SBCC contract administration'),
    (N'Quantity Surveyor', N'Cost management and valuations'),
    (N'Governance Officer',N'Gateway/approval process administration'),
    (N'Committee Officer', N'Committee and cabinet reporting'),
    (N'Read Only User',    N'View-only access');
GO
