using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeBaselines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgrammeBaseline",
                schema: "Programme",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeBaseline", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammeBaseline_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "Projects",
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProgrammeBaseline_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Programme")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "ProgrammeBaselineEntry",
                schema: "Programme",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgrammeBaselineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaselineDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeBaselineEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgrammeBaselineEntry_Milestone_MilestoneId",
                        column: x => x.MilestoneId,
                        principalSchema: "Programme",
                        principalTable: "Milestone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammeBaselineEntry_ProgrammeBaseline_ProgrammeBaselineId",
                        column: x => x.ProgrammeBaselineId,
                        principalSchema: "Programme",
                        principalTable: "ProgrammeBaseline",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeBaseline_ProjectId_Revision",
                schema: "Programme",
                table: "ProgrammeBaseline",
                columns: new[] { "ProjectId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProgrammeBaseline_CurrentPerProject",
                schema: "Programme",
                table: "ProgrammeBaseline",
                column: "ProjectId",
                unique: true,
                filter: "[IsCurrent] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeBaselineEntry_MilestoneId",
                schema: "Programme",
                table: "ProgrammeBaselineEntry",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeBaselineEntry_ProgrammeBaselineId_MilestoneId",
                schema: "Programme",
                table: "ProgrammeBaselineEntry",
                columns: new[] { "ProgrammeBaselineId", "MilestoneId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgrammeBaselineEntry",
                schema: "Programme");

            migrationBuilder.DropTable(
                name: "ProgrammeBaseline",
                schema: "Programme")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ProgrammeBaseline_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Programme")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
