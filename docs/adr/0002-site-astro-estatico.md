# ADR-0002 — Site institucional em Astro, gerado estaticamente

**Status:** Aceito · **Data:** 2026-08-10
**Revisto por:** [ADR-0009](0009-hospedagem-unificada-dokploy.md) — a decisão de gerar
estaticamente permanece; a de hospedar fora do servidor doméstico foi revertida.

## Contexto

O site institucional é majoritariamente conteúdo editorial, atualizado por uma pessoa não
técnica algumas vezes por mês. Precisa carregar rápido, ranquear bem e custar pouco.

## Decisão

Astro em modo **SSG puro**, com o conteúdo buscado do CMS em **tempo de build**.
Hospedagem estática, fora do servidor de casa. Sem SSR, sem adapter de servidor.

## Motivos

- Conteúdo editorial estático é exatamente o caso de uso do Astro.
- Zero JavaScript enviado ao navegador por padrão — importa num público que acessa
  majoritariamente por celular e rede móvel.
- **Desacopla o site do servidor doméstico.** Este é o motivo decisivo: se o servidor de
  casa cair (queda de energia, internet, disco), o site permanece no ar. Apenas deixa de
  receber atualizações de conteúdo até o servidor voltar.

## Consequências

- Publicar conteúdo exige um rebuild. Precisa de webhook do CMS disparando o build, ou o
  editor não entende por que a alteração "não apareceu". Isso é um requisito de UX, não
  um detalhe.
- Nada de personalização por usuário no site. Se um dia surgir "área do membro", ela vai
  para a SPA, não para o site.
- Publicação de conteúdo urgente tem latência de alguns minutos (tempo de build).
