# Pessoas — o cadastro da igreja

O primeiro módulo de negócio. Implementa a
[Spec-0001](../../../../../docs/specs/0001-pessoas.md) e é a raiz única de cadastro da
invariante 4 do `CLAUDE.md`: membro e visitante são situação de vínculo, não entidades
separadas ([ADR-0008](../../../../../docs/adr/0008-pessoa-como-raiz-unica.md)).

> **Estado: o modelo, o anel de transação e duas rotas de leitura.**
> Existem `Pessoa` e `VinculoIgreja` no schema `pessoas`, com as invariantes do histórico
> (RN-1 a RN-4) em `SucederVinculo`, as migrations, e o `PessoasTransactionBehavior`
> registrado. **As rotas são `GET /api/pessoas/search` — a busca da recepção — e
> `GET /api/pessoas` — a lista da secretaria.** Nenhuma escreve. Não existem os quatro
> métodos de transição, nem cadastro, nem ficha, nem autenticação, nem audit log. Este README
> descreve só o que já está escrito.

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

## As duas rotas, e a regra que as separa

A seção 6 da spec punha a busca da recepção e a lista paginada da secretaria na mesma
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

**E o critério vale nos dois sentidos, senão não é critério.** A lista da secretaria também
filtra por nome, e esse `?q=` **não** virou rota: ele devolve a mesma linha de lista, no mesmo
envelope paginado, com o mesmo casamento por token. Envelope igual, parâmetro; envelope
diferente, rota. Aplicar a régua só quando ela justifica separar é usá-la como desculpa.

**Rotas novas entram pelo `MapGroup("/api/pessoas")`.** É a costura onde o
`.RequireAuthorization()` vai entrar uma vez, quando a autenticação existir — em vez de treze
vezes, com a décima terceira esquecida. E toda rota de id nasce com `{id:guid}`: sem a
restrição, `/api/pessoas/qualquercoisa` casa e explode dentro do handler.

## `GET /api/pessoas?q=&situacao=&bairro=&page=&size=`

A lista da secretaria: `items`, `page`, `size`, `total`. Desktop, sentada, com tempo — o
oposto do celular no meio do salão.

**Os três filtros valem ao mesmo tempo**, e cada um tem uma sutileza:

| Filtro | Vira | A sutileza |
| --- | --- | --- |
| `q` | o mesmo predicado da busca | `NomeFilter`, compartilhado. Não há segunda implementação |
| `situacao` | `EXISTS` sobre o vínculo com `DataFim IS NULL` | vínculo **vigente**, não o último. Quem foi membro e hoje está afastado não sai em `?situacao=Membro` |
| `bairro` | `endereco_bairro_normalizado = @bairro` | casa contra o derivado, nunca contra o digitado |

**RN-24 aqui é o oposto da busca, e de propósito.** Na busca, o registro absorvido **resolve
para o sobrevivente** — a recepcionista digitou o nome que está no papel dela e precisa achar
alguém. Na lista ele **some**, com `WHERE fundida_em_id IS NULL`: contá-lo faria o `total`
dizer 87 para uma igreja de 86. Mesma regra, dois comportamentos, porque são duas perguntas.

**`ORDER BY nome_normalizado, id`, e o `id` não é enfeite.** Sem o desempate, homônimos —
que este domínio produz de propósito, sem CPF nem constraint — empatam no `ORDER BY`, o
banco fica livre para ordená-los diferente a cada consulta, e a página 2 repete alguém da 1 e
pula outro. O teste usa seis "Maria Souza" idênticas justamente porque com nomes distintos
ele passaria sem o desempate, sem testar nada.

**Padrão 25, teto 50.** O teto precisa ser menor que a congregação, ou não é teto: com 86
pessoas, o limite reflexo de 100 devolveria a igreja inteira e o requisito "nunca a lista
inteira" viraria letra morta. `page` e `size` voltam **corrigidos** na resposta — a tela
precisa saber que pediu 999 e recebeu 50, senão desenha "1 a 999 de 86". Valor ausente cai no
padrão; valor absurdo é corrigido, não recusado.

**O corte mora no handler, não no endpoint.** Os testes de `CathedrAll.Pessoas.Tests` chamam
handler direto, então teto no endpoint ficaria sem cobertura ali — e um segundo chamador, como
a importação da #50, passaria por fora dele.

