# Pessoas — o cadastro da igreja

O primeiro módulo de negócio. Implementa a
[Spec-0001](../../../../../docs/specs/0001-pessoas.md) e é a raiz única de cadastro da
invariante 4 do `CLAUDE.md`: membro e visitante são situação de vínculo, não entidades
separadas ([ADR-0008](../../../../../docs/adr/0008-pessoa-como-raiz-unica.md)).

> **Estado: o modelo, os quatro atos de transição, cinco rotas de leitura e uma de escrita.**
> Existem `Pessoa` e `VinculoIgreja` no schema `pessoas`, com as invariantes do histórico
> (RN-1 a RN-4) em `SucederVinculo`, os quatro métodos de transição (RN-5 a RN-12), as
> migrations, e o `PessoasTransactionBehavior` registrado.
>
> | Rota | É |
> | --- | --- |
> | `GET /api/pessoas/search?q=` | a busca da recepção |
> | `GET /api/pessoas?q=&situacao=&bairro=&page=&size=` | a lista da secretaria |
> | `GET /api/pessoas/aniversariantes?from=&to=` | a lista de domingo |
> | `GET /api/pessoas/pauta?date=` | o que o dirigente lê no culto |
> | `GET /api/pessoas/{id}` | a ficha com o histórico |
> | `POST /api/pessoas/{id}/anonimizacao` | o Art. 18 da LGPD (RN-16) |
>
> **A anonimização é a única escrita**, e o primeiro `ICommand` do módulo. Os quatro atos de
> transição existem no agregado e **não têm rota**. Não existem cadastro, `PATCH`, `Fundir`,
> autenticação nem audit log — inclusive a restrição da anonimização ao pastor, que é da
> matriz de permissões e ainda não existe. Este README descreve só o que já está escrito.

## Estrutura

| Pasta | Guarda |
| --- | --- |
| `Domain/` | `Pessoa`, `VinculoIgreja`, os objetos de valor, os ids tipados e `TextNormalization` |
| `Application/` | uma pasta por rota, com a query, o handler e os records da resposta dela. Na raiz, só o que mais de uma rota usa |
| `Infrastructure/` | `PessoasDbContext`, as `IEntityTypeConfiguration` e os `ValueConverter` |
| `Endpoints/` | `PessoasEndpoints.MapPessoasEndpoints` — **o único ponto público de rota** |
| `Migrations/` | geradas pelo `dotnet ef`, no schema `pessoas` |

**`Application/` se divide por rota, nunca por tipo técnico.** Nada de `Queries/`, `Handlers/`
ou `Dtos/` — é a mesma regra do `CLAUDE.md` que proíbe `Services/` e `Repositories/`, e o
mesmo motivo do [ADR-0012](../../../../../docs/adr/0012-monolito-modular-estrito-com-mediator-proprio.md)
para o módulo existir: mexer numa rota é abrir uma pasta, não caçar quatro arquivos em quatro
lugares.

```
Application/
  Anonimizar/           o único comando: AnonimizarCommand e o handler
  GetFichaPessoa/       a query, o handler, FichaPessoa e os records que só ela usa
  GetPauta/
  ListAniversariantes/
  ListPessoas/
  SearchPessoas/
  NomeFilter.cs         a raiz é o compartilhado, e por isso é curta
  PessoaRef.cs
```

O nome da pasta é o nome da query sem o sufixo, então `Endpoints/` e `Application/` se leem
em paralelo. **E a raiz é o alarme:** arquivo solto ali é declaração de que duas rotas
dependem dele — hoje o `NomeFilter`, que a busca e a lista compartilham, e o `PessoaRef`, que
a busca, a ficha e a pauta compartilham. Se a raiz crescer, o que cresceu foi o acoplamento
entre rotas, não a bagunça.

**`Aniversariante` é a exceção que confirma a regra**, e está em `ListAniversariantes/` mesmo
sendo usado pela pauta. Ele não subiu para a raiz porque o dono é a rota de aniversariantes: a
pauta o recebe pronto, despachando a query daquela fatia (abaixo). Quem move um record para a
raiz é o compartilhamento entre iguais, não o reuso por quem chama.

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

