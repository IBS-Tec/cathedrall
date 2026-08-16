# Kernel — o que todo módulo compartilha

O kernel compartilhado do [ADR-0012](../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md).
Guarda os blocos de construção que os módulos usam e **não conhece módulo nenhum**.

> **Estado: o `Result`, os blocos de DDD, o mediator e um behavior.**
> `CathedrAll.Kernel.Domain` tem `Result`, `Error`, `ErrorType`, `Entity`, `AggregateRoot`,
> `DomainEvent`, `IAuditable` e `ISoftDeletable`. `CathedrAll.Kernel.Application` tem o
> mediator — `ISender`, `IRequest`, `IRequestHandler`, `IPipelineBehavior` —, o
> `LoggingBehavior` e o registro de DI. **O único behavior escrito é o de log:** não existe
> validação, nem transação, nem autorização, nem `IUnitOfWork`, nem interceptor de
> auditoria. Este README descreve só o que já está escrito.

## Os dois projetos

| Projeto | Guarda | Referencia |
| --- | --- | --- |
| `CathedrAll.Kernel.Domain` | `Result`, `Error`, `ErrorType`, `Entity`, `AggregateRoot`, `DomainEvent` | nada |
| `CathedrAll.Kernel.Application` | o mediator, o contrato dos behaviors e o `LoggingBehavior` | `Kernel.Domain` |

O ADR-0012 esboçou um `CathedrAll.Kernel` único. São dois porque a seta importa: a camada
`Domain` de um módulo referencia apenas `Kernel.Domain`, e aí **a entidade não alcança o
mediator nem por acidente**. Num projeto só, isso seria convenção que alguém precisa
lembrar — exatamente o que o ADR-0012 gastou um projeto por módulo para evitar.

**`Kernel.Domain` nunca ganha `PackageReference`.** Nem ORM, nem serializador, nem
validador. É fácil de verificar em revisão: o `.csproj` continua vazio. No dia em que ele
precisar de um pacote, quase certamente o que entrou ali era código de aplicação ou de
infraestrutura disfarçado, e o lugar dele é `Kernel.Application`.

`Kernel.Application` tem dois `PackageReference`, e os dois terminam em `Abstractions`:
`Microsoft.Extensions.DependencyInjection.Abstractions`, o preço de o kernel saber se
registrar sozinho, e `Microsoft.Extensions.Logging.Abstractions`, o preço do
`LoggingBehavior`. Repare que nenhum dos dois é a coisa concreta — nem o container, nem um
provedor de log. Quem escolhe a implementação é o host; o kernel só descreve o registro e
escreve na interface.

**A regra que fica: aqui só entra `*.Abstractions`.** É o equivalente ao `.csproj` vazio do
`Kernel.Domain`, e igualmente fácil de conferir em revisão. Um pacote concreto neste
projeto arrasta para dentro do kernel uma escolha que é do host, e todo módulo passa a
herdá-la sem ter sido consultado.

## A regra de corte: `Result` ou exceção

Esta é a única coisa deste README que precisa estar na cabeça de quem escreve um
**handler** — quem escreve um **agregado** tem a sua lista mais abaixo. A pergunta é
**quem errou:**

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

## Entidade, agregado e evento

`Entity<TId>` dá identidade e igualdade; `AggregateRoot<TId>` acrescenta eventos de
domínio. Só raiz de agregado é carregada e salva por repositório — o resto do grafo entra
e sai junto com ela.

```csharp
public sealed class Pessoa : AggregateRoot<Guid>
{
    private Pessoa(Guid id, NomeCompleto nome)
        : base(id) => Nome = nome;

    public NomeCompleto Nome { get; private set; }

    public static Pessoa Cadastrar(NomeCompleto nome)
    {
        Pessoa pessoa = new(Guid.CreateVersion7(), nome);
        pessoa.AddDomainEvent(new PessoaCadastrada(pessoa.Id));
        return pessoa;
    }
}
```

Três regras saem daí, e são as únicas que precisam estar na cabeça de quem escreve um
agregado.

### 1. O `Id` nasce no construtor, nunca no banco

