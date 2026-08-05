-- =============================================================================
-- usp_Governance_ApproveGateway
-- Atomically records a gateway decision, updates the gateway and (on approval)
-- advances the RIBA stage instance to Complete — all in one transaction, since
-- these three writes must never be partially applied.
-- Activity/field audit is written by the calling EF Core interceptor, not here;
-- this procedure is invoked via a raw ADO.NET call inside the same
-- DbContext-managed transaction so both participate in one commit/rollback.
-- =============================================================================
CREATE OR ALTER PROCEDURE Governance.usp_Governance_ApproveGateway
    @GatewayId       UNIQUEIDENTIFIER,
    @ApproverUserId  UNIQUEIDENTIFIER,
    @Decision        NVARCHAR(20),      -- Approved | Rejected | ApprovedWithConditions
    @Comments        NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Decision NOT IN (N'Approved', N'Rejected', N'ApprovedWithConditions')
        THROW 51000, 'Invalid decision value.', 1;

    BEGIN TRANSACTION;

    DECLARE @RibaStageInstanceId UNIQUEIDENTIFIER;

    SELECT @RibaStageInstanceId = RibaStageInstanceId
    FROM Governance.Gateway
    WHERE Id = @GatewayId AND IsDeleted = 0;

    IF @RibaStageInstanceId IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 51001, 'Gateway not found.', 1;
    END

    INSERT INTO Governance.Approval (GatewayId, ApproverUserId, Decision, Comments, DecisionDate, CreatedBy)
    VALUES (@GatewayId, @ApproverUserId, @Decision, @Comments, SYSUTCDATETIME(), @ApproverUserId);

    UPDATE Governance.Gateway
    SET Status = CASE WHEN @Decision = N'Rejected' THEN N'Rejected' ELSE N'Approved' END,
        ModifiedBy = @ApproverUserId,
        ModifiedDate = SYSUTCDATETIME()
    WHERE Id = @GatewayId;

    IF @Decision IN (N'Approved', N'ApprovedWithConditions')
    BEGIN
        UPDATE Projects.RibaStageInstance
        SET Status = N'Gated',
            ActualEndDate = CAST(SYSUTCDATETIME() AS DATE),
            ModifiedBy = @ApproverUserId,
            ModifiedDate = SYSUTCDATETIME()
        WHERE Id = @RibaStageInstanceId;
    END

    COMMIT TRANSACTION;
END
GO
