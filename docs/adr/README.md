# Registros de Decisão Arquitetural (ADR)

Um arquivo por decisão, numerado, imutável depois de aceito. Se a decisão mudar, escreva
um ADR novo que a substitua (`Substitui o ADR-000X`) — não edite o antigo. O valor está
em preservar o raciocínio da época, inclusive quando ele se mostrou errado.

Formato: contexto → decisão → consequências (as boas **e** as ruins aceitas).

Escreva um ADR quando a decisão for cara de reverter: escolha de stack, modelo de dados
central, fronteira entre sistemas, estratégia de autenticação. Não escreva para escolha
de biblioteca de máscara de CPF.

| # | Decisão | Status |
|---|---|---|
| [0001](0001-monorepo-unico.md) | Monorepo único para site, SPA e API | Aceito |
| [0002](0002-site-astro-estatico.md) | Site institucional em Astro, estático | Aceito · hospedagem revista pelo [0009](0009-hospedagem-unificada-dokploy.md) |
| [0003](0003-cms-directus-self-hosted.md) | Directus self-hosted como CMS | Aceito |
| [0004](0004-backend-dotnet-monolito-modular.md) | Backend .NET, monólito modular | Aceito |
| [0005](0005-frontend-react-spa-com-trilhos.md) | SPA React com trilhos rígidos | Aceito · componentes revistos pelo [0011](0011-shadcn-tailwind-nos-dois-frontends.md) |
| [0006](0006-postgresql.md) | PostgreSQL como banco único | Aceito · persistência detalhada pelo [0015](0015-um-dbcontext-e-migrations-por-modulo.md) |
| [0007](0007-congregacao-unica.md) | Congregação única, sem multi-tenancy | Aceito |
| [0008](0008-pessoa-como-raiz-unica.md) | `Pessoa` como raiz única de cadastro | Aceito |
| [0009](0009-hospedagem-unificada-dokploy.md) | Hospedagem unificada no Dokploy | Aceito |
| [0010](0010-cloudflare-tunnel-como-ingress.md) | Cloudflare Tunnel como ingress | Aceito |
| [0011](0011-shadcn-tailwind-nos-dois-frontends.md) | shadcn/ui e Tailwind nos dois frontends | Aceito |
| [0012](0012-monolito-modular-estrito-com-mediator-proprio.md) | Monólito modular estrito, DDD e mediator próprio | Aceito |
| [0013](0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md) | Inglês como idioma de código, português no domínio | Aceito · pasta contêiner revista pelo [0016](0016-modules-como-nome-da-pasta-conteiner.md) |
| [0014](0014-problem-details-como-formato-unico-de-erro.md) | ProblemDetails como formato único de erro da API | Aceito |
| [0015](0015-um-dbcontext-e-migrations-por-modulo.md) | Um `DbContext` e um conjunto de migrations por módulo | Aceito |
| [0016](0016-modules-como-nome-da-pasta-conteiner.md) | `Modules/` como nome da pasta contêiner dos módulos | Aceito |
| [0017](0017-ids-fortemente-tipados.md) | Ids fortemente tipados como padrão de todos os módulos | Aceito |