`Guid.CreateVersion7()` gera antes de tocar o Postgres. `Id` é `{ get; }` — não existe
setter, nem privado.

Três coisas dependem disso:

- **`GetHashCode` fica estável.** Objeto cujo hash muda depois de entrar num `HashSet`
  fica inalcançável dentro dele — some da coleção onde está.
- **O agregado está completo antes de existir no banco**, então o evento já pode carregar
  o `Id` da coisa que descreve.
- **Versão 7 é ordenável por tempo** e não fragmenta o índice como a versão 4.

**Se alguém deixar o banco gerar o `Id`, a igualdade desaba:** toda entidade nova fica com
`default`, e `Equals` passa a dizer que todas são a mesma entidade.

### 2. Duas entidades são iguais quando têm o mesmo `Id` e o mesmo tipo

`Equals` e `==` fazem a mesma coisa, de propósito. Ter que lembrar qual dos dois compara
identidade seria convenção, e convenção é o que este kernel gasta projeto para evitar.

Os dois são `sealed`: igualdade de entidade é decisão do kernel, não de quem escreve o
módulo. Ninguém compara `Pessoa` por CPF ou por e-mail.

A comparação exige **tipo exato**, não `is`. Com `is`, base e derivada teriam igualdade
assimétrica — `a.Equals(b)` verdadeiro e `b.Equals(a)` falso — o que viola o contrato de
`Equals` e corrompe qualquer `Dictionary`.

### 3. Evento se levanta de dentro do agregado

`AddDomainEvent` é `protected`. Não dá para um handler fazer
`pessoa.AddDomainEvent(new PessoaDesligada(...))` sem passar por `pessoa.Desligar()`.

Sem isso, o evento diz que a pessoa foi desligada e o estado do agregado diz que não — o
agregado deixa de ser a fonte da verdade sobre o que aconteceu com ele. É o caminho mais
curto para quem está com pressa, e por isso precisa estar fechado.

### `PopDomainEvents` pega e limpa numa operação só

O padrão que todo tutorial mostra é ler `DomainEvents`, despachar, e depois chamar
`Clear()`. Nesse desenho, se um handler provocar um evento novo no mesmo agregado
**durante** o despacho, o `Clear()` apaga um evento que nunca foi despachado. Pegar e
limpar na mesma operação fecha a janela.

Daí o nome ser `Pop` e não `Clear`: quem lê `ClearDomainEvents()` assume `void` e joga os
eventos fora.

### Evento de domínio: `=` e não `=>`

```csharp
public Guid Id { get; } = Guid.CreateVersion7();   // inicializa uma vez
public Guid Id => Guid.CreateVersion7();           // Guid novo a cada leitura
```

A segunda forma é corpo de expressão: executa a cada leitura. Um outbox que grava o `Id` e
depois compara para deduplicar nunca bate. **Herde de `DomainEvent`** e a escolha não
chega a aparecer.

`IDomainEvent` não tem `EventType`. Quando o outbox existir, o nome persistido precisa ser
um literal que o autor do evento escolhe: nome derivado de reflexão quebra ao renomear a
classe ou ao subir a versão do assembly.

### Não existem `IEntity<TId>` nem `IAggregateRoot<TId>`

As interfaces do kernel são não-genéricas, e cada uma tem um consumidor nomeado:

| Interface | Quem consome |
| --- | --- |
| `IAuditable` | o interceptor de auditoria varrendo o `ChangeTracker` |
| `IAggregateRoot` | o dispatcher de eventos varrendo o `ChangeTracker` |

As versões genéricas existiram por um tempo e não tinham consumidor: ninguém escreve
`IEntity<Guid> p` quando pode escrever `Pessoa`. Cobravam covariância (`out TId`, exigida
pela `S3246`) para não servir a ninguém. Se um dia um repositório genérico precisar de
restrição, ela funciona igual escrita sobre a classe: `where T : AggregateRoot<TId>`.

## Auditoria e exclusão lógica

`IAuditable` está em toda `Entity`. `ISoftDeletable` é opt-in, agregado por agregado.

