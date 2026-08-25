# Kernel — o que todo módulo compartilha

O kernel compartilhado do [ADR-0012](../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md).
Guarda os blocos de construção que os módulos usam e **não conhece módulo nenhum**.

> **Estado: o `Result`, os blocos de DDD, o mediator e dois behaviors.**
> `CathedrAll.Kernel.Domain` tem `Result`, `Error`, `ErrorType`, `Entity`, `AggregateRoot`
> e `DomainEvent`. `CathedrAll.Kernel.Application` tem o mediator — `ISender`, `IRequest`,
> `IRequestHandler`, `IPipelineBehavior` —, o `LoggingBehavior`, o `ICurrentUser` e o
> registro de DI. `CathedrAll.Kernel.Infrastructure` tem o `TransactionBehavior`.
> **Os behaviors escritos são o de log e o de transação:** não existe validação, nem
> autorização, nem interceptor de auditoria. O anel de transação está **registrado**, fechado
> sobre o `PessoasDbContext` — `Pessoas` é o único módulo com `DbContext`. **E `ICurrentUser` não é autenticação** — é a porta que
> ela vai preencher. Este README descreve só o que já está escrito.

## Os três projetos

| Projeto | Guarda | Referencia |
| --- | --- | --- |
| `CathedrAll.Kernel.Domain` | `Result`, `Error`, `ErrorType`, `Entity`, `AggregateRoot`, `DomainEvent` | nada |
| `CathedrAll.Kernel.Application` | o mediator, o contrato dos behaviors, o `LoggingBehavior` e o `ICurrentUser` | `Kernel.Domain` |
| `CathedrAll.Kernel.Infrastructure` | o `TransactionBehavior` | `Kernel.Application` |

O ADR-0012 esboçou um `CathedrAll.Kernel` único. São vários porque a seta importa: um módulo
que não persiste nada referencia apenas `Kernel.Domain`, e aí **a entidade não alcança o
mediator nem por acidente**.

**Num módulo com `DbContext` essa garantia enfraquece, e quem revisa precisa saber.** O anel de
transação obriga o projeto do módulo a referenciar `Kernel.Infrastructure`, que arrasta
`Kernel.Application` junto. É o caso de `Pessoas`: `Domain/Pessoa.cs` **compila** se alguém
escrever `ISender` lá dentro. O que era trava de compilação virou regra de revisão, e devolvê-la
custaria quebrar cada módulo em três projetos — preço que não vale a pena pagar antes de o
problema aparecer de verdade.

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

`Kernel.Infrastructure` **existe justamente porque essa regra é para valer.** O anel de
transação precisa conhecer `DbContext`, e `Microsoft.EntityFrameworkCore.Relational` não é
`*.Abstractions` — enfiá-lo no `Kernel.Application` daria a todo módulo, e ao mediator, uma
dependência de ORM que eles não pediram. Então o pacote fica aqui, num terceiro projeto que
só quem precisa de persistência referencia. É o mesmo movimento que separou `Domain` de
`Application`: um projeto a mais em troca de a seta continuar apontando para um lado só.

A escolha do **provider** continua sendo do host: `Relational` sabe o que é uma transação e
uma tabela, e não sabe o que é PostgreSQL ([ADR-0015](../../../../docs/adr/0015-um-dbcontext-e-migrations-por-modulo.md)).

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
Error.Validation("Pessoa.InvalidEmail", "E-mail em formato inválido.")
Error.NotFound("Pessoa.NotFound", "Pessoa não encontrada.")
Error.Conflict("Escala.PessoaUnavailable", "A pessoa está indisponível nesta data.")
Error.Failure("Pessoa.UnexpectedFailure", "Não foi possível concluir.")
```

- **`Code`** é contrato de API. A SPA pode ramificar nele. Uma vez publicado, mudar é
  breaking change. Formato: `<Agregado>.<Situação>`, PascalCase — agregado em português,
  situação em inglês (ADR-0013).
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
public static class PessoaErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Pessoa.NotFound", "Pessoa não encontrada.");
}
```

