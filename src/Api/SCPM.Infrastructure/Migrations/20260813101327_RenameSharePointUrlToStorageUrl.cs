using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SCPM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameSharePointUrlToStorageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SharePointUrl",
                schema: "Documents",
                table: "DocumentFile",
                newName: "StorageUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StorageUrl",
                schema: "Documents",
                table: "DocumentFile",
                newName: "SharePointUrl");
        }
    }
}
