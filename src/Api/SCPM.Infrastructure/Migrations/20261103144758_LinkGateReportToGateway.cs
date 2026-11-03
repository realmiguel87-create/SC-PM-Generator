using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkGateReportToGateway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GatewayId",
                schema: "Reporting",
                table: "CommitteeReport",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeReport_GatewayId",
                schema: "Reporting",
                table: "CommitteeReport",
                column: "GatewayId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommitteeReport_Gateway_GatewayId",
                schema: "Reporting",
                table: "CommitteeReport",
                column: "GatewayId",
                principalSchema: "Governance",
                principalTable: "Gateway",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommitteeReport_Gateway_GatewayId",
                schema: "Reporting",
                table: "CommitteeReport");

            migrationBuilder.DropIndex(
                name: "IX_CommitteeReport_GatewayId",
                schema: "Reporting",
                table: "CommitteeReport");

            migrationBuilder.DropColumn(
                name: "GatewayId",
                schema: "Reporting",
                table: "CommitteeReport");
        }
    }
}
