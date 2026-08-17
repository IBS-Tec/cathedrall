# ADR-0015 — Um `DbContext` e um conjunto de migrations por módulo

**Status:** Aceito · **Data:** 2026-08-17

## Contexto

O [ADR-0006](0006-postgresql.md) decidiu PostgreSQL com EF Core e migrations versionadas no
repositório. O [ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md) decidiu um
projeto por módulo, com `Domain/ Application/ Infrastructure/ Endpoints/` dentro de cada um e
módulos que **não se referenciam**. Os dois juntos deixam uma pergunta em aberto: **quantos
`DbContext` existem, e onde eles moram.**

Ela precisa ser respondida antes do primeiro `SaveChanges`. As três garantias que o
`apps/api/README.md` exige antes do primeiro CRUD — audit log, soft delete e RBAC com escopo —
penduram todas no `DbContext`: a auditoria num `SaveChangesInterceptor`, a exclusão lógica num
filtro global de consulta. Quantos contextos existem decide em quantos lugares cada uma dessas
peças é registrada, e errar aqui é caro de corrigir: quando houver dado de pessoa dentro, mudar
de ideia significa migration de dados sobre a tabela mais sensível do sistema.

## Decisão

**Um `DbContext` por módulo, com schema próprio no banco, e as migrations de cada módulo dentro
do projeto dele.**

| Peça | Onde mora |
| --- | --- |
| `PessoasDbContext`, mapeamentos e entidades | dentro do módulo, `internal` |
| migrations e `ModelSnapshot` | `src/Modules/CathedrAll.Pessoas/Migrations/` |
| tabela de histórico de migration | no schema do próprio módulo |
| escolha do provider, string de conexão, schema | `Program.cs`, no host |

O banco continua único ([ADR-0006](0006-postgresql.md)); o que se separa é o schema:
`pessoas`, `departamentos`, `eventos`, `escalas`. A fronteira do ADR-0012, que hoje é garantia
de compilação, ganha uma segunda expressão no banco — dá para ver de fora, num `\dn` do `psql`,
quem é dono de qual tabela, e um `GRANT` por schema fica sendo uma opção aberta se um dia
interessar.

### O módulo recebe a configuração; o host escolhe o provider

O módulo expõe uma única coisa pública, a extensão de registro, e recebe a configuração como
lambda:

```csharp
public static IServiceCollection AddPessoasModule(
    this IServiceCollection services,
    Action<DbContextOptionsBuilder> configure)
{
    services.AddDbContext<PessoasDbContext>(configure);

    return services;
}
```

```csharp
builder.Services.AddPessoasModule(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pessoas")));
```

**Nenhum módulo chama `UseNpgsql`.** A string de conexão não passa por módulo nenhum, o mapa
módulo → schema fica visível no `Program.cs`, e um módulo não consegue abrir uma segunda
conexão ou trocar de provider sem que isso apareça na composição. É o mesmo argumento que o
README do kernel usa para manter os anéis do pipeline no host: composição é decisão do host, e
esconder decisão de composição dentro de biblioteca tira do `Program.cs` a única visão que
existe do todo.

O `DbContext`, as entidades e os `DbSet` são todos `internal`. **Do módulo, o host vê só o
`AddPessoasModule` e os contratos no kernel.**

### Cada módulo tem a própria tabela de histórico

`MigrationsHistoryTable` põe o `__EFMigrationsHistory` dentro do schema do módulo. O script
gerado confirma:

```sql
CREATE SCHEMA pessoas;
CREATE TABLE IF NOT EXISTS pessoas."__EFMigrationsHistory" (…);
```

**A consequência é que os módulos migram de forma independente**, e a ordem relativa entre as
migrations de módulos diferentes não fica registrada em lugar nenhum. Isso só é seguro por
causa da próxima decisão.

### Sem chave estrangeira atravessando módulo

Um `DbContext` não conhece as entidades do outro, então o EF não tem como declarar a FK de
`EscalaItem.PessoaId` para `pessoas."Pessoas"`. Escrever a FK à mão, com `migrationBuilder.Sql`,
está **descartado**: a restrição existiria no banco e não existiria em nenhum dos dois
snapshots, e o primeiro `migrations add` seguinte geraria um modelo que discorda da realidade.

