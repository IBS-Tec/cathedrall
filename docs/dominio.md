# Modelo de domínio — MVP

> **Status:** rascunho. Precisa ser validado com a secretaria e com os líderes de
> departamento antes de virar migration. Se a igreja já usa planilha ou ficha de papel,
> os campos reais dela têm precedência sobre o que está aqui.

## Princípio central

**Membro, visitante e trabalhador não são entidades distintas.** São a mesma `Pessoa`
em estados e papéis diferentes.

Criar tabelas separadas para cada um é o erro clássico de sistema de igreja: no dia em
que o visitante vira membro, o cadastro é duplicado, o histórico se perde, e a mesma
pessoa passa a existir em três lugares. A essa altura já há dado real dentro e a
correção é dolorosa.

## Cadastro

```
Pessoa                      raiz única — todo ser humano do sistema
  ├─ Nome                   o único obrigatório
  ├─ ConvidadoPorId?        quem trouxe. É o canal de volta: sem contato do
  │                         visitante, fala-se com o membro que o convidou
  ├─ Celular?, Email?       coletados na apresentação, não no primeiro contato
  ├─ DataNascimento?        coletados na apresentação: antes disso a igreja não
  ├─ EstadoCivil?           tem uso para eles, e não os pede de quem acabou
  ├─ DataCasamento?         de chegar. Nascimento e casamento alimentam a
  ├─ Profissao?             oração de domingo; batismo é fato biográfico, pode
  ├─ DataBatismo?           ser de outra igreja e não constitui a membresia
  ├─ Endereco?              objeto de valor; se existe, tem ao menos Bairro
  └─ Vinculos[]             histórico, dentro do agregado

VinculoIgreja               histórico, não campo — não é agregado próprio
  └─ Situacao, DataInicio, DataFim?, Motivo
     Situacao: Visitante → Membro → Afastado | Transferido | Falecido
```

`VinculoIgreja` fica **dentro do agregado `Pessoa`** porque as duas compartilham uma
invariante: no máximo um vínculo vigente por pessoa. Verificar isso exige olhar todos os
vínculos da pessoa na mesma transação, e é essa fronteira de consistência que define um
agregado. Daí não existir `VinculoRepository` nem `POST /vinculos`: alcança-se o vínculo
pela pessoa, e o endpoint é `POST /pessoas/{id}/apresentacao` — a língua da igreja, como o
[ADR-0008](adr/0008-pessoa-como-raiz-unica.md) exige.

A situação da pessoa é o **vínculo vigente**. Mudança de situação cria um registro novo;
nunca um `UPDATE` que apaga o passado. Isso entrega de graça: "quantos visitantes
viraram membros em 2026?", "há quanto tempo fulano frequenta?", quem saiu e voltou mais
de uma vez, e o rastro de alteração exigido pela LGPD.

**A membresia começa na cerimônia de apresentação.** A pessoa sente o desejo de congregar e
assume isso publicamente; a igreja faz uma pequena cerimônia de apresentação e oração no
culto. É esse ato que abre o `VinculoIgreja` de situação `Membro`, e a `DataInicio` é a data
da cerimônia — conhecida, pública, nunca estimada. Batismo não entra nessa conta: há membro
não batizado, e quem chega já batizado de outra igreja é apresentado do mesmo jeito.

Três consequências. A cerimônia é **coletiva** — várias pessoas apresentadas no mesmo
domingo —, então a tela aceita várias pessoas numa data só; mas são N transações, uma por
agregado, e não uma transação que altera N pessoas: o lote é conveniência de UI, não
requisito de consistência. A iniciativa é **da pessoa**, não da secretaria, o que torna a
operação candidata natural ao autosserviço. E o vocabulário é **"apresentação"**: o
ADR-0008 acertou a regra — a tela fala a língua da igreja — mas inventou o exemplo, porque
ninguém aqui diz "efetivar membro". Cuidado com a ambiguidade quando o ministério infantil
chegar: apresentação de criança é outra cerimônia.

**O afastamento registra o reconhecimento, não o sumiço.** O caso mais comum na prática
é o membro que simplesmente para de vir — sem avisar, sem que se saiba para onde foi.
Não existe o dia do sumiço: toda outra situação corresponde a um ato com data, essa não.
O que se registra, então, é o ato da igreja — alguém, num dia, reconhece que a pessoa se
afastou. `DataInicio` é a data desse reconhecimento, **nunca uma estimativa** de quando a
pessoa parou de vir; data chutada vira relatório errado dois anos depois. O `CreatedAt` de
`IAuditable` guarda quando foi digitado, e a distância entre as duas mede quanto tempo a
igreja levou para perceber que perdeu alguém. O que separa "sumiu" de "avisou que ia se
afastar" é o `Motivo`, não a `Situacao`.

