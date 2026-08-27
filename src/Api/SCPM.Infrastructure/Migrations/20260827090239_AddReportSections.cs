using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReportSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Order matters. EF scaffolded the ten DropColumn calls first, which would have
            // discarded every existing report's narrative before there was anywhere to put it.
            // The table is created, the content copied across, and only then are the columns
            // dropped.











            migrationBuilder.AddColumn<DateOnly>(
                name: "ReportDate",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommitteeReportSection",
                schema: "Reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeReportSection", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeReportSection_CommitteeReport_CommitteeReportId",
                        column: x => x.CommitteeReportId,
                        principalSchema: "Reporting",
                        principalTable: "CommitteeReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CommitteeReportSection_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Reporting")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeReportSection_CommitteeReportId_SectionKey",
                schema: "Reporting",
                table: "CommitteeReportSection",
                columns: new[] { "CommitteeReportId", "SectionKey" },
                unique: true);

            // Carry existing reports over. Written as raw SQL because a data migration has to run
            // against the schema as it stands at this point in the history, and the EF model has
            // already moved on — querying through the model here would look for a table that does
            // not exist yet in any database this migration has not finished running against.
            //
            // Empty and whitespace-only values are skipped: a section stored as an empty string
            // and one never written read identically in the document, and carrying blanks across
            // would create rows that mean nothing.
            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'executive-summary', [ExecutiveSummary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [ExecutiveSummary] IS NOT NULL AND LTRIM(RTRIM([ExecutiveSummary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'background', [Background], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [Background] IS NOT NULL AND LTRIM(RTRIM([Background])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'current-position', [CurrentPosition], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [CurrentPosition] IS NOT NULL AND LTRIM(RTRIM([CurrentPosition])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'finance-commentary', [FinanceCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [FinanceCommentary] IS NOT NULL AND LTRIM(RTRIM([FinanceCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'programme-commentary', [ProgrammeCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [ProgrammeCommentary] IS NOT NULL AND LTRIM(RTRIM([ProgrammeCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'risk-commentary', [RiskCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [RiskCommentary] IS NOT NULL AND LTRIM(RTRIM([RiskCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'stakeholder-commentary', [StakeholderCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [StakeholderCommentary] IS NOT NULL AND LTRIM(RTRIM([StakeholderCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'sustainability-commentary', [SustainabilityCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [SustainabilityCommentary] IS NOT NULL AND LTRIM(RTRIM([SustainabilityCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'equality-commentary', [EqualityImpactCommentary], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [EqualityImpactCommentary] IS NOT NULL AND LTRIM(RTRIM([EqualityImpactCommentary])) <> '';");

            migrationBuilder.Sql(@"
                INSERT INTO [Reporting].[CommitteeReportSection]
                    ([Id], [CommitteeReportId], [SectionKey], [Content], [CreatedBy], [CreatedDate])
                SELECT NEWID(), [Id], 'recommendations', [Recommendations], [CreatedBy], [CreatedDate]
                FROM [Reporting].[CommitteeReport]
                WHERE [Recommendations] IS NOT NULL AND LTRIM(RTRIM([Recommendations])) <> '';");

            // Safe now that the content is copied.
            migrationBuilder.DropColumn(
                name: "Background",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "CurrentPosition",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "EqualityImpactCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "ExecutiveSummary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "FinanceCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "ProgrammeCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "Recommendations",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "RiskCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "StakeholderCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
            migrationBuilder.DropColumn(
                name: "SustainabilityCommentary",
                schema: "Reporting",
                table: "CommitteeReport");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitteeReportSection",
                schema: "Reporting")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "CommitteeReportSection_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Reporting")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropColumn(
                name: "ReportDate",
                schema: "Reporting",
                table: "CommitteeReport");

            migrationBuilder.AddColumn<string>(
                name: "Background",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentPosition",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EqualityImpactCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveSummary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FinanceCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgrammeCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StakeholderCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SustainabilityCommentary",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
