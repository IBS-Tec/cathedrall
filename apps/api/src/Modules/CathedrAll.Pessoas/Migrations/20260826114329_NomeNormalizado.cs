using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathedrAll.Pessoas.Migrations
{
    /// <inheritdoc />
    public partial class NomeNormalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pessoas_nome",
                schema: "pessoas",
                table: "pessoas");

            migrationBuilder.AddColumn<string>(
                name: "nome_normalizado",
                schema: "pessoas",
                table: "pessoas",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_pessoas_nome_normalizado",
                schema: "pessoas",
                table: "pessoas",
                column: "nome_normalizado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_pessoas_nome_normalizado",
                schema: "pessoas",
                table: "pessoas");

            migrationBuilder.DropColumn(
                name: "nome_normalizado",
                schema: "pessoas",
                table: "pessoas");

            migrationBuilder.CreateIndex(
                name: "ix_pessoas_nome",
                schema: "pessoas",
                table: "pessoas",
                column: "nome");
        }
    }
}
