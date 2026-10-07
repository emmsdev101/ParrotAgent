using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParrotAgent.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseEmbed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBaseEmbeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AllowedOrigins = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    KnowledgeBaseId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseEmbeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeBaseEmbeds_KnowledgeBases_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "KnowledgeBases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseEmbeds_KnowledgeBaseId",
                table: "KnowledgeBaseEmbeds",
                column: "KnowledgeBaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseEmbeds_Token",
                table: "KnowledgeBaseEmbeds",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeBaseEmbeds");
        }
    }
}
