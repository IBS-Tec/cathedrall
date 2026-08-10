# ADR-0001 — Monorepo único para site, SPA e API

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O projeto tem três aplicações (site Astro, SPA React, API .NET) e duas linguagens
principais. A alternativa seria um repositório por aplicação.

## Decisão

Repositório único, com `apps/` para as aplicações e `packages/` para código compartilhado.
`pnpm workspaces` gerencia o lado JavaScript; o lado .NET fica fora do workspace pnpm.

Sem Nx e sem Turborepo. Overhead de configuração sem retorno em três aplicações.

## Motivos

- **Mudança atômica de contrato:** alterar um endpoint da API e o consumidor no mesmo
  commit. Este é o benefício principal e sozinho já justifica a decisão.
- **Um só lugar** para issues, CI e documentação, num time de voluntários pequeno.
- Contexto unificado para ferramentas de IA — benefício real, mas o mais fraco da lista.
  Não seria motivo suficiente sozinho.

## Consequências

- CI **obrigatoriamente** com filtro de path desde o primeiro workflow. Sem isso, editar
  um arquivo de conteúdo dispara build de .NET. Não é otimização; é requisito.
- Permissões são do repositório inteiro. Quem tem acesso ao site tem acesso ao código da
  API. Aceitável enquanto o time for pequeno e de confiança.
- Se um dia o site institucional for entregue a terceiros (agência), a extração para
  repositório próprio vai custar trabalho.
