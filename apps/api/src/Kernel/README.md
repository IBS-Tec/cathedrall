# Kernel — o que todo módulo compartilha

O kernel compartilhado do [ADR-0012](../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md).
Guarda os blocos de construção que os módulos usam e **não conhece módulo nenhum**.

> **Estado: só o `Result`.** Existe `CathedrAll.Kernel.Domain` com `Result`, `Error` e
> `ErrorType`. Não existe mediator, não existem behaviors, não existem `Entity` nem
> `AggregateRoot`. Este README descreve só o que já está escrito.

## Os dois projetos

| Projeto | Guarda | Referencia |
| --- | --- | --- |
| `CathedrAll.Kernel.Domain` | `Result`, `Error`, `ErrorType`, e adiante os blocos de DDD | nada |
| `CathedrAll.Kernel.Application` | mediator, behaviors, `IUnitOfWork` | `Kernel.Domain` |

O ADR-0012 esboçou um `CathedrAll.Kernel` único. São dois porque a seta importa: a camada
`Domain` de um módulo referencia apenas `Kernel.Domain`, e aí **a entidade não alcança o
mediator nem por acidente**. Num projeto só, isso seria convenção que alguém precisa
lembrar — exatamente o que o ADR-0012 gastou um projeto por módulo para evitar.

**`Kernel.Domain` nunca ganha `PackageReference`.** Nem ORM, nem serializador, nem
validador. É fácil de verificar em revisão: o `.csproj` continua vazio. No dia em que ele
precisar de um pacote, quase certamente o que entrou ali era código de aplicação ou de
infraestrutura disfarçado, e o lugar dele é `Kernel.Application`.

`CathedrAll.Kernel.Application` ainda não existe.

## A regra de corte: `Result` ou exceção

Esta é a única coisa deste README que precisa estar na cabeça de quem escreve um handler.
A pergunta é **quem errou:**

| Quem errou | Ferramenta | Exemplo |
| --- | --- | --- |
| O usuário | `Result` | E-mail mal digitado; escalar quem está indisponível; token de confirmação expirado |
| O programador | `throw` | Ler `.Value` de um `Result` que falhou; violar invariante de agregado |
| O mundo | `throw` | Postgres fora do ar; timeout de rede; disco cheio |

O teste prático: **se um usuário consegue provocar isso preenchendo um formulário, é
`Result`.** Se só acontece porque alguém escreveu código errado ou porque a infraestrutura
caiu, é exceção — e aí você *quer* o stack trace, o log e o 500.

Disso decorre a regra que mais economiza código: **falha de infraestrutura não vira
`Result`.** Nada de `try/catch` em volta do acesso a banco para devolver
`Result.Failure("erro no banco")`. Deixe estourar. Envolver uma falha de infraestrutura em
`Result` transforma um bug barulhento em um bug silencioso, e é o handler global que já
sabe o que fazer com ela.

## Usando

Criar — os operadores implícitos existem para isto:

```csharp
public static Result<Email> Criar(string? valor)
{
    if (string.IsNullOrWhiteSpace(valor))
    {
        return EmailErros.Vazio;
    }

    string normalizado = valor.Trim().ToLowerInvariant();

    if (!normalizado.Contains('@', StringComparison.Ordinal))
    {
        return EmailErros.FormatoInvalido;
    }

    return new Email(normalizado);
}
```

`return erro;` e `return valor;` compilam num método que devolve `Result<Email>`. Sem os
operadores, cada retorno viraria `Result.Failure<Email>(erro)` e a cerimônia derrotaria o
propósito do padrão. As formas explícitas — `Result.Success()`, `Result.Failure(erro)`,
`Result.Success<T>(valor)`, `Result.Failure<T>(erro)` — continuam disponíveis para quando
o tipo não for inferível.

Consumir — do jeito entediante, de propósito:

```csharp
Result<Email> email = Email.Criar(comando.Email);

if (email.IsFailure)
{
    return email.Error;
}
```

**`Value` de um `Result` que falhou lança `InvalidOperationException`.** Não devolve
`null`, não devolve `default`. Ler o valor sem checar `IsFailure` é bug de quem chamou, e
bug quer barulho: devolver nulo caladamente empurraria o `NullReferenceException` para
três camadas adiante, longe da causa.

## `Error`: `Code` é contrato, `Description` é texto

```csharp
Error.Validation("Pessoa.EmailInvalido", "E-mail em formato inválido.")
Error.NotFound("Pessoa.NaoEncontrada", "Pessoa não encontrada.")
Error.Conflict("Escala.PessoaIndisponivel", "A pessoa está indisponível nesta data.")
Error.Failure("Pessoa.FalhaInesperada", "Não foi possível concluir.")
```

- **`Code`** é contrato de API. A SPA pode ramificar nele. Uma vez publicado, mudar é
  breaking change. Formato: `<Agregado>.<Situacao>`, PascalCase.
- **`Description`** é texto para humano. Muda quando quiser, inclusive por pedido da
  secretaria.

Daí a regra: **o front-end nunca lê `Description` para decidir nada.** No dia em que
alguém escrever `if (erro.description === "Pessoa não encontrada")`, o sistema passa a
quebrar quando corrigirem uma vírgula.

`Error` tem construtor privado — as factories são o único caminho. Isso impede
`new Error(..., (ErrorType)99)` e mata o `with`, que produziria um erro cujo `Code` diz
"não encontrada" e cujo `Type` diz `Conflict`.

`Error.None` representa a ausência de erro. Existe para `Result.Error` nunca ser nulo,
poupando um `Error?` e o aviso de nulabilidade em cada consumidor.

