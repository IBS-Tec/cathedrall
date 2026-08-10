# Workflows

> **Status:** vazio. Nenhum workflow criado ainda.

## Requisito ao criar o primeiro

**Filtro de path é obrigatório**, não otimização. Em monorepo sem filtro, editar um
arquivo de conteúdo do site dispara build de .NET, e em pouco tempo ninguém mais olha
para o CI.

Um workflow por aplicação, cada um disparado apenas pelo que lhe diz respeito:

| Workflow | Dispara em | Faz |
|---|---|---|
| `site` | `apps/site/**`, `packages/brand/**` | lint, typecheck, build |
| `admin` | `apps/admin/**`, `packages/**` | lint, typecheck, build |
| `api` | `apps/api/**` | build, testes |

Ver [ADR-0001](../../docs/adr/0001-monorepo-unico.md).
