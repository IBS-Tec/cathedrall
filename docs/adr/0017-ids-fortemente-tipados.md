# ADR-0017 — Ids fortemente tipados como padrão de todos os módulos

**Status:** Aceito · **Data:** 2026-08-24

## Contexto

Nenhum módulo existe ainda. A issue #32 abre o primeiro, e com ele nasce a primeira
entidade com `Id`. O `Entity<TId>` do kernel já é genérico — `where TId : notnull` —, mas
nada no repositório diz qual tipo entra ali.

O default é `Guid`. Vale contar o que o `Guid` esconde, primeiro em `Pessoas`:

| Campo | Que coisa identifica |
| --- | --- |
| `Pessoa.Id` | Pessoa |
| `Pessoa.ConvidadoPorId` | Pessoa |
| `Pessoa.FundidaEmId` | Pessoa |
| `VinculoIgreja.PessoaId` | Pessoa |
| `VinculoIgreja.Id` | VinculoIgreja |

Quatro dos cinco são a mesma coisa. Aqui o tipo forte quase não paga: ele separa `Pessoa`
de `VinculoIgreja`, e nada mais. A **RN-20** da Spec-0001 — *"uma pessoa não pode convidar
a si mesma"* — existe justamente porque `Id` e `ConvidadoPorId` são indistinguíveis para
qualquer sistema de tipos.

Agora os agregados que o [`docs/dominio.md`](../dominio.md#departamentos-e-trabalhadores)
descreve para os módulos seguintes:

```
Alocacao      PessoaId, DepartamentoId, FuncaoId
EscalaItem    EscalaId, FuncaoId, PessoaId
```

**Três identificadores de coisas diferentes, na mesma assinatura, todos `Guid`.** Trocar
dois de lugar compila, grava e não reclama — o [ADR-0015](0015-um-dbcontext-e-migrations-por-modulo.md)
abriu mão de chave estrangeira entre módulos, então nem o banco pega. O sintoma aparece
semanas depois, numa escala de domingo com o nome errado, e ninguém liga uma coisa à outra.

A decisão é transversal: ou o padrão vale em todos os módulos, ou o voluntário que chega
aprende dois jeitos de escrever a mesma coisa.

## Decisão

**Todo identificador de agregado ou entidade é um tipo próprio, declarado dentro do módulo
que o usa.**

```csharp
internal readonly record struct PessoaId(Guid Value);
```

Quatro regras fecham o padrão:

### 1. `readonly record struct`, sempre

`record` dá igualdade por valor sem escrever nada. `struct` evita alocação e torna a
opcionalidade explícita — `PessoaId?` é `Nullable<PessoaId>`, e o compilador cobra.

O preço é que `default(PessoaId)` existe e vale `Guid.Empty`, em silêncio. Não é regressão:
`default(Guid)` tem exatamente o mesmo problema hoje.

### 2. O tipo para em `Endpoints/`

`Domain/`, `Application/` e `Infrastructure/` falam `PessoaId`. DTO, rota e corpo JSON
continuam `Guid`, e a borda converte:

```csharp
app.MapGet("/api/pessoas/{id:guid}", async (Guid id, ISender sender) =>
    await sender.SendAsync(new ObterPessoa(new PessoaId(id))));
```

É o mesmo princípio que o ADR-0015 aplicou ao provider: a capacidade fica dentro, a
conversão aparece na composição. Levar o tipo até o HTTP exigiria um `JsonConverter` e um
`IParsable<T>` por Id — sem o primeiro, a resposta sai `{"id":{"value":"…"}}` e quebra o
contrato da seção 6 da Spec-0001. São dois mecanismos caseiros a mais para economizar uma
linha por endpoint.

### 3. Cada módulo declara os Ids que usa, inclusive os de fora

`Escalas` não referencia `Pessoas` ([ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md)),
então não tem como usar o `PessoaId` de lá. Ele declara o seu:

```csharp
// CathedrAll.Escalas
internal readonly record struct PessoaId(Guid Value);
internal readonly record struct FuncaoId(Guid Value);
```

Dois tipos CLR com o mesmo nome, em assemblies diferentes. **É o ponto que mais precisa
sobreviver ao review**, e é o que faz o padrão valer a pena: sem isso, `EscalaItem.PessoaId`
seria `Guid` cru justamente onde a troca é silenciosa.

Na fronteira entre módulos trafega `Guid`, como já trafegaria.

### 4. Um conversor de uma linha por tipo, registrado uma vez por contexto

```csharp
internal sealed class PessoaIdConverter()
    : ValueConverter<PessoaId, Guid>(id => id.Value, value => new PessoaId(value));
```

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder builder) =>
    builder.Properties<PessoaId>().HaveConversion<PessoaIdConverter>();
