using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRoleNamesForRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "Security",
                table: "Role",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "DisplayName",
                value: "Administrator");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "DisplayName",
                value: "Director");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Project Sponsor", "ProjectSponsor" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Programme Manager", "ProgrammeManager" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Project Manager", "ProjectManager" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Commercial Manager", "CommercialManager" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Quantity Surveyor", "QuantitySurveyor" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Governance Officer", "GovernanceOfficer" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Committee Officer", "CommitteeOfficer" });

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                columns: new[] { "DisplayName", "Name" },
                values: new object[] { "Read Only User", "ReadOnlyUser" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "Security",
                table: "Role");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "Name",
                value: "Project Sponsor");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "Name",
                value: "Programme Manager");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "Name",
                value: "Project Manager");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "Name",
                value: "Commercial Manager");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "Name",
                value: "Quantity Surveyor");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "Name",
                value: "Governance Officer");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "Name",
                value: "Committee Officer");

            migrationBuilder.UpdateData(
                schema: "Security",
                table: "Role",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000010"),
                column: "Name",
                value: "Read Only User");
        }
    }
}