**São duas idas ao banco: o `COUNT` e a página.** Dava para trazer o total junto com uma
função de janela; não vale. A spec pede contagem de verdade sobre ~86 linhas, e duas consultas
legíveis ganham de uma esperta.

**A lista não herdou o `ROW_NUMBER` da busca.** A verruga de lá vem da subconsulta de
`convidadoPor`, que correlaciona `pessoas` com `pessoas`. Aqui `situacao` e `desde` atravessam
para `vinculos_igreja`, e um `LIMIT 1` basta.

## O bairro normalizado

`Endereco` grava `BairroNormalizado` ao lado de `Bairro`, pela **mesma** razão que `Pessoa`
grava `NomeNormalizado`, e a razão não é a que parece à primeira vista.

O primeiro instinto foi uma coluna só, normalizada — a spec dizia "`Bairro` é gravado
normalizado" e listava um campo. Ela não sobrevive à tela: a normalização apaga o acento, e
**isso não tem volta**. De `GROTAO` ninguém recupera `Grotão`; a melhor tentativa do CSS
produz `Grotao`, e a secretaria lê isso toda semana. O caminho inverso — normalizar mantendo
o acento — quebra o filtro, porque quem digita `grotao` deixa de casar, e casar sem acento
exigiria o `unaccent` recusado acima.

**O critério, que vale para todo campo futuro deste módulo: normalização destrutiva em campo
que alguém lê são duas colunas.** `Bairro` fica com o `Trim`, que não perde nada.
`BairroNormalizado` fica com a caixa e o acento, que perdem. A spec foi emendada (seção 4 e
RN-19).

> **Uma armadilha do EF que custou uma migration vazia.** Numa complex type, o EF mapeia
> propriedade sem setter **só quando ela é parâmetro do construtor** — é por isso que `Bairro`
> continua mapeado. `BairroNormalizado` nasceu `{ get; }`, não é parâmetro, e o EF **a ignorou
> em silêncio**: compilou, os testes passaram, e o `dotnet ef migrations add` gerou um `Up`
> vazio. É `{ get; private set; }`, como `Pessoa.NomeNormalizado`. **Gere a migration e leia**
> — compilar não é evidência de que o EF mapeou.

`Bairro` é declarado à mão no record posicional, o que o deixa `{ get; }` sem `init`. Isso
não é estilo: com `init`, um `endereco with { Bairro = … }` produziria um `Bairro` novo e um
`BairroNormalizado` velho. Do jeito que está, o compilador recusa — e o `with` não faz falta,
porque a spec diz que `Endereco` é substituído em bloco.

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

São dois records de cinco campos, e **não um reusado**:

| Record | Rota | Os cinco campos | O quinto existe para |
| --- | --- | --- | --- |
| `PessoaEncontrada` | `/search` | `id`, `nome`, `situacao`, `desde`, `convidadoPor` | desempatar homônimo na recepção |
| `PessoaDaLista` | `/api/pessoas` | `id`, `nome`, `situacao`, `desde`, `bairro` | ser o campo que o próprio filtro usa |

Nenhum dos dois tem endereço, celular ou data de nascimento (seção 6 da spec). `PessoaDaLista`
leva `bairro` — o digitado, nunca `BairroNormalizado`, que não aparece em resposta alguma.

**Reusar um só record seria o argumento da separação de rotas se contradizendo:** em OpenAPI,
path mais método é uma operação com um schema, e dois records é o que faz o cliente gerado da
#49 ter dois tipos honestos em vez de uma união.

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

**Mora em `Application/NomeFilter.cs`, e as duas rotas chamam de lá.** São dois métodos, e a
separação entre eles é deliberada: `Tokenize` devolve os tokens, `Apply` recebe os tokens já
prontos. Parece um passo a mais, mas **"zero tokens" significa coisas opostas nas duas rotas**
— na busca, lista vazia, porque devolver a igreja inteira num celular seria o pior resultado
possível; na lista, filtro nenhum, primeira página de todos. Um `Apply(pessoas, termo)` único
teria que escolher uma das duas e mentir para a outra. Quem chama segura os tokens e é
obrigado a dizer o que faz com zero.

O teto de 10 da busca **não** foi para lá: é política daquela rota, não da regra de casar
nome. A lista tem o teto dela, com outro número e outro motivo.

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