O kernel define a forma do erro; o módulo define o vocabulário. Se um erro de `Pessoas`
precisasse existir no kernel, a fronteira do ADR-0012 já estaria furada.

Repare na mistura de idiomas, que é deliberada e segue o
[ADR-0013](../../../../docs/adr/0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md):
`Pessoa` é a entidade da igreja e fica em português; `NotFound` é o vocabulário de
`ErrorType` e fica em inglês. A fronteira é o ponto. A `Description`, que o usuário lê,
é sempre português.

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

**O primeiro existe: `ErrorResults.ToProblem()`, no host.** O formato que ele produz é o do
[ADR-0014](../../../../docs/adr/0014-problem-details-como-formato-unico-de-erro.md) — RFC
9457, com o `Code` num membro de extensão `code` e a `Description` no `detail`. Repare que
ele recebe o **`Error`**, não o `Result`: o lado do sucesso — 200 com corpo, 201 com
`Location`, 204 sem nada — é conhecimento do endpoint, e passá-lo como lambda nos levaria
exatamente à torre de `Bind`/`Map`/`Tap` que este kernel recusou.

**O segundo também existe: `GlobalExceptionHandler`, no host.** Ele registra o stack trace —
é o único lugar do sistema que o faz — e devolve 500 sem vazar detalhe. Repare que ele produz
o 500 chamando o **mesmo** `ToProblem()`: a garantia de que as duas respostas 500 da API têm
a mesma forma é estrutural, não uma convenção que alguém precisa lembrar.

Com os dois no lugar, **um `try/catch` num handler de módulo não tem mais desculpa.**

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

A única interface do kernel é `IAggregateRoot`, não-genérica, e ela tem um consumidor
nomeado: o dispatcher de eventos varrendo o `ChangeTracker`.

As versões genéricas existiram por um tempo e não tinham consumidor: ninguém escreve
`IEntity<Guid> p` quando pode escrever `Pessoa`. Cobravam covariância (`out TId`, exigida
pela `S3246`) para não servir a ninguém. Se um dia um repositório genérico precisar de
restrição, ela funciona igual escrita sobre a classe: `where T : AggregateRoot<TId>`.

## Auditoria e exclusão lógica

**Nenhum carimbo de auditoria mora na entidade** ([ADR-0018](../../../../docs/adr/0018-auditoria-fora-da-entidade.md)).
`Entity<TId>` tem identidade e igualdade, e mais nada. As colunas existem no banco como
*shadow properties* declaradas no `DbContext` do módulo — sem propriedade no tipo CLR:

```csharp
builder.Entity<Pessoa>().Property<DateTimeOffset>("CreatedAt");
```

O interceptor as escreve pelo modelo, não pelo tipo:

```csharp
entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
```

Existiam `IAuditable` e `ISoftDeletable`, com as quatro propriedades em toda `Entity`. Foram
removidas por três motivos, e o ADR-0018 os desenvolve: setter público numa raiz de agregado;
carimbo técnico não é linguagem ubíqua — ninguém na igreja pergunta quem criou um registro —;
e a interface existia só para o interceptor achar o tipo, coisa de que o `ChangeTracker`
nunca precisou.

**Exclusão lógica continua opt-in, agregado por agregado.** A diferença não é estilo: filtro
global em toda tabela significa passar tardes desligando aviso de navegação obrigatória para
entidade filtrada, e nem tudo merece — linha de escala cancelada some, não vira lápide.
Opt-in obriga a pergunta a ser respondida um a um, que é o que uma revisão de LGPD quer ver
documentado. Mudou **onde a resposta se lê**: era `grep ISoftDeletable`, passa a ser a
configuração do contexto.

Não existe `IsDeleted`: `DeletedAt is not null` já responde e ainda diz **quando**. Dois
campos para a mesma verdade é uma chance de eles discordarem.

### O que cada nulo significa

| Coluna | Nulo quer dizer |
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

