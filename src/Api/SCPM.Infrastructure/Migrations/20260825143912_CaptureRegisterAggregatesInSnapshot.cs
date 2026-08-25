using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CaptureRegisterAggregatesInSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CompensationEventValue",
                schema: "Reporting",
                table: "Snapshot",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExtensionOfTimeDaysAwarded",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HighRiskCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MilestoneCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MilestonesCompleteCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MilestonesDelayedCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenCompensationEventCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenEarlyWarningCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenIssueCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenRiskCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenVariationCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SevereOpenIssueCount",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalOpenRiskScore",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "VariationValue",
                schema: "Reporting",
                table: "Snapshot",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WorstMilestoneDelayDays",
                schema: "Reporting",
                table: "Snapshot",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationEventValue",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "ExtensionOfTimeDaysAwarded",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "HighRiskCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "MilestoneCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "MilestonesCompleteCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "MilestonesDelayedCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "OpenCompensationEventCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "OpenEarlyWarningCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "OpenIssueCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "OpenRiskCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "OpenVariationCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "SevereOpenIssueCount",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "TotalOpenRiskScore",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "VariationValue",
                schema: "Reporting",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "WorstMilestoneDelayDays",
                schema: "Reporting",
                table: "Snapshot");
        }
    }
}
