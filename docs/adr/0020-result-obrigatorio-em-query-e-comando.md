# ADR-0020 — `Result` obrigatório em toda query e todo comando

**Status:** Aceito · **Data:** 2026-09-03

## Contexto

`IQuery<TResponse>` e `ICommand<TResponse>` deixavam a resposta livre, e quem escrevia o
handler decidia se ela era um `Result` ou não. A única coisa que guiava essa escolha era a
"regra de corte" do README do kernel, que é prosa.

O resultado apareceu no primeiro módulo. Dos quatro handlers de `Pessoas`, três devolvem DTO
cru e um devolve `Result<FichaPessoa>` — e o quarto só é diferente porque precisou de um 404.
A diferença não expressa nada sobre o domínio; expressa qual handler foi escrito depois.

**O problema não é a falta de uniformidade. É que behavior não interrompe sem `Result`.** O
`Sender` monta o pipeline sobre `TResponse`. Um `ValidationBehavior` ou um
`AuthorizationBehavior` precisa poder responder no lugar do handler, e se `TResponse` é
`ListPessoasResponse` a única saída é `throw` — exceção como fluxo previsível, que é
exatamente o que o README do kernel manda não fazer, virando 500 num caso que é 400 ou 403.

Isso não é hipótese. A seção 7 da [spec-0001](../specs/0001-pessoas.md) tem uma matriz de
permissões inteira esperando para virar behavior: recepção não vê ficha completa nem lista de
aniversariantes. Essa negação precisa alcançar `ListPessoas` e `SearchAniversariantes`, que
hoje devolvem DTO cru.

Há um sintoma menor já visível. O `LoggingBehavior` faz `if (response is Result result && …)`
— um teste de tipo em tempo de execução que **não faz nada** para três dos quatro handlers.
Eles são sempre logados como `success`, o que hoje por acaso é verdade.

## Decisão

**Os marcadores passam a nomear o valor, não a resposta. Não existe query nem comando que não
devolva `Result`.**

```csharp
public interface ICommandBase;

public interface ICommand : ICommandBase, IRequest<Result>;
public interface ICommand<TValue> : ICommandBase, IRequest<Result<TValue>>;

public interface IQuery<TValue> : IRequest<Result<TValue>>;
```

Quatro coisas junto:

- **`IRequest<TResponse>` continua livre.** É o primitivo do mediator, e os testes do kernel
  usam justamente ele para exercitar o `Sender` sem arrastar `Result`. A obrigação é nos dois
  marcadores que módulo usa.
- **Comando sem valor devolve o `Result` não-genérico** — `return Result.Success();`. Não
  inventamos um `Unit` para preencher um genérico que não tem o que carregar.
- **`ErrorType.Forbidden` → 403 entra agora**, antes de existir o que negue. A matriz da seção
  7 vai precisar, e mapa de status incompleto é o que faz alguém devolver 400 no lugar de 403
  para não ficar sem opção. Isto **reverte** uma linha do README do kernel, que dizia que
  `Forbidden` não existia porque "autorização é resolvida antes do handler rodar" e "enum é
  fácil de acrescentar e caro de limpar". A primeira metade deixa de valer com esta decisão:
  a autorização passa a ser um behavior que devolve `Result`, e um behavior roda **dentro** do
  `Sender`. A segunda continua verdadeira e é o custo aceito abaixo.
- **No endpoint, um método por forma de sucesso**, em `Kernel.Web`: `ToOk<TValue>` e
  `ToNoContent`, e `ToCreated` no dia do primeiro `POST`. Nenhum recebe lambda.

Os quatro endpoints de `Pessoas` ficaram idênticos em forma: envia, `.ToOk()`, acabou.

### Isto não contraria o ADR-0014

O [ADR-0014](0014-problem-details-como-formato-unico-de-erro.md) recusou passar o lado do
sucesso ao mapeador **como lambda**, para não cair na torre de `Bind`/`Map`/`Tap` que o
[ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md) evitou. `ToOk` não é isso:
não recebe lambda nenhuma, e a forma do sucesso continua no **tipo de retorno do endpoint** —
`Results<Ok<FichaPessoa>, ProblemHttpResult>` —, que é de onde o OpenAPI tira o schema e o
cliente gerado da invariante 5 tira o tipo. Um `Match` de duas lambdas devolvendo `IResult`
apagaria as duas coisas. Por isso são três métodos e não um: 200, 201 e 204 não são a mesma
resposta.

