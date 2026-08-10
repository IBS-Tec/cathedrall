# apps/site — Site institucional

Astro 7 + Tailwind 4, gerado estaticamente (SSG). Domínio: `ibscristo.com.br`.

## Comandos

Da raiz do repositório:

```bash
pnpm site:dev                                  # servidor de desenvolvimento
pnpm --filter @cathedrall/site build           # build de produção em dist/
pnpm --filter @cathedrall/site preview         # serve o dist/ localmente
```

## Fronteiras

- Consome o **Directus** em tempo de build — nunca em runtime.
- Consome `GET /public/eventos` da API em tempo de build, para a agenda.
- **Nunca** toca dado de pessoa. Nenhum endpoint autenticado.

`output: "static"` no `astro.config.mjs` não é detalhe de configuração: é invariante de
arquitetura. Trocar para SSR fecha a porta de mover a hospedagem sem tocar em código e
exige ADR próprio (ver [ADR-0002](../../docs/adr/0002-site-astro-estatico.md) e
[ADR-0009](../../docs/adr/0009-hospedagem-unificada-dokploy.md)).

## Estrutura

```
public/          assets estáticos servidos como estão
src/
  components/    componentes .astro reutilizáveis
  content/       content collections (schemas Zod do conteúdo vindo do CMS)
  layouts/       layouts de página
  lib/           clientes de Directus e da API pública, helpers de build
  pages/         rotas — arquivo é rota
  styles/        global.css, com os tokens da marca em @theme
```

Tailwind 4 é configurado em CSS, não em `tailwind.config.js`. Os tokens da marca vivem no
bloco `@theme` de `src/styles/global.css`.