## A busca e a lista, e a regra que as separa

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

## `GET /api/pessoas/pauta?date=` — um handler que despacha outro

É a única rota do módulo com **duas listas numa resposta**, e o único handler que injeta
`ISender` para chamar outro handler. As duas coisas são deliberadas e custam explicação, então
aqui está ela.

**Duas listas porque é uma tela e um momento.** O dirigente lê em voz alta, de pé na frente,
no wi-fi do salão. Duas requisições são duas chances de a tela ficar pela metade com a igreja
olhando (seção 6 da spec). Não é o padrão da casa e não deve virar precedente: as outras
rotas continuam com uma projeção cada.

**`GetPautaHandler` despacha `ListAniversariantesQuery` em vez de repetir a consulta.** A
RN-25 — comparar dia e mês, excluir `Falecido` e `Transferido`, resolver 29/02 em ano comum —
mora em `ListAniversariantesHandler` e em nenhum outro lugar. A alternativa seria extrair a
consulta para um tipo novo que os dois handlers chamassem; ela é mais limpa no papel e foi
recusada porque criaria uma categoria de tipo que não existe neste módulo — nem handler, nem
entidade — só para evitar uma chamada que o `ISender` já sabe fazer. Handler chamando handler
é acoplamento em anel e merece desconfiança **quando há escrita**; aqui são duas leituras,
fora do anel de transação, sem efeito colateral.

O preço, para você não estranhar: cada `GET /pauta` produz **duas linhas** no
`LoggingBehavior` — `ListAniversariantesQuery` aninhada dentro de `GetPautaQuery`, com a de
dentro somando no tempo da de fora. É informação, não ruído.

**A semana vai de segunda a domingo, e `date` é o último dia dela.** Culto de domingo 23/08 lê
os aniversários de 17 a 23 — a semana que a igreja acabou de viver junta. Está em
`WeekContaining`, que devolve `(Monday, Sunday)` justamente para que a regra se leia no ponto
de chamada sem abrir o método. Das sete datas possíveis para `date`, seis caem no meio da
semana e só o domingo revela onde ela termina, então o exemplo da spec não basta: a regra está
escrita lá na seção 6, em palavras.

**Os visitantes vêm ordenados por `NomeNormalizado`, e isso não é enfeite.** A tela tem botão
de atualizar, porque a recepção pode cadastrar alguém durante o louvor (seção 8). Sem `ORDER
BY`, a lista que o dirigente está lendo pode voltar embaralhada. Há teste, e ele passou
**antes** da ordenação existir — o índice em `nome_normalizado` fez o Sqlite devolver em ordem
por acidente do plano. Teste de ordenação sem `ORDER BY` explícito documenta intenção, não
garante comportamento.

**`FundidaEmId IS NULL` filtra os visitantes, e é a RN-24.** O cadastro não tem chave de
idempotência de propósito (seção 6), então a recepcionista **vai** cadastrar o mesmo visitante
duas vezes na rede ruim do salão, e a secretaria vai fundir. Sem o filtro, o dirigente lê
"temos hoje o João… e o João" — o erro que a fusão existe para evitar, no momento exato em que
ela deveria ter funcionado.

O que **não** filtra é `Falecido` e `Transferido`. A RN-25 exclui os dois da lista de
aniversariantes, e não há regra equivalente para visitantes: `visitantes` são os cadastrados
naquele dia, e ponto — a seção 6 diz isso em uma frase. Filtrar por situação ali seria regra
inventada no código.

## `POST /api/pessoas/{id}/anonimizacao` — a única escrita, e a mais definitiva

Atende ao Art. 18 da LGPD **sem excluir a linha**: `Pessoa` nunca some fisicamente (RN-15),
porque o ADR-0015 abriu mão de chave estrangeira entre módulos apoiado nisso e um `DELETE` de
verdade viraria corrupção silenciosa em `EscalaItem`. O `Id` sobrevive à anonimização
justamente para que a escala de 2024 continue apontando para alguém.

