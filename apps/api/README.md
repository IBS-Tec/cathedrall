# apps/api — API do CathedrAll

.NET 10, ASP.NET Core, Minimal API. Domínio: `api.ibscristo.com.br`.

> **Estado: host, kernel, mediator e o formato de erro.** Existem o host com `/health`, a
> configuração de build, o kernel de domínio — `Result`, `Error`, `ErrorType`, `Entity`,
> `AggregateRoot`, `DomainEvent` —, o mediator com um único behavior, o de log, e o mapeador
> de `Error` para HTTP descrito em [Formato de erro](#formato-de-erro). O kernel está em
> [`src/Kernel/README.md`](src/Kernel/README.md). Nenhum módulo, nenhum acesso a banco,
> nenhuma autenticação, nenhum handler, nenhum `IExceptionHandler` global. A API está sendo
> reconstruída do zero, em passos pequenos. Este README descreve só o que já existe — se
> algo não estiver aqui, não foi construído ainda.

## Comandos

```bash
cd apps/api
dotnet build
dotnet test
dotnet run --project src/Bootstrapper/CathedrAll.Api
```

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

Três peças no `Program.cs` produzem esse formato, e as três são necessárias:

| Peça | Cobre |
| --- | --- |
| `ErrorResults.ToProblem()` | a falha de um `Result` nosso |
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

**O que ainda não está coberto na rede: o campo `code`.** O 404 do framework não tem um, e
nenhum endpoint devolve `Result` ainda, então hoje o `code` é verificado só por teste unitário
sobre o `ProblemHttpResult`. O primeiro endpoint de módulo é quem fecha essa lacuna.

## Estrutura

```
src/
  Bootstrapper/
    CathedrAll.Api/                       host; sobe a aplicação, registra o
                                          mediator e o pipeline, mapeia /health
                                          e converte Error em ProblemDetails
  Kernel/
    CathedrAll.Kernel.Domain/             Result, Error, ErrorType, Entity
    CathedrAll.Kernel.Application/        mediator, pipeline, LoggingBehavior
tests/
  CathedrAll.Api.Tests/                   testes do host: integração e mapeamento
  CathedrAll.Kernel.Domain.Tests/         testes unitários do kernel de domínio
  CathedrAll.Kernel.Application.Tests/    testes unitários do mediator
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

A exceção é o `ErrorResultsTests`, que é unitário: chama `ToProblem()` e inspeciona o
`ProblemHttpResult` devolvido, sem HTTP. A tabela `ErrorType` → status é uma função pura, e
subir a aplicação para conferir quatro linhas de `switch` só tornaria a falha mais lenta e
menos legível. **O preço disso está declarado** em [Formato de erro](#formato-de-erro): o
campo `code` fica sem prova na rede até o primeiro endpoint devolver `Result`.

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
convenção que alguém precisa lembrar. Do destino existe só o começo do kernel — nenhum
módulo foi escrito.

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

- [ ] **Audit log** por `SaveChangesInterceptor`, em tabela append-only
- [ ] **Soft delete** com filtro global de consulta
- [ ] **RBAC com escopo** — líder enxerga apenas o próprio departamento
