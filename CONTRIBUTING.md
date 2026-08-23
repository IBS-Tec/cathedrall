# Como contribuir

O time é um mantenedor e voluntários que entram e saem. Este documento existe para que
alguém que chegou ontem consiga entregar sozinho, e para que o trabalho de quem sair
continue de pé.

A regra que sustenta todo o resto:

> **Ninguém abre PR sem issue. Ninguém abre issue de módulo sem spec.**

Se você precisou decidir alguma coisa de domínio no meio do código — se um campo é
obrigatório, quem pode ver o quê, o que acontece quando o dado não existe — a spec estava
incompleta. Pare, pergunte, e a correção vai para a spec. Não para o comentário do PR.

**Uma exceção:** ADR, spec e documentação de processo — este arquivo inclusive —, quando
escritos pelo mantenedor, entram por PR direto, sem issue. A regra do "sem issue" existe para
impedir trabalho não planejado de voluntário; o PR do documento já é o artefato de discussão,
e uma issue dizendo "escrever a spec de Pessoa" não carregaria nada que a própria spec não
carregue.

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

## Da spec às issues

A spec **merga sozinha e antes** das issues que ela origina. Nunca no mesmo PR.

O motivo é prático: o campo "Spec e seção" do template de tarefa é um caminho de arquivo, e
quem vai implementar lê `main`, não a sua branch. Issue apontando para spec que ainda não
existe é issue que ninguém consegue pegar.

**Isso é diferente do ADR**, que costuma vir junto da implementação no mesmo PR — foi assim
com o ADR-0014 e a conversão de erro na fronteira HTTP. Funciona porque quem implementa o ADR
é quem o escreveu. Não funciona para spec: o propósito dela é destravar duas pessoas
trabalhando ao mesmo tempo, e ninguém trabalha contra um arquivo que não está em `main`.

```
1. Mantenedor escreve a spec       branch docs/spec-0001-pessoas
2. PR da spec, revisado, mergeado  → agora existe em main
3. Seção 11 vira N issues          cada uma cita "spec 0001, seção X"
4. Mantenedor atribui a issue      voluntário abre feat/42-cadastro-de-pessoa
5. PR resolve #42                  spec atualizada no mesmo PR, se mudou
```

Ao criar cada issue, **devolva o número dela para a seção 11 da spec** — `- [ ] Migration
inicial (#42)` — e não marque as caixas. O andamento é do GitHub, e uma milestone por spec dá
a barra de progresso de graça. Spec que também rastreia progresso vira segunda fonte de
verdade, e duas fontes de verdade sempre divergem.

### Quando a spec muda no meio do desenvolvimento

A spec é viva, então ela vai mudar com issue aberta. O que decide o que fazer é **se a issue
já está atribuída** — o campo *Assignees* é o sinal.

| Situação | O que fazer |
|---|---|
| Sem assignee | Edita o corpo da issue. Livre |
| Atribuída, mudança pequena | **Comenta** na issue. Nunca edita em silêncio |
| Atribuída, e o escopo mudou de verdade | **Fecha com o motivo e abre outra**, citando a fechada e atribuindo à mesma pessoa |
| A mudança invalida coisa já mergeada | Issue nova, sempre |

Editar o corpo em silêncio é o pior dos quatro, e é o que o GitHub faz por padrão: quem leu a
issue na segunda e voltou na quinta não relê o enunciado.

**O PR que muda a spec lista as issues abertas que ele afeta.** Se não afeta nenhuma, escreve
"nenhuma" — é o mesmo hábito do "onde eu não tive certeza", que obriga a pensar no alcance.

E o alcance varia muito por seção:

| Mudou | Quebra |
|---|---|
| Seção 8, telas | o front |
| Seção 5, regras de negócio | em geral só o back |
| **Seção 6, API** | **os dois, e em silêncio** |

A seção 6 é o contrato que sustenta o trabalho em paralelo. Enquanto houver issue aberta,
**prefira mudança aditiva** — campo novo opcional, `code` de erro novo — à que quebra.
Renomear um campo custa duas pessoas, não uma.

### Quem corrige a spec quando o buraco aparece no código

- **Buraco que dá para resolver sozinho** — redação ambígua, um `code` de erro que faltou:
  mesmo PR, como diz a regra geral.
- **Decisão de domínio de verdade** — precisa perguntar ao pastor ou à secretaria: é do
  mantenedor, e a spec merga **antes**, em PR próprio. Outras issues em voo podem depender da
  mesma resposta, e decisão de domínio enterrada no PR de um voluntário é invisível para as
  outras pessoas.

## Recebendo uma tarefa

**Quem desenvolve cada issue é o mantenedor que define, e o sinal é o campo *Assignees* do
GitHub.** Não é ordem de chegada, não é comentário. Issue sem assignee não está livre para
pegar: está esperando decisão.

Isso é diferente do "pegue o que quiser" de muitos projetos, e a razão é a mesma que fez a
spec vir antes das issues. Boa parte delas está **bloqueada** por outra — o GitHub sabe disso,
e a busca `is:open -is:blocked` mostra só o que está liberado. Mas liberado não é o mesmo que
adequado: qual tarefa prepara o terreno para a próxima, qual é grande demais para quem chegou
esta semana, qual vai mudar quando a spec mudar — isso não está no quadro.

- **Uma issue por pessoa de cada vez.** Quem tem três coisas abertas entrega zero.
- **Não comece antes de a issue estar atribuída a você.** Trabalho não atribuído é trabalho
  que pode estar sendo feito duas vezes, ou jogado fora.
- Se travar por mais de um dia, comente na issue dizendo onde travou. Travar é normal;
  sumir é o que quebra o projeto.
- Se a issue não passa na Definição de Pronta abaixo, devolva. Não adivinhe.
- **Se você quer trabalhar em algo específico, peça.** A decisão é do mantenedor, mas a
  vontade de quem vai codar pesa: ninguém entrega bem o que não quis pegar.

A label `boa primeira tarefa` marca as que não dependem de conhecer o resto do sistema — ela
existe para o mantenedor saber o que oferecer a quem chegou agora.

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

Uma issue só pode ser atribuída quando **todas** valem:

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
- [ ] Código em inglês; vocabulário de negócio, texto de UI e nome de teste em português
  (ADR-0013). Na dúvida, inglês.
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

## Licença da sua contribuição

Ao abrir um PR, você concorda que sua contribuição seja licenciada sob a
[AGPL-3.0](LICENSE), a mesma licença do projeto.

Não é burocracia: trocar a licença depois exige concordância de **todos** os detentores de
direito autoral, e cada pessoa que já contribuiu é um deles. A convenção resolveria na
prática — contribuir para um repositório licenciado é aceitar a licença dele — mas
convenção não é texto, e quem precisar disso daqui a três anos vai precisar achar escrito.

## Discordando de uma decisão

Se algo aqui parece errado, provavelmente há um ADR explicando o trade-off que foi aceito
— comece por [`docs/adr/`](docs/adr/). Se depois de ler o motivo você ainda achar que a
decisão está errada, ótimo: abra uma issue propondo um ADR novo que substitua o antigo.
Isso é bem-vindo. O que não fazemos é editar o ADR antigo, nem contornar a decisão no
código sem discutir.
