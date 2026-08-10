# ADR-0008 — `Pessoa` como raiz única de cadastro

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O escopo do MVP foi enunciado como "cadastro de membros, visitantes, departamentos,
trabalhadores", o que sugere entidades distintas para membro, visitante e trabalhador.

## Decisão

Uma única entidade **`Pessoa`**. Membro e visitante são a **situação vigente** de um
`VinculoIgreja` (histórico). Trabalhador é uma **consulta**: pessoa com `Alocacao` ativa
em algum departamento.

## Motivos

- Tabelas separadas quebram no momento mais comum e mais importante do ciclo de vida: o
  visitante que vira membro. O cadastro é copiado, o histórico se perde ("frequenta desde
  março de 2024") e a mesma pessoa passa a existir em três lugares. Quando isso aparece,
  já há dado real dentro e a correção é dolorosa.
- Vínculo como histórico entrega de graça as perguntas que a liderança faz: "quantos
  visitantes viraram membros neste ano?", "há quanto tempo fulano frequenta?".
- Trabalhador como consulta representa naturalmente quem serve em mais de um departamento
  (mídia + louvor é comum), caso que uma tabela `Trabalhador` só resolve com gambiarra.
- O rastro de mudança de situação atende à exigência de registro de operações da LGPD.

## Consequências

- Toda consulta de "membros" carrega um filtro por situação do vínculo. Precisa estar
  encapsulado em um único lugar, ou alguém vai esquecer e contar visitante como membro
  num relatório para o pastor.
- A UI **não** deve expor a modelagem crua. A secretaria pensa em "cadastrar visitante" e
  "efetivar membro" — as telas falam essa língua, mesmo que por baixo seja um registro de
  vínculo. Vocabulário de domínio é da igreja, não do banco.
- `Pessoa` tende a acumular campos. Resistir: o que for específico de um contexto
  pertence a esse contexto, não à raiz.