A diferença não é estilo. Exclusão lógica implica *global query filter* no EF, e filtro em
toda tabela significa passar tardes desligando aviso de navegação obrigatória para
entidade filtrada. Fora que nem tudo merece: linha de escala cancelada some, não vira
lápide. Opt-in também obriga a pergunta a ser respondida agregado por agregado, que é o
que uma revisão de LGPD quer ver documentado.

`ISoftDeletable` não tem `bool IsDeleted`: `DeletedAt is not null` já responde e ainda diz
**quando**. Dois campos para a mesma verdade é uma chance de eles discordarem.

### O que cada nulo significa

| Campo | Nulo quer dizer |
| --- | --- |
| `CreatedAt` | nunca é nulo — o interceptor preenche no insert |
| `CreatedBy` | ação do sistema: migration, seed, job agendado, importação |
| `LastModifiedAt` / `LastModifiedBy` | nunca foi alterado |

Os carimbos são `DateTimeOffset` porque o Npgsql **rejeita em tempo de execução**
`DateTime` com `Kind` diferente de `Utc`. `DateTime.Now` compila, passa na revisão e
estoura no `SaveChanges` da primeira máquina configurada em `America/Sao_Paulo`.

**`CreatedBy` é `Guid?`, não `string?`.** Guardando a claim `sub` de um IdP, basta alguém
reconfigurar o provedor para emitir e-mail e você espalhou dado pessoal identificável por
toda tabela do sistema — inclusive as que a revisão de LGPD classificou como impessoais.
Ninguém revisa `CreatedBy` procurando PII. Se o `sub` do IdP não for GUID, quem guarda o
`sub` é a tabela de usuários; a chave continua `Guid`.

### Estas colunas não são o audit log

Elas guardam quem mexeu **por último**. Se três pessoas editarem o telefone de um membro,
sobrou o nome da terceira e os dois valores anteriores sumiram. A invariante 6 do
`CLAUDE.md` pede outra coisa: tabela append-only com quem mudou o quê, quando, de qual
valor para qual.

As duas convivem, e nenhuma substitui a outra:

| | Colunas | Tabela |
| --- | --- | --- |
| Responde | qual o estado atual da linha | como a linha chegou aqui |
| Cardinalidade | 1 por entidade | N por entidade, cresce para sempre |
| Retenção | vida da entidade | política própria, mais curta |
| Custo de leitura | zero, já está na linha | agregação sobre a maior tabela do banco |

O que impede a tabela de substituir as colunas é a retenção: no dia em que o expurgo
apagar o INSERT de um membro cadastrado em 2019, `CreatedAt` deixa de ser conhecível.

E `CreatedAt` não é "membro desde". Data de filiação é dado de domínio, pertence ao
vínculo, e sobrevive a recadastro e a troca de sistema. Não deixe o carimbo técnico virar
regra de negócio.

A tabela precisa do EF Core, então **não cabe aqui** — o `.csproj` vazio é a regra. Ela
mora em `Kernel.Application`/infra e merece ADR próprio, com duas coisas para resolver:

- **`ExecuteUpdate` e `ExecuteDelete` não passam pelo `SaveChanges`.** Nem pelo change
  tracker. Auditoria por interceptor é auditoria **da aplicação**, não do banco; a
  diferença entre "auditamos tudo" e "auditamos o que passa pelo EF" é o tipo de promessa
  que se descobre falsa durante um incidente. À prova de desenvolvedor é trigger no
  Postgres.
- **Exclusão lógica não é o direito de eliminação da LGPD.** `DeletedAt = agora` deixa o
  dado exatamente onde estava. Pedido de titular exige remoção real ou anonimização.

E o audit log vai conter valores antigos de campos de `Pessoa`: ele é tão sensível quanto
a tabela original e precisa da mesma proteção e da mesma retenção. Audit log irrestrito é
vazamento com carimbo de conformidade.

Nenhum interceptor existe ainda.

## O mediator

Sete arquivos, 92 linhas, uma classe. O ADR-0012 pôs um teto de ~200 linhas e disse o que
fazer se estourar: **voltar a chamar o handler direto do endpoint.** O `LoggingBehavior`
são outras 64 linhas — o teto vale para o encanamento, não para o que passa dentro dele,
mas o número inteiro do projeto fica visível de propósito: 156.