Vale dizer em voz alta o que o sistema **não** faz: ele registra o afastamento, não o
detecta. Sem frequência (fora do MVP), o `Afastado` só chega porque um ser humano percebeu
socialmente. E a assimetria é cruel — quem some em silêncio é justamente quem não tem
alocação nem escala, ou seja, aquele sobre quem o sistema tem menos sinal.

**`Motivo` fica como texto livre.** A igreja não tem lista de motivos definida, e enum
inventado por quem não vive a prática produz valor que ninguém preenche. Como reconhecer
afastamento é operação rara, ler cinco registros por ano responde "quem precisamos buscar?"
sem estrutura nenhuma. Depois de um ano de registros reais, a lista sai do que as pessoas
escreveram: **taxonomia derivada do texto livre, nunca o contrário.** O campo pode conter
juízo sobre a pessoa — disciplina, conflito —, então nasce com a mesma restrição de leitura
que a invariante 6 do `CLAUDE.md` exige.

**`DataNascimento` e `DataCasamento` servem à mesma prática semanal.** Todo domingo a igreja
ora pelos aniversariantes da semana — de nascimento e de casamento. É a prática mais
frequente e mais estabelecida que este levantamento encontrou: acontece toda semana, já
hoje, sem sistema nenhum. Era isso que as duas colunas calculadas da ficha (`MÊS DE
ANIVERSÁRIO`, `MÊS DE CASAMENTO`) existiam para servir — a fórmula some porque a consulta a
substitui, a necessidade fica, e ganha precisão de semana em vez de mês.

Essa lista **não** é comunicação em massa e não está fora do MVP: é uma consulta sobre dois
campos de data, uma tela, zero infraestrutura. O que está fora é o *envio*. E ela é
provavelmente a melhor relação valor/custo do sistema inteiro, por um motivo que não é o
valor em si — **é a única coisa que dá a alguém razão para abrir o sistema toda semana.**
Cadastro que ninguém abre apodrece; cadastro que produz a lista do domingo se corrige
sozinho, porque erro nele aparece no culto. Detalhe da consulta, não do modelo: quando os
dois cônjuges são cadastrados a mesma data aparece duas vezes — são 8 casais assim hoje.

**Dois campos da ficha de papel ficaram de fora.** Os
dois têm propósito pastoral declarado pelo pastor — a pergunta "para que serve" foi feita e
respondida. O que os derruba é outra coisa em cada caso:

- `O CÔNJUGE É EVANGÉLICO?` serviria para saber quem evangelizar e calibrar comentários e
  convites. Mas são **2 respostas "Não" em 86 pessoas**, que o pastor conhece pelo nome:
  campo é para o que não cabe na cabeça de ninguém. E envelheceria mal — alguém se
  converte e o registro segue mentindo. Some também o problema legal, que continua de pé:
  convicção religiosa (Art. 5º, II) de quem não é fiel desta igreja, logo fora do
  Art. 11, II, "a", e de quem nunca preencheu formulário nem pode exercer o Art. 18.
- `SE CASADO(A), ONDE FOI REALIZADO?` identifica **8 casais só no civil** que poderiam
  querer o religioso. É dado de **campanha**, não de cadastro: extrai-se a lista uma vez,
  age-se, acabou. Campo é para operação que se repete.
**Não há dado de terceiro.** Nome de cônjuge, de pais e a religião do cônjuge estão na
ficha de papel e não entram: quem não tem relação com a igreja não vira `Pessoa` nem fica
guardado como texto. A única exceção prevista — contato de emergência de menor — está em
Perguntas em aberto, travada até o ministério infantil entrar no escopo.

**`Usuario` aponta para `Pessoa`, e não o contrário.** A pessoa existe antes e depois do
login, a esmagadora maioria nunca terá usuário, e credencial é o que se quer poder
arrancar inteiro e trocar por um provedor externo um dia. Com `Pessoa.UsuarioId`, o
cadastro saberia que autenticação existe; com `Usuario.PessoaId`, não sabe de nada — e é
o cadastro que se quer proteger. `Usuario` mora no módulo de acesso, que ainda não existe.

### Operações

**A igreja ainda não tem secretaria estabelecida fazendo esses registros.** Isso inverte o
risco do módulo: ele não vai *registrar* um processo existente, vai **propor** um. Contra
descompasso com a realidade a defesa é perguntar; contra a não-adoção a defesa é encolher,
porque processo novo só é seguido se custar quase nada.

