using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

// RN-16: o Art. 18 da LGPD atendido sem quebrar as escalas de anos anteriores. Anonimizar não
// é excluir — a linha continua na tabela, porque a RN-15 e o ADR-0015 dependem disso.
public sealed class AnonimizarTests
{
    private const string NomeOriginal = "João Guedes";
    private const string SobrenomeOriginal = "Guedes";
    private const string MotivoDoAfastamento = "Mudou de cidade";

    private static readonly DateOnly Chegada = new(2024, 3, 12);
    private static readonly DateOnly Apresentacao = new(2024, 9, 15);
    private static readonly DateOnly Afastamento = new(2025, 6, 1);

    [Fact]
    public async Task Todo_dado_pessoal_da_ficha_deve_ser_substituido()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa pessoa = Completa();
        await SemearAsync(provider, pessoa);

        Result resultado = await AnonimizarAsync(provider, pessoa.Id);

        Assert.True(resultado.IsSuccess);

        Pessoa lida = await LerAsync(provider, pessoa.Id);

        Assert.True(lida.Anonimizada);
        Assert.NotEqual(NomeOriginal, lida.Nome);
        Assert.Null(lida.Celular);
        Assert.Null(lida.Email);
        Assert.Null(lida.DataNascimento);
        Assert.Null(lida.EstadoCivil);
        Assert.Null(lida.DataCasamento);
        Assert.Null(lida.Profissao);
        Assert.Null(lida.DataBatismo);
        Assert.Null(lida.Endereco);
    }

    [Fact]
    public async Task Id_e_historico_de_vinculo_devem_permanecer_intactos()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa pessoa = ComHistorico();
        await SemearAsync(provider, pessoa);

        await AnonimizarAsync(provider, pessoa.Id);

        Pessoa lida = await LerAsync(provider, pessoa.Id);

        // O Id é o que EscalaItem guarda sem chave estrangeira (ADR-0015): se ele mudasse, a
        // escala de 2024 passaria a apontar para ninguém.
        Assert.Equal(pessoa.Id, lida.Id);

        Assert.Equal(
            [
                "Visitante|2024-03-12|2024-09-15",
                "Membro|2024-09-15|2025-06-01",
                "Afastado|2025-06-01|",
            ],
            Resumir(lida));
    }

    // Decisão do projeto: o Motivo do vínculo **não** é anonimizado. Ele é parte do histórico
    // que a RN-16 manda preservar, e já nasce com leitura restrita a secretaria e pastor
    // (seção 7). Se um dia a decisão mudar, é este teste que cai primeiro.
    [Fact]
    public async Task Motivo_do_vinculo_deve_sobreviver_a_anonimizacao()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa pessoa = ComHistorico();
        await SemearAsync(provider, pessoa);

        await AnonimizarAsync(provider, pessoa.Id);

        Pessoa lida = await LerAsync(provider, pessoa.Id);

        VinculoIgreja afastamento = lida.Vinculos.Single(
            vinculo => vinculo.Situacao == Situacao.Afastado);

        Assert.Equal(MotivoDoAfastamento, afastamento.Motivo);
    }

    // O critério vale para a tabela `pessoas`, que é onde mora a ficha. O `motivo` do vínculo
    // fica de fora por decisão, e o teste acima é quem a registra.
    [Fact]
    public async Task Nenhuma_coluna_da_tabela_de_pessoas_deve_conter_o_nome_original()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa pessoa = ComHistorico();
        await SemearAsync(provider, pessoa);

        await AnonimizarAsync(provider, pessoa.Id);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM pessoas";

        await using SqliteDataReader reader =
            await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        // A linha continua existindo: anonimizar não é excluir (RN-15).
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));

        for (int coluna = 0; coluna < reader.FieldCount; coluna++)
        {
            string valor = reader.GetValue(coluna).ToString() ?? string.Empty;

            Assert.DoesNotContain(SobrenomeOriginal, valor, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Id_inexistente_deve_devolver_Pessoa_NotFound()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Result resultado = await AnonimizarAsync(provider, new PessoaId(Guid.CreateVersion7()));

        Assert.True(resultado.IsFailure);
        Assert.Equal(PessoaErrors.NotFound, resultado.Error);
    }

    [Fact]
    public async Task Segunda_anonimizacao_deve_ser_recusada_com_Pessoa_Anonimizada()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa pessoa = Completa();
        await SemearAsync(provider, pessoa);

        await AnonimizarAsync(provider, pessoa.Id);

        Result resultado = await AnonimizarAsync(provider, pessoa.Id);

        Assert.True(resultado.IsFailure);
        Assert.Equal(PessoaErrors.Anonimizada, resultado.Error);
        Assert.Equal(ErrorType.Conflict, resultado.Error.Type);
    }

    [Fact]
    public void Apresentacao_nao_deve_valer_sobre_registro_anonimizado()
    {
        Pessoa visitante = Completa();
        visitante.Anonimizar();

        Assert.Equal(
            PessoaErrors.Anonimizada,
            visitante.RegistrarApresentacao(Apresentacao, Apresentacao).Error);
    }

    // Cada ato é exercido de uma situação em que a matriz da seção 5 o permitiria: o que recusa
    // aqui é a marca, e não a transição. A guarda vive em SucederVinculo, depois da matriz, então
    // um ato que já era inválido continua respondendo Pessoa.TransicaoInvalida — os dois são 409.
    [Fact]
    public void Nenhum_ato_de_vinculo_deve_valer_sobre_membro_anonimizado()
    {
        Pessoa membro = Membro();
        membro.Anonimizar();

        Assert.Equal(
            PessoaErrors.Anonimizada,
            membro.ReconhecerAfastamento(MotivoDoAfastamento, Afastamento).Error);

        Assert.Equal(
            PessoaErrors.Anonimizada,
            membro.RegistrarTransferencia("Igreja Batista de Olinda", Afastamento, Afastamento).Error);

        Assert.Equal(
            PessoaErrors.Anonimizada,
            membro.RegistrarFalecimento(Afastamento, Afastamento).Error);
    }

    [Fact]
    public async Task Convite_de_quem_ela_trouxe_deve_continuar_apontando_para_o_id()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa anfitria = Completa();
        Pessoa convidado = Visitante("Maria Souza", convidadoPor: anfitria.Id);

        await SemearAsync(provider, anfitria, convidado);

        await AnonimizarAsync(provider, anfitria.Id);

        Pessoa lido = await LerAsync(provider, convidado.Id);

        // O canal de volta some junto com o nome, mas a referência não fica órfã: o Id
        // continua resolvendo, que é o que a ausência de FK entre módulos exige.
        Assert.Equal(anfitria.Id, lido.ConvidadoPorId);
        Assert.True(await ExisteAsync(provider, anfitria.Id));
    }

    private static ServiceProvider Provedor(SqliteConnection connection) =>
        Scenario.ProvedorComAnel(connection, services => services.AddPessoasHandlers());

    private static async Task<Result> AnonimizarAsync(ServiceProvider provider, PessoaId id)
    {
        using IServiceScope scope = provider.CreateScope();

        return await scope.ServiceProvider
            .GetRequiredService<ISender>()
            .SendAsync<AnonimizarCommand, Result>(
                new AnonimizarCommand(id.Value),
                TestContext.Current.CancellationToken);
    }

    private static async Task<Pessoa> LerAsync(ServiceProvider provider, PessoaId id)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        return await context.Pessoas
            .Include(pessoa => pessoa.Vinculos)
            .SingleAsync(pessoa => pessoa.Id == id, TestContext.Current.CancellationToken);
    }

    private static async Task<bool> ExisteAsync(ServiceProvider provider, PessoaId id)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        return await context.Pessoas.AnyAsync(
            pessoa => pessoa.Id == id,
            TestContext.Current.CancellationToken);
    }

    private static async Task SemearAsync(ServiceProvider provider, params Pessoa[] pessoas)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        context.Pessoas.AddRange(pessoas);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static string[] Resumir(Pessoa pessoa) =>
        [.. pessoa.Vinculos
            .OrderBy(vinculo => vinculo.DataInicio)
            .Select(vinculo =>
                $"{vinculo.Situacao}|{vinculo.DataInicio:yyyy-MM-dd}|{vinculo.DataFim:yyyy-MM-dd}")];

    private static Pessoa Membro()
    {
        Pessoa pessoa = Completa();
        pessoa.RegistrarApresentacao(Apresentacao, Apresentacao);

        return pessoa;
    }

    private static Pessoa ComHistorico()
    {
        Pessoa pessoa = Completa();

        pessoa.RegistrarApresentacao(Apresentacao, Apresentacao);
        pessoa.ReconhecerAfastamento(MotivoDoAfastamento, Afastamento);

        return pessoa;
    }

    private static Pessoa Completa() =>
        Pessoa.Cadastrar(
            hoje: Chegada,
            nome: NomeOriginal,
            convidadoPorId: null,
            celular: new Celular("+5581999998888"),
            email: new Email("joao@exemplo.com"),
            dataNascimento: new DateOnly(1990, 3, 12),
            estadoCivil: EstadoCivil.Casado,
            dataCasamento: new DateOnly(2015, 6, 20),
            profissao: "Eletricista",
            dataBatismo: new DateOnly(2010, 8, 1),
            endereco: new Endereco("52000000", "Rua das Flores", "123-A", "Apto 2", "Grotão", "Recife", "PE")).Value;

    private static Pessoa Visitante(string nome, PessoaId convidadoPor) =>
        Pessoa.Cadastrar(
            hoje: Chegada,
            nome: nome,
            convidadoPorId: convidadoPor,
            celular: null,
            email: null,
            dataNascimento: null,
            estadoCivil: null,
            dataCasamento: null,
            profissao: null,
            dataBatismo: null,
            endereco: null).Value;
}