| Peça | O que é |
| --- | --- |
| `IRequest<TResponse>` | marcador. `ICommand<T>` e `IQuery<T>` herdam dele |
| `IRequestHandler<TRequest, TResponse>` | quem faz o trabalho. Um por requisição |
| `IPipelineBehavior<TRequest, TResponse>` | o que envolve o handler. Zero ou muitos |
| `RequestHandlerDelegate<TResponse>` | o `next` que o behavior chama |
| `ISender` | o que o endpoint injeta |
| `Sender` | a implementação, `internal` — ninguém fora do kernel a nomeia |

Ponta a ponta. O comando e o handler, dentro do módulo:

```csharp
public sealed record CadastrarPessoa(string Nome) : ICommand<Result<Guid>>;

internal sealed class CadastrarPessoaHandler(IPessoaRepository repositorio)
    : IRequestHandler<CadastrarPessoa, Result<Guid>>
{
    public async Task<Result<Guid>> HandleAsync(
        CadastrarPessoa request,
        CancellationToken cancellationToken)
    {
        Result<NomeCompleto> nome = NomeCompleto.Criar(request.Nome);

        if (nome.IsFailure)
        {
            return nome.Error;
        }

        Pessoa pessoa = Pessoa.Cadastrar(nome.Value);
        await repositorio.AdicionarAsync(pessoa, cancellationToken);

        return pessoa.Id;
    }
}
```

O registro, no host:

```csharp
builder.Services.AddKernelApplication();
builder.Services.AddLoggingBehavior();
builder.Services.AddScoped<
    IRequestHandler<CadastrarPessoa, Result<Guid>>,
    CadastrarPessoaHandler>();
```

`AddKernelApplication()` registra só o `ISender`. Cada behavior tem a sua extensão e é
pedido à parte — inclusive o de log, que é o único que existe. Ver "Ordem dos behaviors".

E o endpoint:

```csharp
app.MapPost("/pessoas", async (CadastrarPessoa comando, ISender sender, CancellationToken ct) =>
{
    Result<Guid> resultado = await sender.SendAsync<CadastrarPessoa, Result<Guid>>(comando, ct);

    return resultado.IsSuccess ? Results.Ok(resultado.Value) : resultado.Error.ParaHttp();
});
```

O `ParaHttp()` do exemplo é o mapeador `Result` → HTTP da seção "Onde os `try/catch`
desaparecem". Ele não existe ainda, e o nome aqui é ilustrativo — quem escrevê-lo escolhe.

### `SendAsync` pede os dois tipos, e isso é de propósito

C# não infere argumento de tipo a partir de restrição. `TResponse` só aparece em
`where TRequest : IRequest<TResponse>`, então `sender.SendAsync(comando, ct)` **não
compila** — os dois vão escritos, sempre.

O MediatR não pede porque resolve o handler por reflexão: `MakeGenericType`, cache de
delegate, dicionário de tipos. A mitigação 1 do ADR-0012 pede "sem reflexão além do
estritamente necessário", e aqui ela não é necessária — **`Sender` não tem uma linha de
reflexão**. O preço é repetir o tipo de retorno na chamada, uma vez por endpoint.

Daí sai uma armadilha: `TRequest` é inferido do tipo **estático** do argumento. Se alguém
guardar a requisição numa variável declarada como `IRequest<Result<Guid>>`, o `TRequest`
liga na interface, o container procura `IRequestHandler<IRequest<Result<Guid>>, ...>` e não
acha nada. Falha em tempo de execução, não de compilação. Passe sempre o tipo concreto.

### Ordem dos behaviors: quem registra primeiro fica por fora

```
A antes → B antes → C antes → handler → C depois → B depois → A depois
```

Registrar é no host, uma linha por behavior, e a ordem das linhas é a ordem de execução.
Fica no `Program.cs` **de propósito**: dentro do `AddKernelApplication` seria decisão de
composição escondida numa biblioteca — quem lê o `Program.cs` veria o mediator e não veria
o pipeline.

