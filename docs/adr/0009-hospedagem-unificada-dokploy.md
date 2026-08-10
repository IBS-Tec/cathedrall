# ADR-0009 — Hospedagem unificada no Dokploy

**Status:** Aceito · **Data:** 2026-08-10
**Substitui parcialmente:** [ADR-0002](0002-site-astro-estatico.md) (apenas a parte de hospedagem)

## Contexto

O [ADR-0002](0002-site-astro-estatico.md) previa o site institucional hospedado
externamente, fora do servidor doméstico, justamente para sobreviver a quedas dele.

Optou-se por concentrar **tudo** no Dokploy rodando no servidor de casa, adiando qualquer
hospedagem externa. Motivo: uma única plataforma de deploy, um único lugar para
configurar, um único fluxo para aprender. Com um mantenedor e time iniciante, reduzir o
número de sistemas distintos tem valor real.

## Decisão

Todas as aplicações no Dokploy, no servidor doméstico:

| Aplicação | Tipo no Dokploy | Domínio |
|---|---|---|
| Site institucional | build estático servido por container web | `ibscristo.com.br` |
| SPA CathedrAll | build estático servido por container web | `app.ibscristo.com.br` |
| API | container .NET | `api.ibscristo.com.br` |
| Directus | imagem oficial | `cms.ibscristo.com.br` |
| PostgreSQL | serviço de banco, sem domínio | — |

**O site permanece SSG puro.** Muda apenas *onde* o resultado do build é servido, nunca
*como* ele é gerado. Isto não é detalhe: é o que mantém a migração futura barata.

## Consequências

### Aceitas conscientemente

- **O site institucional passa a ser um ponto único de falha junto com o resto.** Queda de
  energia, de internet ou de disco derruba o site público — não só o sistema interno. É o
  benefício que o ADR-0002 buscava e que se está abrindo mão.
- **Builds passam a rodar no servidor de casa**, competindo por CPU e RAM com Postgres,
  Directus e API. Build de Node é guloso em memória. Se a máquina for apertada, um deploy
  do site pode degradar a API em produção.
- Uptime do site passa a depender de disciplina operacional (energia, retorno automático
  após queda, monitoramento externo) em vez de vir de graça.

### Requisitos que isto cria

- **Retorno automático após queda de energia** vira obrigatório, não recomendável: BIOS em
  *restore on AC power loss* e política de restart em todos os containers.
- **Monitoramento externo** — serviço fora da máquina observando os domínios. Monitor
  rodando no próprio servidor não avisa quando o servidor cai.
- Dimensionar a máquina para o pico de build, não para o repouso.

### Reversão

Barata **enquanto o site continuar SSG**. Migrar para hospedagem estática externa é
apontar o deploy e o DNS para outro lugar — nenhuma mudança de código.

Essa porta se fecha no dia em que alguém introduzir SSR, middleware ou qualquer
dependência de runtime no site. Se essa necessidade aparecer, ela merece um ADR próprio,
porque o custo real não é o recurso em si: é perder a saída barata.

Gatilhos para reavaliar: primeira queda de energia que derrube o site em horário de culto,
ou builds passando a degradar a API.

## Decidido em separado

A forma de expor o servidor doméstico à internet — resolvida pelo
[ADR-0010](0010-cloudflare-tunnel-como-ingress.md).
