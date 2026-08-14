# CathedrAll — contexto para agentes

Monorepo da Igreja Bíblica Semear — Cristo Redentor. Dois produtos: o site institucional
(`ibscristo.com.br`) e o sistema de gestão CathedrAll (`app.ibscristo.com.br`).

**Estado atual: fundação.** O site (Astro) e o admin (React) sobem localmente. A API
(.NET 10) tem host, `/health` e o kernel de domínio — `Result`, `Error`, `Entity`,
`AggregateRoot`, `DomainEvent`. O Compose de `infra/` levanta Postgres e Directus com
bancos e usuários separados. Cada PR passa por CI de api, admin e site.

**Ainda não existem:** módulo de negócio, acesso a banco pela API, autenticação, mediator,
audit log, e nenhum ambiente publicado. Cada README descreve só o que já está escrito —
comece por eles, não pelo código.

## Leia antes de propor mudanças

- `docs/arquitetura.md` — visão geral e, principalmente, as **fronteiras** entre as peças
- `docs/dominio.md` — modelo de domínio do MVP
- `docs/adr/` — o porquê de cada escolha e os trade-offs aceitos

Antes de sugerir troca de stack ou de abordagem, verifique se já existe ADR sobre o
assunto. Se existir e ainda assim houver motivo para mudar, proponha um ADR novo que
substitua o anterior — não edite o antigo.

## Invariantes

Estas regras não são preferência de estilo; violá-las quebra a arquitetura.

1. **Directus jamais enxerga dado de pessoa.** Databases separados, usuários separados.
2. **`/public/*` da API** é somente leitura, sem autenticação, e expõe apenas o que está
   explicitamente marcado como público. Nunca dado de pessoa.
3. **O site é estático** e consome CMS e API somente em tempo de build. Sem SSR.
4. **`Pessoa` é a raiz única de cadastro.** Membro e visitante são situação de vínculo;
   trabalhador é consulta por alocação ativa. Não criar entidades separadas.
5. **Nenhum `fetch` escrito à mão** na SPA. Só o cliente gerado em `packages/api-client`.
6. **Audit log, RBAC com escopo e soft delete** vêm antes do primeiro CRUD, não depois.
   Dado de membro de igreja é dado pessoal sensível pela LGPD.
7. **Agenda tem uma fonte de verdade só:** o CathedrAll. Não duplicar eventos no CMS.

## Convenções

- Domínio e UI em **português** (`Pessoa`, `Departamento`, `Escala`). Infraestrutura e
  termos de framework em inglês. Não misturar dentro da mesma camada.
- Vertical slices dos dois lados: `apps/api/src/CathedrAll.Api/Modules/<Modulo>/` e
  `apps/admin/src/modules/<modulo>/` se espelham.
- Nada de pastas genéricas por tipo técnico (`Services/`, `Repositories/`, `DTOs/`).
- Nada de segredo versionado. Só `.env.example`.

## Contexto do time

Um mantenedor experiente em .NET e desenvolvedores iniciantes voluntários, com
rotatividade alta. **Bus factor 1 é o maior risco do projeto.** Prefira sempre a solução
entediante e legível à elegante e clever. Se uma abstração exige explicação, ela
provavelmente não vale o custo aqui.