Cada behavior do kernel expõe a própria extensão (`AddLoggingBehavior()`); behavior de
módulo se registra na mão, com `AddScoped(typeof(IPipelineBehavior<,>), typeof(...))`. As
extensões do kernel usam `TryAddEnumerable`, então chamar duas vezes não duplica o anel —
e, ao contrário do `TryAddScoped` do `ISender`, aqui duplicar **quebraria de verdade**: o
behavior rodaria duas vezes por requisição.

Isso importa mais do que parece:

| Ordem | Anel | Existe? | Por que aqui |
| --- | --- | --- | --- |
| 1 | `LoggingBehavior` | **sim** | Por fora de tudo, para a duração medida ser a que o usuário esperou e para a rejeição dos anéis de dentro também virar linha de log |
| 2 | autorização (RBAC com escopo) | não | Antes da validação: quem não pode nem ver o recurso não deve descobrir quais campos estão errados nele |
| 3 | validação | não | Antes da transação, senão você abre transação para requisição que já ia ser rejeitada |
| 4 | transação / `IUnitOfWork` | não | O mais interno, colado no handler, para segurar o menor trecho possível |

A auditoria não aparece na tabela porque **não é anel**: ela pendura no `SaveChanges`, ou
seja, dentro da transação. É isso que a frase "transação por fora de auditoria" quer dizer.

**Ordem errada não quebra teste de handler nenhum**, e é por isso que a ordem tem teste
próprio no kernel.

Dentro do `Sender`, tudo isso é um `.Reverse()` e uma cópia de variável dentro do `foreach`.
Duas linhas que não parecem nada. Veja "Testes".

### `ICommand` e `IQuery` existem para o behavior filtrar

O `Sender` não distingue os dois — ele aceita `IRequest`. Elas existem para um behavior
poder restringir a si mesmo:

```csharp
internal sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
```

Registrado como genérico aberto, o container **pula esse behavior em silêncio** quando a
requisição é uma query. Sem `if (request is ICommand)` dentro do behavior, sem registro
duplicado. Isso funciona a partir do .NET 7; antes, o container lançava em vez de pular.

É o mesmo teste que matou `IEntity<TId>` mais acima: interface do kernel precisa de
consumidor nomeado. O destas duas é o `TransactionBehavior` — e o mecanismo já tem teste,
antes de o behavior existir.

### `ISender` é `Scoped`, e não é detalhe

`Sender` recebe `IServiceProvider` e resolve o handler dentro do `SendAsync`. **Qual**
provider ele recebe depende do lifetime dele próprio:

| Lifetime | Provider injetado | Consequência |
| --- | --- | --- |
| `Singleton` | a raiz | handler `Scoped` não resolve; sem validação, vira *captive dependency* |
| `Transient` | o do escopo | funciona, e aloca à toa: o objeto não tem estado |
| `Scoped` | o do escopo | handler, `DbContext` e o futuro `IUnitOfWork` caem todos no mesmo escopo |

É isso que torna seguro o `Sender` ser um service locator: ele não escolhe escopo nenhum,
ele herda o escopo de quem o resolveu.

O registro usa `TryAddScoped`, não `AddScoped`: chamar `AddKernelApplication()` duas vezes
não duplica nada. Como `GetRequiredService` fica com o último, duplicar não quebraria
hoje — mas "não quebra hoje" é como dívida entra.

## O `LoggingBehavior`

O anel mais externo, e por enquanto o único. Uma linha por requisição, sempre — sucesso,
falha de negócio e exceção:

```
info: CathedrAll.Kernel.Application.Pipeline[0]
      Requisição CadastrarPessoa terminou com sucesso em 12,4 ms
warn: CathedrAll.Kernel.Application.Pipeline[0]
      Requisição CadastrarPessoa terminou com falha em 3,1 ms, erro Pessoa.EmailInvalido
```

| Desfecho | Nível | Campo extra |
| --- | --- | --- |
| `Result` bem-sucedido, ou resposta que não é `Result` | `Information` | — |
| `Result` com `IsFailure` | `Warning` | `Codigo` |
| Exceção | `Error` | — |

