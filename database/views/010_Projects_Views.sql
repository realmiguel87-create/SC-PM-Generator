-- =============================================================================
-- Reporting views over the Projects/Governance schemas.
-- Report and Power BI queries read from views, never base tables directly.
-- =============================================================================

CREATE OR ALTER VIEW Projects.vw_ProjectSummary
AS
SELECT
    p.Id,
    p.ProjectRef,
    p.Name,
    p.Status,
    p.CurrentRibaStage,
    sd.StageName            AS CurrentRibaStageName,
    p.ApprovedBudget,
    p.ForecastCost,
    (p.ForecastCost - p.ApprovedBudget) AS ForecastVariance,
    p.StartDate,
    p.TargetCompletionDate,
    pr.Name                 AS ProgrammeName,
    sponsor.DisplayName     AS SponsorName,
    pm.DisplayName          AS ProjectManagerName
FROM Projects.Project p
LEFT JOIN Projects.Programme pr        ON pr.Id = p.ProgrammeId AND pr.IsDeleted = 0
LEFT JOIN Projects.RibaStageDefinition sd ON sd.StageNumber = p.CurrentRibaStage
LEFT JOIN Security.[User] sponsor      ON sponsor.Id = p.SponsorUserId
LEFT JOIN Security.[User] pm           ON pm.Id = p.ProjectManagerUserId
WHERE p.IsDeleted = 0;
GO

CREATE OR ALTER VIEW Projects.vw_ProjectsByRibaStage
AS
SELECT
    p.CurrentRibaStage,
    sd.StageName AS StageName,
    COUNT(*) AS ProjectCount,
    SUM(p.ApprovedBudget) AS TotalApprovedBudget,
    SUM(p.ForecastCost) AS TotalForecastCost
FROM Projects.Project p
JOIN Projects.RibaStageDefinition sd ON sd.StageNumber = p.CurrentRibaStage
WHERE p.IsDeleted = 0
GROUP BY p.CurrentRibaStage, sd.StageName;
GO

CREATE OR ALTER VIEW Governance.vw_UpcomingGateways
AS
SELECT
    g.Id AS GatewayId,
    g.ProjectId,
    p.ProjectRef,
    p.Name AS ProjectName,
    g.GatewayType,
    g.Status,
    g.DueDate
FROM Governance.Gateway g
JOIN Projects.Project p ON p.Id = g.ProjectId AND p.IsDeleted = 0
WHERE g.IsDeleted = 0 AND g.Status = N'Pending';
GO

CREATE OR ALTER VIEW Projects.vw_PortfolioSummary
AS
SELECT
    (SELECT COUNT(*) FROM Projects.Programme WHERE IsDeleted = 0)                         AS TotalProgrammes,
    (SELECT COUNT(*) FROM Projects.Project WHERE IsDeleted = 0)                            AS TotalProjects,
    (SELECT ISNULL(SUM(ApprovedBudget), 0) FROM Projects.Project WHERE IsDeleted = 0)      AS TotalCapitalValue,
    (SELECT ISNULL(SUM(ForecastCost), 0) FROM Projects.Project WHERE IsDeleted = 0)        AS TotalForecastCost,
    (SELECT COUNT(*) FROM Governance.Gateway WHERE IsDeleted = 0 AND Status = N'Pending')  AS OpenApprovals;
GO
