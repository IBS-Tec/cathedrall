# CathedrAll — contexto para agentes

Monorepo da Igreja Bíblica Semear — Cristo Redentor. Dois produtos: o site institucional
(`ibscristo.com.br`) e o sistema de gestão CathedrAll (`app.ibscristo.com.br`).

**Ainda não existem:** módulo de negócio, acesso a banco pela API, autenticação, behavior
de pipeline, audit log, e nenhum ambiente publicado. Cada README descreve só o que já está
escrito — comece por eles, não pelo código.

## Leia antes de propor mudanças

- `docs/arquitetura.md` — visão geral e, principalmente, as **fronteiras** entre as peças
- `docs/dominio.md` — modelo de domínio do MVP
- `docs/adr/` — o porquê de cada escolha e os trade-offs aceitos

Antes de sugerir troca de stack ou de abordagem, verifique se já existe ADR sobre o
assunto. Se existir e ainda assim houver motivo para mudar, proponha um ADR novo que
substitua o anterior — não edite o antigo.

## Invariantes

Estas regras não são preferência de estilo; violá-las quebra a arquitetura.

1. **Directus jamais enxerga dado de pessoa.** Databases separados, usuários separados.
2. **`/public/*` da API** é somente leitura, sem autenticação, e expõe apenas o que está
   explicitamente marcado como público. Nunca dado de pessoa.
3. **O site é estático** e consome CMS e API somente em tempo de build. Sem SSR.
4. **`Pessoa` é a raiz única de cadastro.** Membro e visitante são situação de vínculo;
   trabalhador é consulta por alocação ativa. Não criar entidades separadas.
5. **Nenhum `fetch` escrito à mão** na SPA. Só o cliente gerado em `packages/api-client`.
6. **Audit log, RBAC com escopo e soft delete** vêm antes do primeiro CRUD, não depois.
   Dado de membro de igreja é dado pessoal sensível pela LGPD.
7. **Agenda tem uma fonte de verdade só:** o CathedrAll. Não duplicar eventos no CMS.

## Convenções

- **O idioma de código é o inglês** — padrão de projeto se escreve com o nome pelo qual é
  conhecido (`Factory`, `Repository`, `Handler`, `Behavior`), e o mesmo vale para variáveis,
  testes de apoio e campos de log. **A exceção é o vocabulário de negócio, que fica em
  português** (`Pessoa`, `Departamento`, `Escala`, `DataInicio`) — junto com todo texto que
  o usuário lê e os nomes de método de teste. A fronteira não é a camada, é a natureza do
  nome: coisa da igreja em português, coisa de computador em inglês. Na dúvida, inglês.
  Ver [ADR-0013](docs/adr/0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md).
- Vertical slices dos dois lados: `apps/api/src/CathedrAll.Api/Modules/<Modulo>/` e
  `apps/admin/src/modules/<modulo>/` se espelham.
- Nada de pastas genéricas por tipo técnico (`Services/`, `Repositories/`, `DTOs/`).
- Nada de segredo versionado. Só `.env.example`.

## Contexto do time

Um mantenedor experiente em .NET e desenvolvedores iniciantes voluntários, com
rotatividade alta. **Bus factor 1 é o maior risco do projeto.** Prefira sempre a solução
entediante e legível à elegante e clever. Se uma abstração exige explicação, ela
provavelmente não vale o custo aqui.