Os níveis são a mesma regra de corte do começo deste README, dita de outro jeito: **falha
de negócio é o usuário errando, e usuário errando não é incidente.** Se e-mail mal digitado
saísse como `Error`, o alerta dispararia várias vezes por dia sem nada para fazer a
respeito, e o time aprenderia a ignorar o canal — inclusive nas vezes em que ele estivesse
certo.

Repare na primeira linha da tabela: um handler que devolve `string` termina em "sucesso"
mesmo tendo recusado o pedido, porque o behavior só sabe ler o que passa por `Result`. É
mais um motivo para handler devolver `Result`.

### O que ele nunca registra

**A requisição não vai para o log.** Nem inteira, nem em pedaço: o que entra no template é
`typeof(TRequest).Name`, e a instância nunca é passada ao logger.

Isso não é economia de espaço, é a invariante 6 do `CLAUDE.md`. `CadastrarPessoa` é um
`record`, e `ToString()` de `record` despeja todas as propriedades — CPF, e-mail, telefone,
endereço. Bastaria alguém passar `request` em vez do nome do tipo para o cadastro inteiro da
igreja começar a ser copiado para o agregador de logs, que tem outra retenção, outro
controle de acesso e não aparece em nenhum mapa de dados pessoais. Vazamento assim não dá
erro, não fica lento e não aparece em revisão de código — só aparece quando alguém procura.

Pelo mesmo motivo, da falha vai o `Code` e **não** a `Description`. `Code` é contrato e não
tem como carregar dado de ninguém; `Description` é texto para humano, que qualquer dia é
reescrito para ficar mais útil e vira `"O e-mail joao@exemplo.com já está cadastrado"`.

Os dois casos têm teste que falha se o dado vazar pela mensagem ou por qualquer campo
estruturado.

### Categoria fixa, e por que não `ILogger<T>`

O behavior recebe `ILoggerFactory` e cria o logger com uma categoria literal:
`CathedrAll.Kernel.Application.Pipeline`.

Com o `ILogger<LoggingBehavior<TRequest, TResponse>>` que seria o caminho natural, a
categoria sairia do **genérico fechado** — uma categoria diferente para cada tipo de
requisição do sistema. Filtrar o anel no `appsettings.json` passaria a exigir uma entrada
por comando, e ninguém manteria isso. Com a categoria fixa, é uma linha:

```json
"Logging": { "LogLevel": { "CathedrAll.Kernel.Application.Pipeline": "Warning" } }
```

Essa string tem teste. Errar uma letra nela não quebra build nem teste de comportamento —
só faz o filtro parar de casar, em silêncio, e a descoberta acontece quando alguém precisar
baixar o volume de log em produção.

### `try/finally` sem `catch`

A exceção sobe intacta: o behavior registra o **desfecho** e não toca no objeto. Quem loga
stack trace é o `IExceptionHandler` global do host, que ainda não existe.

O caminho que todo exemplo mostra — `catch`, logar, `throw` — registraria a mesma falha
duas vezes com o mesmo peso, uma vez aqui e outra no handler global, e quem estivesse
lendo o log contaria dois incidentes onde houve um. Os analisadores já sabem disso:
`S2139` e `S6667` reprovam logar e relançar. `try/finally` resolve os dois de uma vez e
ainda garante a linha de log no caminho de exceção.

### O que ainda não está aqui

- **Cancelamento cai como exceção.** Cliente que desiste no meio produz
  `OperationCanceledException`, que hoje vira `Error` e "exceção". Deveria ser rotina, não
  incidente. Fica assim de propósito até o handler global existir: os dois vão precisar
  concordar sobre o que é cancelamento, e decidir agora é adivinhar sozinho.
- **Sem *trace*, sem métrica.** Quando o OpenTelemetry entrar, entra por este arquivo:
  ele já é exatamente onde a requisição começa e termina. `ActivitySource` e `Meter` estão
  no framework compartilhado do .NET 10, então **não custam `PackageReference` novo** — a
  regra do `*.Abstractions` continua de pé. A escolha de backend, amostragem e retenção
  merece ADR próprio, com uma cláusula herdada desta seção: nenhum sinal carrega dado de
  pessoa, e log não é o único sinal que vaza.

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

