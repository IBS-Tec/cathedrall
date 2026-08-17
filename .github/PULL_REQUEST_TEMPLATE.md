<!--
Título do PR no mesmo formato do commit: tipo(escopo): descrição no imperativo.
Ex.: feat(api): cadastro de pessoa com vínculo inicial
-->

Resolve #

## O que muda

<!-- Duas ou três frases. O *porquê* — o *o quê* já está no diff. -->

## Critério de aceite

<!--
Copie os bullets da issue e marque só o que você conferiu na mão, rodando o sistema.
Não marque o que "deve funcionar".
-->

- [ ] …

## Onde eu não tive certeza

<!--
Aponte o trecho que você quer que seja olhado com atenção, e a alternativa que você
considerou. Isso direciona o review para onde ele rende — e é a parte em que você mais
aprende. Se não teve dúvida nenhuma, escreva "nenhuma".
-->

## Antes de marcar como pronto

- [ ] CI verde
- [ ] Um PR, uma issue — nada de carona
- [ ] Sem `fetch` à mão na SPA (só `packages/api-client`)
- [ ] Vertical slice por módulo, sem pasta genérica por tipo técnico
- [ ] Código em inglês; negócio, UI e nome de teste em português (ADR-0013)
- [ ] Nenhum segredo versionado
- [ ] Tocou em dado de pessoa? Audit log, escopo de permissão e soft delete contemplados
- [ ] Documentação e spec atualizadas neste mesmo PR, se o comportamento mudou

<!-- Detalhes em CONTRIBUTING.md. -->
