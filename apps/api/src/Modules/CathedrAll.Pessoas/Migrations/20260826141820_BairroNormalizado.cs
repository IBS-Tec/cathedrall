using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathedrAll.Pessoas.Migrations
{
    /// <inheritdoc />
    public partial class BairroNormalizado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "endereco_bairro_normalizado",
                schema: "pessoas",
                table: "pessoas",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "endereco_bairro_normalizado",
                schema: "pessoas",
                table: "pessoas");
        }
    }
}