**Sem base para objeto de valor.** `record` já dá igualdade estrutural pronta, e as
implementações de `ValueObject` que circulam comparam componentes por reflexão: lentas e
ilegíveis para resolver o que a linguagem resolve sozinha. Objeto de valor aqui é
`sealed record`, ou `readonly record struct` quando for pequeno.

**Uma conta que vai chegar.** O `ValidationBehavior` do pipeline será genérico em
`TResponse`, vai precisar abortar devolvendo uma falha, e não sabe que `TResponse` é um
`Result<T>` nem como construir um. As saídas são todas feias (reflexão sobre `Result<>`,
constranger o pipeline, ou lançar e capturar no handler global). Continua não resolvido: o
mediator foi escrito sem decidir isso, o que só foi possível porque o pipeline é genérico
em `TResponse` e não precisa saber o que ele é. A conta chega inteira no primeiro behavior
que precisar abortar.

## Armadilhas de build

Seis formas neste código foram impostas pelos analisadores, não escolhidas. Valem
registro porque **todo exemplo que você achar na internet falha aqui**:

- **`Result` e `Result<T>` em arquivos separados** (`Result.cs`, `ResultOfT.cs`), por
  `SA1402` — "File may only contain a single type", que é erro com
  `TreatWarningsAsErrors`. Os dois continuam sendo um conceito só.
- **`Result<T>` guarda o valor numa propriedade privada `StoredValue`, não num campo
  `_value`.** `IDE0032` proíbe campo privado atribuído só no construtor
  (`dotnet_style_prefer_auto_properties`), e renomear o campo não resolve — a regra mira o
  campo, não o par com a propriedade.
- **O `#pragma warning disable S3875` no `operator ==` do `Entity`.** A `S3875` proíbe
  sobrecarga de `==` em tipo de referência. A saída documentada — implementar
  `IEquatable<T>` — cai na `S4035`, que exige classe `sealed`, e `Entity` é abstrata. As
  duas regras se cancelam: não existe combinação que satisfaça as duas. Sem o operador,
  `a == b` compararia referência enquanto `a.Equals(b)` compara identidade, e o erro
  passaria despercebido porque dentro de um mesmo `DbContext` o change tracker devolve a
  mesma instância para a mesma chave — `==` **acerta por acidente** no caso comum e só
  falha entre contextos diferentes, que é o que teste unitário não pega. Só o `==` dispara
  a regra; o `!=` passa sozinho.
- **O `#pragma warning disable S2326` no `IRequest<TResponse>`.** A `S2326` proíbe
  parâmetro de tipo não usado, e `IRequest<TResponse>` não usa `TResponse` em membro
  nenhum — é interface marcadora, e o `TResponse` existe para o compilador amarrar
  requisição e resposta na assinatura do handler. Satisfazer a regra exigiria inventar um
  membro que ninguém implementa.
- **O `typeof(TRequest).Name` repetido nas duas chamadas de log**, em vez de guardado num
  `static readonly`. A `S2743` proíbe campo estático em tipo genérico, e com razão: em
  `LoggingBehavior<TRequest, TResponse>` existe um campo por combinação fechada, o que
  quase sempre é o bug de quem esperava um só. Aqui seria o comportamento desejado, mas o
  ganho é uma leitura de metadado por requisição — não vale a supressão.
- **O `if (_logger.IsEnabled(logLevel))` em volta do `finally`.** A `CA1873` reprova
  calcular argumento caro que pode ser descartado, e `typeof(TRequest).Name` conta como
  caro. A guarda paga por si: com o anel desligado por configuração, nem o nome do tipo nem
  a duração chegam a ser calculados. O cronômetro é `Stopwatch.GetTimestamp()` com
  `GetElapsedTime`, e não `Stopwatch.StartNew()`, para não alocar um objeto por requisição
  por anel.

## Testes

| Projeto | Cobre |
| --- | --- |
| `tests/CathedrAll.Kernel.Domain.Tests/` | `Result`, `Error`, `Entity`, `AggregateRoot`, `DomainEvent` |
| `tests/CathedrAll.Kernel.Application.Tests/` | despacho, cadeia de behaviors, `LoggingBehavior`, registro de DI |

