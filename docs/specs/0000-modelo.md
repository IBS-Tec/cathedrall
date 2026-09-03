<!--
Modelo de spec. Copie este arquivo para `000X-<modulo>.md`, preencha e apague os
comentários como este.

Os exemplos citam o módulo de Pessoa só para mostrar o nível de detalhe esperado.
Substitua tudo.

Uma spec está pronta quando alguém que nunca viu o projeto consegue implementá-la sem
te perguntar nada. Se sobrou pergunta, ela vai para a seção 10 e a spec continua em
rascunho.
-->

# Spec-000X — \<Módulo\>

**Status:** Rascunho · **Data:** AAAA-MM-DD · **Responsável:** \<quem escreveu\>

<!--
Depois de aprovada, toda correção acrescenta `**Revista em:** AAAA-MM-DD` ao cabeçalho e
uma linha ao bloco abaixo. Enquanto a spec é rascunho, apague os dois — rascunho se
reescreve à vontade.

**Revisões**

- **AAAA-MM-DD** — o que mudou e por quê (#issue).
-->

Deriva de: [`docs/dominio.md#<seção>`](../dominio.md) · Decisões relacionadas:
[ADR-000X](../adr/000X-....md)

## 1. Objetivo

<!--
Duas ou três frases, na língua da igreja, não na do banco. Quem é atendido e que dor
some. Se você não consegue escrever isso sem falar de tabela, o módulo ainda não foi
entendido.
-->

## 2. Fora de escopo

<!--
Tão importante quanto o objetivo, e a única defesa contra o módulo crescer no meio da
implementação. Liste o que alguém razoavelmente esperaria encontrar aqui e não vai.
Diga para onde foi, quando for o caso.
-->

- …

## 3. Vocabulário

<!--
O termo que a igreja usa e o nome no código. Existe porque a UI fala a língua da
secretaria enquanto o código fala a do domínio, e essas duas nem sempre coincidem —
"efetivar membro" na tela é um registro novo de VinculoIgreja por baixo.
-->

| Termo na igreja | No código | Observação |
|---|---|---|
| Efetivar membro | `VinculoIgreja` com `Situacao = Membro` | Registro novo, nunca `UPDATE` |

## 4. Modelo de dados

<!--
Entidades do módulo, com campos. Marque o que é obrigatório e o que tem regra. Não
repita o que já está em docs/dominio.md — aponte para lá e detalhe só o que falta.
-->

### `<Entidade>`

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `Nome` | `string(120)` | sim | Não vazio depois de `Trim` |
| `DataNascimento` | `date?` | não | Não pode ser futura |

**Índices e unicidade:** …

**Migrations:** …

## 5. Regras de negócio

<!--
Numeradas, porque as issues e os testes vão citar o número. Uma regra por linha, no
indicativo, verificável. "O sistema valida os dados" não é regra; "CPF duplicado é
recusado com 409" é.
-->

- **RN-1** — …
- **RN-2** — …

## 6. API

<!--
O contrato. É o que permite que a tela e o endpoint sejam feitos em paralelo por duas
pessoas, então precisa estar completo antes das issues: campos, tipos, códigos de erro.
-->

| Método | Rota | Faz | Papéis |
|---|---|---|---|
| `GET` | `/api/pessoas` | Lista paginada | Secretaria, Pastor |
| `POST` | `/api/pessoas` | Cadastra | Secretaria |

### `POST /api/pessoas`

```jsonc
// requisição
{ "nome": "…", "dataNascimento": "1990-05-02" }

// 201
{ "id": "…", "nome": "…" }
```

| Erro | Quando |
|---|---|
| `400` | Corpo inválido — detalhe por campo |
| `409` | Violação da RN-2 |

## 7. Permissões

<!--
Matriz papel × operação, com o escopo. RBAC com escopo é invariante do projeto: líder
enxerga o próprio departamento e nada além. Célula vazia significa "não pode".
-->

| Operação | Secretaria | Líder de departamento | Pastor |
|---|---|---|---|
| Listar | tudo | só o próprio departamento | tudo |
| Criar | ✓ | | |

## 8. Telas do admin

<!--
Uma linha por tela, com a rota e o que ela resolve. Fluxo e estados de erro/vazio, não
layout. O espelho do módulo da API fica em apps/admin/src/modules/<modulo>/.
-->

| Rota | Tela | Resolve |
|---|---|---|
| `/pessoas` | Lista com busca | Achar alguém rápido no atendimento |

**Estado vazio:** … **Erro de carregamento:** …

## 9. Dados pessoais e LGPD

<!--
Obrigatório em todo módulo que toque em Pessoa. Se o módulo não toca, escreva
"Não trata dado pessoal" e siga. Ver docs/arquitetura.md, seção LGPD.
-->

- **Dados tratados:** …
- **Quem enxerga:** …
- **Vai para o audit log:** … <!-- leitura também, não só escrita -->
- **Soft delete e retenção:** …
- **Menores de idade:** …

## 10. Perguntas em aberto

<!--
Enquanto houver qualquer item aqui, a spec fica em Rascunho e nenhuma issue é aberta.
Toda pergunta tem um dono e uma data — pergunta sem dono não se responde sozinha.
-->

- [ ] … — *perguntar a \<quem\>*

## 11. Fatias

<!--
A spec quebrada em issues. Cada linha vira uma issue e deve caber em 2 a 4 horas.
Ordene por dependência: quem pegar a de cima não pode ficar esperando ninguém.
Marque com [P] o que dá para fazer em paralelo.
-->

- [ ] Migration e entidades de `<Entidade>` (RN-1, RN-2)
- [ ] `POST /api/pessoas` (seção 6)
- [ ] [P] Tela de lista contra dados falsos (seção 8)
