# ADR-0014 — ProblemDetails como formato único de erro da API

**Status:** Aceito · **Data:** 2026-08-17

## Contexto

O kernel já separa as duas metades de uma falha de negócio: `Error.Code` é contrato de API,
e `Error.Description` é texto para humano que pode ser reescrito a pedido da secretaria. O
que não estava decidido é **onde cada uma cai na rede**.

Isso precisa ser resolvido antes do primeiro endpoint, não depois. A SPA é o único consumidor
de `/api/*` e vai ramificar no código do erro; a partir do momento em que ela lê um campo,
mover esse campo é breaking change em duas aplicações que fazem deploy separado. Sem uma
forma decidida, o primeiro endpoint inventa a dele e o segundo inventa outra.

Há uma segunda fonte de erro que é fácil esquecer: **o próprio framework**. Rota inexistente,
método não permitido, corpo JSON malformado. Esses erros não passam por handler nenhum e não
conhecem o nosso `Result`. Se saírem em outro formato, a SPA precisa de dois caminhos de
tratamento — e descobre o segundo em produção.

## Decisão

**Todo erro da API sai em `application/problem+json`, no formato da RFC 9457.**

O status vem do `ErrorType`, conforme a tabela que já estava no README do kernel:

| `ErrorType` | HTTP |
| --- | --- |
| `Validation` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |

E os campos:

| Campo | Vem de | É |
| --- | --- | --- |
| `code` | `Error.Code` | **contrato.** A SPA ramifica nele |
| `detail` | `Error.Description` | texto para humano, em português |
| `traceId` | `Activity.Current?.Id` ou `TraceIdentifier` | correlação com o log |
| `title` | default do framework, por status | genérico. **A SPA nunca renderiza** |
| `status`, `type` | default do framework, por status | o padrão da RFC |

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Pessoa não encontrada.",
  "code": "Pessoa.NotFound",
  "traceId": "00-225d4de6e680a921a8b8bbefad510b66-d494e8ff06a97bca-00"
}
```

Três peças no host produzem isso, e as três são necessárias:

- **`ErrorResults.ToProblem()`** converte a falha de um `Result` nosso. Recebe o `Error`, não
  o `Result` — o lado do sucesso (200 com corpo, 201 com `Location`, 204) é conhecimento do
  endpoint, e passá-lo como lambda nos levaria à torre de `Bind`/`Map`/`Tap` que o
  [ADR-0012](0012-monolito-modular-estrito-com-mediator-proprio.md) se comprometeu a evitar.
- **`AddProblemDetails` com `CustomizeProblemDetails`** acrescenta o `traceId` a *todo*
  problem+json da aplicação, uma vez. Se o mapeador fizesse isso, só os nossos erros teriam.
- **`UseStatusCodePages`** é o que faz os erros do framework entrarem no formato.
  `AddProblemDetails` apenas registra o serviço; ele não é invocado por um 404 de
  roteamento, que sem isso responde com corpo vazio.

### `code` em membro de extensão, e não no `type`

A leitura purista da RFC põe o identificador do problema no `type`, como URI. Foi
considerada nas duas formas e recusada:

- **URN no `type`** (`urn:cathedrall:error:Pessoa.NotFound`) obriga a SPA a fatiar string, e
  um prefixo digitado errado falha em silêncio.
- **URL http no `type`** nos obrigaria a manter uma página publicada por código de erro.

Membro de extensão é o mecanismo que a própria RFC prevê para isto, e a SPA lê
`problem.code` direto. **O custo declarado é o desvio da leitura purista:** um consumidor
genérico de problem+json usaria `type` para distinguir tipos de problema, e o nosso não
distingue nada por ali.

### Por que o `traceId` é obrigatório

Quando o `IExceptionHandler` global existir, uma falha inesperada vai gerar duas linhas de
log: o `LoggingBehavior` registrando o desfecho e a duração, e o handler registrando o stack
trace. É a objeção que o README do kernel levanta contra logar e relançar — quem lê o log
conta dois incidentes onde houve um. Um identificador comum nas duas linhas **e** no corpo
da resposta resolve os dois problemas de uma vez: o log volta a ser um incidente, e a
secretaria consegue relatar um erro por um código que acha a requisição.

## Consequências

**Boas.** Uma forma de erro na API inteira, inclusive nos erros que não são nossos. `code` é
estável e legível por máquina, `detail` é livre para ser reescrito sem quebrar ninguém — a
mesma separação que o kernel já fazia, agora atravessando a fronteira HTTP. E o formato na
rede tem teste: as respostas de rota inexistente são verificadas por HTTP de verdade.

**Ruins, aceitas.**

**O envelope passou a ser contrato publicado.** Renomear `code`, movê-lo para `type` ou
trocar o significado de `detail` é breaking change em duas aplicações com deploy
independente. Era o objetivo — contrato serve para isso — mas o custo é real e chega no dia
em que quisermos mudar de ideia.

**`title` em inglês num corpo cujo `detail` é português.** É desconfortável e é deliberado:
`title` nomeia a classe do status e é igual para todo 404, enquanto `detail` descreve *esta*
ocorrência. Como a SPA renderiza `detail` e nunca `title`, o `title` não é texto que o
usuário lê e não cai na regra do [ADR-0013](0013-ingles-como-idioma-de-codigo-portugues-no-dominio.md).
**No dia em que alguém renderizar `title`, esta decisão vira bug** — a regra precisa
sobreviver no README e na revisão de código, porque nada no compilador a protege.

**`detail` chega ao cliente, e por isso não pode carregar dado de pessoa.** É a mesma
preocupação que o `LoggingBehavior` já documenta para o log, em outro canal: uma
`Description` reescrita para ficar mais útil vira
`"O e-mail joao@exemplo.com já está cadastrado"`, e aí PII passa a viajar para qualquer
ferramenta de rastreamento de erro que a SPA use — que tem outra retenção, outro controle de
acesso e não aparece em nenhum mapa de dados pessoais.

**Erro de validação múltiplo ainda não tem forma.** Fica **reservado** o membro `errors`,
que é a convenção em ProblemDetails. Não está construído: quando o `Result` ganhar uma
coleção de erros, ela vai para lá, e o contrato **cresce** em vez de mudar. Escolher o lugar
agora é o que garante isso.

**Duas respostas 500 vão precisar concordar.** `ErrorType.Failure` produz 500 com um `detail`
escrito por nós; o `IExceptionHandler` global vai produzir 500 sem detalhe nenhum. Se as duas
não tiverem a mesma forma, a SPA passa a ter dois tratamentos para o mesmo status.

## Gatilho de reavaliação

**Este ADR não decide onde o mapeador mora.** Hoje ele está no host, que é o que o README do
kernel já determinava, e nenhum módulo existe. Mas o ADR-0012 põe `Endpoints/` **dentro** de
cada módulo, e módulo não referencia o host. No dia em que o primeiro módulo tiver endpoints,
uma das duas coisas acontece: o mapeador vira um `CathedrAll.Kernel.Web` referenciado só
pelas pontas de entrada, ou os endpoints ficam no host e o ADR-0012 é que precisa ser
revisto. Nada aqui depende dessa escolha — o formato do corpo é o mesmo nos dois casos.