Unitários puros nos dois: sem host, sem banco. Os dublês são classes escritas à mão, e
**não há biblioteca de mock no `Directory.Packages.props`** — nem deve haver. Um
`HandlerFalso` de dez linhas é mais legível para quem chega do que a API de setup de
qualquer framework de mock, e este projeto tem rotatividade alta.

Uma regra que parece detalhe: o rastro que os behaviors escrevem **nunca pode ser
`static`**. xUnit roda classes de teste em paralelo, e lista estática compartilhada produz
teste intermitente — o pior tipo, porque ensina o time a re-rodar o CI até passar. O rastro
entra pelo container e sai pelo construtor do dublê.

### O teste que carrega peso em `Kernel.Domain`

É o do `HashSet` em `Entity`: duas instâncias com o mesmo `Id`, `Count == 1`. Ele
representa a classe inteira de bug que a igualdade por identidade existe para evitar —
`Contains`, `Distinct`, `Except` e `GroupBy` erram todos pelo mesmo motivo, e nenhum deles
reclama.

A guarda "sucesso carregando erro" do construtor do `Result` está sem teste de propósito:
nenhuma factory pública alcança ela, porque todas passam `Error.None` fixo. Alcançá-la por
reflexão amarraria o teste ao construtor privado sem provar nada sobre o contrato. A
guarda fica como defesa para código futuro dentro do kernel; o teste, não.

### O teste que carrega peso em `Kernel.Application`

Três behaviors registrados e o rastro inteiro conferido:

```
["A antes", "B antes", "C antes", "handler", "C depois", "B depois", "A depois"]
```

Com dois behaviors ele passaria por sorte. Com três, as duas linhas invisíveis do `Sender`
ficam travadas — e vale saber como cada uma falha, porque as falhas não se parecem:

- **Tirar o `.Reverse()`** inverte a ordem da cadeia. Teste vermelho, diff legível.
- **Tirar a cópia da variável dentro do `foreach`** (`RequestHandlerDelegate<TResponse>
  interno = next;`) causa **stack overflow**. Sem ela, todas as closures capturam a mesma
  *variável* `next` em vez do valor dela naquela volta, e cada behavior acaba chamando a si
  mesmo. Stack overflow não é exceção capturável em .NET: o processo de teste morre
  inteiro, e o resumo sai com o projeto reprovado e **`falhou: 0`**. Se o CI mostrar isso,
  procure closure capturando variável de loop — não assert quebrado.

O `ValidateScopes = true` na construção do provider é o que transforma "registrei o
`ISender` com o lifetime errado" em teste vermelho, em vez de captive dependency
descoberta sob carga em produção. Não tire.

### O teste que carrega peso no `LoggingBehavior`

É o do vazamento: a requisição leva a string `"cpf-do-membro"` no corpo, e o teste varre a
mensagem formatada **e** todos os campos estruturados de todos os registros procurando por
ela. Ele existe porque essa é a única falha desta classe que não se anuncia — anel na ordem
errada quebra o teste de ordem, nível errado quebra o teste de nível, e vazamento de dado
pessoal passa em tudo.

Vale saber que ele é o mais sensível dos sete. Trocando `typeof(TRequest).Name` por
`request` no behavior, o teste do vazamento fica vermelho na hora; o que confere o nome da
requisição **continua verde**, porque `RequisicaoFalsa.ToString()` também contém
`"RequisicaoFalsa"`. Se um dia for preciso escolher qual manter, é este.

O `ILoggerFactory` e o `ILogger` dos testes também são dublês escritos à mão, pelo mesmo
motivo dos outros — e com um efeito colateral bem-vindo: o projeto de teste não precisa do
`Microsoft.Extensions.Logging` concreto, então a regra do `*.Abstractions` vale dos dois
lados. O `IsEnabled` do dublê devolve `true` sempre, e é ele que abre a guarda da `CA1873`:
um dublê mais realista, que filtrasse por nível, derrubaria os sete testes por ausência de
log — vermelho confuso, cuja causa estaria no dublê e não no behavior.
