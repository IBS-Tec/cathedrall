# ADR-0004 — Backend em .NET, monólito modular com vertical slices

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O mantenedor principal é experiente em .NET. O time restante é composto por
desenvolvedores iniciantes e voluntários, com rotatividade esperada alta. A escala é de
centenas de pessoas, não milhares.

## Decisão

.NET (LTS), ASP.NET Core, **monólito modular** organizado em *vertical slices*: uma pasta
por módulo (`Pessoas/`, `Departamentos/`, `Eventos/`, `Escalas/`), cada uma com seus
próprios endpoints, handlers e modelos.

Explicitamente **não**: microsserviços, CQRS com event sourcing, MediatR, arquitetura em
camadas com `Services/` + `Repositories/` + `DTOs/` genéricos.

## Motivos

- Produtividade do mantenedor principal é o recurso mais escasso do projeto.
- O maior risco é **bus factor 1**. Escolha entediante vence escolha elegante: se o
  mantenedor ficar indisponível por três meses, o sistema precisa continuar de pé e
  alguém precisa conseguir mexer.
- Pastas genéricas por tipo técnico viram depósito. Vertical slice mantém junto o que
  muda junto e é mais fácil para iniciante navegar: tudo sobre escala está em `Escalas/`.
- Microsserviços resolvem problema organizacional de times grandes. Não há times grandes.

## Consequências

- Deploy é único e indivisível. Aceitável.
- Compartilhar código entre módulos exige disciplina para não virar acoplamento
  acidental. Sem tribunal de arquitetura: se dois módulos precisam da mesma coisa,
  duplicar é preferível a criar abstração prematura.
- Auditoria (`SaveChangesInterceptor`) e RBAC com escopo entram na fundação, não no
  backlog — LGPD exige (ver `docs/arquitetura.md`).
- Se o sistema virar produto multi-igreja, parte disso será revisitada. Custo aceito.