Esse `Guid` **não entra no domínio**. Quando o interceptor existir, ele lê o ator do
[`ICurrentUser`](#icurrentuser), em `Kernel.Application`, e é lá que o tipo mora — por isso o
[ADR-0017](../../../../docs/adr/0017-ids-fortemente-tipados.md) não precisa de exceção.

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

Nenhum interceptor existe ainda, e por isso **nenhuma coluna de auditoria existe ainda**:
elas entram na migration que acompanhar o interceptor, não antes
([ADR-0018](../../../../docs/adr/0018-auditoria-fora-da-entidade.md)). Coluna que nada
escreve é aforância falsa, e `CreatedAt` sem interceptor gravaria `0001-01-01` em silêncio.

## `ICurrentUser`

`Kernel.Application` guarda o contrato de quem está executando a requisição. Duas
propriedades, nenhuma opcional:

```csharp
public interface ICurrentUser
{
    Guid Id { get; }

    Papel Papel { get; }
}
```

`Id` responde "quem fez", e é o que o interceptor de auditoria vai gravar em `CreatedBy`.
`Papel` responde "pode fazer", com os quatro valores da matriz da seção 7 da Spec-0001:
`Recepcao`, `Dirigente`, `Secretaria`, `Pastor`. `Recepcao` é `0` de propósito — é o menos
privilegiado, e é nele que cai todo valor de enum não inicializado.

**Isto não é autenticação, e o contrato prova.** Não há `ClaimsPrincipal`, `HttpContext`,
token nem cookie em lugar nenhum dele. Um handler que o recebe não sabe sequer que existe
HTTP, e é essa ignorância que permite a autenticação de verdade entrar por baixo sem tocar em
handler nenhum. Se o contrato falasse de claim, trocar a implementação mudaria quem depende
dela — o oposto do que uma porta existe para garantir.

**O `Guid` cru é intencional** e não abre exceção no
[ADR-0017](../../../../docs/adr/0017-ids-fortemente-tipados.md): a proibição vale dentro de
um módulo, e o kernel não é módulo. Um `UsuarioId` aqui seria o kernel declarando vocabulário
de um módulo de acesso que não existe.

**Nada de `Nome`.** Seria conveniente no log, e é exatamente por isso que fica de fora: nome
de pessoa em arquivo de log é dado pessoal fora do banco, sem retenção definida e sem
anonimização possível. Vale a mesma cláusula do `LoggingBehavior` — nenhum sinal carrega dado
de pessoa. O `Guid`, cruzado com a tabela de usuários no dia em que ela existir, responde à
mesma pergunta sem espalhar o dado.

**O kernel declara a porta e não implementa nenhuma.** `AddKernelApplication` não registra
`ICurrentUser`, de propósito: quem escolhe a implementação é o host — hoje um adapter de
desenvolvimento que só entra quando o ambiente é `Development`, amanhã o módulo de acesso. O host também é
quem se recusa a subir sem nenhuma, e os dois lados estão em
[`apps/api/README.md`](../../README.md#usuário-atual).

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

    return resultado.IsSuccess ? Results.Ok(resultado.Value) : resultado.Error.ToProblem();
});
```

O `ToProblem()` é o mapeador da seção "Onde os `try/catch` desaparecem". Ele mora no host e é
`internal` — o que significa que este exemplo, como está, só compila **dentro do host**. O
ADR-0012 põe `Endpoints/` dentro de cada módulo, e módulo não referencia o host; a
reavaliação disso está registrada no
[ADR-0014](../../../../docs/adr/0014-problem-details-como-formato-unico-de-erro.md) e vence
no dia em que o primeiro módulo tiver endpoint.

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

Cada behavior expõe a própria extensão de registro: `AddLoggingBehavior()` no kernel,
`AddPessoasTransactionBehavior()` no módulo. O anel de módulo **não tem como** ser registrado à
mão pelo host — a subclasse recebe o `DbContext` do módulo, que é `internal`, então ela também
é `internal`, e o `Program.cs` está noutro assembly. A extensão é o que mantém o anel visível
numa linha do `Program.cs` sem abrir o tipo. As extensões usam `TryAddEnumerable`, então chamar
duas vezes não duplica o anel —
e, ao contrário do `TryAddScoped` do `ISender`, aqui duplicar **quebraria de verdade**: o
behavior rodaria duas vezes por requisição.

Isso importa mais do que parece:

| Ordem | Anel | Existe? | Por que aqui |
| --- | --- | --- | --- |
| 1 | `LoggingBehavior` | **sim** | Por fora de tudo, para a duração medida ser a que o usuário esperou e para a rejeição dos anéis de dentro também virar linha de log |
| 2 | autorização (RBAC com escopo) | não | Antes da validação: quem não pode nem ver o recurso não deve descobrir quais campos estão errados nele |
| 3 | validação | não | Antes da transação, senão você abre transação para requisição que já ia ser rejeitada |
| 4 | `TransactionBehavior` | **sim**, registrado por `Pessoas` | O mais interno, colado no handler, para segurar o menor trecho possível |

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
consumidor nomeado. O destas duas é o `TransactionBehavior`, que agora existe — o `ICommand`
no `where` dele é o que mantém query fora de transação.

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
      Request CadastrarPessoa finished with success in 12,4 ms
warn: CathedrAll.Kernel.Application.Pipeline[0]
      Request CadastrarPessoa finished with failure in 3,1 ms, error Pessoa.InvalidEmail
```

