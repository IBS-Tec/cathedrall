# packages/brand

Tokens de marca compartilhados entre o site institucional e a SPA: cores, tipografia,
espaçamento, logotipos.

> **Status:** vazio. Aguarda a definição da identidade visual com a igreja.
>
> Hoje cada frontend tem seus próprios tokens provisórios — `@theme` no `global.css` do
> site, variáveis CSS do shadcn no admin. A unificação acontece quando houver identidade
> real para unificar; antes disso seria abstração prematura sobre valores de placeholder.

## Escopo

Deliberadamente pequeno. **Tokens, não componentes.**

O site é editorial e a SPA é ferramenta de trabalho — têm necessidades de UI muito
diferentes, e uma biblioteca de componentes compartilhada entre os dois acabaria
carregando as necessidades de ambos e servindo mal aos dois. O que precisa ser igual é a
identidade: as cores da igreja, a fonte, o logo.

## Por que isto é viável

Com Tailwind nos dois frontends ([ADR-0011](../../docs/adr/0011-shadcn-tailwind-nos-dois-frontends.md)),
os tokens podem ser **um único bloco `@theme`** importado pelos dois. Se as stacks de
estilo fossem diferentes, este pacote seria uma tabela de cores duplicada em dois formatos
incompatíveis — que é como pacotes de marca costumam morrer.
