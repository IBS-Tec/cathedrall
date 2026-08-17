# CathedrAll

Monorepo da Igreja Bíblica Semear — Cristo Redentor.

Contém dois produtos distintos que compartilham identidade visual e uma fonte de dados
de eventos, mas nada além disso:

| Produto | O quê | Público |
|---|---|---|
| **Site institucional** | `ibscristo.com.br` — conteúdo editorial, estático | Visitantes, público geral |
| **CathedrAll** | `app.ibscristo.com.br` — sistema de gestão eclesiástica | Secretaria, líderes, pastores |

> **Status:** em construção. As três aplicações sobem; nenhuma está publicada.
>
> | Peça | Situação |
> |---|---|
> | Site | Casca no ar, consumindo o CMS. Falta conteúdo e as páginas internas |
> | CMS | 8 coleções modeladas, schema versionado. Conteúdo em preenchimento |
> | Admin | Roteamento, estado de servidor e formulários validados em pt-BR |
> | API | Estrutura, mediator e `/health`. Sem persistência e sem domínio |

## Estrutura

```
apps/
  site/         Astro — site institucional (SSG, consome o CMS em build time)
  admin/        React + Vite — SPA do CathedrAll
  api/          .NET — API do CathedrAll (monólito modular)
packages/
  api-client/   Cliente TypeScript gerado a partir do OpenAPI da API
  brand/        Tokens de marca compartilhados entre site e admin
infra/
  compose/      Docker Compose (Postgres + Directus)
  cms/          Schema do Directus versionado e scripts de provisionamento
  tunnel/       Configuração do Cloudflare Tunnel
  backup/       Scripts de backup/restauração
docs/
  setup.md              do clone até tudo rodando
  arquitetura.md        fronteiras entre as peças
  dominio.md            modelo de domínio do MVP
  site-mapa-de-paginas.md
  runbook.md            operação e recuperação
  adr/                  registros de decisão arquitetural — o porquê
  specs/                o que cada módulo faz, em detalhe de implementação
```

## Por onde começar

**Acabou de clonar?** [`docs/setup.md`](docs/setup.md) leva do zero a tudo rodando.

1. Leia [`docs/arquitetura.md`](docs/arquitetura.md) — visão geral e fronteiras entre as peças.
2. Leia [`docs/adr/`](docs/adr/) — o *porquê* de cada escolha. Antes de propor uma mudança
   de stack, veja se a decisão já foi tomada e qual foi o trade-off aceito.
3. Leia [`docs/dominio.md`](docs/dominio.md) — o modelo de domínio do MVP.
4. Vai contribuir? [`CONTRIBUTING.md`](CONTRIBUTING.md) — o fluxo do trabalho, a Definição
   de Pronta e a Definição de Feita.

## Pré-requisitos

- Node.js 22+ e pnpm 11+
- .NET SDK 10.0.302+ (fixado por `apps/api/global.json`)
- Docker + Docker Compose v2

Passo a passo completo em [`docs/setup.md`](docs/setup.md).

## Contribuindo em Linux e Windows

O time é misto. As armadilhas conhecidas:

**Fim de linha — não configure nada.** O [`.gitattributes`](.gitattributes) tem precedência
sobre `core.autocrlf` e normaliza tudo para LF, inclusive na cópia de trabalho no Windows.
Não rode `git config core.autocrlf true`: ele será ignorado onde importa e vai te confundir
onde não importa. O motivo de LF na cópia de trabalho é que o código roda em container
Linux — o que você edita deve ser byte a byte o que entra no container. Script `.sh` com
CRLF falha lá dentro com `bad interpreter: /bin/sh^M`.

Se o `.gitattributes` mudar depois de haver arquivos versionados, rode uma vez:
`git add --renormalize . && git commit -m "renormaliza fim de linha"`.

**Maiúsculas e minúsculas.** Linux diferencia; Windows não. Renomear `Pessoa.cs` para
`pessoa.cs` no Windows não gera mudança visível para o git local, mas quebra o build no
Linux e no CI. Para renomear só a caixa, faça em dois passos com `git mv`.

**Caminhos longos (Windows).** `node_modules` aninhado estoura o limite de 260 caracteres.
Habilite antes do primeiro `pnpm install`:
`git config --global core.longpaths true` (e `LongPathsEnabled` no Windows).

**Docker no Windows:** Docker Desktop com backend WSL2, e mantenha o repositório **dentro**
do sistema de arquivos do WSL, não em `/mnt/c/...` — do lado errado, o I/O fica ordens de
grandeza mais lento e o hot reload deixa de funcionar direito.

## Convenções

- **Inglês é o idioma de código**; o vocabulário de negócio é a exceção e fica em português.
  Padrão de projeto usa o nome consagrado (`Factory`, `Repository`, `Handler`); entidade da
  igreja usa o nome da igreja (`Pessoa`, `Departamento`, `Escala`). Todo texto de UI e todo
  nome de método de teste em português. Na dúvida, inglês —
  [ADR-0013](docs/adr/0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md).
- Um PR por assunto. CI verde é obrigatório para merge.
- Decisão arquitetural relevante vira ADR em `docs/adr/` antes de virar código.

## Licença

Copyright (C) 2026 Igreja Bíblica Semear — Cristo Redentor

Este programa é software livre: você pode redistribuí-lo e/ou modificá-lo sob os termos da
**GNU Affero General Public License**, versão 3, publicada pela Free Software Foundation.
O texto completo está em [`LICENSE`](LICENSE).

Ele é distribuído na esperança de ser útil, mas **sem nenhuma garantia** — nem mesmo a
garantia implícita de comercialização ou adequação a um fim específico.

A escolha da AGPL é deliberada: qualquer igreja pode usar, adaptar e cobrar por serviço em
cima disto. O que não pode é rodar uma versão modificada como serviço na internet sem
oferecer o código dessa versão a quem a usa — é o artigo 13 da licença, e é o que impede
que o trabalho de voluntários vire produto fechado de outra pessoa.

A obrigação vale para nós também: `app.ibscristo.com.br` roda este código, e é por isso que
este repositório é público.
