# apps/api — API do CathedrAll

.NET, ASP.NET Core, monólito modular. Domínio: `api.ibscristo.com.br`.

> **Status:** apenas estrutura de pastas. Nenhum projeto .NET criado, nenhuma solution,
> nenhum código.

## Projetos planejados

```
src/
  CathedrAll.Api/              endpoints, autenticação, OpenAPI, composição
    Modules/
      Pessoas/                 endpoints + handlers + modelos do módulo, juntos
      Departamentos/
      Eventos/
      Escalas/
  CathedrAll.Domain/           entidades, regras, invariantes — sem dependência de infra
  CathedrAll.Infrastructure/   EF Core, migrations, integrações externas
tests/
  CathedrAll.Tests/
```

## Organização

Vertical slices. Uma pasta por módulo, contendo tudo daquele módulo. **Não** criar
`Services/`, `Repositories/` e `DTOs/` no nível raiz — pastas por tipo técnico viram
depósito e obrigam a abrir quatro diretórios para entender uma funcionalidade.

## Na fundação, não no backlog

Dado de membro de igreja é dado pessoal sensível (LGPD). Antes do primeiro CRUD:

- **Audit log** de leitura e escrita sobre dados de pessoa
- **RBAC com escopo** — líder enxerga apenas o próprio departamento
- **Soft delete** e política de retenção
- **`ForwardedHeaders` configurado para origens confiáveis** — atrás de Cloudflare Tunnel
  e Traefik, a aplicação enxerga o IP do container, não o do visitante. Sem isso o audit
  log registra o IP errado, o que é pior do que não registrar: parece correto
  ([ADR-0010](../../docs/adr/0010-cloudflare-tunnel-como-ingress.md))

Retrofitar qualquer um dos três depois de haver dado real é caro.

## Superfície pública

`/public/*` é a única parte sem autenticação, somente leitura, e expõe **apenas** o que
estiver explicitamente marcado como público (`Evento.Publico`). Nunca dado de pessoa.
Todo endpoint novo sob `/public` merece revisão dedicada.

## Referências

- [ADR-0004](../../docs/adr/0004-backend-dotnet-monolito-modular.md)
- [ADR-0006](../../docs/adr/0006-postgresql.md)
- [ADR-0008](../../docs/adr/0008-pessoa-como-raiz-unica.md)
