# Setup inicial

Do `git clone` até tudo rodando na sua máquina. Leva uns 15 minutos na primeira vez.

Se algo aqui não funcionar, **o manual está errado, não você** — abra uma issue ou
corrija. Este arquivo é a porta de entrada do projeto e envelhece rápido.

## Pré-requisitos

| Ferramenta | Versão | Conferir com |
|---|---|---|
| Node.js | 22+ | `node -v` |
| pnpm | 11+ | `pnpm -v` |
| .NET SDK | 10.0.302+ | `dotnet --version` |
| Docker | com Compose v2 | `docker compose version` |

O `apps/api/global.json` fixa o SDK; se a sua versão for anterior, o build recusa em vez
de compilar com algo diferente do resto do time.

### Windows, antes de clonar

```bash
git config --global core.longpaths true
```

`node_modules` aninhado estoura o limite de 260 caracteres sem isso. Use Docker Desktop com
backend WSL2 e mantenha o repositório **dentro** do sistema de arquivos do WSL, não em
`/mnt/c/...`. Detalhes e as demais armadilhas estão no [README](../README.md#contribuindo-em-linux-e-windows).

Fim de linha você **não** precisa configurar: o `.gitattributes` resolve, e sobrepõe
`core.autocrlf`.

---

## 1. Clonar

```bash
git clone <url-do-repositorio> cathedrall
cd cathedrall
```

## 2. Subir a infraestrutura

O Postgres e o Directus vêm primeiro: o site não constrói sem o CMS no ar.

```bash
cd infra/compose
cp .env.example .env
```

Gere os cinco segredos de máquina:

```bash
for k in POSTGRES_SUPERUSER_PASSWORD CMS_DB_PASSWORD APP_DB_PASSWORD DIRECTUS_KEY DIRECTUS_SECRET; do
  sed -i "s|^$k=.*|$k=$(openssl rand -hex 32)|" .env
done
```

> Use `rand -hex`, não `-base64`: base64 gera `/`, `+` e `=`, que quebram o `sed` e
> strings de conexão quando não são escapados.

Preencha `DIRECTUS_ADMIN_PASSWORD` à mão — é com ela que você entra no painel.

```bash
docker compose up -d
docker compose logs -f directus     # aguarde "Server started"
```

No primeiro start com volume vazio, `initdb/01-bancos.sh` cria os bancos `cms` e
`cathedrall` com usuários separados e sem permissão de conexão cruzada. Ele roda **uma
única vez**; para reaplicar é preciso `docker compose down -v`, que apaga os dados.

## 3. Aplicar o modelo de conteúdo

Um Directus recém-subido está vazio. O modelo está versionado em `infra/cms/schema.yaml`:

```bash
docker compose cp ../cms/schema.yaml directus:/directus/snapshot.yaml
docker compose exec directus npx directus schema apply /directus/snapshot.yaml
```

Sem `--yes` ele mostra o diff e pede confirmação. Rodar de novo com tudo aplicado responde
`No changes to apply.` — é idempotente.

Depois, libere a leitura pública das coleções que o site consome:

```bash
set -a && . ./.env && set +a
python3 ../cms/permissoes-publicas.py
```

Painel em **http://localhost:8055**, com o e-mail e a senha do `.env`.

## 4. Instalar os frontends

Da raiz do repositório:

```bash
cd ../..
pnpm install
cp apps/site/.env.example apps/site/.env
```

> `pnpm install` cria um `node_modules` na raiz **e** um em cada app. É esperado: o da
> raiz guarda os pacotes de verdade, os dos apps são só links simbólicos. Confira com
> `du -sh apps/site/node_modules` — deve dar alguns KB.

## 5. Compilar a API

```bash
cd apps/api
dotnet build
dotnet test
cd ../..
```

O build trata **aviso como erro**, incluindo alerta de vulnerabilidade em pacote. Se
falhar com `NU1903`, é uma dependência vulnerável — não silencie, atualize.

---

## Verificação

Quatro terminais, ou rode um de cada vez:

| Comando | Onde | Esperado |
|---|---|---|
| `docker compose ps` (em `infra/compose`) | — | `postgres` e `directus` Up |
| `pnpm site:dev` | http://localhost:4321 | Nome da igreja, endereço e "Nossos encontros" |
| `pnpm admin:dev` | http://localhost:5173 | Barra "CathedrAll" e o painel |
| `dotnet run --project apps/api/src/Bootstrapper/CathedrAll.Api` | porta impressa no console | `/health` responde `Healthy`. `/health/ready` responde `Healthy` só com o Compose no ar e a connection string configurada |

Formulário de referência do admin: **http://localhost:5173/pessoas/nova** — enviar vazio
deve mostrar erros de validação **em português**. Se aparecerem em inglês, o
`src/lib/validacao.ts` não foi importado no `main.tsx`.

Está pronto quando os quatro respondem.

---

## Armadilhas conhecidas

Todas já custaram tempo de alguém.

**O build do site quebra se o Directus estiver fora do ar.** É de propósito: melhor build
vermelho que site publicado sem endereço e sem horário. Suba o Compose antes.

**`ASPNETCORE_URLS` é ignorada pelo `dotnet run`.** O `launchSettings.json` tem
precedência. Para fixar a porta, use `--no-launch-profile`.

**Linux, .NET instalado pelo script da Microsoft: o VS Code não roda os testes.** O erro
que aparece é enganoso — "Connection to test host was cancelled before it could be
established", sugerindo tempo esgotado. Não é: o processo morre em milissegundos.

A causa está no log da extensão, não na mensagem da interface:

```
You must install .NET to run this application.
.NET location: Not found
```

O C# Dev Kit executa o binário nativo do projeto de teste, e esse binário procura o
runtime em `DOTNET_ROOT`, `/etc/dotnet/install_location_x64` e `/usr/share/dotnet`. Quem
instalou em `~/.dotnet` tem a variável definida no perfil do shell, mas o VS Code aberto
pelo lançador gráfico não herda o perfil. Registre o local de uma vez:

```bash
sudo mkdir -p /etc/dotnet
echo "$HOME/.dotnet" | sudo tee /etc/dotnet/install_location_x64
```

Recarregue a janela depois. Vale para qualquer aplicação .NET da máquina, não só para os
testes — executar ou depurar a API pelo VS Code falha do mesmo jeito. Quem instalou o
.NET pelo pacote da distribuição não passa por isso.

O log fica em
`~/.config/Code/logs/<sessão>/window1/exthost/ms-dotnettools.csdevkit/`. O arquivo
`C# Dev Kit - Test Explorer.log` mostra o sintoma; o erro de verdade está nos arquivos
de `ServiceHub/`.

**O VS Code insiste em compilar um projeto que você já apagou.** O log mostra
`Building project: .../AlgoAntigo.csproj` seguido de `Build failed`, para um `.csproj`
que não existe mais. O C# Dev Kit mantém um cache de projetos que **acumula e nunca
remove entradas** — todo projeto criado e depois apagado fica lá para sempre.

O cache mora no banco de estado do workspace, na chave `ms-dotnettools.csdevkit`:

```
~/.config/Code/User/workspaceStorage/<hash>/state.vscdb
```

Ache o `<hash>` procurando o `workspace.json` que aponta para este repositório. **Feche o
VS Code antes de mexer** — ele reescreve o banco ao sair e desfaz a edição. Com o editor
fechado, o caminho mais simples é apagar o diretório `<hash>` inteiro: a extensão
reconstrói tudo na próxima abertura, ao custo de perder o layout de editores e o
histórico de arquivos abertos daquele workspace.

A árvore do Test Explorer guarda os fantasmas em separado, na chave `testing.treeState`
do mesmo banco. Se o projeto sumiu do cache mas continua aparecendo no painel, é ela.

**VS Code reclamando de `astro/tsconfigs/strict` ou de tipos que existem.** Servidor de
TypeScript com estado velho. Paleta de comandos → `TypeScript: Restart TS Server`.

**A CLI do shadcn não dá erro ao instalar componente inexistente.** Ela consulta o
registro, não encontra e encerra em silêncio. Ao instalar em lote, confira o que
realmente apareceu em `apps/admin/src/components/ui/`.

**Mudou coleção ou campo no Directus?** Exporte o snapshot e commite, senão a mudança
existe só na sua máquina:

```bash
cd infra/compose
docker compose exec directus npx directus schema snapshot --yes /directus/snapshot.yaml
docker compose cp directus:/directus/snapshot.yaml ../cms/schema.yaml
```

O modelo de conteúdo vive no banco, não no git — este snapshot é o que impede os
ambientes de divergirem ([ADR-0003](adr/0003-cms-directus-self-hosted.md)).

---

## O que não fazer

**Não copie o banco `cathedrall` de produção para a sua máquina.** Ele guarda nome,
telefone, endereço e vínculo religioso de centenas de pessoas — dado pessoal sensível pela
LGPD. Para desenvolver, use dados sintéticos. Cópia do banco `cms` é permitida: ele só tem
conteúdo público.

**Não versione `.env`.** Só `.env.example`, com as chaves e sem os valores.

**Não troque `DIRECTUS_KEY` nem `DIRECTUS_SECRET` depois que houver conteúdo.** O `SECRET`
assina as sessões e a `KEY` participa da criptografia; trocar invalida sessões e pode
tornar dados ilegíveis.

---

## Antes do primeiro PR

- [`docs/arquitetura.md`](arquitetura.md) — as **fronteiras** entre as peças
- [`docs/adr/`](adr/) — o porquê de cada escolha. Antes de propor troca de stack, veja se
  já existe ADR sobre o assunto
- [`CLAUDE.md`](../CLAUDE.md) — os invariantes que não podem ser violados
- README do app em que você vai mexer: [`apps/site`](../apps/site/README.md),
  [`apps/admin`](../apps/admin/README.md), [`apps/api`](../apps/api/README.md)
