# ADR-0013 — Inglês como idioma de código, português no domínio

**Status:** Aceito · **Data:** 2026-08-17

## Contexto

A convenção anterior, escrita no `README.md` e no `CLAUDE.md`, dizia: *"domínio e UI em
português; infraestrutura e framework em inglês; não misturar dentro da mesma camada"*.

A fronteira **camada** não se sustentou na prática. Uma camada não é português nem inglês:
dentro do mesmo arquivo convivem um padrão de projeto e uma entidade de negócio. O
resultado foi tradução de vocabulário técnico consagrado. Alguns exemplos do código que
existia até este ADR:

| Escrito | O que realmente é |
| --- | --- |
| `FabricaDeLogFalsa` | `ILoggerFactory` — o padrão é *Factory* |
| `RequisicaoFalsa`, `HandlerFalso` | *fake* de *request* e de *handler* |
| `BehaviorQueCurtoCircuita` | um *pipeline behavior* |
| `CampoTexto` com prop `rotulo` | um *field* com um *label* |
| `{DuracaoMs}`, `{Desfecho}` | campos estruturados de log |

Ninguém procura "Fábrica" atrás de uma *Factory*, nem "Repositório" atrás de um
*Repository*. Os padrões foram formulados, documentados e são discutidos em inglês; a
tradução obriga o leitor a fazer o caminho de volta antes de reconhecer o padrão. Para um
time de voluntários iniciantes com rotatividade alta — o risco central do projeto — isso é
custo puro, e vai na direção contrária de qualquer material que eles vão encontrar
pesquisando.

Ao mesmo tempo, o sentido oposto é igualmente ruim. `Pessoa` não é `Person`: é o cadastro
da igreja, com as regras do [ADR-0008](0008-pessoa-como-raiz-unica.md). `Escala` não é
`Schedule` — é a escala de um departamento num culto. Traduzir isso perde precisão e afasta
o código do vocabulário que a secretaria usa ao descrever a regra.

## Decisão

**O idioma de código é o inglês. A exceção é o vocabulário de negócio, que fica em
português.** A fronteira não é a camada — é a natureza do nome.

### Inglês

Tudo que é vocabulário técnico, de framework ou de padrão de projeto:

- padrões e estruturas: `Factory`, `Repository`, `Handler`, `Behavior`, `Sender`, `Builder`
- tipos e membros de infraestrutura: `PostgresHealthCheck`, `ConnectionName`
- variáveis, parâmetros, campos e constantes locais: `connection`, `provider`, `scope`,
  `response`, `trace`, `expected`
- tipos de apoio de teste: `FakeRequest`, `FakeHandler`, `FakeLoggerFactory`, `Scenario`
- campos estruturados de log e mensagens de diagnóstico: `{Request}`, `{Outcome}`,
  `{DurationMs}`, `{ErrorCode}`, `"Postgres is unreachable."`
- arquivos e componentes de infraestrutura da SPA: `routes.tsx`, `text-field.tsx`,
  `validation.ts`, `TextField` com props `label` e `description`

### Português

Tudo que nomeia uma coisa do negócio da igreja:

- entidades, agregados, objetos de valor e enums de domínio: `Pessoa`, `Departamento`,
  `Escala`, `Situacao`, `Papel`
- propriedades de domínio: `DataInicio`, `Motivo`, `HoraInicio` (ver `docs/dominio.md`)
- módulos e as fatias verticais dos dois lados: `Modulos/Pessoas/`, `modules/pessoas/`
- campos de contrato da API e, por consequência, campos de formulário na SPA:
  `nome`, `telefone`
- **todo** texto que o usuário lê: rótulo, mensagem de validação, título de tela
- nomes de método de teste, que são frases descrevendo comportamento:
  `Falha_de_negocio_deve_registrar_warning_com_o_codigo_do_erro`

### As duas fronteiras internas

**Código de erro: prefixo em português, sufixo em inglês.**

```csharp
Error.NotFound("Pessoa.NotFound", "Pessoa não encontrada.")
Error.Conflict("Escala.AlreadyPublished", "Escala já publicada.")
```

O prefixo é a entidade; o sufixo é o vocabulário de `ErrorType`, que existe no kernel e é
técnico. A descrição chega ao usuário e fica em português.

**Teste: nome do método em português, corpo em inglês.**

```csharp
[Fact]
public async Task Nao_deve_registrar_o_conteudo_da_requisicao()
{
    List<LogRecord> records = [];
    // ...
}
```

O nome do teste é o que aparece no relatório de falha — é documentação executável, lida por
quem quer entender a regra. O corpo é código.

## Consequências

**Boas.** O nome do padrão no código é o nome pelo qual ele é pesquisável. Um voluntário
que leia `TextField`, `Scenario.Build` ou `FakeLoggerFactory` reconhece o que são sem
tradução intermediária. A fronteira nova é decidível olhando um nome só — *isto é coisa da
igreja ou coisa de computador?* — em vez de exigir que se determine a camada primeiro.

**Ruins, aceitas.** A mistura de idiomas fica mais visível, não menos: `PessoaForm`,
`services.AddSingleton<IRequestHandler<FakeRequest, string>>`, `Error.NotFound("Pessoa.NotFound", …)`.
É desconfortável de ler em voz alta e não há como evitar — a alternativa é traduzir um dos
dois lados e perder ou o padrão ou o vocabulário do negócio.

Existe uma faixa cinzenta real: um `Culto` é entidade de negócio, mas um `Slot` de escala
pode ser argumentado nos dois sentidos. **Na dúvida, use inglês.** O português é a exceção
que se justifica, e ela se justifica quando a secretaria usaria a palavra numa conversa.

**Renomeações feitas neste ADR.** O [ADR-0011](0011-shadcn-tailwind-nos-dois-frontends.md)
cita `components/campo-texto.tsx` e `lib/validacao.ts` pelos nomes antigos; ADR aceito não
se edita, então o mapa fica aqui:

| Antes | Depois |
| --- | --- |
| `components/campo-texto.tsx`, `CampoTexto` | `components/text-field.tsx`, `TextField` |
| props `rotulo`, `descricao` | props `label`, `description` |
| `lib/validacao.ts` | `lib/validation.ts` |
| `app/rotas.tsx`, `rotas` | `app/routes.tsx`, `routes` |
| `app/Inicio.tsx`, `Inicio` | `app/Home.tsx`, `Home` |

No site, pelo mesmo critério:

| Antes | Depois |
| --- | --- |
| `components/Cabecalho.astro` | `components/Header.astro` |
| `components/Rodape.astro` | `components/Footer.astro` |
| `buscar`, `formatarHora`, `enderecoCompleto` | `fetchItems`, `formatTime`, `fullAddress` |
| props `titulo`, `descricao` do `BaseLayout` | props `title`, `description` |

**O que o site NÃO renomeou:** `Configuracao`, `HorarioCulto`, `cms.horarios()` e os campos
`nome_igreja`, `dia_semana`, `publico_alvo`. Esses nomes são o schema do Directus, não
escolha nossa — traduzi-los descolaria o tipo do contrato que chega pela rede. A mesma
regra vale para os campos do formulário da SPA, que espelham o contrato da API.

A troca custou pouco porque foi feita com o sistema ainda vazio: não há módulo de negócio,
banco, migration nem ambiente publicado. Feita depois do primeiro CRUD, seria caríssima.
**Este ADR só é barato agora.**
