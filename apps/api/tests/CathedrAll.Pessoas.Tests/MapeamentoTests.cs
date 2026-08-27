using System.Reflection;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CathedrAll.Pessoas.Tests;

public sealed class MapeamentoTests
{
    [Fact]
    public void Pessoa_deve_ter_as_colunas_da_secao_4_da_spec()
    {
        string[] colunas = Scenario.Colunas(Scenario.Tabela("pessoas"));

        Assert.Equal(
        [
            "celular character varying(20)",
            "convidado_por_id uuid",
            "data_batismo date",
            "data_casamento date",
            "data_nascimento date",
            "email character varying(200)",
            "endereco_bairro character varying(80)",
            "endereco_bairro_normalizado character varying(80)",
            "endereco_cep character varying(8)",
            "endereco_cidade character varying(80)",
            "endereco_complemento character varying(60)",
            "endereco_logradouro character varying(150)",
            "endereco_numero character varying(10)",
            "endereco_uf character varying(2)",
            "estado_civil character varying(20)",
            "fundida_em_id uuid",
            "id uuid NOT NULL",
            "nome character varying(120) NOT NULL",
            "nome_normalizado character varying(120) NOT NULL",
            "profissao character varying(120)",
        ],
        colunas);
    }

    [Fact]
    public void VinculoIgreja_deve_ter_as_colunas_da_secao_4_da_spec()
    {
        string[] colunas = Scenario.Colunas(Scenario.Tabela("vinculos_igreja"));

        Assert.Equal(
        [
            "data_fim date",
            "data_inicio date NOT NULL",
            "id uuid NOT NULL",
            "motivo character varying(500)",
            "pessoa_id uuid NOT NULL",
            "situacao character varying(20) NOT NULL",
        ],
        colunas);
    }

    [Fact]
    public void Nenhuma_coluna_deve_ter_restricao_de_unicidade()
    {
        ITableIndex[] unicos =
        [
            .. Scenario.ModeloRelacional().Tables
                .SelectMany(tabela => tabela.Indexes)
                .Where(indice => indice.IsUnique),
        ];

        Assert.Empty(unicos);
    }

    [Fact]
    public void Busca_do_atendimento_e_vinculo_vigente_devem_ter_indice()
    {
        string[] indices =
        [
            .. Scenario.ModeloRelacional().Tables
                .SelectMany(tabela => tabela.Indexes)
                .Select(indice => $"{indice.Table.Name}({string.Join(", ", indice.Columns.Select(c => c.Name))})")
                .Order(StringComparer.Ordinal),
        ];

        Assert.Equal(
        [
            "pessoas(nome_normalizado)",
            "vinculos_igreja(pessoa_id, data_fim)",
        ],
        indices);
    }

    [Fact]
    public void ConvidadoPorId_e_FundidaEmId_nao_devem_ter_chave_estrangeira()
    {
        string[] colunasComFk =
        [
            .. Scenario.Tabela("pessoas").ForeignKeyConstraints
                .SelectMany(fk => fk.Columns)
                .Select(coluna => coluna.Name),
        ];

        Assert.Empty(colunasComFk);
    }

    [Fact]
    public void VinculoIgreja_deve_apontar_para_a_raiz_do_agregado()
    {
        IForeignKeyConstraint fk = Assert.Single(Scenario.Tabela("vinculos_igreja").ForeignKeyConstraints);

        Assert.Equal("pessoa_id", Assert.Single(fk.Columns).Name);
        Assert.Equal("pessoas", fk.PrincipalTable.Name);
    }

    [Fact]
    public void Nenhuma_coluna_de_auditoria_deve_existir()
    {
        string[] auditoria =
        [
            .. Scenario.ModeloRelacional().Tables
                .SelectMany(tabela => tabela.Columns)
                .Select(coluna => coluna.Name)
                .Where(nome => nome is "created_at" or "created_by" or "last_modified_at"
                    or "last_modified_by" or "deleted_at" or "deleted_by"),
        ];

        Assert.Empty(auditoria);
    }

    [Fact]
    public void O_contexto_deve_expor_um_unico_DbSet()
    {
        PropertyInfo[] conjuntos =
        [
            .. typeof(PessoasDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(propriedade =>
                    propriedade.PropertyType.IsGenericType
                    && propriedade.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)),
        ];

        Assert.Equal(nameof(PessoasDbContext.Pessoas), Assert.Single(conjuntos).Name);
    }
}
