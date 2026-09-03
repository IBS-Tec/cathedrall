# ADR-0019 — `Kernel.Web` como casa do mapeador de erro

**Status:** Aceito · **Data:** 2026-09-03
**Resolve:** [ADR-0014](0014-problem-details-como-formato-unico-de-erro.md) — o "Gatilho de
reavaliação", que deixou em aberto onde o mapeador mora.

## Contexto

O [ADR-0014](0014-problem-details-como-formato-unico-de-erro.md) decidiu o **formato** do erro
e deixou uma pergunta marcada para depois: o `ErrorResults.ToProblem()` estava no host porque
naquele dia não existia módulo nenhum, e o [ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md)
põe `Endpoints/` **dentro** de cada módulo. Módulo não referencia o host. O próprio ADR-0014
escreveu o gatilho: *"no dia em que o primeiro módulo tiver endpoints, uma das duas coisas
acontece"* — ou o mapeador vira um `CathedrAll.Kernel.Web`, ou os endpoints voltam para o host
e o ADR-0012 é que precisa ser revisto.

Esse dia chegou e passou sem que ninguém notasse. `Pessoas` tem endpoints desde a busca da
recepção, mas as três primeiras rotas eram `GET` que só respondem 200 — nenhuma precisava
converter um `Error`. A quarta precisa: `GET /api/pessoas/{id}` deve responder
`Pessoa.NotFound` em 404 quando o id não existe. O gatilho não disparou no dia do primeiro
endpoint; disparou no dia do primeiro **erro**.

Sem decidir, o resultado é conhecido e ruim: o handler devolve `null`, o endpoint declara
`Ok<FichaPessoa>` e o id inexistente sai como 500 — ou o módulo escreve o próprio mapeamento
para `problem+json`, que é exatamente o "cada endpoint inventa a dele" que o ADR-0014 existiu
para impedir.

## Decisão

**Um quarto projeto de kernel, `CathedrAll.Kernel.Web`, guarda o `ErrorResults.ToProblem()`.
Referencia `Kernel.Domain` e o framework do ASP.NET Core, e nada mais. Quem tem ponta de
entrada o referencia: o host e todo módulo com `Endpoints/`.**

```
src/Kernel/
  CathedrAll.Kernel.Domain/           Result, Error, ErrorType, Entity
  CathedrAll.Kernel.Application/      mediator, pipeline, ICurrentUser
  CathedrAll.Kernel.Infrastructure/   TransactionBehavior
  CathedrAll.Kernel.Web/              ErrorResults.ToProblem()
```

A segunda opção do ADR-0014 — trazer os endpoints de volta para o host — está **recusada**.
Ela desfaz a fatia vertical do ADR-0012 e teria que ser redecidida a cada módulo novo.

Duas coisas mudam junto:

- **`ToProblem()` passa a devolver `ProblemHttpResult`**, não `IResult`. É o que permite ao
  endpoint declarar `Results<Ok<FichaPessoa>, ProblemHttpResult>` na assinatura — o 404 fica
  visível para o OpenAPI, e daí para o cliente gerado da invariante 5 do `CLAUDE.md`. Com
  `IResult` o contrato do erro sumia da assinatura e a SPA descobriria o 404 em produção.
- **O `ErrorResultsTests` sai do `CathedrAll.Api.Tests`** e vira `CathedrAll.Kernel.Web.Tests`.
  Ele já era unitário e nunca precisou subir a aplicação; estava ali só porque o mapeador
  estava.

**As outras três peças do ADR-0014 continuam no host**: `AddProblemDetails` com
`CustomizeProblemDetails`, `UseStatusCodePages` e o `GlobalExceptionHandler`. Nenhuma é
chamada por um endpoint — são montagem da aplicação, e montagem é do host. O que sobe para o
kernel é só a função pura de `Error` para resposta.

## Motivos

**É o mesmo movimento que já criou o `Kernel.Infrastructure`, pelo mesmo motivo.** O README do
kernel fixa a regra de que em `Kernel.Application` só entra `*.Abstractions`, e o
`Kernel.Infrastructure` existe porque `EntityFrameworkCore.Relational` não é abstração nenhuma.
`Microsoft.AspNetCore.App` é o oposto de uma abstração: é o framework inteiro. Enfiá-lo no
`Kernel.Application` daria a todo módulo — inclusive a um futuro que não exponha rota — e ao
próprio mediator uma dependência de web que eles não pediram. Um projeto a mais em troca de a
seta continuar apontando para um lado só.

**O custo é medido hoje e é o menor que vai ser.** Um arquivo, uma classe de teste, um módulo
com endpoints. Cada módulo novo que nascer antes desta decisão a torna mais cara, e o segundo
módulo a precisar de um 404 teria copiado o mapeamento do primeiro.

**A alternativa cobra em outro lugar.** Endpoints no host significam o host conhecendo o DTO,
a query e o handler de cada módulo — e o ADR-0012 recusou isso antes de existir módulo. Trocar
uma fatia vertical por um projeto de kernel de um arquivo é o lado barato da troca.

## Consequências

**Boas.** O formato de erro do ADR-0014 fica alcançável de dentro de qualquer módulo, que era a
condição para ele valer na API inteira. O 404 aparece na assinatura do endpoint e chega ao
cliente gerado. E o teste do mapeador deixa de depender do host para rodar.

**Ruins, aceitas.**

**O kernel passa de três projetos para quatro**, e a disciplina da seta ganha mais uma aresta
para quem revisa manter. A tabela do README do kernel cresce, e com ela a chance de alguém pôr
a coisa no projeto errado.

**`Kernel.Web` é o primeiro projeto de kernel preso a um framework.** Um módulo que o
referencia consegue escrever `IResult` dentro de `Domain/` e o compilador não reclama. É a
mesma perda que o README do kernel já documenta para o `Kernel.Infrastructure`: o que era trava
de compilação vira regra de revisão. A diferença é que agora são duas.

**O texto do ADR-0014 continua dizendo "três peças no host".** ADR não se edita; a correção
mora aqui, e quem chegar naquele documento a encontra pelo cabeçalho **Resolve**.

**A decisão assume que todo módulo fala HTTP do mesmo jeito.** Hoje é verdade e é desejável —
é a invariante 5 do `CLAUDE.md` funcionando. No dia em que um módulo precisar de outro envelope,
o lugar onde isso vai doer é este projeto.

## Gatilho de reavaliação

**Se aparecer uma segunda ponta de entrada que não seja HTTP** — um worker consumindo fila, um
comando de importação — e ela precisar converter `Error` em outra coisa, `Kernel.Web` deixa de
ser o nome e a casa certos. O que sobe nesse dia é a tabela `ErrorType` → desfecho, que hoje
está dentro do `ToProblem()` e é a única parte dele que não é HTTP.
