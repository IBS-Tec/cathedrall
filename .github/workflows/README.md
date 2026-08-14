# Workflows

Um workflow por aplicação, cada um disparado apenas pelo que lhe diz respeito
([ADR-0001](../../docs/adr/0001-monorepo-unico.md)).

| Workflow | Reage a | Faz |
|---|---|---|
| `api` | `apps/api/**` | `dotnet build` e `dotnet test` em Release |
| `admin` | `apps/admin/**`, `packages/**`, manifestos pnpm | lint, typecheck, build |
| `site` | `apps/site/**`, `packages/brand/**`, manifestos pnpm | `astro check` |

Cada workflow também reage ao próprio arquivo, para que mudar o CI exercite o CI.

## Por que o filtro de path não está no `on:`

O jeito óbvio de filtrar é `on: pull_request: paths:`. Ele é incompatível com exigir
checagem verde para aprovar PR: quando os paths não batem, o workflow **não roda**, a
checagem obrigatória nunca aparece, e o PR fica travado em "esperando" para sempre.

Por isso cada workflow dispara em todo PR e decide dentro: o job `filtro` compara os
arquivos alterados e o job de verificação só executa se houver o que verificar. Job pulado
conta como sucesso para a proteção de branch, então a checagem aparece e o PR não trava.

O motivo do filtro continua valendo integralmente — editar conteúdo do site **não** dispara
build de .NET. O que roda é um job de segundos que decide não fazer nada.

## Checagens obrigatórias

São os nomes dos jobs de verificação, um por aplicação:

- `api`
- `admin`
- `site`

Os jobs `filtro (…)` não entram: eles sempre passam e não dizem nada sobre o código.

## O que ainda não está aqui

**Build do site.** `astro build` busca o Directus em tempo de build
([ADR-0002](../../docs/adr/0002-site-astro-estatico.md)) e falha com `fetch failed` sem CMS
acessível. Enquanto o Directus não estiver de pé, o workflow roda só `astro check`, que é
análise estática e não depende de rede. Quando houver CMS, o build entra com `DIRECTUS_URL`
vindo de secret — e aí o CI passa a depender da disponibilidade dele, o que precisa ser
decisão consciente.

**CD.** Nada aqui faz deploy. O Dokploy constrói na própria máquina de produção
([ADR-0009](../../docs/adr/0009-hospedagem-unificada-dokploy.md)), e o item "Fazer deploy"
do `docs/runbook.md` continua aberto — não há ambiente para onde publicar. Quando houver, o
passo é disparar o Dokploy, não construir aqui.

**Cobertura, análise de segurança e verificação de dependências.** Nenhum existe. Entram
quando alguém for usar o resultado; métrica que ninguém olha só deixa o CI mais lento.
