# apps/api — API do CathedrAll

.NET 10, ASP.NET Core, Minimal API. Domínio: `api.ibscristo.com.br`.

> **Estado: host, kernel, mediator, formato de erro, o anel de transação e o módulo
> `Pessoas`.** Existem o host
> com `/health`, a configuração de build, o kernel de domínio — `Result`, `Error`,
> `ErrorType`, `Entity`, `AggregateRoot`, `DomainEvent` —, o mediator com dois behaviors, o de
> log e o de transação, e os dois pontos de conversão da fronteira HTTP descritos em
> [Formato de erro](#formato-de-erro). O kernel está em
> [`src/Kernel/README.md`](src/Kernel/README.md). **O primeiro módulo existe:**
> `CathedrAll.Pessoas`, com o `PessoasDbContext` no schema `pessoas`, as entidades mapeadas
> e a migration inicial aplicável num banco limpo — **estado e mapeamento, sem regra de
> negócio**. Não existe handler, endpoint, autenticação, nem invariante de domínio, e **o
> anel de transação continua sem quem o registre**. Existe `ICurrentUser`, com implementação
> de desenvolvimento — e ela **não é autenticação**, ver [Usuário atual](#usuário-atual).
> A API está sendo reconstruída do zero, em passos pequenos. Este README descreve só o que já
> existe — se algo não estiver aqui, não foi construído ainda.

## Comandos

```bash
cd apps/api
dotnet build
dotnet test
dotnet run --project src/Bootstrapper/CathedrAll.Api
```

Migrations, com a ferramenta fixada em `.config/dotnet-tools.json`:

```bash
dotnet tool restore

dotnet ef migrations add <Nome> \
  --project src/Modules/CathedrAll.Pessoas \
  --startup-project src/Bootstrapper/CathedrAll.Api \
  --context PessoasDbContext

dotnet ef database update \
  --project src/Modules/CathedrAll.Pessoas \
  --startup-project src/Bootstrapper/CathedrAll.Api \
  --context PessoasDbContext
```

O `--context` é obrigatório a partir do segundo módulo e está aqui desde já para o comando
não mudar depois. A connection string vem de `ConnectionStrings:CathedrAll` — não é
versionada, então exporte-a antes:

```bash
export ConnectionStrings__CathedrAll="Host=localhost;Port=5432;Database=cathedrall;Username=…;Password=…"
```

O usuário e a senha estão em `infra/compose/.env`, criados pelo `initdb/01-bancos.sh`.

## Endpoints

| Rota | Auth | Verifica | Descrição |
| --- | --- | --- | --- |
| `GET /health` | anônimo | nada | O processo responde. Sempre `200 Healthy` |
| `GET /health/ready` | anônimo | Postgres | `200 Healthy` ou `503 Unhealthy` |

**São perguntas diferentes, e a separação é deliberada.** `/health` responde "estou vivo,
me reinicie se eu não responder isto". `/health/ready` responde "consigo atender pedidos
de verdade" — hoje isso significa alcançar o banco.

Misturar as duas sai caro. O healthcheck do container aponta para `/health`; no Docker
Swarm, que é o que o Dokploy usa ([ADR-0009](../../docs/adr/0009-hospedagem-unificada-dokploy.md)),
tarefa marcada como *unhealthy* é reagendada. Se o banco entrasse nessa conta, o Postgres
cair derrubaria a API em ciclo de reinício — que não conserta o banco, apaga o log e
atrasa a volta. O monitoramento externo e o alerta apontam para `/health/ready`.

Isso vale ainda mais se o banco um dia sair da máquina. Instância gerenciada em plano
gratuito costuma dormir por inatividade: indisponibilidade **esperada**, que precisa
aparecer no alerta sem reiniciar coisa nenhuma.

Os dois são anônimos de propósito — o monitoramento externo não tem credencial. Por isso
**nenhum expõe detalhes**: o corpo é `Healthy` ou `Unhealthy` em texto puro. Um health
check tagarela conta a um desconhecido quais dependências você tem e como elas falham. Há
teste garantindo que nem o nome do check nem o host do banco vazam no corpo. Se um dia
precisarmos de diagnóstico detalhado, ele vai numa rota separada e autenticada.

O check do Postgres é escrito à mão (`PostgresHealthCheck`): abre conexão e roda
`SELECT 1`, com timeout de 3 segundos. Banco fora do ar recusa conexão rápido — o caso
ruim é banco **pendurado**, e sem timeout a requisição ficaria esperando até o monitor
desistir, sem distinguir isso de processo morto. Não usamos o pacote da comunidade
(`AspNetCore.HealthChecks.NpgSql`): ele está uma major atrás do nosso .NET e traria
dezenas de checks que não usamos, para substituir dez linhas.

Repare no que ele **não** verifica: migrações aplicadas, versão de schema, ou se o usuário
tem só as permissões que deveria. `SELECT 1` funciona para qualquer usuário conectado. A
separação de bancos do [ADR-0006](../../docs/adr/0006-postgresql.md) é garantida pelo
`initdb` e por teste de integração, não por health check.

### Configuração

`ConnectionStrings:CathedrAll` — conexão com o banco `cathedrall`. Sem ela,
`/health/ready` responde `503`, que é a resposta correta: sem banco a API não está pronta.
Em desenvolvimento vai no `appsettings.Development.json`, que é ignorado pelo git; em
produção, na variável `ConnectionStrings__CathedrAll`. Nunca versionada.

Para começar, copie o exemplo e preencha a senha — a mesma `APP_DB_PASSWORD` do
`infra/compose/.env`:

```bash
cp src/Bootstrapper/CathedrAll.Api/appsettings.Development.json{.example,}
```

`CurrentUser:Id` e `CurrentUser:Papel` — o usuário fictício de desenvolvimento, descrito em
[Usuário atual](#usuário-atual). Os dois têm default, então a API sobe sem configuração
nenhuma. Trocar o papel para ver o sistema como a recepção o vê é uma linha no
`appsettings.Development.json`, ou `CurrentUser__Papel=Recepcao` no ambiente, e vale a partir
da requisição seguinte — sem reiniciar. Em produção a seção é ignorada, porque lá não há
implementação de desenvolvimento para configurar.

## Formato de erro

Todo erro da API sai em `application/problem+json` (RFC 9457). O porquê de cada campo está no
[ADR-0014](../../docs/adr/0014-problem-details-como-formato-unico-de-erro.md).

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Pessoa não encontrada.",
  "code": "Pessoa.NotFound",
  "traceId": "00-225d4de6e680a921a8b8bbefad510b66-d494e8ff06a97bca-00"
}
```

O status vem do `ErrorType`: `Validation` 400, `NotFound` 404, `Conflict` 409, `Failure` 500.

**Duas regras para quem escreve a SPA.** As duas quebram em silêncio se forem ignoradas —
nada no compilador as protege:

1. **Ramifique em `code`, nunca em `detail`.** `code` é contrato de API; `detail` é texto que
   a secretaria pode pedir para reescrever a qualquer momento.
2. **Nunca renderize `title`.** Ele é genérico por status e fica em inglês de propósito. O
   texto para o usuário é o `detail`.

Quatro peças no `Program.cs` produzem esse formato, e as quatro são necessárias:

| Peça | Cobre |
| --- | --- |
| `ErrorResults.ToProblem()` | a falha de um `Result` nosso |
| `GlobalExceptionHandler` | a exceção não capturada, e o cancelamento |
| `AddProblemDetails` + `CustomizeProblemDetails` | o `traceId`, em todo problem+json |
| `UseStatusCodePages` | os erros do framework: rota inexistente, método não permitido |

A terceira é a menos óbvia e a mais fácil de perder num refactor. **`AddProblemDetails`
sozinho não faz nada aparecer:** ele apenas registra o `IProblemDetailsService`, e um 404 de
roteamento não invoca esse serviço — responde com corpo vazio e sem content-type. Há teste
batendo numa rota inexistente por HTTP de verdade justamente para que remover essa linha
fique vermelho.

O `traceId` sai do `Activity.Current?.Id`, com o `TraceIdentifier` como reserva, e existe para
juntar log e resposta: é por ele que um erro relatado pela secretaria acha a requisição no
log. Ele é acrescentado uma vez, na customização — não no mapeador. Se fosse no mapeador, só
os nossos erros o teriam.

### Exceção não capturada e cancelamento

O `GlobalExceptionHandler` tem três caminhos, e a ordem entre eles é o desenho:

| Situação | O que faz |
| --- | --- |
| `RequestAborted` cancelado | log de rotina, sem corpo. Não há ninguém do outro lado do socket |
| `Response.HasStarted` | log de erro e devolve `false`: não dá mais para trocar o status |
| qualquer outra exceção | log de erro **com stack trace**, e 500 em problem+json |

**O teste do cancelamento é o token, não o tipo da exceção.** Um timeout interno também lança
`OperationCanceledException`, e esse **é** falha de verdade — se a classificação fosse pelo
tipo, ela silenciaria justamente o caso que interessa. Quem distingue é o
`RequestAborted.IsCancellationRequested`.

**O 500 sai pelo mesmo `ToProblem()`.** O handler monta um
`Error.Failure("Server.UnexpectedFailure", …)` e chama o mapeador, em vez de escrever o corpo
por conta própria. Assim as duas respostas 500 da API — a de um `Result` que falhou e a de uma
exceção — têm a mesma forma **por construção**. Escrever o corpo à mão aqui funcionaria hoje e
divergiria no primeiro ajuste feito só de um lado.

O `exception.Message` **nunca** entra no corpo. Há teste que joga uma string reconhecível na
exceção e varre a resposta inteira procurando por ela: é a única falha desta classe que não se
anuncia, porque vazar detalhe não deixa a resposta lenta nem errada, só mais informativa para
quem não devia.

**Um custo declarado:** quando o cliente desiste, o handler registra em `Information` mesmo se
a exceção era um bug de verdade que por acaso coincidiu com a desconexão. A exceção vai para o
log junto, então o stack trace não se perde, mas **nenhum alerta dispara**. É o preço de não
tratar cada aba fechada como incidente, e o caminho ao contrário — `Error` em todo
cancelamento — treina o time a ignorar o canal.

### O que os testes não cobrem

**O caminho do 500 é verificado no handler, não pelo middleware.** Os testes chamam
`TryHandleAsync` direto, com um `DefaultHttpContext`; ninguém sobe a aplicação e provoca uma
exceção de verdade, porque não existe endpoint que lance. O `UseExceptionHandler` estar na
ordem certa do pipeline é hoje conferido a olho.

Vale saber de uma armadilha para quando esse teste existir: em Development o `WebApplication`
põe o `DeveloperExceptionPage` na frente do pipeline, e o `WebApplicationFactory` sobe em
Development por padrão — o teste veria HTML em vez de problem+json. A saída é fixar
`UseEnvironment("Production")`.

**O campo `code` na rede** é verificado na serialização — o teste do handler lê o JSON escrito
no corpo — mas não através de um endpoint real, porque nenhum devolve `Result` ainda. O
primeiro módulo é quem fecha essa lacuna.

## Usuário atual

`ICurrentUser`, em `Kernel.Application`, responde "quem está fazendo isto": um `Guid Id` e um
`Papel`. Nada além disso.

**Isto não é autenticação.** Não existe login, token nem sessão, e nada nesta API verifica
quem quer que seja. O que existe é a **porta**: um contrato do qual todo handler depende
desde o primeiro, para que o interceptor de auditoria tenha de onde tirar "quem fez" e para
que a autenticação de verdade entre por baixo sem tocar em handler nenhum. É por isso que o
contrato não fala em JWT, claim ou cookie — se falasse, trocar a implementação mudaria quem
depende dela.

**O portão da seção 7 da Spec-0001 continua inteiro:** nenhum dado de pessoa real entra em
banco nenhum — inclusive o de desenvolvimento — antes de autenticação e audit log existirem.
`ICurrentUser` é o que aquela seção chama de barato agora e caro depois; ele não antecipa um
milímetro do portão, e **a matriz de papéis da seção 7 não está aplicada a rota nenhuma**.

Quem implementa hoje é o `DevelopmentCurrentUser`, que mora no host e não no kernel, de
propósito: o host é o único assembly que ninguém referencia, então "não existe código em
produção capaz de instanciar isto" vira topologia em vez de convenção. Ele entra apenas
quando o ambiente é `Development`, e a escolha fica visível no `Program.cs`:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentCurrentUser(builder.Configuration);
}

// ...

builder.Services.RequireCurrentUser();
```

A última linha é o portão do host, e ela pergunta **se existe alguma implementação
registrada** — não que ambiente é este. A diferença aparece no futuro: hoje isso significa
que produção não sobe; no dia em que o módulo de acesso registrar um adapter de verdade, a
linha para de disparar sozinha, sem ninguém remover nada. E um ambiente novo qualquer, um
`Staging`, falha ao subir em vez de herdar usuário fictício em silêncio.

## Estrutura

```
src/
  Bootstrapper/
    CathedrAll.Api/                       host; sobe a aplicação, registra o
                                          mediator e o pipeline, mapeia /health,
                                          converte Error em ProblemDetails e
                                          trata a exceção não capturada
  Kernel/
    CathedrAll.Kernel.Domain/             Result, Error, ErrorType, Entity
    CathedrAll.Kernel.Application/        mediator, pipeline, LoggingBehavior,
                                          ICurrentUser
    CathedrAll.Kernel.Infrastructure/     TransactionBehavior
tests/
  CathedrAll.Api.Tests/                   testes do host: integração e mapeamento
  CathedrAll.Kernel.Domain.Tests/         testes unitários do kernel de domínio
  CathedrAll.Kernel.Application.Tests/    testes unitários do mediator
  CathedrAll.Kernel.Infrastructure.Tests/ testes do anel de transação, sobre SQLite
```

O kernel compartilhado tem [README próprio](src/Kernel/README.md), com a regra que decide
quando uma falha é `Result` e quando é exceção. É a única parte dele que precisa estar na
cabeça de quem escreve um handler.

O host é o único lugar que monta o pipeline, e ele fica visível no `Program.cs`:

```csharp
builder.Services.AddKernelApplication();
builder.Services.AddLoggingBehavior();
```

A segunda linha é opcional por desenho — **a ordem das linhas é a ordem dos anéis**, e
escondê-la dentro do registro do mediator tiraria do `Program.cs` a única visão que existe
do pipeline inteiro. O porquê de cada anel ficar onde fica está no README do kernel.

**Falta aqui uma terceira linha, e ela chega com o primeiro módulo:** o registro do anel de
transação, que precisa do `DbContext` do módulo para existir. O behavior já está escrito em
`Kernel.Infrastructure`; quem o registra é o `Program.cs`, depois do anel de log, com a
subclasse de três linhas que o [README do kernel](src/Kernel/README.md) mostra.

**Cada projeto de origem tem o próprio projeto de teste.** Um projeto de teste único
precisaria referenciar todos os módulos, e seria o único assembly onde tipos de módulos
diferentes coexistem — daria para escrever um teste que prova algo que o código de
produção não consegue fazer. Além disso os tipos de teste são diferentes: os do kernel são
unitários e rodam em menos de meio segundo, enquanto os do host sobem a aplicação inteira.

Os testes **do host** são de dois tipos, e a diferença não é estilo. A maioria sobe a
aplicação inteira em memória, com `WebApplicationFactory`, e bate nos endpoints por HTTP —
sem mock e sem porta de rede, é o `Program.cs` de verdade que responde. Para um host desta
espessura, testar por dentro não valeria a pena: o que pode quebrar é justamente o
encanamento entre rota, middleware e serviço registrado, e é o que só aparece subindo tudo.

As exceções são o `ErrorResultsTests` e o `GlobalExceptionHandlerTests`, os dois unitários: um
chama `ToProblem()` e inspeciona o `ProblemHttpResult`, o outro chama `TryHandleAsync` com um
`DefaultHttpContext`. A tabela `ErrorType` → status é função pura, e o handler precisa de
estados que uma requisição de verdade não produz sob comando — cliente desistindo, resposta já
iniciada. Subir a aplicação para isso tornaria a falha mais lenta e menos legível. **O preço
está declarado** em [O que os testes não cobrem](#o-que-os-testes-não-cobrem).

Os projetos se chamam `.Tests` de propósito: o `Directory.Build.props` reconhece o sufixo e
relaxa as regras que brigam com teste. A localização em `tests/` é livre — a condição é
por nome, não por pasta.

**xUnit v3 sobre a Microsoft.Testing.Platform.** Isso muda a forma do projeto: ele é um
`Exe`, não uma biblioteca, porque cada assembly de teste hospeda o próprio runner. Some
o `Microsoft.NET.Test.Sdk` e o `xunit.runner.visualstudio` — a plataforma substitui os
dois. O `global.json` escolhe esse runner para o `dotnet test`.

Uma consequência prática: argumentos que o `dotnet test` antigo aceitava agora são
repassados ao executável de teste, e ele recusa o que não conhece. `dotnet test --nologo`,
por exemplo, falha com "opção desconhecida" e **zero testes executados** — o que parece
suíte quebrada, mas é só a flag.

O destino é o monólito modular estrito do
[ADR-0012](../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md): um
projeto por módulo, e **módulos não se referenciam** — conversam por contratos e eventos
no kernel compartilhado. Isso torna a fronteira uma garantia de compilação, não uma
convenção que alguém precisa lembrar. Do destino existem o kernel e o primeiro módulo,
`CathedrAll.Pessoas`.

## O módulo `Pessoas`

`src/Modules/CathedrAll.Pessoas/`, com as pastas do ADR-0012. Hoje só `Domain/` e
`Infrastructure/` têm conteúdo — `Application/` e `Endpoints/` aparecem quando houver o quê
pôr nelas.

**Todo tipo do módulo é `internal`, com uma exceção: `AddPessoasModule`.** É por ela que o
host compõe o módulo, e é tudo o que ele enxerga — nem o `PessoasDbContext` nem `Pessoa`
são alcançáveis de fora. Quem escolhe provider e connection string é o `Program.cs`; o
módulo recebe a configuração como lambda e acrescenta a convenção de nome
([ADR-0015](../../docs/adr/0015-um-dbcontext-e-migrations-por-modulo.md)).

**Nenhum `Guid` cru atravessa o `Domain/`:** `PessoaId` e `VinculoIgrejaId` são
`readonly record struct`, com um `ValueConverter` de uma linha cada
([ADR-0017](../../docs/adr/0017-ids-fortemente-tipados.md)). `Celular` e `Email` são objetos
de valor e usam o mesmo padrão de conversor.

**As entidades são estado e mapeamento, sem comportamento.** Não há fábrica, invariante nem
método de transição — eles vêm nas tarefas seguintes, e separá-los é o que fez a migration
nascer antes de qualquer regra poder atrasá-la. Uma consequência disso é visível no código:
as propriedades opcionais são `{ get; init; }`, e não `{ get; private set; }`, porque o
Sonar recusa setter privado sem chamador (`S1144`) e o build trata aviso como erro. Elas
viram `private set` quando os métodos existirem.

### Duas armadilhas do EF que custaram tempo

**O EF só mapeia propriedade que consegue escrever.** `{ get; }` puro não conta — o
*backing field* que o compilador gera é `readonly`. Sem parâmetro de construtor de mesmo
nome, a coluna **some do modelo em silêncio**, com build verde; com parâmetro, o tipo
inteiro é recusado em tempo de execução. `Id` é a exceção, e só porque o `HasKey` força a
propriedade a entrar no modelo.

**O código gerado pelo `dotnet ef` não passa nos analisadores daqui.** `IDE0161`, `IDE0053`
e `CA1861` disparam em toda migration, e com `TreatWarningsAsErrors` isso é build vermelho.
O `.editorconfig` marca `[**/Migrations/*.cs] generated_code = true`, que faz todo
analisador ignorá-las — em vez de silenciar uma regra por vez a cada `migrations add`.

### O schema

Schema `pessoas`, com o `__EFMigrationsHistory` dentro dele. Tabelas e colunas em
`snake_case`, via `EFCore.NamingConventions`: o default do EF no PostgreSQL é PascalCase
entre aspas, o que obriga a citar identificador em toda query escrita à mão no `psql`.

`Situacao` e `EstadoCivil` são gravados como **texto**, não como inteiro. Custa alguns bytes
e paga na leitura direta do banco — e o modo de falha é alto: renomear um membro do enum
estoura na materialização, enquanto com inteiro reordenar valores mudaria o significado de
toda linha existente sem erro nenhum.

**Só `nome` é `NOT NULL`**, e **não existe restrição de unicidade em coluna nenhuma** — a
ficha não tem CPF nem documento, e duplicata se resolve com fusão, não com constraint.
`convidado_por_id` e `fundida_em_id` são `uuid` sem chave estrangeira e sem propriedade de
navegação. As colunas de auditoria **não existem ainda**: elas chegam com o interceptor que
as escreve ([ADR-0018](../../docs/adr/0018-auditoria-fora-da-entidade.md)).

## Build

Quatro arquivos governam o build, todos na raiz de `apps/api` e aplicados a qualquer
projeto criado abaixo dela:

| Arquivo | Papel |
| --- | --- |
| `Directory.Build.props` | Propriedades comuns e analisadores |
| `Directory.Packages.props` | Versão dos pacotes, centralizada |
| `.editorconfig` | Convenções de C#, complementa o da raiz do monorepo |
| `global.json` | Versão do SDK e runner de teste |

**Aviso é erro** (`TreatWarningsAsErrors`), e a análise roda em `AnalysisMode=All` com
StyleCop e Sonar. É severo de propósito: com um mantenedor só, aviso ignorado vira aviso
permanente. O custo de discutir estilo em revisão é maior que o de o compilador decidir.

Projetos cujo nome termina em `.Tests` relaxam as regras que brigam com teste — nome em
frase (`Deve_criar_pessoa`) viola `CA1707`, método sem estado viola `CA1822`. A condição é
por **nome de projeto**, não por pasta, para não amarrar onde os testes vão morar.

O `.editorconfig` daqui não define fim de linha de propósito. A fonte de verdade é o
`.gitattributes` da raiz. Se os dois divergirem, editor e git brigam pelo mesmo arquivo a
cada save.

## Antes do primeiro CRUD

Dado de membro de igreja é dado pessoal sensível (LGPD). Nada disso é backlog — vem antes
do primeiro endpoint que toque em `Pessoa`:

- [x] **Transação por requisição** — o anel existe; `Pessoas` ainda não o registra
- [x] **Usuário atual** — `ICurrentUser`, com implementação de desenvolvimento
- [ ] **Audit log** por `SaveChangesInterceptor`, em tabela append-only
- [ ] **Soft delete** com filtro global de consulta
- [ ] **RBAC com escopo** — líder enxerga apenas o próprio departamento

Os três que faltam penduram no `DbContext`, e a forma de persistência está decidida no
[ADR-0015](../../docs/adr/0015-um-dbcontext-e-migrations-por-modulo.md): um `DbContext` por
módulo, com schema próprio. A auditoria fica **dentro** da transação, porque pendura no
`SaveChanges` — é o anel de transação que garante isso, e é por isso que ele vem primeiro.
