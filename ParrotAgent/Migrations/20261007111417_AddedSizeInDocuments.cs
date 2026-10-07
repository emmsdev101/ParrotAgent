using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddedSizeInDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "KnowledgeBases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "Documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "KnowledgeBases");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
