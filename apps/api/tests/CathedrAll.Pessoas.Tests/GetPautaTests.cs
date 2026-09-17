using CathedrAll.Kernel.Application;
using CathedrAll.Kernel.Domain;
using CathedrAll.Pessoas.Application;
using CathedrAll.Pessoas.Domain;
using CathedrAll.Pessoas.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace CathedrAll.Pessoas.Tests;

public sealed class GetPautaTests
{
    // 2026-08-23 é domingo. A semana do culto vai da segunda anterior, 17, até ele.
    private static readonly DateOnly DomingoDoCulto = new(2026, 8, 23);
    private static readonly DateOnly SegundaDaSemana = new(2026, 8, 17);
    private static readonly DateOnly QuartaDaSemana = new(2026, 8, 19);
    private static readonly DateOnly DomingoAnterior = new(2026, 8, 16);
    private static readonly DateOnly SegundaSeguinte = new(2026, 8, 24);

    [Fact]
    public async Task Visitantes_devem_ser_so_os_cadastrados_na_data_do_culto()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(
            provider,
            Visitante("Ana Souza", DomingoDoCulto),
            Visitante("Bento Lima", DomingoAnterior),
            Membro("Carla Dias", DomingoDoCulto));

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        Assert.Equal(["Ana Souza"], Nomes(pauta));
    }

    [Fact]
    public async Task Visitante_deve_vir_com_quem_o_convidou()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa maria = Membro("Maria Souza", DomingoAnterior);
        Pessoa joao = Visitante("João Guedes", DomingoDoCulto, convidadoPor: maria);

        await SemearAsync(provider, maria, joao);

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        VisitanteDaPauta visitante = Assert.Single(pauta.Visitantes);
        Assert.Equal(new PessoaRef(maria.Id.Value, "Maria Souza"), visitante.ConvidadoPor);
    }

    [Fact]
    public async Task Visitante_sem_convite_deve_vir_sem_convidado_por()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(provider, Visitante("João Guedes", DomingoDoCulto));

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        VisitanteDaPauta visitante = Assert.Single(pauta.Visitantes);
        Assert.Null(visitante.ConvidadoPor);
    }

    // O dirigente lê a lista com o microfone na mão e pode tocar em atualizar no meio da
    // leitura (seção 8). Sem ordem no servidor, a lista volta embaralhada. O acento decide
    // entre ordenar por Nome e por NomeNormalizado: em ordem binária, "Â" vem depois de "B".
    [Fact]
    public async Task Visitantes_devem_vir_em_ordem_de_nome_ignorando_acento()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(
            provider,
            Visitante("Bento Lima", DomingoDoCulto),
            Visitante("Ângela Souza", DomingoDoCulto));

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        Assert.Equal(["Ângela Souza", "Bento Lima"], Nomes(pauta));
    }

    [Fact]
    public async Task Domingo_sem_visitante_deve_devolver_lista_vazia()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(
            provider,
            Aniversariante("Ana Souza", nascimento: new DateOnly(1990, 8, 19)));

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        Assert.Empty(pauta.Visitantes);
        Assert.NotEmpty(pauta.Aniversariantes);
    }

    // A semana é de segunda a domingo e termina no domingo do culto: quem faz aniversário
    // na segunda seguinte é da pauta da semana que vem.
    [Fact]
    public async Task Aniversariantes_devem_ser_os_da_semana_que_termina_no_domingo_do_culto()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(
            provider,
            Aniversariante("Ana Souza", nascimento: Em(SegundaDaSemana)),
            Aniversariante("Bento Lima", nascimento: Em(DomingoDoCulto)),
            Aniversariante("Carla Dias", nascimento: Em(DomingoAnterior)),
            Aniversariante("Davi Melo", nascimento: Em(SegundaSeguinte)));

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        Assert.Equal(
            ["Ana Souza|Nascimento|2026-08-17", "Bento Lima|Nascimento|2026-08-23"],
            Aniversarios(pauta));
    }

    [Fact]
    public async Task Culto_no_meio_da_semana_deve_usar_a_semana_que_contem_a_data()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        await SemearAsync(
            provider,
            Aniversariante("Ana Souza", nascimento: Em(SegundaDaSemana)),
            Aniversariante("Bento Lima", nascimento: Em(DomingoDoCulto)),
            Aniversariante("Carla Dias", nascimento: Em(DomingoAnterior)));

        PautaResponse pauta = await BuscarAsync(provider, QuartaDaSemana);

        Assert.Equal(
            ["Ana Souza|Nascimento|2026-08-17", "Bento Lima|Nascimento|2026-08-23"],
            Aniversarios(pauta));
    }

    // A pauta despacha a consulta de aniversariantes em vez de repetir a RN-25. Se alguém
    // voltar a escrever a consulta aqui dentro, é este teste que avisa.
    [Fact]
    public async Task Aniversariantes_da_pauta_devem_herdar_a_exclusao_da_RN_25()
    {
        await using SqliteConnection connection = await Scenario.AbrirAsync();
        await using ServiceProvider provider = Provedor(connection);

        Pessoa falecida = Membro("Ana Souza", DomingoAnterior, nascimento: Em(QuartaDaSemana));
        Assert.True(falecida.RegistrarFalecimento(DomingoAnterior, DomingoAnterior).IsSuccess);

        await SemearAsync(provider, falecida);

        PautaResponse pauta = await BuscarAsync(provider, DomingoDoCulto);

        Assert.Empty(pauta.Aniversariantes);
    }

    private static ServiceProvider Provedor(SqliteConnection connection) =>
        Scenario.ProvedorComAnel(connection, services => services.AddPessoasHandlers());

    private static async Task<PautaResponse> BuscarAsync(ServiceProvider provider, DateOnly data)
    {
        using IServiceScope scope = provider.CreateScope();

        Result<PautaResponse> resultado = await scope.ServiceProvider
            .GetRequiredService<ISender>()
            .SendAsync<GetPautaQuery, Result<PautaResponse>>(
                new GetPautaQuery(data),
                TestContext.Current.CancellationToken);

        Assert.True(resultado.IsSuccess);

        return resultado.Value;
    }

    private static string[] Nomes(PautaResponse pauta) =>
        [.. pauta.Visitantes.Select(visitante => visitante.Nome)];

    private static string[] Aniversarios(PautaResponse pauta) =>
        [.. pauta.Aniversariantes.Select(
            aniversariante =>
                $"{aniversariante.Nome}|{aniversariante.Tipo}|{aniversariante.Data:yyyy-MM-dd}")];

    // O ano não importa para a RN-25, que compara só dia e mês.
    private static DateOnly Em(DateOnly diaDaSemana) =>
        new(1990, diaDaSemana.Month, diaDaSemana.Day);

    private static Pessoa Visitante(string nome, DateOnly chegada, Pessoa? convidadoPor = null) =>
        Cadastrar(nome, chegada, convidadoPor?.Id, null, comoMembro: false);

    private static Pessoa Membro(string nome, DateOnly chegada, DateOnly? nascimento = null) =>
        Cadastrar(nome, chegada, null, nascimento, comoMembro: true);

    private static Pessoa Aniversariante(string nome, DateOnly nascimento) =>
        Cadastrar(nome, DomingoAnterior, null, nascimento, comoMembro: true);

    private static Pessoa Cadastrar(
        string nome,
        DateOnly chegada,
        PessoaId? convidadoPorId,
        DateOnly? nascimento,
        bool comoMembro) =>
        Pessoa.Cadastrar(
            hoje: chegada,
            nome: nome,
            convidadoPorId: convidadoPorId,
            celular: null,
            email: null,
            dataNascimento: nascimento,
            estadoCivil: null,
            dataCasamento: null,
            profissao: null,
            dataBatismo: null,
            endereco: null,
            comoMembro: comoMembro).Value;

    private static async Task SemearAsync(ServiceProvider provider, params Pessoa[] pessoas)
    {
        using IServiceScope scope = provider.CreateScope();
        PessoasDbContext context = scope.ServiceProvider.GetRequiredService<PessoasDbContext>();

        context.Pessoas.AddRange(pessoas);

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
