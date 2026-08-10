# ADR-0007 — Congregação única, sem multi-tenancy

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O sistema nasce para a Igreja Bíblica Semear — Cristo Redentor. Existe possibilidade real
de outras igrejas quererem adotá-lo no futuro, mas nenhuma demanda concreta hoje.

Três caminhos foram considerados:

1. Single-tenant puro.
2. Single-tenant com `CongregacaoId` já presente nas tabelas centrais.
3. Multi-tenancy real (isolamento por schema ou RLS, onboarding, billing).

## Decisão

**Opção 1: single-tenant puro.** Nada de `CongregacaoId`, nada de conceito de "unidade" no
domínio. Prioridade explícita à simplicidade do MVP.

Se outra igreja quiser usar antes de existir multi-tenancy, a saída é **subir outra
instância isolada** — banco próprio, deploy próprio. Para duas ou três igrejas isso é
perfeitamente sustentável e provavelmente preferível.

## Motivos

- A opção 3 triplicaria a complexidade do MVP em nome de uma demanda que não existe.
- A opção 2 é barata, mas carrega um campo que ninguém usa por toda a base de código,
  convidando a implementações pela metade — o pior dos mundos: complexidade sem
  isolamento real.
- Instâncias separadas dão isolamento **mais forte** que multi-tenancy lógico, o que é
  um argumento relevante tratando-se de dado pessoal sensível.

## Consequências

- Se a decisão for revertida com dado real em produção, a migração vai doer: adicionar a
  coluna, retropreencher, revisar **toda** consulta, e auditar cada caminho de leitura em
  busca de vazamento entre igrejas. Custo aceito conscientemente.
- O gatilho para reavaliar é concreto: **a terceira igreja interessada**. Com duas,
  instâncias separadas ainda valem mais a pena. Na terceira, escreva o ADR que substitui
  este.
- Nada no código deve assumir "existe só uma igreja no mundo" de forma gratuita. Evite
  singletons de configuração global e IDs fixos onde um parâmetro serviria — não por
  multi-tenancy, mas por higiene.