| Desfecho | Nível | Campo extra |
| --- | --- | --- |
| `Result` bem-sucedido, ou resposta que não é `Result` | `Information` | — |
| `Result` com `IsFailure` | `Warning` | `ErrorCode` |
| Exceção | `Error` | — |
| `OperationCanceledException` | `Information` | — |

Os níveis são a mesma regra de corte do começo deste README, dita de outro jeito: **falha
de negócio é o usuário errando, e usuário errando não é incidente.** Se e-mail mal digitado
saísse como `Error`, o alerta dispararia várias vezes por dia sem nada para fazer a
respeito, e o time aprenderia a ignorar o canal — inclusive nas vezes em que ele estivesse
certo.

Repare na primeira linha da tabela: um handler que devolve `string` termina em `success`
mesmo tendo recusado o pedido, porque o behavior só sabe ler o que passa por `Result`. É
mais um motivo para handler devolver `Result`.

A última linha é a mesma regra aplicada ao cancelamento: **usuário que fecha a aba não é
incidente.** Sai `canceled` em `Information`.

Note o que este behavior **não** consegue decidir: se aquele cancelamento foi o cliente
desistindo ou um timeout interno estourando. A diferença está no `RequestAborted` do
`HttpContext`, e o kernel não conhece HTTP — nem deve. Então a divisão de trabalho é esta: o
behavior registra o **fato** (a requisição foi cancelada), e o `GlobalExceptionHandler` do
host, que tem o `HttpContext` na mão, faz o **juízo** (isso foi rotina ou falha). Nenhum
incidente se perde nessa divisão, porque quando é falha de verdade é o handler que emite a
linha de `Error` com o stack trace.

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

### O `catch` que não loga

**Nenhum `catch` aqui registra log.** O único que existe classifica o cancelamento — mexe em
duas variáveis locais e relança —, e quem escreve a linha continua sendo o `finally`. A
exceção sobe intacta: o behavior registra o **desfecho** e não toca no objeto.

O caminho que todo exemplo mostra — `catch`, logar, `throw` — registraria a mesma falha duas
vezes com o mesmo peso, uma vez aqui e outra no handler global, e quem estivesse lendo o log
contaria dois incidentes onde houve um. Os analisadores já sabem disso: `S2139` e `S6667`
reprovam logar e relançar. Manter o log no `finally` resolve os dois de uma vez e ainda
garante a linha no caminho de exceção.

O que junta as duas linhas que sobram — o desfecho daqui e o stack trace do
`GlobalExceptionHandler` — é o `traceId`, que aparece nas duas e no corpo da resposta
([ADR-0014](../../../../docs/adr/0014-problem-details-como-formato-unico-de-erro.md)). Sem
ele, duas linhas seriam de fato dois incidentes para quem lê o log.