Referência entre módulos é por `Id`, sem navegação e sem integridade referencial declarada.
**O que segura o buraco é o soft delete:** como `Pessoa` nunca é apagada fisicamente (invariante
6 do `CLAUDE.md`), o `EscalaItem` órfão — o risco real de perder a FK — deixa de ser o caso
comum. A garantia mudou de lugar, do banco para uma decisão de modelagem, e isso precisa
sobreviver na revisão de código: **no dia em que alguém escrever um `DELETE` de verdade em
`Pessoa`, esta decisão vira corrupção silenciosa de dado.**

### O comando é o padrão do EF, sem flag nenhuma

Rodando de `apps/api`:

```bash
dotnet ef migrations add Inicial \
  --project src/Modules/CathedrAll.Pessoas \
  --startup-project src/Bootstrapper/CathedrAll.Api \
  --context PessoasDbContext
```

O `--context` é obrigatório a partir do segundo contexto, e vale para todo comando do
`dotnet ef` — `migrations add`, `database update`, `migrations list`. **É o custo operacional
real desta decisão:** atualizar o banco local passa a ser um comando por módulo. A mitigação é
um script em `apps/api/scripts/` que roda o laço dos contextos, e ele é obrigação de quem
criar o segundo módulo, não sugestão.

## A alternativa recusada: todas as migrations numa pasta única

Foi a primeira versão deste ADR, e ela **funciona** — verificado com SDK 10.0.302, EF Core
10.0.11 e Npgsql 10.0.3, num spike com dois contextos. Migrations de todos os módulos em
`src/Bootstrapper/CathedrAll.Api/Migrations/`, com `MigrationsAssembly("CathedrAll.Api")` e um
namespace por módulo. Foi recusada pelo que ela cobra:

| | pasta única no host | pasta por módulo |
| --- | --- | --- |
| `DbContext` do módulo | **`public` obrigatório** (`CS0122`) | `internal` |
| Pacote do provider no módulo | dispensável | **necessário** |
| Flags no `migrations add` | `--output-dir` e `--namespace` | nenhuma |
| Ritual na primeira migration do módulo | mover o `ModelSnapshot` à mão | nenhum |
| Colisão de nome entre módulos | possível; quebra o build | impossível |
| Onde procurar um arquivo | 1 pasta | N pastas |
| Comandos para atualizar o banco | **N, um por contexto** | N, um por contexto |

**A última linha é a que decide.** A pasta única não economiza comando nenhum: com mais de um
contexto, o `--context` é obrigatório de qualquer forma. Ela compra onde os arquivos ficam, e
nada além disso — não era a ergonomia que parecia ser.

O resto pesa na mesma direção:

- **`public` no `DbContext` é uma porta aberta no host.** Ela não fura a fronteira que o
  ADR-0012 mais protege — `Escalas` não referencia `Pessoas` e continua sem alcançar nada —,
  mas o host passa a poder consultar a tabela de pessoas direto, sem passar por handler. O
  ADR-0012 põe `Endpoints/` dentro do módulo justamente para o host não ter o que fazer com
  dado.
- **A pasta por módulo é o default do EF.** Sem flag, sem ritual, snapshot no lugar certo de
  primeira. Para um time de voluntários iniciantes com rotatividade alta, isso vale mais que
  pasta arrumada: todo tutorial que a pessoa achar na internet descreve o que ela está vendo.
  O `apps/api/README.md` já tem uma seção inteira sobre o custo de as coisas não baterem com
  os exemplos de fora.
- **O módulo volta a ser autocontido.** Apagar ou extrair um módulo leva o histórico de schema
  dele junto, em vez de deixar arquivos identificáveis só pelo namespace numa pasta comum.

E o preço da pasta por módulo é pequeno **porque o ADR-0012 já o previa**: o
`Npgsql.EntityFrameworkCore.PostgreSQL` entra no módulo porque o gerador do snapshot emite
`NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder)` — uma linha, gerada,
independente de a gente usar coluna identity ou não. O projeto de módulo é
`Domain/ Application/ Infrastructure/ Endpoints/`; provider é infraestrutura, e é ali que ele
mora. A regra severa de `PackageReference` continua sendo do `Kernel.Domain`, e segue intacta.

## Consequências

