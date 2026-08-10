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
- **Use `CampoTexto`** (`components/campo-texto.tsx`) em vez de montar
  `Controller` + `Field` + `Input` + `FieldError` à mão. O padrão cru repete `field.name`
  em três lugares e é onde se esquece o `htmlFor` e o `aria-invalid`.
- **Mensagens em português são automáticas.** O locale `pt` do Zod é configurado uma vez
  em `lib/validacao.ts`, importado no `main.tsx`. Só escreva mensagem manual quando ela
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