### O que ainda não está aqui

- **Sem *trace*, sem métrica.** Quando o OpenTelemetry entrar, entra por este arquivo:
  ele já é exatamente onde a requisição começa e termina. `ActivitySource` e `Meter` estão
  no framework compartilhado do .NET 10, então **não custam `PackageReference` novo** — a
  regra do `*.Abstractions` continua de pé. A escolha de backend, amostragem e retenção
  merece ADR próprio, com uma cláusula herdada desta seção: nenhum sinal carrega dado de
  pessoa, e log não é o único sinal que vaza.

## O `TransactionBehavior`

Mora em `Kernel.Infrastructure` e é o anel 4, o mais interno. Faz três coisas, nesta ordem:
abre uma transação, chama o resto do pipeline, e então **salva e confirma** — ou desfaz.

```csharp
await using IDbContextTransaction transaction =
    await _context.Database.BeginTransactionAsync(cancellationToken);

TResponse response = await next();

if (response is Result { IsFailure: true })
{
    await transaction.RollbackAsync(cancellationToken);

    return response;
}

await _context.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

**O handler não chama `SaveChanges`.** Ele muda o rastreador de mudanças e devolve; quem
grava é o anel. É uma regra a menos para esquecer no endpoint número onze, e ela combina com
o `Id` que nasce no construtor: como o `Guid` já existe antes de o Postgres ver a entidade,
nenhum handler precisa de um `SaveChanges` no meio para descobrir a chave.

**Falha de negócio desfaz a transação.** O reconhecimento é `response is Result { IsFailure:
true }` — o mesmo `is Result` que o `LoggingBehavior` já usa, sem reflexão. Um `Result` que
falhou é uma requisição rejeitada, e requisição rejeitada não deixa rastro no banco. Resposta
que não é `Result` nenhum conta como sucesso e confirma.

**Exceção não tem `catch`.** O `await using` desfaz a transação ao sair do escopo, porque o
`Dispose` do `IDbContextTransaction` faz rollback do que não foi confirmado. Isso mantém o
behavior na regra da seção "Onde os `try/catch` desaparecem": quem transforma exceção em
resposta é a fronteira HTTP, não o pipeline.

### Por que a transação explícita, se `SaveChanges` já é atômico

Uma chamada de `SaveChanges` já roda em transação implícita, então no caminho comum — handler
mexe no rastreador, anel salva uma vez — o `BeginTransactionAsync` não acrescenta nada. Ela
está ali pelo caminho **incomum**: o handler que chama `SaveChanges` por conta própria e
depois falha, seja devolvendo `Result` de falha, seja lançando. Sem a transação explícita,
essa escrita ficaria gravada e a requisição ainda responderia erro.

**São os dois casos que ficariam vermelhos se alguém tirasse a transação daqui**, e os dois
têm teste. Não é hipótese: os testes foram conferidos removendo a transação e vendo
exatamente esses dois falharem, e mais nenhum.

### Um módulo fecha o genérico em três linhas

O behavior é `abstract` e recebe um `DbContext` qualquer. Ele não pode ser registrado direto
porque o container precisa de um tipo de aridade 2 — `IPipelineBehavior<,>` — e um behavior
que também fosse genérico no contexto teria aridade 3. Então cada módulo escreve a sua
subclasse fechada, que é só um repasse de construtor:

```csharp
internal sealed class PessoasTransactionBehavior<TRequest, TResponse>(PessoasDbContext context)
    : TransactionBehavior<TRequest, TResponse>(context)
    where TRequest : ICommand<TResponse>
{
}
```

Ela é `internal` por obrigação, não por gosto: o construtor recebe o `PessoasDbContext`, que é
`internal` ([ADR-0015](../../../../docs/adr/0015-um-dbcontext-e-migrations-por-modulo.md)), e
tipo público com construtor que recebe tipo interno não compila — CS0051. Como o `Program.cs`
está noutro assembly, ele **não consegue nomear a subclasse**. Por isso o módulo expõe uma
extensão só para o anel, ao lado da do contexto, e o host fica com duas linhas:

```csharp
builder.Services.AddPessoasDbContext(options => options.UseNpgsql(...));
builder.Services.AddPessoasTransactionBehavior();
```

### Duas linhas, e não uma que registre o módulo inteiro

Houve uma `AddPessoasModule` que registrava o contexto, e a pergunta natural é por que o anel
não entrou nela — é peça do módulo, afinal. A resposta é que **a ordem de registro é a ordem
dos anéis**, e um anel escondido dentro de uma extensão chamada "módulo" faz a posição daquela
linha no `Program.cs` decidir o pipeline em silêncio. Quem chegasse depois para encaixar o anel
de autorização leria

```csharp
builder.Services.AddKernelApplication();
builder.Services.AddLoggingBehavior();
...
builder.Services.AddPessoasModule(options => options.UseNpgsql(...));
```

e não teria como saber que a última linha carrega o anel mais interno de todos. Registrar
autorização depois dela — que é onde se acrescenta qualquer coisa — poria autorização **dentro**
da transação, e isso não quebra teste de handler nenhum.

O guarda-chuva também convidava a próxima peça a entrar escondida: o interceptor de auditoria é
"do módulo" pelo mesmo argumento. Então ele foi desfeito. Cada linha tem o nome do que registra,
`AddPessoasDbContext` não promete mais do que entrega, e o custo aceito é que **esquecer a linha
do anel faz os comandos pararem de persistir em silêncio**.

É o teste que paga esse custo, não a boa intenção:
`PipelineRegistrationTests.O_anel_de_transacao_deve_ser_o_mais_interno_do_pipeline`, em
`CathedrAll.Api.Tests`, resolve o pipeline do `Program.cs` de verdade e afirma quantos anéis
existem e qual é o mais interno. Ele fica vermelho se a linha sumir e se ela trocar de lugar —
as duas coisas foram conferidas removendo e reordenando. Quando entrarem os anéis 2 e 3, a
contagem quebra de propósito: quem os escrever tem que vir aqui dizer onde eles entram.

### Os anéis se acumulam, e essa conta chega no segundo módulo

Um `DbContext` por módulo ([ADR-0015](../../../../docs/adr/0015-um-dbcontext-e-migrations-por-modulo.md))
significa **um anel de transação por módulo**. Todos são registrados no mesmo
`IPipelineBehavior<,>` aberto, e a restrição `where TRequest : ICommand<TResponse>` é
satisfeita por *qualquer* comando — então **todos entram na cadeia de todo comando**, inclusive
os de outro módulo. Isso está medido, não suposto: registrar dois anéis fechados sobre o mesmo
contexto faz o comando morrer com `InvalidOperationException: The connection is already in a
transaction`, e há teste fixando exatamente isso.

Duas leituras saem daí, e as duas importam:

**Registrar o anel do mesmo módulo duas vezes quebra alto.** É a advertência da seção "Ordem
dos behaviors" acontecendo de verdade — e é o bom modo de falhar: primeira requisição,
exceção clara.

**Com N módulos, um comando abre N transações em N contextos.** Elas não colidem, porque cada
contexto tem conexão própria, e as N−1 alheias abrem e confirmam vazias. Mas cada uma toma uma
conexão do pool pela duração da requisição, e isso não é desperdício simbólico: com cinco
módulos são cinco conexões por requisição no lugar de uma.

**Está tolerado hoje porque só existe um módulo, e com um módulo o custo é zero.** A conta
chega no segundo, e não deve ser paga antes: as saídas plausíveis — filtrar por um marcador de
módulo na requisição, ou registrar o anel fechado por tipo de comando — têm trade-offs
diferentes, e a escolha melhora muito com dois módulos concretos na mão. **O que não é opção é
descobrir isso em produção:** quem criar o segundo módulo decide, e o gatilho está escrito aqui.

### O que ainda não está aqui

- **Sem `IUnitOfWork`.** O que a interface existe para garantir — **um ponto de confirmação
  por requisição**, em vez de `SaveChangesAsync` espalhado pelos handlers — está atendido pelo
  próprio anel, e de forma mais forte: o handler não persiste porque não tem o que chamar, não
  porque combinou-se que não chamaria. O que a interface acrescentaria é um nome a mais entre
  o behavior e o `DbContext` que ele já recebe. Ela entra no dia em que houver um segundo tipo
  de unidade de trabalho — o outbox é o candidato —, com o caso concreto na mão.
- **Sem despacho de evento de domínio.** O `PopDomainEvents` existe e ninguém o chama. O lugar
  dele é aqui, entre o `SaveChangesAsync` e o `CommitAsync`, e essa posição é uma decisão de
  consequência — quem publica dentro da transação obriga o assinante a fazer parte dela. Fica
  para quando o primeiro evento tiver assinante.
- **Sem transação distribuída entre módulos.** Um comando toca um módulo. Se um dia precisar
  tocar dois, a resposta não é um anel que abre duas transações: é evento e consistência
  eventual, ou é sinal de que a fronteira entre os dois módulos está no lugar errado.

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
| `tests/CathedrAll.Kernel.Infrastructure.Tests/` | `TransactionBehavior`: o que confirma, o que desfaz e o que quebra alto |

Unitários puros nos dois primeiros: sem host, sem banco. Os dublês são classes escritas à mão, e
**não há biblioteca de mock no `Directory.Packages.props`** — nem deve haver. Um
`FakeHandler` de dez linhas é mais legível para quem chega do que a API de setup de
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
requisição **continua verde**, porque `FakeRequest.ToString()` também contém
`"FakeRequest"`. Se um dia for preciso escolher qual manter, é este.

O `ILoggerFactory` e o `ILogger` dos testes também são dublês escritos à mão, pelo mesmo
motivo dos outros — e com um efeito colateral bem-vindo: o projeto de teste não precisa do
`Microsoft.Extensions.Logging` concreto, então a regra do `*.Abstractions` vale dos dois
lados. O `IsEnabled` do dublê devolve `true` sempre, e é ele que abre a guarda da `CA1873`:
um dublê mais realista, que filtrasse por nível, derrubaria os sete testes por ausência de
log — vermelho confuso, cuja causa estaria no dublê e não no behavior.

### O teste que carrega peso no `TransactionBehavior`

São dois, e os dois falam do mesmo ponto: **o handler salvou e a requisição falhou depois.**
Um caso devolve `Result` de falha, o outro lança. Eles existem porque, sem a transação
explícita, os dois gravariam — e todos os outros testes desta classe continuariam verdes.

Isso foi conferido de verdade: removendo o `BeginTransactionAsync` e o `CommitAsync`,
**exatamente esses dois ficam vermelhos**, e nenhum outro. É a única prova de que a transação
não é decoração em cima de um `SaveChanges` que já é atômico.

**O banco daqui é SQLite em memória, e não um dublê.** É a exceção à regra dos dublês escritos
à mão, e o motivo é que o objeto sob teste é justamente o encanamento de transação: um
`DbContext` falso teria que fingir `BeginTransaction`, `Commit`, `Rollback` e o efeito de
`Dispose` sem confirmar — ou seja, o dublê teria que conter a resposta que o teste deveria
verificar. O provedor `InMemory` do EF também não serve: ele **ignora transação em silêncio**,
que é precisamente o comportamento errado que estamos tentando detectar.

A conexão fica aberta pelo tempo do teste, numa variável `await using`, porque banco SQLite em
memória vive enquanto a conexão viver. Fechá-la cedo apaga o schema, e o sintoma é "tabela não
existe" num teste que não fala de schema.

**Nada disso substitui um teste contra Postgres de verdade.** SQLite confirma a *forma* — que
o anel abre, salva, confirma e desfaz na hora certa —, e não o comportamento do Postgres em
isolamento, deadlock ou timeout de bloqueio. Esses aparecem com o primeiro módulo e pedem
teste de integração com banco real.
