# ADR-0010 — Cloudflare Tunnel como ingress

**Status:** Aceito · **Data:** 2026-08-10
**Resolve o item em aberto do:** [ADR-0009](0009-hospedagem-unificada-dokploy.md)

## Contexto

O servidor doméstico precisa atender `ibscristo.com.br` e subdomínios. Alternativas:
abrir portas no roteador com TLS no Traefik do Dokploy, ou túnel de saída.

Abrir portas exige IP público estável (não garantido em conexão residencial, e impossível
sob CGNAT), expõe a máquina de casa à varredura da internet e coloca a superfície de
ataque dentro da rede doméstica.

## Decisão

**Cloudflare Tunnel** (`cloudflared`) como único caminho de entrada. Nenhuma porta aberta
no roteador. Os domínios ficam sob os nameservers da Cloudflare.

**O túnel aponta para o Traefik, não para cada serviço.** Uma única rota `*.ibscristo.com.br`
→ Traefik, que faz o roteamento por host. Definir rota por serviço no painel da Cloudflare
espalharia a configuração de roteamento por dois lugares e amarraria o roteamento ao
provedor — o Traefik continua roteando sozinho se o túnel sair de cena.

**Cloudflare Access** na frente de `cms.ibscristo.com.br` e do painel do Dokploy. Nenhum
painel administrativo exposto apenas com usuário e senha.

## Consequências

- TLS termina na borda da Cloudflare. Entre `cloudflared` e Traefik o tráfego é HTTP na
  rede interna do Docker — normal e adequado. **Não** configurar Let's Encrypt no Traefik
  agora: seria certificado duplicado sem ganho.
- `cloudflared` vira dependência de disponibilidade. Política de restart obrigatória; duas
  réplicas do conector custam pouco e removem o ponto único.
- Postgres **não passa pelo túnel** — o plano gratuito roteia apenas HTTP/HTTPS, e o banco
  não deve ser exposto de forma alguma. Acesso administrativo ao banco é por SSH.

### Restrições que impõem decisão de produto

- **Limite de ~100 MB por requisição** no plano gratuito. Isso quebra upload de vídeo no
  Directus. Somado às restrições de conteúdo não-HTML no proxy da Cloudflare, a conclusão
  prática é: **vídeo de culto e pregação vai para o YouTube, embedado no site.** Fotos e
  documentos seguem no Directus normalmente. Melhor decidir isso agora do que descobrir
  quando a pessoa de conteúdo tentar subir a gravação do domingo.

### Requisito técnico decorrente

- Atrás de túnel + proxy reverso, a aplicação enxerga o IP do container, não o do
  visitante. A API precisa tratar `CF-Connecting-IP` / `X-Forwarded-For`
  (`ForwardedHeaders`), **e apenas de origens confiáveis**, sob pena de o IP ser
  falsificável. Sem isso, o audit log exigido pela LGPD registra o IP errado — o que é
  pior do que não registrar, porque parece correto.

### Reversão

Barata. Tirar o túnel e ligar Let's Encrypt no Traefik é troca de camada de entrada, sem
tocar em aplicação — justamente porque o roteamento vive no Traefik. Exigiria, aí sim,
IP público estável.
