# Pessoas — o cadastro da igreja

O primeiro módulo de negócio. Implementa a
[Spec-0001](../../../../../docs/specs/0001-pessoas.md) e é a raiz única de cadastro da
invariante 4 do `CLAUDE.md`: membro e visitante são situação de vínculo, não entidades
separadas ([ADR-0008](../../../../../docs/adr/0008-pessoa-como-raiz-unica.md)).

> **Estado: o modelo, o anel de transação e uma rota — a busca da recepção.**
> Existem `Pessoa` e `VinculoIgreja` no schema `pessoas`, com as invariantes do histórico
> (RN-1 a RN-4) em `SucederVinculo`, as migrations, e o `PessoasTransactionBehavior`
> registrado. **A única rota é `GET /api/pessoas/search`.** Não existem os quatro métodos de
> transição, nem cadastro, nem ficha, nem autenticação, nem audit log. Este README descreve
> só o que já está escrito.

## Estrutura

| Pasta | Guarda |
| --- | --- |
| `Domain/` | `Pessoa`, `VinculoIgreja`, os objetos de valor, os ids tipados e `TextNormalization` |
| `Application/` | uma query, um response e um handler por caso de uso |
| `Infrastructure/` | `PessoasDbContext`, as `IEntityTypeConfiguration` e os `ValueConverter` |
| `Endpoints/` | `PessoasEndpoints.MapPessoasEndpoints` — **o único ponto público de rota** |
| `Migrations/` | geradas pelo `dotnet ef`, no schema `pessoas` |

**Tudo é `internal`, exceto `ServiceCollectionExtensions` e `PessoasEndpoints`.** O host
compõe o módulo por dois verbos e não alcança nada de dentro
([ADR-0012](../../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md)).
É por isso que os endpoints moram aqui e não no bootstrapper: um endpoint lá fora obrigaria os
contratos a serem públicos. O preço aceito é que o módulo referencia
`Microsoft.AspNetCore.App` — mais barato que uma camada de mapeamento duplicando cada
contrato.

**Handler se registra à mão**, um por linha em `AddPessoasHandlers()`. Sem varredura de
assembly: a pergunta "onde isto é ligado?" precisa ter resposta grepável para um voluntário
que chegou ontem.

## `GET /api/pessoas/search?q=`

A seção 6 da spec põe a busca da recepção e a lista paginada da secretaria na mesma
`GET /api/pessoas`. **Elas foram separadas em rotas distintas**, e a spec foi emendada.

O padrão da indústria separa quando a projeção, o envelope e a paginação diferem — Stripe
(`/v1/customers/search`), GitHub (`/search/users`), a AIP-136 do Google. Aqui diferem todos:
projeção pobre contra linha de lista, `results` contra `items/page/size/total`, teto fixo de
10 contra paginação, recepção contra secretaria. E há a razão que decide: em OpenAPI, path +
método é **uma** operação com **um** schema de resposta. Na mesma rota, o cliente gerado da
#49 herdaria uma união de tipos, e a invariante 5 do `CLAUDE.md` entregaria um cliente sem
tipos úteis.

O parâmetro é `q`, não `search`: `search?search=` é redundante, e `q` é o que GitHub, OData e
Elasticsearch usam.

**Rotas novas entram pelo `MapGroup("/api/pessoas")`.** É a costura onde o
`.RequireAuthorization()` vai entrar uma vez, quando a autenticação existir — em vez de treze
vezes, com a décima terceira esquecida. E toda rota de id nasce com `{id:guid}`: sem a
restrição, `/api/pessoas/qualquercoisa` casa e explode dentro do handler.

## O nome normalizado

`Pessoa` grava `NomeNormalizado` — sem acento, em maiúsculas — ao lado de `Nome`. A busca
filtra por essa coluna, e o índice está nela.

**Por que na escrita, e não na consulta.** A alternativa era `unaccent` do Postgres. Ela ficaria
sem teste no CI, que não sobe Postgres, e `unaccent()` não é `IMMUTABLE` — sem uma função
wrapper, não dá para indexar. Normalizar na escrita é a mesma decisão que a spec já tinha
tomado para `Bairro` (RN-19), funciona igual em Postgres e no Sqlite dos testes, e deixa a
regra no domínio.

**Maiúsculas, não minúsculas**, porque o CA1308 recusa `ToLowerInvariant` e tem razão: a
conversão para minúsculas não é reversível em todo o Unicode. E `Invariant`, sempre — em
turco, `'i'` maiúsculo é `'İ'`, e um servidor com aquele locale quebraria a busca de todo nome
com `i`.

**A consequência que ninguém deduz sozinho: não existe backfill em SQL.** Preencher
`nome_normalizado` a partir de `nome` exigiria tirar acento dentro do banco, que é o
`unaccent` recusado acima. Todo preenchimento em massa passa pelo código da aplicação — a
importação da #50 constrói `Pessoa`, nunca `INSERT`.

