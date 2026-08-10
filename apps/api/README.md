# apps/api — API do CathedrAll

.NET 10, ASP.NET Core, Minimal API. Monólito modular estrito com DDD tático
([ADR-0012](../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md)).
Domínio: `api.ibscristo.com.br`.

## Comandos

```bash
cd apps/api
dotnet build
dotnet test
dotnet run --project src/CathedrAll.Api
```

Em desenvolvimento, o documento OpenAPI fica em `/openapi/v1.json`. `/health` é anônimo e
existe para o monitoramento externo.

## Estrutura

```
src/
  CathedrAll.Api/         host: Program.cs, composição, /health
  CathedrAll.Kernel/      mediator, behaviors, IModule, Result — ver README próprio
  Modulos/
    CathedrAll.Pessoas/   um projeto por módulo
tests/
  CathedrAll.Tests/
```

**Um projeto por módulo, e módulos não se referenciam.** Conversam por contratos e eventos
publicados no Kernel. Isso torna a fronteira uma garantia de compilação: `Escalas` não
alcança as entidades de `Pessoas` por acidente.

Dentro de cada módulo, vertical slice: `Domain/`, `Application/`, `Infrastructure/`,
`Endpoints/`. **Nada de pastas genéricas por tipo técnico** (`Services/`, `Repositories/`,
`DTOs/`) — regra que sobreviveu do ADR-0004.

Handlers são `internal`. Se um tipo precisa ser público, pergunte-se quem fora do módulo
deveria enxergá-lo.

## Antes do primeiro CRUD

Dado de membro de igreja é dado pessoal sensível (LGPD). Nada disso é backlog:

- [ ] **Audit log** por `SaveChangesInterceptor`, em tabela append-only
- [ ] **Soft delete** com filtro global de consulta
- [ ] **RBAC com escopo** — líder enxerga apenas o próprio departamento
- [x] **`ForwardedHeaders`** restrito a origens confiáveis
- [x] **`/health`** para monitoramento externo
- [x] **OpenAPI** exposto — contrato de onde nasce `packages/api-client`

O `MapGroup("/api/pessoas")` está sem `RequireAuthorization()` porque o RBAC ainda não
existe. **Nenhum endpoint pode ser adicionado ali antes disso.**

## Configuração

`ProxiesConfiaveis` — lista de IPs do proxy reverso. Vazia por padrão, e vazia significa
que os cabeçalhos `X-Forwarded-*` são **ignorados**. Confiar neles sem restringir a origem
torna o IP do cliente falsificável, e o audit log passa a registrar mentira convincente
([ADR-0010](../../docs/adr/0010-cloudflare-tunnel-como-ingress.md)).

## Banco

`cathedrall`, no mesmo Postgres do CMS, com usuário próprio e sem permissão de conexão
cruzada — verificado empiricamente. Sobe com `infra/compose`.

## Nota sobre dependências

`Microsoft.OpenApi` tem referência direta apenas para sobrepor uma transitiva com
vulnerabilidade conhecida. O motivo está comentado no `CathedrAll.Api.csproj`. O build
trata aviso como erro, incluindo alerta de vulnerabilidade — se falhar por `NU1903`, é
isso.
