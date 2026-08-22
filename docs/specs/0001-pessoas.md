# Spec-0001 — Pessoas

**Status:** Rascunho · **Data:** 2026-08-22 · **Responsável:** Miquéias Filho

Deriva de: [`docs/dominio.md`](../dominio.md#cadastro) · Decisões relacionadas:
[ADR-0008](../adr/0008-pessoa-como-raiz-unica.md),
[ADR-0012](../adr/0012-monolito-modular-estrito-com-mediator-proprio.md),
[ADR-0013](../adr/0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md),
[ADR-0014](../adr/0014-problem-details-como-formato-unico-de-erro.md),
[ADR-0015](../adr/0015-um-dbcontext-e-migrations-por-modulo.md)

## 1. Objetivo

Guardar quem é a igreja — quem visitou, quem é membro, quem se afastou, quem se transferiu,
quem faleceu — com a **história** de cada um, e não só a situação de hoje.

Hoje isso vive num formulário do Google e numa planilha de 90 linhas para ~86 pessoas. Não
há como saber quando alguém virou membro, a mesma pessoa aparece duas vezes, e a lista dos
aniversariantes que se ora todo domingo sai de uma fórmula que alguém mantém à mão.

O que some é a resposta "não sei" para as perguntas que a liderança faz toda semana: quem
faz aniversário domingo, quem apareceu pela primeira vez este mês, há quanto tempo fulano
congrega, quantos membros a igreja tem de verdade.

## 2. Fora de escopo

- **`Familia`.** Sem dado de terceiro ela ligaria só pessoas já cadastradas, o que cobre
  ~25 das 86. Volta quando houver regra que a exija.
- **Contato de emergência e responsável por menor.** Entra junto com o check-in do
  ministério infantil, e entra estreito.
- **Frequência e presença.** Por isso o afastamento é registrado, nunca detectado.
- **Transferência entre instâncias.** `Transferido` é anotação local; não existe integração
  com outra igreja. Congregação única, [ADR-0007](../adr/0007-congregacao-unica.md).
- **Envio** de mensagem a aniversariante. A **lista** está dentro; o disparo, não.
- **Experiência prévia em ministério e área de interesse.** Respondem "onde essa pessoa
  poderia servir?" — vão para o módulo `Departamentos`. Até lá ficam na planilha.
- **`Usuario`, login e permissões de acesso.** Módulo de acesso, que ainda não existe.
  `Usuario` aponta para `Pessoa`, nunca o contrário.

## 3. Vocabulário

| Termo na igreja | No código | Observação |
|---|---|---|
| Apresentação | `RegistrarApresentacao` → `VinculoIgreja` com `Situacao = Membro` | Cerimônia pública de apresentação e oração. É o que constitui a membresia — não o batismo |
| Visitante | `VinculoIgreja` com `Situacao = Visitante` | Estado de entrada. Nunca se conta o total; conta-se por período |
| Membro | Vínculo vigente com `Situacao = Membro` | |
| Afastado | `ReconhecerAfastamento` | Registra o **reconhecimento** da igreja, não o dia em que a pessoa sumiu |
| Transferido | `RegistrarTransferencia` | Saiu em boa ordem, com carta. Diferente de afastado, e a diferença não se recupera depois |
| Aniversariantes da semana | consulta sobre `DataNascimento` e `DataCasamento` | Nascimento **e** casamento. Lida no culto de domingo |

A tela nunca diz "efetivar membro" nem "criar vínculo". Diz "registrar apresentação".

## 4. Modelo de dados

Schema `pessoas`, `DbContext` próprio, tudo `internal` ([ADR-0015](../adr/0015-um-dbcontext-e-migrations-por-modulo.md)).

### `Pessoa` — raiz de agregado

A ficha se preenche em **duas etapas**, e isso não é acidente do formulário: é a prática da
igreja, e é minimização da LGPD funcionando por instinto. No primeiro contato coleta-se o
mínimo, para não tomar o tempo de quem acabou de chegar. O resto vem na apresentação — que é
quando a igreja passa a ter uso para o dado.

| Campo | Tipo | Coletado | Regra |
|---|---|---|---|
| `Id` | `Guid` | — | |
| `Nome` | `string(120)` | cadastro | **Único obrigatório.** Não vazio depois de `Trim`. Parcial no cadastro, completado na apresentação |
| `ConvidadoPorId` | `Guid?` | cadastro | Referência a outra `Pessoa`. Sem navegação |
| `FundidaEmId` | `Guid?` | — | Preenchido pela fusão. Não nulo = este registro foi absorvido |
| `Celular` | `string(20)?` | apresentação | E.164. **Não é único** — 10 números são compartilhados na ficha real |
| `Email` | `string(200)?` | apresentação | Formato válido; gravado em minúsculas |
| `DataNascimento` | `date?` | apresentação | Não futura |
| `EstadoCivil` | enum? | apresentação | `Solteiro`, `Casado`, `UniaoEstavel`, `Divorciado`, `Viuvo` |
| `DataCasamento` | `date?` | apresentação | Não futura. Alimenta a oração de domingo |
| `Endereco` | `Endereco?` | apresentação | Objeto de valor, abaixo |
| `Profissao` | `string(120)?` | apresentação | Texto livre. Serve para achar quem **presta** o serviço |
| `DataBatismo` | `date?` | apresentação | Não futura. Pode ser de outra igreja |
| `Vinculos` | coleção | — | Dentro do agregado. Sem setter público |

**Só `Nome` é `NOT NULL`.** Obrigatoriedade aqui não é propriedade do campo, é propriedade
da situação: a mesma `Pessoa` tem exigências diferentes conforme o vínculo, e a coluna só
sabe dizer "sempre" ou "nunca". Se `DataNascimento` fosse obrigatória, o resultado não seria
a igreja passar a coletá-la do visitante — seria alguém digitando `01/01/1900` para o
formulário passar. Nulo significa **"ainda não se sabe"**, e dado inventado é pior que dado
ausente porque depois não se distingue um do outro.

**A completude da ficha do membro é garantida pela oração de domingo, não pela coluna.**
Membro sem `DataNascimento` não aparece na lista da semana, ninguém ora por ele, alguém
nota, e o dado é preenchido. A prática que já existe é o mecanismo de qualidade — mais
confiável que uma trava, e sem o custo de recusar o registro de uma apresentação que
aconteceu de fato. **Nunca recuse o registro de um fato que já ocorreu.**

**O cadastro de visitante tem dois campos: nome e quem convidou.** Nem celular, nem e-mail.
A consequência precisa ser dita: **a igreja não tem como contatar um visitante** — e é por
isso que `ConvidadoPorId` não é curiosidade estatística, é o **canal de volta**. O
acompanhamento aqui é relacional: não se liga para o visitante, fala-se com o membro que o
trouxe. Isso muda o que a tela útil mostra — não "lista de visitantes", e sim "visitantes
que fulano trouxe".

`ConvidadoPorId` é a primeira auto-referência do modelo, e segue a mesma regra de sempre:
outro agregado, **referência por `Id`, sem propriedade de navegação** — senão alguém carrega
uma árvore de convites sem perceber. Ela nunca fica órfã, porque `Pessoa` não é excluída
fisicamente (RN-15), o mesmo argumento que sustenta a ausência de FK entre módulos no
ADR-0015.

Quando quem convidou não está cadastrado, o campo fica **nulo** — nunca texto livre com o
nome. Seria dado de terceiro, e o propósito do campo se autolimita: ele existe para se falar
com quem convidou, e com quem não está no sistema não se fala. Categoria de canal de chegada
("convite", "redes sociais", "passou na porta") seria enum inventado; se a igreja quiser essa
análise, ela sai do dado depois — mesmo caminho do `Motivo` e do `Bairro`.

Com só o nome, dois visitantes homônimos são indistinguíveis e a chave de deduplicação não
existe para eles. Duplicata de visitante vai acontecer e quase sempre não vai importar — mais
uma razão para "quantos visitantes temos" não ser pergunta com resposta.

### `Endereco` — objeto de valor

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `Cep` | `string(8)?` | não | Só dígitos |
| `Logradouro` | `string(150)?` | não | |
| `Numero` | `string(10)?` | não | **String, não inteiro** — existe `s/n` e existe `123-A` |
| `Complemento` | `string(60)?` | não | |
| `Bairro` | `string(80)` | **sim** | Gravado normalizado |
| `Cidade` | `string(80)?` | não | |
| `Uf` | `string(2)?` | não | Uma das 27 |

Serve a propósitos declarados: visita pastoral, entrega, ação social e filtro por região.
Dado pessoal **comum** (Art. 5º, I), da própria pessoa — nada a ver com as restrições de
dado sensível e de terceiro que valem em outros campos.

`Endereco` inteiro é opcional em `Pessoa`. Mas **se existe, tem pelo menos `Bairro`.** É o
que a ficha real tem hoje, em 90 de 90 linhas, e é o que o filtro precisa; exigir endereço
completo faria o objeto de valor rejeitar o dado que já existe. Os demais campos se
preenchem conforme a igreja tem motivo para saber onde a pessoa mora. Endereço com precisão
variável, e honesto sobre isso.

É objeto de valor pelos dois critérios: dois endereços iguais são intercambiáveis, e ele é
substituído **em bloco** — ninguém edita só o logradouro; a pessoa se muda e o endereço
inteiro troca.

`Bairro` é normalizado na escrita — `Trim`, caixa e acento consistentes. Sem isso o filtro é
inútil: na ficha real são 50 valores distintos que viram 34 depois de normalizados. Lista
fixa de bairros fica para depois e sai **do dado**, não da imaginação; mesmo caminho do
`Motivo`.

### `VinculoIgreja` — dentro do agregado

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `Situacao` | enum | sim | `Visitante`, `Membro`, `Afastado`, `Transferido`, `Falecido` |
| `DataInicio` | `date` | sim | Não futura. Igual à `DataFim` do vínculo anterior |
| `DataFim` | `date` | não | Nula = vigente |
| `Motivo` | `string(500)` | condicional | Obrigatório em `Afastado` e `Transferido` |

`Motivo` é **texto livre**, não enum: a igreja não tem lista de motivos definida, e enum
inventado por quem não vive a prática produz valor que ninguém escolhe. Depois de um ano de
registros reais a taxonomia sai do que as pessoas escreveram. O campo pode conter juízo
sobre a pessoa — disciplina, conflito —, então nasce com leitura restrita.

Não existe `VinculoRepository` nem rota `/vinculos`. O vínculo se alcança pela pessoa.

**Índices e unicidade:** índice em `Pessoa.Nome` para a busca do atendimento; índice em
`(PessoaId, DataFim)` para achar o vínculo vigente. **Nenhuma restrição de unicidade** — a
ficha não tem CPF nem documento, e nome + data de nascimento é heurística, não chave (nas
90 linhas há 2 pares de homônimos com datas diferentes). Duplicata se resolve com `Fundir`,
não com constraint.

**Migrations:** em `src/Modulos/CathedrAll.Pessoas/Migrations/`, com
`__EFMigrationsHistory` no schema `pessoas`. *(O ADR-0015 grafa a pasta como `Modules/` e o
repositório usa `Modulos/`; pela convenção do ADR-0013 o ADR está certo e a pasta está
errada. Resolver antes da primeira migration.)*

## 5. Regras de negócio

### Invariantes do histórico

- **RN-1** — Uma `Pessoa` tem no máximo um vínculo vigente (`DataFim` nula).
- **RN-2** — A história é contínua: a `DataInicio` de um vínculo é igual à `DataFim` do
  anterior. Sem buraco, sem sobreposição.
- **RN-3** — Nenhuma data de vínculo pode estar no futuro.
- **RN-4** — Mudança de situação **cria** um vínculo e fecha o anterior. Nunca um `UPDATE`
  que apague o passado.

### Matriz de transições

| De ↓ / Para → | Visitante | Membro | Afastado | Transferido | Falecido |
|---|:---:|:---:|:---:|:---:|:---:|
| *(cadastro novo)* | ✓ | ✓ | | | |
| Visitante | — | ✓ | | | ✓ |
| Membro | | — | ✓ | ✓ | ✓ |
| Afastado | | ✓ | — | ✓ | ✓ |
| Transferido | | ✓ | | — | ✓ |
| Falecido | | | | | — |

- **RN-5** — `Cadastrar` abre vínculo `Visitante` por padrão, ou `Membro` quando informado.
  A segunda entrada existe para a importação — as 90 linhas são membros sem passado de
  visitante — e serve a quem chega transferido e é apresentado logo depois.
- **RN-6** — `RegistrarApresentacao(data)`: válida sem vínculo, ou de `Visitante`,
  `Afastado`, `Transferido`. Abre `Membro` com `DataInicio` = data da cerimônia.
- **RN-7** — `ReconhecerAfastamento(motivo, data)`: válida só de `Membro`. Exige `Motivo`.
  A data **não pode ser retroativa** — seria chute sobre quando a pessoa parou de vir, e
  chute vira relatório errado. Registra-se o dia do reconhecimento.
- **RN-8** — `RegistrarTransferencia(destino, data)`: válida de `Membro` e `Afastado`. Exige
  o destino em `Motivo`. Não há integração com outro sistema.
- **RN-9** — `RegistrarFalecimento(data)`: válida de qualquer situação exceto `Falecido`. A
  data **pode ser retroativa**, ao contrário da RN-7: aqui o fato é conhecido e só a notícia
  chegou tarde.
- **RN-10** — `Falecido` é terminal. Nenhuma transição parte dele.
- **RN-11** — Não se volta para `Visitante` de situação nenhuma.
- **RN-12** — Transição fora da matriz é recusada com `Conflict` → `409`
  ([ADR-0014](../adr/0014-problem-details-como-formato-unico-de-erro.md)).

A matriz **não** é tabela no banco. Ela vive em três lugares: aqui, para leitura humana; no
teste parametrizado da seção 11, para não regredir; e nas assinaturas dos quatro métodos
acima, para ser cumprida. Cada célula tem condição, não só permissão — e condição não cabe
em duas colunas.

### Dados e ciclo de vida

- **RN-13** — `Nome` não pode ficar vazio depois de `Trim`.
- **RN-14** — `Email` é gravado em minúsculas. Sem isso a mesma pessoa duplica.
- **RN-15** — `Pessoa` **nunca** é excluída fisicamente. O ADR-0015 abre mão de chave
  estrangeira entre módulos apoiado nisso; um `DELETE` de verdade vira corrupção silenciosa
  em `EscalaItem`.
- **RN-16** — `Anonimizar()` substitui os dados pessoais, preserva `Id` e histórico de
  vínculo, marca o registro e é **irreversível**. Atende ao Art. 18 da LGPD sem quebrar as
  escalas de anos anteriores. É operação de domínio, não script de banco.
- **RN-17** — `Fundir(outra)` unifica dois cadastros da mesma pessoa, preservando a união
  dos históricos. A ficha real já traz 8 linhas que são reenvio da mesma pessoa.
- **RN-18** — Só `Nome` é obrigatório. Os demais campos ficam nulos até serem coletados, e
  nulo nunca é substituído por valor de preenchimento. A apresentação **não** é recusada por
  ficha incompleta.
- **RN-19** — `Endereco`, se presente, tem `Bairro`. `Bairro` é gravado normalizado.
- **RN-20** — `ConvidadoPorId` referencia uma `Pessoa` cadastrada, ou é nulo. Nunca o nome
  de quem não está no sistema. Uma pessoa não pode convidar a si mesma.
- **RN-21** — `Nome` pode ser parcial: no cadastro de visitante a recepção pega o primeiro
  nome e às vezes o sobrenome. **Nenhuma validação exige sobrenome, número mínimo de
  palavras ou "nome completo"** — ela quebraria a tela mais crítica do sistema, num caso que
  ninguém testa. O nome ganha precisão depois, na apresentação, e isso é funcionamento
  normal, não correção de erro.
- **RN-22** — `Nome` não é identificador e não é estável. Nada deriva dele chave, slug,
  referência ou agrupamento.
- **RN-23** — Visitante que retorna **não** gera vínculo novo. A situação dele não mudou —
  ele já era visitante. Vínculo registra mudança de relação, não ocorrência; abrir um
  segundo `Visitante` quebraria a RN-1 e a linha do tempo da RN-2.
- **RN-24** — `Fundir` não apaga o registro absorvido: preenche `FundidaEmId` e toda busca
  por ele resolve para o sobrevivente. Outros módulos guardam `PessoaId` sem FK
  ([ADR-0015](../adr/0015-um-dbcontext-e-migrations-por-modulo.md)) e não têm como ser
  avisados. Escrita sobre registro absorvido é recusada com `Conflict`.
- **RN-25** — A lista de aniversariantes compara dia e mês, ignorando o ano, e exclui
  `Falecido` e `Transferido`. Ninguém ora pelo aniversário de quem faleceu.

## 6. API

Todas as rotas sob `/api`, autenticadas. Nada de `Pessoa` aparece em `/public/*` — invariante
2 do `CLAUDE.md`.

| Método | Rota | Faz |
|---|---|---|
| `GET` | `/api/pessoas?busca=` | Busca por nome. A tela da recepção |
| `GET` | `/api/pessoas?situacao=&bairro=&pagina=&tamanho=` | Lista filtrada e paginada |
| `GET` | `/api/pessoas/aniversariantes?de=&ate=` | A lista do domingo |
| `GET` | `/api/pessoas/pauta?data=` | Visitantes do dia e aniversariantes da semana, juntos |
| `GET` | `/api/pessoas/{id}` | Ficha completa com histórico |
| `POST` | `/api/pessoas` | Cadastra |
| `PATCH` | `/api/pessoas/{id}` | Atualiza dados. **Nunca a situação** |
| `POST` | `/api/pessoas/{id}/apresentacao` | RN-6 |
| `POST` | `/api/pessoas/{id}/afastamento` | RN-7 |
| `POST` | `/api/pessoas/{id}/transferencia` | RN-8 |
| `POST` | `/api/pessoas/{id}/falecimento` | RN-9 |
| `POST` | `/api/pessoas/{id}/fusao` | RN-17 |
| `POST` | `/api/pessoas/{id}/anonimizacao` | RN-16 |

**Situação não é campo.** Cada transição é um recurso próprio, com o nome do ato — o mesmo
nome do método do agregado. Se `situacao` fosse editável por `PATCH`, a matriz da seção 5
deixaria de existir: vinte e três regras contornáveis por um campo a mais no corpo. Do jeito
que está, inventar uma transição exige criar uma rota, e rota aparece no code review.

**`PATCH`, não `PUT`.** A ficha se preenche em duas etapas: um `PUT` com o objeto inteiro
faria a tela da recepção, que conhece dois campos, apagar tudo o que a apresentação
preencheu. Campo ausente = não mexe; `null` explícito = limpa. `Endereco` é exceção: sendo
objeto de valor substituído em bloco, se vier, vem inteiro.

**`{id:guid}` como restrição de rota**, ou `/pessoas/aniversariantes` colide com
`/pessoas/{id}`.

### `GET /api/pessoas?busca=joão gue`

A rota mais crítica do sistema: roda enquanto a recepcionista digita, no celular, com a
pessoa esperando. Casamento por token, sem acento, nos dois sentidos (RN-21). Máximo de 10
resultados.

```jsonc
// 200
{ "resultados": [
  { "id": "…", "nome": "João Guedes",
    "situacao": "Visitante", "desde": "2024-03-12",
    "convidadoPor": { "id": "…", "nome": "Maria Souza" } }
] }
```

**A projeção é deliberadamente pobre.** Sem endereço, sem telefone, sem data de nascimento —
a recepção não precisa e endereço é o campo que mais eleva o custo de um vazamento. Busca
que devolve ficha completa espalha dado pessoal por toda tela que faz autocomplete.
`desde` e `convidadoPor` estão aí porque são o único desempate de homônimo que o visitante
tem.

### `POST /api/pessoas`

```jsonc
// requisição mínima — o cadastro de visitante
{ "nome": "João Guedes", "convidadoPorId": "…" }

// 201
{ "id": "…", "nome": "João Guedes", "situacao": "Visitante" }
```

Abre vínculo `Visitante` com `DataInicio` de hoje. Aceita `situacao: "Membro"` e os demais
campos da seção 4 para a importação e para quem chega já transferido (RN-5).

**Sem chave de idempotência.** A recepcionista vai tocar duas vezes na rede ruim do salão, e
sem CPF o servidor não tem como saber que é a mesma pessoa. O botão desabilita no cliente e o
resto cai no `Fundir`, que existe de qualquer forma — duplicata é caso esperado neste
domínio, não anomalia.

### `POST /api/pessoas/{id}/apresentacao`

```jsonc
{ "data": "2026-08-23" }
```

**Não existe endpoint de lote**, embora a cerimônia seja coletiva: são N chamadas, uma por
agregado, e a falha de uma não derruba as outras. O lote é da tela (seção 8).

Não é recusada por ficha incompleta (RN-18). Membro sem `DataNascimento` some da lista de
domingo, alguém nota, e o dado é preenchido — a prática corrige, a API não trava um fato que
já aconteceu.

### `POST /api/pessoas/{id}/fusao`

```jsonc
{ "absorvidaId": "…" }
```

`{id}` sobrevive; `absorvidaId` é absorvida. Irreversível. O registro absorvido **não some**:
ganha `FundidaEmId` e continua resolvendo para o sobrevivente (RN-24). `ConvidadoPorId` que
apontava para ele é repontado; `EscalaItem.PessoaId` não pode ser, porque o ADR-0015 abriu
mão de FK entre módulos e `Pessoas` não conhece `Escalas`. É por isso que a fusão redireciona
em vez de apagar.

### `GET /api/pessoas/aniversariantes?de=2026-08-23&ate=2026-08-29`

```jsonc
// 200
{ "aniversariantes": [
  { "id": "…", "nome": "…", "tipo": "Nascimento", "data": "2026-08-25" },
  { "id": "…", "nome": "…", "tipo": "Casamento",  "data": "2026-08-27" }
] }
```

Compara dia e mês, ignorando o ano. Exclui `Falecido` e `Transferido` (RN-25). Quando os dois
cônjuges são cadastrados, a mesma data de casamento aparece duas vezes — são 8 casais assim
hoje, e resolver isso exigiria `Familia`, que está fora do MVP.

### `GET /api/pessoas/pauta?data=2026-08-23`

O que o dirigente do culto lê em voz alta, vindo do cadastro.

```jsonc
// 200
{ "visitantes": [
    { "id": "…", "nome": "João Guedes",
      "convidadoPor": { "id": "…", "nome": "Maria Souza" } } ],
  "aniversariantes": [
    { "id": "…", "nome": "…", "tipo": "Nascimento", "data": "2026-08-25" } ] }
```

**Duas listas numa chamada só**, o que normalmente eu evitaria. Justifica-se porque é uma
tela, uma permissão e um momento em que a rede é inimiga: duas requisições no wi-fi do salão
são duas chances de a tela ficar pela metade com a igreja olhando.

`visitantes` são os cadastrados **naquele dia** — `DataInicio` do vínculo `Visitante` igual a
`data`. `convidadoPor` vai junto porque é assim que se apresenta: *"temos hoje o João,
convidado pela Maria."*

**A rota mora sob `/api/pessoas` de propósito.** "Pauta do culto" é vocabulário de `Eventos`,
que um dia terá hino, aviso e oferta. Este módulo entrega só a parte que vem do cadastro;
reservar `/api/pauta` seria avançar sobre nome que não é dele.

### Erros

Formato único em `application/problem+json` ([ADR-0014](../adr/0014-problem-details-como-formato-unico-de-erro.md)).
**Ramifique em `code`, nunca em `detail`** — `detail` é texto que a secretaria pode pedir para
reescrever a qualquer momento.

| `code` | Status | Quando |
|---|---|---|
| `Pessoa.NotFound` | 404 | `{id}` não existe |
| `Pessoa.TransicaoInvalida` | 409 | Fora da matriz da seção 5 (RN-12) |
| `Pessoa.Fundida` | 409 | Operação de escrita sobre registro absorvido |
| `Pessoa.Anonimizada` | 409 | Operação de escrita sobre registro anonimizado |
| `Pessoa.NomeObrigatorio` | 400 | Vazio depois de `Trim` (RN-13) |
| `Pessoa.MotivoObrigatorio` | 400 | Afastamento ou transferência sem motivo |
| `Pessoa.DataFutura` | 400 | Qualquer data no futuro (RN-3) |
| `Pessoa.DataRetroativa` | 400 | Afastamento com data no passado (RN-7) |
| `Pessoa.AutoConvite` | 400 | `ConvidadoPorId` igual ao próprio `Id` (RN-20) |
| `Pessoa.FusaoConsigoMesma` | 400 | `absorvidaId` igual a `{id}` |

## 7. Permissões

**Escrita agora como requisito, implementada no fim do MVP.** Autenticação e audit log são as
últimas peças antes do lançamento, e o portão é que **nenhum dado de pessoa real entra em
banco nenhum — inclusive o de desenvolvimento — antes de os dois existirem.** A importação
das 90 linhas roda uma vez, no ambiente de verdade, depois disso; até lá o desenvolvimento é
contra dado gerado. A invariante 6 do `CLAUDE.md` diz "antes do primeiro CRUD"; a régua que
vale aqui é o primeiro dado real, e o resultado que ela protege é o mesmo — nada de pessoa
sem auditoria.

Ficam desde já, porque são baratos agora e caros depois: `ICurrentUser` com implementação de
desenvolvimento, para que todo handler seja escrito como se houvesse usuário logado e o
interceptor de auditoria tenha o que gravar; e **filtro sempre no servidor**, mesmo quando o
filtro de hoje é "tudo" — lista que sai inteira para a SPA filtrar é impossível de restringir
depois, porque o dado já saiu.

| Operação | Recepção | Dirigente | Secretaria | Pastor |
|---|:---:|:---:|:---:|:---:|
| Buscar por nome | ✓ | | ✓ | ✓ |
| Cadastrar **visitante** | ✓ | | ✓ | ✓ |
| Cadastrar já como membro (RN-5) | | | ✓ | ✓ |
| Pauta do culto | ✓ | ✓ | ✓ | ✓ |
| Lista de aniversariantes | | ✓ | ✓ | ✓ |
| Ver ficha completa | | | ✓ | ✓ |
| Atualizar dados | | | ✓ | ✓ |
| Registrar apresentação | | | ✓ | ✓ |
| Afastamento, transferência, falecimento | | | ✓ | ✓ |
| Ler `Motivo` de afastamento | | | ✓ | ✓ |
| `Fundir` | | | ✓ | ✓ |
| `Anonimizar` | | | | ✓ |

**A recepção é um papel que não estava em lugar nenhum**, e usa a tela mais crítica do
sistema. Ela busca e cadastra visitante — e só. Não abre ficha, o que não é mesquinhez: a
ficha tem endereço, e a recepção está com o celular na mão no meio do salão. A projeção pobre
da busca (seção 6) é a expressão disso no formato da resposta, e é por isso que ela é
projeção separada e não a ficha com menos campos.

**O dirigente do culto é papel estático, embora a função rode.** Quem apresenta visitantes e
ora pelos aniversariantes às vezes é o pastor e às vezes não. A rigor isso é alocação a um
evento — "dirigente *do culto de domingo*" pede complemento, logo é relação, não coisa —, e
caberia em `Escalas`. Mas **permissão derivada de alocação temporal é armadilha**: "ontem eu
via e hoje não" gera chamado no domingo de manhã, é difícil de depurar, e amarraria este
módulo a dois que ainda não existem. E é desnecessário, porque **o dado já é limitado pelo
tempo**: a pauta mostra os visitantes de hoje e os aniversariantes da semana. Acesso
permanente a uma lista efêmera não acumula nada. Os poucos que sobem ao púlpito têm o papel o
ano inteiro.

**`Motivo` é restrição de campo, não de rota** — o único caso do módulo. A ficha responde sem
ele para quem não pode lê-lo; um `if` na projeção, não um endpoint separado.

**Líder de departamento não aparece nesta matriz, de propósito.** O escopo dele é o próprio
departamento, e `Pessoas` não sabe o que é um departamento — a fronteira do ADR-0012 impede.
O líder alcança quem precisa através do módulo `Departamentos`, que compõe o que quiser
mostrar. Resolver isso aqui exigiria `Pessoas` conhecer `Departamentos`, que é exatamente o
que a fronteira existe para impedir.

**Contas individuais para a recepção, mesmo sendo voluntário rotativo.** Conta compartilhada
faz o audit log dizer "a recepção fez isso", que é o mesmo que não ter log — e o log é o
motivo de tudo isto existir. Se o atrito se provar inviável no domingo de manhã, isso vira
decisão consciente e registrada, não um jeitinho.

**Granularidade do audit de leitura.** Abrir a ficha de alguém é um evento e vai para o log.
Digitar na busca **não** gera um evento por tecla — isso afogaria o log em ruído, e log
ruidoso é indistinguível de log ausente. Registra-se a consulta, não o autocomplete.

## 8. Telas do admin

Espelho do módulo da API em `apps/admin/src/modules/pessoas/`. **Nenhum `fetch` escrito à
mão** — só o cliente gerado em `packages/api-client`, invariante 5 do `CLAUDE.md`.

**A recepção e a secretaria são produtos diferentes, não a mesma tela com botões escondidos.**
Uma é celular em pé, no meio do salão, quinze segundos, duas mãos ocupadas. A outra é
desktop, sentada, com tempo. Compartilham o backend e quase nada mais — inclusive a rota, que
é `/recepcao` e não `/pessoas` com um modo.

| Rota | Tela | Resolve | Quem |
|---|---|---|---|
| `/recepcao` | Busca, cadastro e pauta do culto | Cadastrar visitante antes de apresentá-lo | Recepção |
| `/pessoas` | Lista com busca e filtros | Achar alguém no atendimento | Secretaria |
| `/pessoas/{id}` | Ficha com histórico e ações | Tudo o que se faz com uma pessoa | Secretaria |
| `/pauta` | Visitantes de hoje e aniversariantes | O que se lê no culto | Dirigente |
| `/aniversariantes` | Lista da semana | A oração de domingo | Secretaria, Pastor |

### `/recepcao` — a tela mais hostil, e a primeira que alguém usa

Um campo de busca. A pessoa digita o nome e o resultado aparece enquanto digita:

- **Achou** → toca no nome. *"Que bom te ver de novo, João!"* A pergunta "é a primeira vez?"
  sai do roteiro: o sistema já sabe, e o humano deixa de ser o índice. Resolve o recadastro
  de quem volta depois de muito tempo, sem custar campo nenhum — o `DataInicio` do vínculo
  `Visitante` já é a data da primeira visita.
- **Não achou** → o botão de cadastrar já vem com o nome digitado preenchido. Falta um campo,
  quem convidou, e acabou.

Cada resultado mostra **o que desempata homônimo**: a data da primeira visita e quem
convidou. É o único desempate que o visitante tem — sem data de nascimento e sem telefone —,
e é bom, porque a recepcionista costuma conhecer quem convidou.

**A pauta é metade da tela, e é o que mata o papel.** Aquele papel não é só um formulário: é
a lista de quem vai ser apresentado hoje, entregue a quem apresenta, depois do louvor. Se a
tela não devolver essa lista, a recepção vai usar o sistema **e** o papel — e aí só o papel
sobrevive. Então `/recepcao` mostra, embaixo da busca, **os cadastrados de hoje**, em ordem,
prontos para serem lidos em voz alta.

Nenhum campo obrigatório escondido, nenhum passo a mais, nenhuma confirmação. Dois campos.

### `/pessoas/{id}` — a ficha

Dados, histórico de vínculos em ordem, e as ações. As cinco operações raras não têm tela
própria: são diálogos abertos a partir daqui.

- **`Fundir` mora aqui**, e não numa tela de administração. A correção quase sempre chega
  tarde e por memória humana — o pastor lembrando no microfone, na hora da apresentação, que
  aquela pessoa já visitou. Nenhuma validação pega isso; a operação precisa estar à mão
  depois, na ficha que a secretaria vai abrir na segunda.
- **O rótulo é "registrar apresentação de membro".** A igreja chama de apresentação duas
  coisas: a dos visitantes, todo culto após o louvor, que não altera registro nenhum; e a de
  membro, que abre o vínculo. Com o ministério infantil serão três.
- **`Anonimizar` é irreversível** e só o pastor vê. Confirmação que exige digitar o nome,
  não um "tem certeza?".
- **Busca de CEP preenche o endereço.** Menos digitação é adoção; a chamada sai da SPA.

`AtualizarDados` não é transição de vínculo — é edição da pessoa — e some das listas de
modelagem justamente por isso, para depois virar a tela mais usada do sistema.

### `/pauta` — o segundo ambiente hostil

Quem apresenta os visitantes e ora pelos aniversariantes está **de pé na frente, microfone na
mão, luz em cima, lendo em voz alta**. É diferente da recepção: a recepcionista tem as duas
mãos e erra em silêncio; o dirigente erra na frente de todo mundo.

Daí três coisas. **Uma tela, não duas** — ninguém navega entre páginas no meio do culto: as
duas listas ficam juntas, na ordem de leitura, com tipo grande. **Uma chamada só** (seção 6).
E **atualização por toque**, porque a recepção pode ter cadastrado alguém dois minutos atrás,
durante o louvor: refaz a busca ao abrir e um botão de atualizar. Nada de tempo real.

Só leitura. Sem busca, sem ficha, sem cadastro. Cada visitante aparece com quem o convidou,
porque é assim que se apresenta.

### `/aniversariantes`

Padrão: a semana corrente, nascimento e casamento juntos, em ordem de data. É lida em voz
alta num domingo de manhã, então precisa caber na tela de um celular e ser legível de longe.
É também a tela que dá a alguém razão para abrir o sistema toda semana — e, por isso, o que
mantém o cadastro vivo.

### Estados vazios e de erro

| Situação | O que a tela faz |
|---|---|
| Busca sem resultado | Não é erro. É o caminho normal do visitante novo: o botão de cadastrar vem com o nome preenchido |
| Rede caiu no meio do cadastro | Mensagem clara e **o que foi digitado permanece**. Botão de tentar de novo. Sem fila offline |
| Ficha de pessoa fundida | Avisa que o cadastro foi fundido e leva ao sobrevivente (RN-24). Nunca 404 |
| Ficha de pessoa anonimizada | Mostra como anonimizada, sem ações de edição |
| Lote de apresentação com falha parcial | Diz **quais** falharam. As outras foram gravadas — são N transações (seção 6) |

A linha da rede é a que mais me preocupa e a que menos dá para resolver por código: se o
salão não tiver sinal num domingo cheio, a recepção volta ao papel na primeira falha. Vale
medir antes de descartar — a resposta pode ser um repetidor de wi-fi, não uma fila offline.

### O que não construir

- **Tela de lote para a apresentação.** A cerimônia é coletiva, mas acontece com duas a cinco
  pessoas, poucas vezes por ano. Pela ficha já dá. Se o volume justificar, aí sim.
- **Fila offline na recepção.** É um projeto próprio. Antes dele, medir o sinal.
- **Tela de administração de duplicatas.** `Fundir` se chega pela ficha, que é de onde a
  suspeita vem.

### Contas na recepção

Contas individuais (seção 7) só funcionam se o voluntário usar **o próprio celular** e ficar
logado. Aparelho compartilhado com login a cada domingo é atrito que ninguém aceita às nove
da manhã — e a saída fácil, a conta compartilhada, faz o audit log dizer "a recepção fez
isso", que é o mesmo que não ter log.

## 9. Dados pessoais e LGPD

- **Dados tratados:** nome, data de nascimento, estado civil, e-mail, celular, endereço,
  profissão, data de casamento, data de batismo, e o histórico de vínculo com a igreja.
  Base legal: Art. 11, II, "a" — entidade religiosa, dados dos seus fiéis. Todos são dados
  **comuns** (Art. 5º, I) e da própria pessoa.
- **`ConvidadoPorId` liga duas pessoas cadastradas** e por isso não é dado de terceiro. O
  nome de quem convidou e não é da igreja não é guardado.
- **Coleta em duas etapas.** Do visitante coleta-se só nome e contato; o resto vem na
  apresentação. Isso não é ergonomia de formulário — é minimização: o dado se coleta quando
  passa a existir uso para ele.
- **Endereço** é o campo que mais eleva o custo de um vazamento, não por classificação legal
  mas porque permite encontrar alguém fisicamente, e o cadastro inclui crianças. Não muda a
  decisão de coletar; reforça o RBAC com escopo e o audit log de leitura.
- **Nenhum dado de terceiro.** Nome de cônjuge, nome dos pais e religião do cônjuge estão na
  ficha de papel e **não** entram. A base do Art. 11, II, "a" não alcança quem não é fiel
  desta igreja, e quem nunca preencheu o formulário não tem como exercer o Art. 18 sobre um
  registro que desconhece.
- **Quem enxerga:** a definir na seção 7. `Motivo` de afastamento é mais restrito que o
  resto da ficha.
- **Vai para o audit log:** leitura e escrita. Ler a ficha de uma pessoa é evento auditável,
  não só alterá-la.
- **Soft delete e retenção:** `Pessoa` nunca é removida fisicamente (RN-15). O direito à
  eliminação se atende por `Anonimizar()` (RN-16), que é coisa diferente de excluir e
  precisa continuar sendo.
- **Menores de idade:** `DataNascimento` é obrigatória em parte por isso. O tratamento
  específico de menores — contato do responsável, consentimento — entra com o ministério
  infantil e está fora desta spec.

## 10. Perguntas em aberto

Nenhuma sobre o domínio. As quatro que existiam foram respondidas e estão registradas em
[`docs/dominio.md`](../dominio.md#perguntas-em-aberto).

As onze seções estão escritas. Pelo critério do [`README`](README.md) — seção 10 sem
pergunta em aberto — a spec pode passar a **Aprovada** e gerar issues.

**Registrar a visita de retorno** é a extensão óbvia, e está deliberadamente fora. O que a
recepção hoje chama de duplicata é, no mundo real, o dado mais valioso do funil: quem voltou.
Visita única é ruído; retorno é sinal, e é o melhor indicador antecedente de quem vira
membro. A igreja o perde duas vezes — pela regra "se já visitou, não anota", e pela limpeza
da duplicata. Se um dia entrar, entra como entidade própria e pequena (`Visita`: pessoa,
data), **nunca** como vínculo novo (RN-23), e fora do agregado. Não entra agora porque é a
semente do controle de frequência, que está fora do MVP, e porque exige da recepção um
registro que ela não faz hoje. Ficaria quase de graça, isso sim: a recepção já vai estar
digitando o nome para desambiguar, e marcar "veio de novo" é um toque a mais num fluxo que
já existe.

Uma coisa para **observar em uso**, que não bloqueia: quem se afastou e volta passa por
apresentação nova ou por um retorno de outra natureza? Nas duas leituras o modelo é o mesmo
— um vínculo novo de situação `Membro` —, então a diferença existe só no rótulo do botão.
Só é bloqueante a pergunta que muda o modelo.

## 11. Fatias

- [ ] Migration inicial, `Pessoa` e `VinculoIgreja`, schema `pessoas` (seção 4)
- [ ] Invariantes do histórico na entidade: RN-1 a RN-4
- [ ] Os quatro métodos de transição: RN-5 a RN-12
- [ ] [P] Teste parametrizado da matriz inteira — 25 pares mais as 5 entradas de cadastro,
      exaustivo. É ele que garante que uma situação nova no enum não passe sem decisão
- [ ] [P] `GET /api/pessoas?busca=` — a rota mais crítica (seção 6)
- [ ] `POST /api/pessoas` e `PATCH /api/pessoas/{id}` (seção 6)
- [ ] As quatro rotas de transição (seção 6)
- [ ] `GET /api/pessoas/aniversariantes`: RN-25
- [ ] `Anonimizar` e `Fundir`: RN-16, RN-17, RN-24
- [ ] Conciliação da planilha: 90 linhas, 8 reenvios prováveis e 13 datas em texto livre
      marcados para decisão humana. Não é script — o dado desconhecido entra como nulo,
      nunca como valor inventado para o import passar
- [ ] [P] `/recepcao` contra dados falsos: busca, cadastro de dois campos e a pauta do dia
- [ ] [P] `/pessoas` e `/pessoas/{id}` contra dados falsos: lista, ficha e histórico
- [ ] [P] `/pauta` contra dados falsos: as duas listas, tipo grande, botão de atualizar
- [ ] [P] `/aniversariantes` contra dados falsos
- [ ] Diálogos das cinco operações raras, a partir da ficha
- [ ] Estados vazios e de erro da seção 8 — em especial a queda de rede na recepção
