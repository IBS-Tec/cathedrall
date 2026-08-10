# infra

> **Status:** vazio. Nada de infraestrutura foi definido ou provisionado.

```
compose/    Docker Compose — API, Postgres, Directus
tunnel/     configuração do cloudflared (sem credenciais)
cms/        snapshot do schema do Directus (versionado)
backup/     scripts de backup e restauração
```

## Plataforma

**Dokploy**, em servidor doméstico, hospedando tudo — site, SPA, API, Directus e Postgres
([ADR-0009](../docs/adr/0009-hospedagem-unificada-dokploy.md)).

Entrada por **Cloudflare Tunnel**, sem porta aberta no roteador
([ADR-0010](../docs/adr/0010-cloudflare-tunnel-como-ingress.md)). O túnel entrega tudo ao
Traefik; o roteamento por host vive no Traefik, não no painel da Cloudflare.

O token do túnel é segredo: nunca em `infra/tunnel/`, apenas em variável de ambiente.

## Princípio: o Dokploy é conveniência, não fundação

Tudo aqui deve ser **portável**. O MVP roda em casa e a intenção é migrar para VPS depois,
possivelmente com o site indo para hospedagem estática externa.

Na prática: os Compose e os scripts precisam funcionar com `docker compose up` numa
máquina limpa, sem o Dokploy. Ele pode gerenciar deploy, domínio e TLS; não pode ser o
único lugar onde existe informação necessária para subir o sistema. Configuração que só
vive no painel é configuração que se perde.

**Regra prática:** se reinstalar o servidor do zero exige lembrar de algo que não está
neste diretório ou no [runbook](../docs/runbook.md), isso é um bug de infraestrutura.

## Dimensionamento

Builds rodam na mesma máquina que a produção. Build de Node é guloso em memória e vai
competir com Postgres, Directus e API. Dimensione para o pico de build — um deploy do site
não pode derrubar a API no domingo de manhã.

## Backup — antes de existir dado real

Não é item de backlog. No dia em que houver o primeiro cadastro de verdade, isto precisa
existir e ter sido **testado**:

- `pg_dump` dos **dois** databases (`cathedrall` e `cms`)
- destino **fora da máquina**, criptografado
- alerta quando o backup falhar (dead-man switch) — backup silenciosamente quebrado é o
  modo de falha mais comum
- procedimento de restauração escrito em [`docs/runbook.md`](../docs/runbook.md), testado
  trimestralmente, com a data do último teste registrada

Backup que nunca foi restaurado não é backup.

## Segredos

Nada de credencial versionada. `.env` é ignorado pelo git; versione apenas `.env.example`
com as chaves e sem os valores.
