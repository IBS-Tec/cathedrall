# apps/admin — CathedrAll (SPA)

React + Vite + TypeScript strict, com Tailwind e shadcn/ui.
Domínio: `app.ibscristo.com.br`.

## Comandos

Da raiz do repositório:

```bash
pnpm admin:dev                                  # servidor de desenvolvimento (porta 5173)
pnpm --filter @cathedrall/admin build           # build de produção em dist/
pnpm --filter @cathedrall/admin typecheck       # tsc --noEmit
pnpm --filter @cathedrall/admin lint
```

Para adicionar um componente do shadcn, a partir de `apps/admin`:

```bash
pnpm dlx shadcn@latest add <componente>
```

## Regras que não se negociam

1. **Ninguém escreve `fetch` à mão.** Toda chamada passa pelo cliente gerado em
   `packages/api-client`. Se falta um endpoint, ele é adicionado na API e o cliente é
   regerado.
2. **Estado de servidor é do TanStack Query.** Sem store global no MVP.
3. **Ninguém cria componente base do zero.** Componentes compostos (tabela de dados,
   seletor de pessoa) são construídos **uma vez** em `components/` e reutilizados. Um
   segundo desenvolvedor reimplementando tabela é sintoma de falha.
4. `src/modules/` **espelha** os módulos da API. Código de uma feature mora junto.
5. **Nenhuma tela conhece a origem do dado.** Componente consome hook; hook consome
   `api.ts`; só `api.ts` sabe se o dado vem do cliente gerado ou de dados falsos. A regra
   é `no-restricted-imports` no `eslint.config.js`, não convenção.

## Estrutura

```
src/
  app/           bootstrap, providers, rotas, layout, tema
  components/
    ui/          gerado pelo shadcn — código nosso, editável
  modules/       fatias verticais — espelham os módulos da API
    pessoas/
    departamentos/
    eventos/
    escalas/
  hooks/         hooks compartilhados
  lib/           utilitários, configuração do cliente de API
```

Componente usado por um único módulo mora **dentro** do módulo. `components/` não é
depósito.

## Dados de servidor

Três camadas, e a ordem dos imports é de mão única:

```
Tela.tsx  →  queries.ts  →  api.ts  →  cliente gerado  (hoje: fake/)
```

- **`types.ts`** espelha as respostas da API, uma forma por resposta — nunca um tipo gordo
  reusado. É o arquivo que o cliente gerado substitui.
- **`api.ts`** é a costura, e é o **único** lugar que muda no dia em que o cliente gerado
  existir. Nada além de `queries.ts` importa daqui.
- **`queries.ts`** tem os hooks e as `queryKey`. Todo parâmetro que muda a resposta entra
  na chave — chave que ignora o termo da busca devolve o resultado da busca anterior.
- **`fake/`** é a implementação temporária: gerador com semente fixa, para que os mesmos
  dados e os mesmos ids saiam em toda recarga. Tem atraso artificial de propósito, para que
  ninguém escreva tela sem estado de carregamento.

Enquanto a API não existe, a fatia inteira nasce por aqui. Quando existir, o `fake/` é
apagado e os corpos de `api.ts` passam a chamar `packages/api-client`. Nenhuma tela muda.

Erro da API é `ProblemDetails` ([ADR-0014](../../docs/adr/0014-problem-details-como-formato-unico-de-erro.md)),
em `lib/problem-details.ts`. **Ramifique em `problem.code`**, nunca no texto de `detail`.
O tipo omite `title` e `type` de propósito: eles chegam no JSON, mas a SPA não os renderiza,
e o compilador é quem garante isso.

## O que você precisa saber sobre o shadcn

**Não é dependência.** A CLI copia o código para `src/components/ui/`. Os componentes são
nossos, versionados no git, e podem ser editados à vontade — mas correção de bug upstream
**não chega por `pnpm update`**. Atualizar exige rodar a CLI de novo para aquele
componente e revisar o diff.

Esta versão é construída sobre **Base UI**, não Radix. Tutoriais que falam em Radix vão
divergir do que está aqui.

## Formulários

React Hook Form + Zod, sobre os componentes `Field` do shadcn.

- **Schema mora no módulo**, em `modules/<modulo>/schemas.ts`. Não existe pasta `schemas/`
  global — mesma lógica de fatia vertical do backend.
- **Use `TextField`** (`components/text-field.tsx`) em vez de montar
  `Controller` + `Field` + `Input` + `FieldError` à mão. O padrão cru repete `field.name`
  em três lugares e é onde se esquece o `htmlFor` e o `aria-invalid`.
- **Mensagens em português são automáticas.** O locale `pt` do Zod é configurado uma vez
  em `lib/validation.ts`, importado no `main.tsx`. Só escreva mensagem manual quando ela
  explicar uma regra de negócio ("Informe o telefone com DDD"), não para traduzir.
- **O Zod aqui valida experiência de uso, não regra de negócio.** A API valida de novo,
  sempre, e é ela quem decide. Qualquer pessoa com o DevTools aberto pula esta validação.

Referência viva: `modules/pessoas/PessoaForm.tsx`. É o arquivo a copiar — os campos são
exemplo, a estrutura é o padrão.

Não gere schemas Zod a partir do OpenAPI. São coisas diferentes: o OpenAPI descreve o que
a API aceita; o Zod descreve o que o formulário exige do usuário — que frequentemente é
mais (confirmar senha) ou menos (campo preenchido pelo servidor).

## Referências

- [ADR-0005](../../docs/adr/0005-frontend-react-spa-com-trilhos.md) — trilhos do frontend
- [ADR-0011](../../docs/adr/0011-shadcn-tailwind-nos-dois-frontends.md) — shadcn + Tailwind
