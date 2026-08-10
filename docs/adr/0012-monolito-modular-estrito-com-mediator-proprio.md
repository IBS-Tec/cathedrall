# ADR-0012 — Monólito modular estrito, DDD tático e mediator próprio

**Status:** Aceito · **Data:** 2026-08-10
**Revisa:** [ADR-0004](0004-backend-dotnet-monolito-modular.md) — a parte que proibia
MediatR e CQRS, e a organização em projetos por camada.

## Contexto

O [ADR-0004](0004-backend-dotnet-monolito-modular.md) escolheu vertical slices e proibiu
explicitamente MediatR, CQRS e camadas cerimoniosas. A justificativa era o time:
desenvolvedores iniciantes e voluntários com rotatividade alta.

**Fato novo:** a API será desenvolvida **apenas pelo mantenedor principal**, experiente em
.NET. Os voluntários iniciantes atuam no `apps/admin`, em React. A premissa que sustentava
a proibição não se aplica ao backend.

Havia também uma contradição não resolvida no scaffold: os projetos `Api`, `Domain` e
`Infrastructure` são organização **por camada**, o que espalha cada módulo por três
projetos — o oposto de vertical slice.

## Decisão

**Monólito modular estrito.** Um projeto por módulo. Módulos não se referenciam; conversam
por contratos e eventos publicados no kernel compartilhado.

```
src/
  CathedrAll.Api/                 host: Program.cs, composição, mapeamento de endpoints
  CathedrAll.Kernel/              mediator, behaviors, Result, erros, auditoria, primitivos
  Modulos/
    CathedrAll.Pessoas/           Domain/ Application/ Infrastructure/ Endpoints/
    CathedrAll.Departamentos/
    CathedrAll.Eventos/
    CathedrAll.Escalas/
tests/
  CathedrAll.Tests/
```

Dentro de cada módulo, DDD tático: agregados, objetos de valor, invariantes na entidade.
Minimal API para os endpoints. Mediator próprio, com pipeline behaviors.

## Motivos

- **Um projeto por módulo dá a trava de compilação** que a organização por camada dava,
  mas na fronteira que importa aqui: `Escalas` não consegue alcançar as entidades de
  `Pessoas` por acidente. Com pastas dentro de um projeto único, isso seria só convenção.
- **MediatR passou a ter licença comercial.** Escrever o próprio deixou de ser reinvenção
  gratuita e virou a alternativa razoável a uma dependência paga.
- **Behaviors são o lugar certo para o que a LGPD exige em toda requisição:** validação,
  transação, auditoria e autorização com escopo. Espalhar isso por endpoint garante que um
  dia alguém esqueça em um — e é justamente o endpoint esquecido que vaza.

## Consequências

### O custo, declarado

**O bus factor piora.** Este ADR troca conforto do mantenedor atual por degrau de entrada
de quem vier depois: além do domínio de igreja, a pessoa vai encontrar infraestrutura
caseira sem documentação na internet.

Mitigações, que são obrigações e não sugestões:

1. **O mediator fica minúsculo e sem mágica.** Uma interface, um dispatcher, uma cadeia de
   behaviors. Sem varredura de assembly obscura, sem geração de código, sem reflexão
   além do estritamente necessário para resolver o handler.
2. **`CathedrAll.Kernel` tem README próprio** explicando o mediator e cada behavior, com
   exemplo de requisição ponta a ponta.
3. **Se passar de ~200 linhas, é sinal de que virou framework** — e aí a decisão certa é
   voltar a chamar o handler direto do endpoint.

### Outras

- Mais projetos, mais arquivos de `.csproj`, build um pouco mais lento.
- Comunicação entre módulos exige contrato explícito. É o objetivo, mas custa cerimônia
  quando dois módulos precisam mesmo conversar (Escalas depende de Pessoas e de Eventos).
- A regra do ADR-0004 que **permanece intacta**: nada de pastas genéricas por tipo técnico
  (`Services/`, `Repositories/`, `DTOs/`) dentro dos módulos.

## Gatilho de reavaliação

Se um segundo desenvolvedor passar a trabalhar na API e levar mais de um dia para entregar
o primeiro endpoint, o problema é este ADR — não a pessoa.
