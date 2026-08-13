# apps/api — API do CathedrAll

.NET 10, ASP.NET Core, Minimal API. Domínio: `api.ibscristo.com.br`.

> **Estado: casca vazia.** Existe o host e a configuração de build. Nenhum endpoint,
> nenhum módulo, nenhum acesso a banco. A API está sendo reconstruída do zero, em passos
> pequenos. Este README descreve só o que já existe — se algo não estiver aqui, não foi
> construído ainda.

## Comandos

```bash
cd apps/api
dotnet build
dotnet run --project src/Bootstrapper/CathedrAll.Api
```

Ainda não há `dotnet test`: o projeto de testes não foi criado.

## Estrutura

```
src/
  Bootstrapper/
    CathedrAll.Api/       host; hoje só sobe a aplicação
```

O destino é o monólito modular estrito do
[ADR-0012](../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md): um
projeto por módulo, e **módulos não se referenciam** — conversam por contratos e eventos
no kernel compartilhado. Isso torna a fronteira uma garantia de compilação, não uma
convenção que alguém precisa lembrar. Nada disso existe ainda.

## Build

Três arquivos governam o build, todos na raiz de `apps/api` e aplicados a qualquer
projeto criado abaixo dela:

| Arquivo | Papel |
| --- | --- |
| `Directory.Build.props` | Propriedades comuns e analisadores |
| `Directory.Packages.props` | Versão dos pacotes, centralizada |
| `.editorconfig` | Convenções de C#, complementa o da raiz do monorepo |

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
