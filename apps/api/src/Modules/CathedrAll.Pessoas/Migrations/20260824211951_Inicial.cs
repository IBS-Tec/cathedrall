using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CathedrAll.Pessoas.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pessoas");

            migrationBuilder.CreateTable(
                name: "pessoas",
                schema: "pessoas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    convidado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fundida_em_id = table.Column<Guid>(type: "uuid", nullable: true),
                    celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    estado_civil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    data_casamento = table.Column<DateOnly>(type: "date", nullable: true),
                    profissao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    data_batismo = table.Column<DateOnly>(type: "date", nullable: true),
                    endereco_bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    endereco_cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    endereco_cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    endereco_complemento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    endereco_logradouro = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    endereco_numero = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    endereco_uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pessoas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vinculos_igreja",
                schema: "pessoas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pessoa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    situacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vinculos_igreja", x => x.id);
                    table.ForeignKey(
                        name: "fk_vinculos_igreja_pessoas_pessoa_id",
                        column: x => x.pessoa_id,
                        principalSchema: "pessoas",
                        principalTable: "pessoas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pessoas_nome",
                schema: "pessoas",
                table: "pessoas",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "ix_vinculos_igreja_pessoa_id_data_fim",
                schema: "pessoas",
                table: "vinculos_igreja",
                columns: new[] { "pessoa_id", "data_fim" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vinculos_igreja",
                schema: "pessoas");

            migrationBuilder.DropTable(
                name: "pessoas",
                schema: "pessoas");
        }
    }
}