O fracasso a evitar tem forma conhecida: dois anos depois, todo mundo que saiu continua
`Membro` porque ninguém clicou em "reconhecer afastamento", e o relatório diz 340 onde há
180 — pior que a planilha, porque tem cara de autoridade. O teste para cada operação e cada
valor de enum é **quem registra isso, em que momento da semana, e por que se daria ao
trabalho.** Sem resposta para as três, o campo nasce vazio, e campo vazio não é neutro:
faz o relatório mentir. Foi esse teste que derrubou `Frequentador` — a transição não tem
autor sem controle de frequência, e a igreja nunca usou a palavra.

| Frequência | Operação | Quem, quando |
|---|---|---|
| Semanal | `Cadastrar` | quem atende o visitante, no domingo |
| Semanal | `AtualizarDados` | qualquer um, quando alguém troca de telefone |
| Semanal | `RegistrarApresentacao` | no domingo da cerimônia, em lote |
| Rara | `ReconhecerAfastamento`, `RegistrarTransferencia`, `RegistrarFalecimento` | quando acontece; sem tela dedicada |
| Recorrente | `Fundir` | quando alguém lembra que a pessoa já era cadastrada — em geral dias depois |
| Operacional | `Anonimizar` | pedido de eliminação, Art. 18 |

`AtualizarDados` não é transição de vínculo — é edição da `Pessoa`. Some das listas de
modelagem justamente por isso, e depois vira a tela mais usada do sistema. **Se o MVP fizer
bem só cadastrar e editar, já ganhou;** as outras cinco podem ser um formulário feio cada.

Duas alavancas de adoção, as duas já validadas neste projeto. **Pendurar o registro no ato
que a pessoa já tem motivo para fazer:** ninguém entra no sistema para reconhecer
afastamento, mas o líder entra toda semana para montar escala, e um aviso de "fulano não
confirma há 4 meses" ali faz o reconhecimento acontecer onde a pessoa já está — o módulo de
escalas pode acabar sendo o que mantém o cadastro vivo. **Preferir autosserviço:** 90
pessoas preencheram um formulário sobre si mesmas e ninguém mantém registro sobre
terceiros, então projete para a capacidade demonstrada. É a mesma razão pela qual a
confirmação de escala é por link sem login.

**Duplicata é inevitável e precisa de operação própria.** A ficha real tem 90 linhas para
~86 pessoas, e nenhum campo que identifique alguém — sem CPF, sem documento. `Fundir` é
operação de domínio de primeira classe, irmã de `Anonimizar`. Se não nascer com o módulo,
nasce como `UPDATE` manual no psql.

## Departamentos e trabalhadores

```
Departamento                Louvor, Mídia, Som, Diaconato, Infantil, Recepção, Intercessão…
Funcao                      Vocal, Tecladista, Operador de som, Projeção, Berçário…
  └─ pertence a um Departamento

Alocacao                    "fulano é trabalhador do Louvor como Tecladista"
  └─ PessoaId, DepartamentoId, FuncaoId, Papel (Membro | Lider), DataInicio, DataFim?
```

**Trabalhador não é uma tabela — é uma consulta:** pessoa com ao menos uma `Alocacao`
ativa. Isso representa naturalmente quem serve em mais de um departamento (mídia +
louvor é comum), caso que uma tabela `Trabalhador` só resolve com gambiarra.

**Experiência e interesse pertencem aqui, não a `Pessoa`.** Dois campos da ficha de papel
respondem à mesma pergunta — "onde essa pessoa poderia servir?" —, um por experiência e o
outro por interesse declarado: `CARGO QUE OCUPAVA` na igreja anterior (39/90) e `ÁREA QUE
GOSTARIA DE TRABALHAR` (63/90), juntos 68 das 90 linhas. O nome do primeiro sugere
histórico; o propósito que o pastor declarou é alocação, e **propósito decide onde o campo
mora, não só se ele entra.** Até este módulo existir os dois ficam na planilha — importar
para `Pessoa` "por enquanto" é onde eles morariam para sempre.

## Eventos

```
SerieEvento                 Titulo, Tipo (Culto | Ensaio | Reuniao | Especial),
                            RRule, HoraInicio, Duracao, Local, Publico

Evento                      SerieId?, DataHoraInicio, Status (Agendado | Cancelado | Realizado),
                            Local, Publico
                            └─ evento avulso = SerieId nulo. Mesma tabela, mesmo CRUD.
```

**Recorrência é materializada, não calculada.** Um job em background gera as ocorrências
concretas de cada `SerieEvento` num horizonte móvel (~6 meses). É menos elegante que
calcular sob demanda, mas cada ocorrência individual precisa carregar coisas próprias: a
escala daquele domingo, o cancelamento do ensaio na semana de feriado, o culto especial
que mudou de horário. A alternativa (regra + tabela de exceções) é mais bonita e
significativamente mais difícil de manter.