**Boas.** As três peças de LGPD ganham um lugar óbvio: interceptor de auditoria e filtro de
soft delete se registram por contexto, e um módulo novo os herda ao ser composto no host, não
por lembrança de quem escreve. Um módulo não consegue ler a tabela do outro nem por acidente, e
agora nem o host consegue — o contexto é `internal`. O ferramental é o default do EF, então o
que a documentação de fora descreve é o que se vê aqui.

**Ruins, aceitas.**

**Atualizar o banco local é um comando por módulo.** No fim do MVP, cinco. É o custo direto de
ter cinco contextos, ele não tem jeito bonito, e o script do laço é o que o torna suportável.

**Integridade referencial entre módulos deixou de ser garantida pelo banco.** O soft delete é o
que torna isso aceitável, e não é troca gratuita: um bug de aplicação que hoje esbarraria numa
violação de FK passa a gravar um `Id` que não aponta para nada.

**Consulta que atravessa módulo não é mais um `JOIN` do EF.** "Escala de domingo com o nome dos
convocados" toca `escalas` e `pessoas`, que são contextos diferentes. O caminho é o que o
ADR-0012 já previa — pedir os dados ao outro módulo por contrato, e juntar na aplicação —, e é
mais verboso e mais lento que o `JOIN` que o banco faria de graça. **Este é o preço principal
desta decisão**, e ele chega já na primeira tela de escala.

**O audit log fica por schema, não numa tabela só.** Uma tabela de auditoria compartilhada,
escrita pelo `SaveChanges` de qualquer módulo, exigiria que ela pertencesse a algum contexto — e
aí ou os módulos passam a depender da ordem de migration de um contexto comum, ou a escrita sai
da transação que ela audita, o que é pior: auditoria que faz commit separado do dado é
auditoria que mente quando a transação volta atrás. Com uma tabela por schema, a linha de
auditoria e a linha de dado commitam juntas por construção. O custo é que a consulta completa
do rastro vira `UNION` entre schemas, ou uma view mantida à mão. **O desenho da auditoria é
outro ADR**; este aqui só fecha a porta da tabela única, e é honesto dizer que fecha.

**Uma conta que vai chegar no anel de transação.** O `TransactionBehavior` que o README do
kernel deixou desenhado precisa saber *qual* contexto abrir, e o `Kernel.Application` não pode
conhecer EF Core — a regra dos `PackageReference` só de `*.Abstractions` continua valendo. A
saída provável é um `CathedrAll.Kernel.Infrastructure` novo, com um behavior genérico no
`DbContext`, e três linhas por módulo fechando o genérico no contexto dele. É pequeno, mas é
código que só existe por causa desta decisão.

**Mais cerimônia por módulo novo.** Um contexto, um schema, uma pasta de migrations, uma linha
de `AddDbContext` no host, uma extensão de registro. Contra um `DbContext` único, é meia hora a
mais por módulo — cinco vezes no MVP.

## O que este ADR não decide

**Quem aplica as migrations.** Hoje o `docs/runbook.md` tem "aplicar migrations" como passo
manual, e não existe ambiente publicado — decidir agora entre `Database.Migrate()` na subida,
script idempotente no deploy ou passo à mão seria escolher sem conhecer o mecanismo de deploy do
Dokploy. Vale registrar o argumento que já está em jogo: migrar na subida daria ao usuário da
aplicação direito de DDL e transformaria migration ruim em ciclo de reinício no Swarm — o mesmo
raciocínio pelo qual `/health` não olha o banco. **É ADR próprio, antes do primeiro deploy.**

**A convenção de nome de tabela e coluna.** O default do EF no PostgreSQL é PascalCase entre
aspas (`pessoas."Pessoas"`, `"NomeCompleto"`), o que obriga a citar identificador em toda query
escrita à mão no `psql`. Trocar para `snake_case` é uma linha de configuração **antes** da
primeira migration aplicada num banco com dado, e uma migration de renomear tudo depois.
Precisa ser resolvido no PR que cria o primeiro contexto — não é ADR, é decisão barata que fica
cara em uma semana.

## Gatilho de reavaliação

Se a contagem de consultas que atravessam módulo crescer ao ponto de a junção na aplicação
virar o gargalo — ou se aparecer um relatório que precise de `JOIN` entre três schemas —, a
resposta **não** é voltar a um contexto único: é um contexto de leitura separado, mapeado só
para consulta, com o ADR próprio que isso merece. Um `DbContext` de escrita por módulo continua
valendo; o que se discute é o lado da leitura.
