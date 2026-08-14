# Como contribuir

O time é um mantenedor e voluntários que entram e saem. Este documento existe para que
alguém que chegou ontem consiga entregar sozinho, e para que o trabalho de quem sair
continue de pé.

A regra que sustenta todo o resto:

> **Ninguém abre PR sem issue. Ninguém abre issue de módulo sem spec.**

Se você precisou decidir alguma coisa de domínio no meio do código — se um campo é
obrigatório, quem pode ver o quê, o que acontece quando o dado não existe — a spec estava
incompleta. Pare, pergunte, e a correção vai para a spec. Não para o comentário do PR.

## Antes da primeira contribuição

1. [`docs/setup.md`](docs/setup.md) — do clone até tudo rodando.
2. [`docs/arquitetura.md`](docs/arquitetura.md) — as **fronteiras** entre as peças. É a
   parte que mais dói descobrir tarde.
3. [`CLAUDE.md`](CLAUDE.md) — a lista de invariantes. Foi escrita para agentes, mas vale
   igual para gente. Violar qualquer uma delas quebra a arquitetura, não o estilo.

**Sua primeira tarefa é corrigir o `docs/setup.md`.** Sério. Anote tudo que não funcionou
como o manual dizia enquanto você montava o ambiente e mande um PR. Você é a única pessoa
que consegue enxergar esses buracos — em uma semana você também já terá esquecido que eles
existiam.

## Os três níveis de documento

| Documento | Responde | Onde | Quem escreve |
|---|---|---|---|
| **ADR** | *por que* decidimos assim | [`docs/adr/`](docs/adr/) | mantenedor |
| **Spec** | *o que* o módulo faz | [`docs/specs/`](docs/specs/) | mantenedor |
| **Issue** | *qual fatia* eu implemento agora | GitHub | mantenedor |
| **PR** | *como* eu implementei | GitHub | você |

ADR é imutável depois de aceito. Spec é viva: quando a realidade discorda dela, quem
descobriu atualiza a spec no mesmo PR que muda o código.

## Pegando uma tarefa

- **Uma issue por pessoa de cada vez.** Quem tem três coisas abertas entrega zero.
- Comente na issue antes de começar, para ninguém trabalhar duas vezes no mesmo.
- Se travar por mais de um dia, comente na issue dizendo onde travou. Travar é normal;
  sumir é o que quebra o projeto.
- Se a issue não passa na Definição de Pronta abaixo, devolva. Não adivinhe.

Procurando por onde entrar? A label `boa primeira tarefa` marca as que não dependem de
conhecer o resto do sistema.

## Branch, commit e PR

```
tipo/123-descricao-curta        # 123 é o número da issue
```

Commits seguem [Conventional Commits](https://www.conventionalcommits.org/pt-br/), com o
escopo sendo a aplicação ou o módulo:

```
feat(api): cadastro de pessoa com vínculo inicial
fix(site): agenda quebrava sem eventos futuros
docs: registra o processo de contribuição
```

O corpo do commit é para o **porquê**. O *o quê* já está no diff.

**Um PR resolve uma issue.** Se você encontrou outro problema pelo caminho, abra outra
issue — não aproveite a carona. PR acima de ~400 linhas trocadas para de ser revisado e
passa a ser aprovado no olho, e aí o review deixou de servir para alguma coisa.

## Definição de Pronta

Uma issue só pode ser pegada quando **todas** valem:

- [ ] Título no imperativo, descrevendo o resultado (`Cadastrar pessoa com vínculo inicial`).
- [ ] Aponta para a seção da spec que a origina.
- [ ] Critério de aceite escrito em bullets verificáveis.
- [ ] Nenhuma decisão de domínio em aberto.
- [ ] Cabe em uma sessão de 2 a 4 horas.

Se o mantenedor não consegue escrever o critério de aceite, a tarefa não está pronta para
ninguém — inclusive para ele.

## Definição de Feita

Um PR só entra quando **todas** valem:

- [ ] CI verde: build, lint e testes.
- [ ] Cada bullet do critério de aceite conferido, na mão, rodando o sistema.
- [ ] Nada de `fetch` escrito à mão na SPA — só o cliente de `packages/api-client`.
- [ ] Nada de pasta genérica por tipo técnico (`Services/`, `DTOs/`, `Repositories/`).
  Vertical slice por módulo, dos dois lados.
- [ ] Domínio e UI em português; infraestrutura e framework em inglês. Sem mistura dentro
  da mesma camada.
- [ ] Nenhum segredo versionado. Só `.env.example`.
- [ ] Se mexeu em dado de pessoa: audit log, escopo de permissão e soft delete
  contemplados. Não é backlog — é requisito de LGPD.
- [ ] Documentação atualizada no mesmo PR, se o comportamento documentado mudou.

## Review

Todo PR é revisado pelo mantenedor. Duas coisas que fazem o review valer o tempo de todo
mundo:

**Para quem escreve:** deixe um comentário no próprio PR apontando o que você não teve
certeza. Isso direciona o review para onde ele rende, em vez de gastar a atenção em
formatação.

**Para quem revisa:** escreva o *porquê* junto com o pedido de mudança. O review é o único
canal de transmissão de conhecimento que este projeto tem, e o maior risco daqui não é bug
em produção — é o mantenedor ficar indisponível e ninguém entender o sistema.

## Discordando de uma decisão

Se algo aqui parece errado, provavelmente há um ADR explicando o trade-off que foi aceito
— comece por [`docs/adr/`](docs/adr/). Se depois de ler o motivo você ainda achar que a
decisão está errada, ótimo: abra uma issue propondo um ADR novo que substitua o antigo.
Isso é bem-vindo. O que não fazemos é editar o ADR antigo, nem contornar a decisão no
código sem discutir.
