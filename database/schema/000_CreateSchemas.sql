-- =============================================================================
-- SC-PM Platform — Schema creation
-- One SQL schema per bounded context (see docs/architecture.md §6)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Security')    EXEC('CREATE SCHEMA Security');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Projects')    EXEC('CREATE SCHEMA Projects');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Governance')  EXEC('CREATE SCHEMA Governance');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Cost')        EXEC('CREATE SCHEMA Cost');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Programme')   EXEC('CREATE SCHEMA Programme');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Risk')        EXEC('CREATE SCHEMA Risk');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Stakeholder') EXEC('CREATE SCHEMA Stakeholder');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Documents')   EXEC('CREATE SCHEMA Documents');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'NEC4')        EXEC('CREATE SCHEMA NEC4');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'SBCC')        EXEC('CREATE SCHEMA SBCC');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Reporting')   EXEC('CREATE SCHEMA Reporting');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Handover')    EXEC('CREATE SCHEMA Handover');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Audit')       EXEC('CREATE SCHEMA Audit');
GO