```

Repetitivo de propósito. A alternativa — um `EntityIdConverter<TId>` genérico no kernel,
com `static abstract` numa interface — mata a repetição e cobra em genéricos de nível alto
no lugar mais lido do repositório.

### Verificado

Spike com SDK 10.0.302, EF Core 10.0.11, Npgsql 10.0.3 e EFCore.NamingConventions 10.0.1.
O `Id` como chave, o `PessoaId?` sem navegação e a coleção dentro do agregado geram:

```sql
CREATE TABLE pessoas.pessoa (
    id uuid NOT NULL,
    convidado_por_id uuid,
    ...
    CONSTRAINT pk_pessoa PRIMARY KEY (id)
);
```

Nenhuma chave estrangeira nasce de `convidado_por_id`, que é o comportamento exigido pela
seção 4 da Spec-0001.

## Motivos

- **O erro que ele impede é silencioso por construção.** Sem FK entre módulos, um `Guid`
  trocado não bate em nada: nem no compilador, nem no banco, nem no teste que ninguém
  escreveu. É a pior categoria de defeito que este desenho pode produzir, e a única
  barreira disponível é o tipo.
- **Consistência vale mais que economia.** Adotar só onde a conta fecha — `Departamentos` e
  `Escalas` — deixaria o repositório com dois padrões, e quem chega teria de descobrir qual
  vale em qual pasta. Um padrão em todo lugar se aprende uma vez.
- **O kernel já foi escrito para isso.** `Entity<TId>` e `AggregateRoot<TId>` são genéricos
  com `where TId : notnull`; um `readonly record struct` satisfaz a restrição. **Nenhuma
  linha do kernel muda.**
- **É o momento mais barato.** O tipo não aparece no schema — a coluna é `uuid` de um jeito
  ou de outro —, então adotar depois não custaria migration nenhuma. Custaria assinatura: um
  módulo hoje, cinco no fim do MVP.

## A alternativa recusada: `Guid` agora, tipo forte quando `Alocacao` chegar

Foi a recomendação inicial, e o argumento dela continua bom: este projeto tem decidido bem
porque decide com dado na mão — a ficha de 90 linhas derrubou a `Familia`, o enum de
`Motivo` e a unicidade de celular. O Id tipado é a primeira decisão tomada por antecipação
de um erro que ninguém cometeu ainda.

Foi recusada porque o que ela adia não é o custo, é a inconsistência. Entre "decidir com o
caso real na mão" e "ter um só padrão no repositório", o segundo pesa mais num projeto cujo
risco declarado é a rotatividade de quem lê o código.

## Consequências

**Boas.** A troca de identificadores entre agregados deixa de compilar, e ela é o defeito
mais silencioso que a ausência de FK entre módulos torna possível. O nome do tipo diz o que
a variável é, sem depender do nome do parâmetro. E `Fundir(PessoaId absorvida)` passa a ser
legível na assinatura, sem consultar a spec.

**Ruins, aceitas.**

**É o terceiro mecanismo caseiro do backend**, depois do mediator e do `Result`. O ADR-0012
já declarou o que isso significa: *"além do domínio de igreja, a pessoa vai encontrar
infraestrutura caseira sem documentação na internet"*. A mitigação é a mesma — o padrão é
minúsculo, sem geração de código e sem reflexão —, mas a conta cresce, e o `CLAUDE.md` avisa
que abstração que exige explicação raramente vale o custo aqui. Esta exige.

**Dois tipos com o mesmo nome em módulos diferentes.** `Pessoas.Domain.PessoaId` e
`Escalas.Domain.PessoaId` são tipos distintos que significam a mesma coisa. Quem abrir os
dois vai perguntar por quê, e a resposta está aqui.

**Uma linha de conversão em toda borda HTTP.** Barato, mas é um lugar novo onde se pode
errar — e onde o compilador não ajuda, porque `new PessoaId(id)` aceita qualquer `Guid`.

**`IAuditable` fica de fora, e isso é uma exceção declarada.** `CreatedBy`,
`LastModifiedBy` e `DeletedBy` continuam `Guid?` no `Kernel.Domain`: são `UsuarioId` de um
módulo de acesso que não existe, e o kernel não pode conhecer vocabulário de módulo nenhum.
Enquanto esse módulo não nascer, o padrão tem um buraco visível.

**Os testes ficam mais verbosos.** `new PessoaId(Guid.NewGuid())` no lugar de
`Guid.NewGuid()`, em todo cenário.

**A Spec-0001 grafa `Guid` na seção 4.** A leitura correta passa a ser `PessoaId`. A spec
não é reescrita por causa disto; quem a ler encontra aqui a tradução.

## O que este ADR não decide

**Como o `Guid` de dentro é gerado.** `Guid.NewGuid()` produz v4, aleatório, o que fragmenta
o índice B-tree do PostgreSQL na inserção; `Guid.CreateVersion7()` produz um valor
ordenável no tempo e resolve isso. É decisão de uma linha, independente deste ADR, e cabe no
PR que criar o primeiro `Cadastrar` — não neste, que só cria a forma do tipo.

**Quem gera o Id: domínio ou banco.** Segue sendo o domínio, como já era com `Guid`; este
ADR não muda nada aí.

**Serialização em contrato de integração.** Não existe nenhum ainda. Se um dia um módulo
publicar evento com Id no corpo, a forma desse Id é decisão de lá.

## Gatilho de reavaliação

Se o número de conversores passar de um por agregado — se alguém precisar de conversor para
consulta, para serialização e para binding —, o padrão deixou de ser minúsculo e a decisão
certa é revê-lo, não empilhar peça. E se algum dia o `Guid` cru voltar a aparecer dentro de
`Domain/`, o padrão falhou pela única via que importa: a de não ser seguido.
