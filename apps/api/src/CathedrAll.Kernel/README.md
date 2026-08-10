# CathedrAll.Kernel

Infraestrutura compartilhada pelos módulos: mediator, pipeline, contrato de módulo e
tipo de resultado. **Sem regra de negócio.**

Escrito à mão em vez de usar MediatR, que passou a ter licença comercial
([ADR-0012](../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md)).
O ADR assume o custo disso e este README é a mitigação: se você chegou agora no projeto,
esta página é o que você precisa ler.

> Código do Kernel é **infraestrutura**, portanto em inglês. Domínio dos módulos
> (`Pessoa`, `Departamento`, `Escala`) é em português. Não misture dentro da mesma camada.

## O mediator

Três interfaces e um dispatcher:

```csharp
public interface IRequest<out TResponse>;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}
```

`Mediator.Send` resolve o handler pelo contêiner de DI e o embrulha nos behaviors
registrados. Usa reflexão apenas para fechar os genéricos abertos, com o `MethodInfo`
em cache. Não há varredura mágica nem geração de código.

**Se este projeto passar de ~200 linhas, ele virou framework** — e a decisão certa passa a
ser chamar o handler direto do endpoint. Está escrito no ADR-0012 e vale como limite.

## Ordem do pipeline

O **primeiro** behavior registrado é o **mais externo**: vê a requisição primeiro e a
resposta por último.

```
LoggingBehavior      entra
  ValidationBehavior   entra
    Handler
  ValidationBehavior   sai
LoggingBehavior      sai
```

Isso está coberto por teste (`MediatorTests.Primeiro_behavior_registrado_e_o_mais_externo`).
Se alguém trocar a ordem em `Program.cs` achando que é indiferente, o teste não pega — mas
transação por fora de validação, por exemplo, abre transação para requisição que sequer
era válida. Pense na ordem.

## Escrevendo uma requisição

```csharp
// Modulos/CathedrAll.Pessoas/Application/ObterPessoa.cs
public sealed record ObterPessoa(Guid Id) : IRequest<Result<PessoaResumo>>;

internal sealed class ObterPessoaHandler(PessoasDbContext db)
    : IRequestHandler<ObterPessoa, Result<PessoaResumo>>
{
    public async Task<Result<PessoaResumo>> Handle(ObterPessoa request, CancellationToken ct)
    {
        var pessoa = await db.Pessoas.FindAsync([request.Id], ct);

        return pessoa is null
            ? Error.NotFound("pessoa.nao_encontrada", "Pessoa não encontrada.")
            : new PessoaResumo(pessoa.Id, pessoa.Nome);
    }
}
```

O handler é `internal`: ninguém fora do módulo o instancia. É a fronteira do monólito
modular valendo em tempo de compilação, não por convenção.

## Result, e quando usar exceção

`Result<T>` carrega falhas **esperadas**: pessoa inexistente, documento duplicado, sem
permissão. Exceção fica para o que é genuinamente excepcional — banco fora do ar, bug.

Essa separação é o que permite o log distinguir "o sistema quebrou" de "o usuário digitou
errado". Só o primeiro merece acordar alguém de madrugada.

## Contrato de módulo

```csharp
public interface IModule
{
    string Name { get; }
    IServiceCollection Register(IServiceCollection services, IConfiguration configuration);
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
```

O host lista os módulos **explicitamente** em `Program.cs`. Nada é descoberto por
varredura: para saber o que a aplicação carrega, leia aquelas linhas.

## Behaviors

| Behavior | Situação | Papel |
|---|---|---|
| `LoggingBehavior` | pronto | Duração e falha de cada requisição |
| `ValidationBehavior` | a fazer | Rejeita antes do handler |
| `TransactionBehavior` | a fazer | Uma transação por comando |
| `AuditBehavior` | a fazer | Quem fez o quê — exigência de LGPD |
| `AuthorizationBehavior` | a fazer | RBAC com escopo |

**`LoggingBehavior` registra o TIPO da requisição, nunca o conteúdo.** Requisições carregam
nome, telefone e endereço de membros; arquivo de log é uma cópia desses dados sem nenhum
dos controles de acesso que o banco tem.
