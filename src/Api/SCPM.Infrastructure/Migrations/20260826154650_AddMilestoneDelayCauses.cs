using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneDelayCauses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MilestoneDelayCause",
                schema: "Programme",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DelayDays = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExtensionOfTimeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompensationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_MilestoneDelayCause", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestoneDelayCause_CompensationEvent_CompensationEventId",
                        column: x => x.CompensationEventId,
                        principalSchema: "NEC4",
                        principalTable: "CompensationEvent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MilestoneDelayCause_ExtensionOfTime_ExtensionOfTimeId",
                        column: x => x.ExtensionOfTimeId,
                        principalSchema: "SBCC",
                        principalTable: "ExtensionOfTime",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MilestoneDelayCause_Milestone_MilestoneId",
                        column: x => x.MilestoneId,
                        principalSchema: "Programme",
                        principalTable: "Milestone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "MilestoneDelayCause_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Programme")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneDelayCause_CompensationEventId",
                schema: "Programme",
                table: "MilestoneDelayCause",
                column: "CompensationEventId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneDelayCause_ExtensionOfTimeId",
                schema: "Programme",
                table: "MilestoneDelayCause",
                column: "ExtensionOfTimeId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneDelayCause_MilestoneId",
                schema: "Programme",
                table: "MilestoneDelayCause",
                column: "MilestoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MilestoneDelayCause",
                schema: "Programme")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "MilestoneDelayCause_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Programme")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
