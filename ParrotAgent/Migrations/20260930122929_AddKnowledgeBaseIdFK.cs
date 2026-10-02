using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseIdFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeBaseId",
                table: "DocumentVectors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KnowledgeBaseId",
                table: "DocumentVectors");
        }
    }
}
