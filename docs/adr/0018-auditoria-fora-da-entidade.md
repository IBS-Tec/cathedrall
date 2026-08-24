# ADR-0018 — Auditoria fora da entidade, em shadow properties

**Status:** Aceito · **Data:** 2026-08-24
**Revisa:** [ADR-0017](0017-ids-fortemente-tipados.md) — a consequência que declara
`IAuditable` como exceção ao padrão. A exceção deixa de existir.

## Contexto

`Entity<TId>` implementa `IAuditable`, e com isso **toda entidade de todo módulo** nasce com
quatro propriedades:

```csharp
public DateTimeOffset CreatedAt { get; set; }
public Guid? CreatedBy { get; set; }
public DateTimeOffset? LastModifiedAt { get; set; }
public Guid? LastModifiedBy { get; set; }
```

O [ADR-0017](0017-ids-fortemente-tipados.md) proibiu `Guid` cru dentro de um módulo e teve
de abrir uma exceção para estas, porque `CreatedBy` é um `UsuarioId` de um módulo de acesso
que não existe, e o `Kernel.Domain` não pode conhecer vocabulário de módulo nenhum.

Ao olhar de perto para escrever essa exceção, apareceram **três** problemas, e o do `Guid`
é o menos grave:

1. **Setter público em raiz de agregado.** Qualquer código escreve
   `pessoa.CreatedAt = DateTimeOffset.MinValue`. Num objeto cuja razão de existir é proteger
   invariante, e num dado cuja razão de existir é ser confiável numa revisão de LGPD.
2. **Auditoria não é linguagem ubíqua.** O teste é "alguém da igreja pergunta isso?".
   Ninguém pergunta quem criou um registro — a seção 6 da Spec-0001 não tem `criadoPor` em
   resposta nenhuma, e a "ficha completa com histórico" quer dizer vínculo, não carimbo.
3. **A interface existe por causa da infraestrutura.** O `src/Kernel/README.md` nomeia o
   consumidor: *"o interceptor de auditoria varrendo o `ChangeTracker`"*. É um conceito no
   `Kernel.Domain` cuja única razão de ser é um detalhe do EF Core.

E o consumidor nomeado **nunca precisou da interface**. O `ChangeTracker` alcança qualquer
propriedade pelo modelo, não pelo tipo CLR:

```csharp
entry.Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow;
```

Isso funciona com propriedade normal, com setter privado, e com propriedade que não existe
na classe. Nenhum interceptor foi escrito ainda: `IAuditable` aparece em três arquivos do
repositório — a própria interface, `Entity.cs` e o README — e em nenhum outro lugar.

## Decisão

**Os carimbos de auditoria saem da entidade e viram *shadow properties*.**

### 1. `IAuditable` e `ISoftDeletable` deixam de existir

`Entity<TId>` fica com o que uma entidade é: identidade e igualdade.

```csharp
public abstract class Entity<TId>(TId id)
    where TId : notnull
{
    public TId Id { get; } = id;

    // igualdade
}
```

### 2. As colunas são declaradas no `DbContext` do módulo

Sem propriedade no tipo CLR. Quais agregados têm exclusão lógica passa a se ler no
`OnModelCreating`, num lugar só, em vez de `grep ISoftDeletable`.

### 3. Elas chegam junto com o interceptor que as escreve

**Não entram na migration `Inicial`.** É o mesmo critério que manteve `DeletedAt` fora de
`Pessoa`: coluna que nada escreve é uma aforância falsa. Aqui seria pior — sem interceptor,
`CreatedAt` gravaria `0001-01-01` em silêncio, e o README do kernel promete que ela nunca é
nula. Sem ambiente publicado e sem dado, a segunda migration não custa nada.

### 4. A identidade de quem executou vem de uma porta, não do domínio

Quando o interceptor existir, ele lê o ator de uma abstração em `Kernel.Application` — a
camada que o README já admite depender de `*.Abstractions`. **O `Guid?` mora ali.** O
`Kernel.Domain` não ganha nada, e o `Domain/` de nenhum módulo chega perto.

## O que este ADR não muda

- **`CreatedBy` continua `Guid`, e não `string`.** O argumento do `src/Kernel/README.md`
  segue de pé: guardar a claim `sub` de um IdP espalha PII por toda tabela do sistema no dia
  em que alguém reconfigurar o provedor para emitir e-mail. Muda só o endereço do `Guid`.
- **A distinção entre as colunas e o audit log.** Elas guardam quem mexeu por último; a
  tabela append-only guarda como a linha chegou aqui. Nenhuma substitui a outra, e **o
  desenho da tabela continua sendo ADR próprio**, como o [ADR-0015](0015-um-dbcontext-e-migrations-por-modulo.md)
  e o README do kernel já registraram.
- **Exclusão lógica continua opt-in, agregado por agregado.** O argumento de que isso obriga
  a pergunta a ser respondida um a um continua valendo. Muda de onde a resposta se lê.

## Consequências

**Boas.** Nenhum setter público sobra na raiz de agregado. `Entity<TId>` volta a caber numa
tela e a dizer uma coisa só. A exceção do ADR-0017 desaparece em vez de ser administrada — e
some sem inventar tipo nenhum, porque o valor deixa de entrar no domínio. E o modelo de
`Pessoa` passa a ter exatamente os campos da seção 4 da Spec-0001, nem um a mais.

**Ruins, aceitas.**

**Nome de coluna vira string.** Concentrado numa classe de constantes, mas é acoplamento que
o compilador não verifica: renomear e esquecer um lugar só falha em tempo de execução.

**Consulta que precisar de auditoria escreve `EF.Property<DateTimeOffset>(p, "CreatedAt")`.**
Mais feio que `pessoa.CreatedAt`, e o filtro global de exclusão lógica fica menos legível
pelo mesmo motivo.

**A coluna existe no banco e não existe na classe.** Quem abrir `Pessoa` e depois o `psql`
vai estranhar, e a explicação está aqui — não no código.

**"Quais agregados têm exclusão lógica" deixa de ser um `grep`.** Vira leitura da
configuração do contexto. Menos grepável, mais concentrado; foi a troca aceita.

**Uma segunda migration** para as colunas, quando o interceptor chegar.

## Gatilho de reavaliação

Se `EF.Property` para carimbo de auditoria aparecer em mais de dois ou três lugares, o custo
saiu do lugar previsto e a decisão merece ser revista.

E se uma tela precisar mostrar o carimbo com frequência, o sinal é outro: a pergunta virou
de domínio. Aí o caminho não é reabrir `IAuditable` — é dar ao dado um nome de igreja e um
campo próprio no agregado, do jeito que `ConvidadoPorId` já responde "quem trouxe o João".
