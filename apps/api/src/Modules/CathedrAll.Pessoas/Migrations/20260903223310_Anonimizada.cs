using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathedrAll.Pessoas.Migrations
{
    /// <inheritdoc />
    public partial class Anonimizada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "anonimizada",
                schema: "pessoas",
                table: "pessoas",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "anonimizada",
                schema: "pessoas",
                table: "pessoas");
        }
    }
}