**Quem apaga é o agregado.** `Pessoa.Anonimizar()` substitui os nove campos da ficha e liga a
marca; o handler carrega, chama e devolve. Não há `SaveChanges` no handler — o
`PessoasTransactionBehavior` fecha o anel, como em qualquer `ICommand`. Não existe caminho que
desfaça: `Anonimizada` só é escrita como `true`, e um segundo `POST` responde `409`
`Pessoa.Anonimizada` em vez de reanonimizar.

**A troca de `init` por `private set` nos campos pessoais é o que torna isso possível**, e ela
tem um efeito que aparece longe daqui: nenhum teste monta mais uma `Pessoa` por inicializador
de objeto. Os fixtures passam por `Pessoa.Cadastrar(…)`, que é a única porta de entrada dos
dados — e, de quebra, já abre o vínculo. O que ainda entra pelo construtor é a marca de fusão,
porque `Fundir` (RN-17) não existe.

**`Motivo` do vínculo não é anonimizado, e isso é decisão registrada** na RN-16 e na seção 9
da spec. Ele é histórico — a mesma regra que manda preservar situação e datas —, e a seção 7
já o entrega restrito a secretaria e pastor. A consequência: o critério "nenhuma coluna guarda
o nome original" vale para a tabela `pessoas`, e o teste que o verifica varre `SELECT *`
daquela tabela, não do schema. `Motivo_do_vinculo_deve_sobreviver_a_anonimizacao` é quem
segura a decisão; se ela mudar, é esse que cai primeiro.

**A guarda de escrita mora em `SucederVinculo`**, por onde os quatro atos passam — um `if`,
não quatro. Ela roda **depois** da matriz da seção 5, então um ato que já era inválido continua
respondendo `Pessoa.TransicaoInvalida`: um afastado anonimizado recebendo `ReconhecerAfastamento`
ouve "transição inválida", não "registro anonimizado". Os dois são `409` e a tela não muda; o
que muda é o `code`. Se a precedência importar, o lugar é um `if` no topo de cada ato.

**As quatro leituras filtram; a ficha, não.** Busca, lista, pauta e aniversariantes excluem
`Anonimizada`; `GET /api/pessoas/{id}` continua respondendo `200`, marcado, com o histórico —
é o que a seção 8 pede ("mostra como anonimizada, sem ações de edição") e o que mantém o `Id`
resolvendo. Sumir da busca não é sumir do sistema.

Dois detalhes das consultas que não se deduzem:

- **A busca filtra nas duas consultas, não numa.** A primeira casa o nome e resolve
  `FundidaEmId ?? Id`; a segunda materializa. Sem o filtro na segunda, buscar pelo nome de um
  registro absorvido devolveria a ficha **anonimizada** do sobrevivente.
- **O nome de substituição é um valor de busca como outro qualquer.** Com `NomeNormalizado`
  virando `ANONIMIZADO`, digitar "anon" na recepção listaria, com um termo só, exatamente quem
  exerceu o Art. 18 — se o filtro não existisse. Há teste para o termo, e não só para o nome
  antigo.

Em aniversariantes o filtro é redundante hoje, porque `DataNascimento` e `DataCasamento` ficam
nulas. Está lá assim mesmo: proteção implícita é a que some no próximo refactor.

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

**`VisitanteDaPauta` é mais pobre ainda**: `id`, `nome` e `convidadoPor`, três campos. O
dirigente precisa do nome para chamar e de quem convidou para apresentar — *"temos hoje o
João, convidado pela Maria"* — e de mais nada. E repare no que a pauta **não** revela mesmo
levando aniversariantes: sai a data deste ano, nunca a de nascimento. A RN-25 compara só dia e
mês, então o ano nem sai do banco. Idade é dado pessoal que a lista lida em voz alta não
precisa expor, e `PautaEndpointTests` afirma isso sobre o corpo HTTP.

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