### Por que `ICommandBase` existe

O anel de transação se prendia à aridade de `ICommand`:

```csharp
where TRequest : ICommand<TResponse>
```

É essa constraint que mantém consulta fora do anel de forma **estrutural** — a DI simplesmente
não resolve o behavior para uma query. No momento em que `ICommand<TValue>` passa a nomear o
valor, ela para de fechar: o comando é enviado como `TResponse = Result<TValue>`, e a
constraint passaria a exigir `ICommand<Result<TValue>>`. Tornar o behavior genérico sobre o
valor não resolve, porque a DI fecha genérico aberto **por posição**: o `TValue` casaria com
`Result<TValue>` e a base viraria `Result<Result<TValue>>`.

Então o anel se prende a um marcador sem genérico, e a constraint vira
`where TRequest : IRequest<TResponse>, ICommandBase`.

Duas alternativas foram consideradas e recusadas. Fazer `ICommand<TValue> : ICommand` dispensa
o marcador, mas aí todo comando com valor também é `IRequest<Result>`, e
`SendAsync<Cmd, Result>` compila e só estoura na resolução da DI — armadilha silenciosa. E
trocar a constraint por registro comando a comando troca uma garantia de compilador por uma
lista que alguém esquece de atualizar.

### A obrigação é na assinatura, não no `try/catch`

Isto precisa estar escrito junto da regra, ou a regra produz o contrário do que quer: **"todo
handler devolve `Result`" não significa "envolva tudo em `Result`"**. Falha de infraestrutura
continua estourando — Postgres fora do ar, timeout, disco cheio. A "regra de corte" do README
do kernel não mudou nem um item. O que mudou é o tipo que a assinatura carrega, não o que o
handler captura. Um `try/catch` num handler continua sendo sinal de que ou aquele erro deveria
ser `Result` desde o começo, ou alguém está engolindo o que deveria subir.

## Consequências

**Boas.** Validação e autorização passam a ser escrevíveis como behavior, sem exceção como
fluxo de controle — que era o motivo de tudo isto. O `is Result` do `LoggingBehavior` deixa de
ser "se der sorte" e passa a ser total. Os endpoints ficaram de uma linha. E o
`FakeResultCommand` dos testes do kernel foi apagado: "comando cuja resposta é um `Result`"
deixou de ser uma categoria que precisa de nome.

**Ruins, aceitas.**

**Três dos quatro handlers de hoje não têm como falhar, e ainda assim devolvem `Result`.** E
não por descuido: a lista corrige `size=999` em vez de recusar, a busca devolve vazio, os
aniversariantes truncam o intervalo — a spec-0001 diz isso em prosa, três vezes. São três
`Result.Success` sem ramo do outro lado. O `ToOk` é o que torna esse custo barato, mas ele
existe.

**`ICommandBase` não significa nada no domínio.** Existe por causa de como a DI fecha genérico
aberto. É o tipo de abstração de que o `CLAUDE.md` manda desconfiar, e o que a salva é ser uma
linha que quem escreve handler nunca digita — aparece só na assinatura de um behavior.

**A chamada ao `Sender` ficou mais verbosa, não menos:**
`SendAsync<GetFichaPessoaQuery, Result<FichaPessoa>>`. O `Sender` resolve o
`IRequestHandler<TRequest, TResponse>` fechado direto da DI, sem reflexão, então os dois
parâmetros continuam obrigatórios — e o `Result<>` que sumiu da declaração da query reaparece
na chamada.

**O 403 existe antes de haver autenticação.** É código morto até o módulo de acesso chegar.
Custou uma linha no enum e uma no mapeador, e a alternativa era descobrir a falta dele no dia
em que o behavior de autorização estivesse sendo escrito.

## Gatilho de reavaliação

**Se daqui a dois ou três módulos a maioria dos handlers continuar sem ramo de falha e nenhum
behavior de validação ou autorização tiver sido escrito**, esta decisão comprou cerimônia e não
entregou o que prometeu. A pergunta a fazer nesse dia é quantos `Result.Success` sem par
existem no repositório.
