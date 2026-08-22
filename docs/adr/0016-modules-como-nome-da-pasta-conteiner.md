# ADR-0016 — `Modules/` como nome da pasta contêiner dos módulos

**Status:** Aceito · **Data:** 2026-08-22
**Revisa:** [ADR-0013](0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md) — o item
que lista `Modulos/Pessoas/` como português.

## Contexto

Três documentos aceitos grafam de três maneiras o mesmo diretório:

| Onde | Grafia |
|---|---|
| [ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md), árvore de projetos | `src/Modulos/CathedrAll.Pessoas/` |
| [ADR-0013](0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md), lista do português | `Modulos/Pessoas/`, `modules/pessoas/` |
| [ADR-0015](0015-um-dbcontext-e-migrations-por-modulo.md), comando do `dotnet ef` | `src/Modules/CathedrAll.Pessoas` |

O `CLAUDE.md` acrescenta uma quarta variação — `apps/api/src/CathedrAll.Api/Modules/<Modulo>/`
—, que além da grafia carrega o caminho anterior ao ADR-0012, de quando os módulos eram
pastas dentro do projeto do host.

O ADR-0013 é internamente inconsistente na própria linha: põe `Modulos/` de um lado e
`modules/` do outro. E o `CLAUDE.md` afirma que os dois lados **se espelham**, o que hoje não
é verdade.

Isso precisa ser resolvido antes da primeira migration. O ADR-0015 põe as migrations dentro
do projeto do módulo, e o caminho fica gravado no `ModelSnapshot`: corrigir depois é mexer em
arquivo gerado, com histórico de schema dentro.

## Decisão

**A pasta contêiner é `Modules/`, em inglês, nos dois lados. A fatia dentro dela continua em
português.**

```
apps/api/src/Modules/CathedrAll.Pessoas/
apps/admin/src/modules/pessoas/
```

Este ADR substitui, no [ADR-0013](0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md),
apenas o item que classifica `Modulos/Pessoas/` como português. Todo o resto daquele ADR
permanece: `Pessoa`, `Departamento`, `Situacao`, `DataInicio` e a fatia `pessoas/` seguem em
português, e as duas fronteiras internas dele — código de erro e nome de teste — não mudam.

## Motivos

- **A fronteira do ADR-0013 é a natureza do nome, não a camada:** coisa da igreja em
  português, coisa de computador em inglês. "Módulo" é conceito de arquitetura de software,
  não da igreja — ninguém na secretaria fala em módulo. O item que a pasta contêiner ocupava
  na lista do português era a exceção, não a regra, e nada no ADR-0013 justifica a exceção.
- **Contradição entre ADRs aceitos é pior que qualquer uma das grafias.** Quem chega novo lê
  um dos três e acerta um terço das vezes. O custo não é estético: é o voluntário criando a
  pasta errada e o mantenedor corrigindo no review.
- **Os dois lados passam a espelhar de verdade**, como o `CLAUDE.md` já afirmava.
- **É o momento mais barato possível.** A pasta contém apenas artefato de build, nada
  versionado, nenhum `.csproj`, nada na `CathedrAll.slnx`. Depois da primeira migration, não é
  mais assim.

## Consequências

- O `CLAUDE.md` passa a grafar `apps/api/src/Modules/<Modulo>/`, sem o `CathedrAll.Api/` que
  sobrou da organização anterior ao ADR-0012.
- O ADR-0012 e o ADR-0013 continuam com a grafia antiga no texto, porque ADR não se edita.
  Quem os ler encontra aqui a correção, pelo cabeçalho **Revisa**.
- Uma decisão deste tamanho normalmente não mereceria ADR — o `proposta.yml` dispensa
  proposta para o que é barato de reverter. O que exige registro aqui não é o custo da
  mudança, é que ela torna **falsa** uma linha de um ADR aceito. Sem este documento, a
  correção seria invisível para quem lesse o ADR-0013 daqui a um ano.