A regra é guardada como **RRULE** (padrão iCalendar). Formato consolidado, biblioteca
pronta, e habilita exportação `.ics` no futuro quase de graça.

`Publico = true` é o que alimenta `GET /public/eventos` → agenda no site institucional.

## Escalas

```
Escala                      EventoId, DepartamentoId
EscalaItem                  EscalaId, FuncaoId, PessoaId,
                            Status (Convocado | Confirmado | Recusado | Substituido)
Indisponibilidade           PessoaId, DataInicio, DataFim, Motivo
```

Dois detalhes que decidem se a escala será usada ou abandonada:

1. **`Indisponibilidade` é o que separa o sistema de uma planilha.** O líder de louvor
   sofre porque monta a escala e só depois descobre que o baterista viaja. Tabela
   pequena, valor alto. Não cortar do MVP.

2. **Confirmação por link com token, sem login.** O trabalhador recebe o link no
   WhatsApp, abre e responde "Confirmo" / "Não posso". Exigir login para confirmar
   escala mata a adoção. Login é para quem **administra**: secretaria, líderes, pastores.

## Ordem de construção

A ordem de *valor* declarada foi: cadastro → escalas → eventos. A ordem de
*implementação* precisa inverter parcialmente, porque escala é sempre escala **de um
evento**:

1. Pessoa, VinculoIgreja
2. Departamento, Funcao, Alocacao
3. Evento (avulso e recorrente)
4. Escala, EscalaItem, Indisponibilidade
5. `/public/eventos` → agenda no site

## Fora do MVP

- Financeiro (dízimos e ofertas) — maior risco do sistema: dado sensível + dinheiro +
  prestação de contas. Fica fora até o resto estar estável.
- `Familia`. Sem dado de terceiro ela só liga pessoas já cadastradas, e a ficha real
  mostra que isso é pouco: dos 43 cônjuges informados, 17 são cadastrados; dos 79 pais,
  9. Cobriria o núcleo de ~25 das 86 pessoas, quase só casais — informação que
  `EstadoCivil` já dá quase inteira. Volta quando houver regra que a exija, e como
  agregado próprio, referenciado por `FamiliaId`.
- Células / pequenos grupos e frequência.
- Check-in do ministério infantil.
- Comunicação em massa — envio automático de mensagem a aniversariante, avisos gerais. A
  **lista** dos aniversariantes da semana fica dentro do MVP: é consulta, não envio.
- Contato de emergência e responsável por menor. Sem dado de terceiro não há onde registrar
  quem responde por uma criança cuja mãe não é da igreja — e o dado não some, migra para o
  celular do líder, sem controle de acesso, sem audit log e sem retenção. Quando o check-in
  do infantil entrar, entra estreito: nome, telefone, parentesco, no registro da criança,
  com propósito declarado.

## Perguntas em aberto

Levantadas ao confrontar este modelo com a ficha de cadastro real — a planilha de
respostas do formulário, 90 linhas, ~86 pessoas. Cada pergunta tem dono e data; pergunta
sem dono não se responde sozinha. Enquanto houver item aqui, o módulo de Pessoa não vira
spec aprovada e nenhuma issue é aberta ([`specs/README.md`](specs/README.md)).

**Válvula de escape.** Aquela regra do `specs/README.md` — "pergunte à secretaria antes" —
pressupõe uma secretaria estabelecida, que não existe. Onde não há prática consolidada, o
pastor também pode não ter resposta, e spec travada esperando resposta que não vem nunca
sai do rascunho. Então: sem resposta em duas ou três semanas, o mantenedor decide o default
entediante, registra como **decisão** e não como descoberta, e marca para revisitar depois
de seis meses de uso real. Prática se forma em cima de um default; não se forma no vácuo.

**Nenhuma pergunta em aberto.** As quatro que existiam foram respondidas: três pelo pastor
— o propósito de cada campo, o que constitui a membresia, e que a triagem por profissão é
a pessoa como quem *presta* o serviço — e uma pela válvula acima, já que não há lista de
motivos de afastamento definida. A que restava, contato de emergência de menor, foi para
"Fora do MVP", que é onde ela pertence enquanto o ministério infantil não entrar no escopo.

O modelo está pronto para virar spec. **Isso não quer dizer que ele está certo** — quer
dizer que não tem mais buraco conhecido. O que ele tem é um punhado de suposições sobre uma
prática que ainda não existe, e essas só se verificam em uso.