### Onde os erros concretos moram

**Não no kernel.** Cada módulo declara os seus, junto do agregado dono deles:

```csharp
public static class PessoaErros
{
    public static readonly Error NaoEncontrada =
        Error.NotFound("Pessoa.NaoEncontrada", "Pessoa não encontrada.");
}
```

O kernel define a forma do erro; o módulo define o vocabulário. Se um erro de `Pessoas`
precisasse existir no kernel, a fronteira do ADR-0012 já estaria furada.

Repare na mistura de idiomas, que é deliberada: `Error.NotFound(...)` é framework e fica
em inglês; `"Pessoa.NaoEncontrada"` é o domínio da igreja e fica em português. A fronteira
é o parêntese.

## `ErrorType` e a resposta HTTP

| `ErrorType` | HTTP | Quando |
| --- | --- | --- |
| `Validation` | 400 | A entrada está malformada |
| `NotFound` | 404 | O identificador não existe |
| `Conflict` | 409 | Entrada válida, estado do agregado não permite |
| `Failure` | 500 | Rede de segurança |

A distinção que mais aparece é `Validation` vs `Conflict`. O corte: **`Validation` é a
falha detectável olhando só o dado que chegou; `Conflict` exige olhar o estado atual do
sistema.** Por isso o objeto de valor só produz `Validation`, e a raiz de agregado é quem
produz `Conflict`.

**`Failure = 0` é intencional.** Zero é o valor de quem não preencheu nada:
`default(ErrorType)`, um campo desserializado errado. Se `Validation` fosse zero, uma
falha não inicializada viraria 400 com mensagem para o usuário, escondendo o bug. Sendo
`Failure`, vira 500 e aparece no log. A ordem dos membros de um enum raramente importa; o
membro zero importa.

`Failure` ficou sem exemplo na tabela de propósito: se falha de infraestrutura estoura como
exceção, quase nada deveria chegar nele deliberadamente. **No dia em que você escrever
`ErrorType.Failure` de propósito, pare e pergunte se falta um membro** — provavelmente
falta, e você vai ter o caso concreto na mão para nomeá-lo bem.

Não existem `Unauthorized` nem `Forbidden`: autenticação e autorização são resolvidas antes
do handler rodar. Se o behavior de RBAC com escopo precisar devolver `Result`, isso se
decide escrevendo o behavior. Enum é fácil de acrescentar e caro de limpar.

## Onde os `try/catch` desaparecem

O `Result` sozinho não elimina `try/catch`. Dois pontos de conversão é que eliminam, e
ambos ficam no host, fora dos módulos:

1. **Um mapeador `Result` → HTTP**, que lê `Error.Type` e devolve `ProblemDetails` com o
   status da tabela acima.
2. **Um `IExceptionHandler` global**, que loga e devolve 500 sem vazar detalhe.

Com os dois, um handler de módulo não tem motivo para ter `try/catch`. **Se você escrever
um, é sinal de que ou aquele erro deveria ser `Result`, ou você está engolindo algo que
deveria subir.**

Nenhum dos dois existe ainda.

## O que não está aqui, e por quê

**Sem `Bind`, `Map`, `Tap` ou `Ensure`.** É o próximo passo natural do padrão e é uma
armadilha neste projeto: railway-oriented programming em C# transforma um handler legível
numa torre de lambdas que exige explicação — o que o ADR-0012 se comprometeu a evitar, e
o que quem herdar isto vai encarar sem documentação na internet. Entram quando houver uma
cadeia real de vários passos que doa, com o caso concreto na mão.

**Sem erro de validação múltiplo.** Quando um comando precisar acusar vários campos de uma
vez, a saída é uma coleção no `Result` — **não** uma subclasse de `Error`. Herança de
`record` traz `EqualityContract` junto e estraga a igualdade estrutural de formas que
custam uma tarde para entender. `Error` é `sealed`; mantenha.

**Uma conta que vai chegar.** O `ValidationBehavior` do pipeline será genérico em
`TResponse`, vai precisar abortar devolvendo uma falha, e não sabe que `TResponse` é um
`Result<T>` nem como construir um. As saídas são todas feias (reflexão sobre `Result<>`,
constranger o pipeline, ou lançar e capturar no handler global). Não está resolvido — só
registrado, porque a escolha influencia o desenho do mediator.

## Armadilhas de build

Duas formas neste código foram impostas pelos analisadores, não escolhidas. Valem
registro porque **todo exemplo de `Result` que você achar na internet falha aqui**:

- **`Result` e `Result<T>` em arquivos separados** (`Result.cs`, `ResultOfT.cs`), por
  `SA1402` — "File may only contain a single type", que é erro com
  `TreatWarningsAsErrors`. Os dois continuam sendo um conceito só.
- **`Result<T>` guarda o valor numa propriedade privada `StoredValue`, não num campo
  `_value`.** `IDE0032` proíbe campo privado atribuído só no construtor
  (`dotnet_style_prefer_auto_properties`), e renomear o campo não resolve — a regra mira o
  campo, não o par com a propriedade.

## Testes

`tests/CathedrAll.Kernel.Domain.Tests/` — unitários puros, sem host e sem banco.

A guarda "sucesso carregando erro" do construtor do `Result` está sem teste de propósito:
nenhuma factory pública alcança ela, porque todas passam `Error.None` fixo. Alcançá-la por
reflexão amarraria o teste ao construtor privado sem provar nada sobre o contrato. A
guarda fica como defesa para código futuro dentro do kernel; o teste, não.
