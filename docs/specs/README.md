# Specs de módulo

Uma spec descreve **o que** um módulo faz, com detalhe suficiente para alguém implementar
sem entrevistar o mantenedor. É o degrau que falta entre o [modelo de domínio](../dominio.md),
que é panorâmico, e a issue, que é uma fatia de poucas horas.

Formato: [`0000-modelo.md`](0000-modelo.md). Numeradas, um arquivo por módulo.

## Por que isso existe

Duas razões, as duas ligadas ao mesmo risco:

**Rotatividade.** Quando o voluntário some no meio da implementação — e alguém sempre some
— a spec é o que sobra. A próxima pessoa retoma de onde parou sem precisar reconstruir as
decisões pela conversa do WhatsApp.

**Paralelismo.** O contrato de API escrito antes é o que permite duas pessoas trabalharem
no mesmo módulo ao mesmo tempo: uma implementa os endpoints, a outra monta a tela contra
dados falsos, e elas só se encontram no fim. Sem contrato escrito, todo módulo passa pelo
mantenedor duas vezes e ele vira o gargalo.

## Spec ≠ ADR

O ADR registra **por que** uma decisão foi tomada, é caro de reverter e fica imutável.
A spec registra **o que** será construído e é viva: quando a realidade discorda dela, quem
descobriu corrige a spec no mesmo PR que muda o código. Spec desatualizada é pior do que
spec inexistente, porque alguém vai confiar nela.

Se ao escrever uma spec você se pegar justificando uma escolha cara de reverter — o banco,
a fronteira entre sistemas, a estratégia de autenticação — isso é um ADR. Escreva o ADR e
deixe a spec apenas apontar para ele.

## Ciclo de vida

| Status | Significa |
|---|---|
| **Rascunho** | Em escrita. Ainda tem pergunta em aberto. Ninguém implementa. |
| **Aprovada** | Seção "Perguntas em aberto" vazia. As issues podem ser abertas. |
| **Implementada** | O módulo está no ar. A spec vira documentação de referência. |

Uma spec com pergunta em aberto **não gera issue**. A pergunta seria respondida por
adivinhação de quem estiver codando às onze da noite, e domínio de igreja não se adivinha:
quando houver dúvida sobre o processo real, pergunte à secretaria ou ao líder do
departamento antes.

**O status volta.** Pergunta nova numa spec Aprovada faz ela voltar a Rascunho até ser
respondida. O efeito é o que interessa: para de gerar issue enquanto a dúvida existe.

**A numeração das regras congela na aprovação.** Antes disso, renumerar é livre. Depois, as
issues e os testes citam "RN-7", e o número virou contrato: **acrescente no fim, nunca
renumere.** Regra que sai vira uma linha de lápide — `RN-9 — removida em 2027-03-14, ver
RN-31` — em vez de deixar buraco na sequência. Inserir no meio empurra as seguintes e quebra
as referências cruzadas **em silêncio**, que é o pior tipo de erro: o texto continua parecendo
certo.

**A seção 11 não é o rastreador.** Quando as issues nascem, o número de cada uma volta para a
linha correspondente e as caixas não se marcam — o andamento é do GitHub. O fluxo completo,
da spec até o PR, está em [`CONTRIBUTING.md`](../../CONTRIBUTING.md#da-spec-às-issues).

## Índice

| # | Módulo | Status |
|---|---|---|
| [0001](0001-pessoas.md) | Pessoas | Aprovada |