**Existe exatamente um lugar que escreve `Nome`: o construtor de `Pessoa`.** É o que garante
que os dois campos não divirjam, e um nome atualizado sem o normalizado correspondente produz
uma pessoa que existe na ficha e não existe na busca, sem erro e sem log.

> **Quem precisar de um segundo escritor de `Nome` promove `NomeDePessoa` a objeto de valor
> antes, não depois.** É a tarefa já descrita na #45, onde ele nasce carregando também a
> RN-13 e a RN-21.

## A projeção pobre

`PessoaEncontrada` tem cinco campos — `id`, `nome`, `situacao`, `desde`, `convidadoPor` — e
nada mais. Sem endereço, sem celular, sem data de nascimento (seção 6 da spec).

**A pobreza é do SQL, não do C#.** O `SELECT` lista cinco colunas; o dado pessoal não sai do
banco. Se a consulta materializasse `Pessoa` e projetasse em memória, a resposta seria pobre
mas o endereço estaria no heap do processo e em qualquer dump. É a diferença que a seção 9 da
spec cobra ao chamar endereço de "o campo que mais eleva o custo de um vazamento".

Dois testes guardam isso, de propósito redundantes:

- `ProjecaoPobreTests` afirma a lista **exata** de propriedades, e tem um `[Theory]` nomeando
  cada campo coletado na apresentação. O primeiro se conserta editando uma lista; o segundo só
  passa se alguém **apagar uma asserção de LGPD**. O atrito é o ponto.
- `SearchEndpointTests` afirma sobre o **corpo HTTP**, e sobre os **valores**, não sobre nomes
  de campo: pergunta se o número de celular saiu, não se existe um campo chamado `celular`.

## Casamento por token — um sentido só

Cada token digitado precisa ser prefixo de algum token do nome. `joão gue` acha `João Guedes`;
a ordem dos tokens não importa.

**O sentido inverso não é suportado**: `joão guedes` **não** acha um registro gravado como
`João Gue`. Suportá-lo exigiria os tokens do nome como linhas numa tabela filha, porque o SQL
não quebra string em tokens dentro do `WHERE` de um jeito que o EF traduza. O caso perdido —
nome gravado truncado no meio da palavra — é raro o bastante para não pagar o esquema a mais.

O predicado usa `StartsWith`/`Contains`, nunca `EF.Functions.Like` com concatenação: o EF
escapa o parâmetro, então um `%` digitado é por cento, não curinga.

## Duas coisas conhecidas e aceitas

**A subconsulta de `convidadoPor` gera um `ROW_NUMBER` inútil.** Para garantir "no máximo uma
linha" no `FirstOrDefault`, o EF emite uma função de janela particionada pelo `Id` — que é a
chave primária, logo cada partição tem uma linha por definição. É trabalho para nada, sobre a
tabela inteira. As saídas custam legibilidade permanente (`join … into … DefaultIfEmpty()`, ou
projeção anônima com `var`, que este código-base não usa) para ganhar desempenho que não
existe em 86 linhas. Se a tabela crescer uma ordem de grandeza, revisite.

**Uma `Pessoa` sem vínculo vigente sai como `Visitante` em `0001-01-01`.** É o valor padrão
que o `FirstOrDefault` vira no SQL. O estado é inalcançável pelo agregado — RN-1 e RN-5
garantem sempre exatamente um vínculo aberto —, então não há defesa construída para ele. O que
existe é `Pessoa_sem_vinculo_nao_deve_derrubar_a_busca`, que garante a única propriedade que
importa: **a rota mais crítica do sistema responde**, mesmo com uma linha malformada.

## Testes

| Projeto | Cobre | Enxerga internos |
| --- | --- | --- |
| `CathedrAll.Pessoas.Tests` | mapeamento, invariantes, o anel, a consulta contra Sqlite | sim |
| `CathedrAll.Api.Tests` | a rota, o JSON, o vazamento — via HTTP | sim, **só para o arrange** |

O `InternalsVisibleTo` para `CathedrAll.Api.Tests` existe para montar cenário: trocar o
`PessoasDbContext` por Sqlite e construir uma `Pessoa` com vínculo. **As asserções são
caixa-preta.** Um teste ali que afirme sobre `PessoaEncontrada` em vez de sobre o corpo da
resposta deixou de testar a API.

`PessoasApiFactory` dá um banco novo por teste, e remove os registros do Postgres por
varredura de tudo que menciona `PessoasDbContext` — e não por uma lista fixa de tipos, porque
o conjunto de serviços que o EF registra por contexto mudou entre versões.

**Para ver o SQL de verdade**, o que compila não basta: um teste descartável com
`LogTo(linha => log.AppendLine(linha), [DbLoggerCategory.Database.Command.Name], LogLevel.Information)`
e um `Assert.Fail(log.ToString())` no fim. Foi assim que o `ROW_NUMBER` acima apareceu. Vale
repetir em toda consulta não trivial.
