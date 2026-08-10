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
  ├─ dados pessoais, contato, endereço, foto, data de nascimento
  ├─ FamiliaId?             agrupa cônjuge e filhos
  └─ UsuarioId?             apenas quem acessa o sistema tem login

Familia                     unidade familiar (endereço comum, ministério infantil)

VinculoIgreja               histórico, não campo
  └─ PessoaId, Situacao, DataInicio, DataFim?, Motivo
     Situacao: Visitante → Frequentador → Membro → Afastado | Transferido | Falecido
```

A situação da pessoa é o **vínculo vigente**. Mudança de situação cria um registro novo;
nunca um `UPDATE` que apaga o passado. Isso entrega de graça: "quantos visitantes
viraram membros em 2026?", "há quanto tempo fulano frequenta?", e o rastro de alteração
exigido pela LGPD.

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

1. Pessoa, Familia, VinculoIgreja
2. Departamento, Funcao, Alocacao
3. Evento (avulso e recorrente)
4. Escala, EscalaItem, Indisponibilidade
5. `/public/eventos` → agenda no site

## Fora do MVP

- Financeiro (dízimos e ofertas) — maior risco do sistema: dado sensível + dinheiro +
  prestação de contas. Fica fora até o resto estar estável.
- Células / pequenos grupos e frequência.
- Check-in do ministério infantil.
- Comunicação em massa (aniversariantes, avisos).
