# apps/api — API do CathedrAll

.NET 10, ASP.NET Core, Minimal API. Domínio: `api.ibscristo.com.br`.

> **Estado: quase vazia.** Existe o host, a configuração de build e o `/health`. Nenhum
> módulo, nenhum acesso a banco, nenhuma autenticação. A API está sendo reconstruída do
> zero, em passos pequenos. Este README descreve só o que já existe — se algo não estiver
> aqui, não foi construído ainda.

## Comandos

```bash
cd apps/api
dotnet build
dotnet test
dotnet run --project src/Bootstrapper/CathedrAll.Api
```

## Endpoints

| Rota | Auth | Descrição |
| --- | --- | --- |
| `GET /health` | anônimo | `200 Healthy` ou `503 Unhealthy`, em texto puro |

`/health` é anônimo de propósito — o monitoramento externo não tem credencial. Por isso
ele **não** expõe detalhes: um health check tagarela conta a um desconhecido quais
dependências você tem e como elas falham. Se um dia precisarmos de diagnóstico detalhado,
ele vai numa rota separada e autenticada, não nesta.

## Estrutura

```
src/
  Bootstrapper/
    CathedrAll.Api/       host; sobe a aplicação e mapeia /health
tests/
  CathedrAll.Api.Tests/   testes de integração do host
```

Os testes sobem a aplicação inteira em memória, com `WebApplicationFactory`, e batem nos
endpoints por HTTP. Sem mock e sem porta de rede: é o `Program.cs` de verdade que
responde. Para um host desta espessura, testar por dentro não valeria a pena — o que pode
quebrar é justamente o encanamento entre rota, middleware e serviço registrado.

O projeto se chama `.Tests` de propósito: o `Directory.Build.props` reconhece o sufixo e
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
convenção que alguém precisa lembrar. Nada disso existe ainda.

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
