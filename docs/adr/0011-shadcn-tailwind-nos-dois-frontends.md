# ADR-0011 — shadcn/ui e Tailwind nos dois frontends

**Status:** Aceito · **Data:** 2026-08-10
**Revisa:** [ADR-0005](0005-frontend-react-spa-com-trilhos.md) — linha "Componentes" da tabela de trilhos

## Contexto

O [ADR-0005](0005-frontend-react-spa-com-trilhos.md) exigia "biblioteca de componentes
pronta com DataTable, DatePicker, forms e modais" sem nomear qual, com a justificativa de
que ninguém deveria construir componente base do zero.

A recomendação inicial foi **Mantine**, por entregar tabela e calendário prontos. Foi
recusada em favor de uma solução baseada em Tailwind, com o argumento de manter **uma
única stack de estilo entre o site e o admin**.

O argumento recusado é mais forte no eixo que importa neste projeto. O ADR-0004 estabelece
que o maior risco é bus factor 1, com voluntários iniciantes e rotativos. Duas formas de
estilizar significa dois modelos mentais, dois conjuntos de dúvidas e duas curvas de
aprendizado para um time que já é escasso. Componente pronto economiza dias; stack única
economiza em cada pessoa nova que entra.

## Decisão

**Tailwind nos dois frontends.** O site usa Tailwind direto; o admin usa Tailwind +
**shadcn/ui**. Nenhuma biblioteca de componentes com sistema de estilo próprio.

## Consequências

### O ganho que não era o objetivo declarado

`packages/brand` deixa de ser aspiracional. Com Tailwind dos dois lados, os tokens da marca
viram **um** bloco `@theme` consumido por ambos, em vez de uma tabela de cores duplicada em
dois formatos incompatíveis.

### shadcn/ui não é dependência

A CLI **copia o código do componente para dentro do repositório**. O botão vira arquivo
nosso, versionado e editável.

- Ganha-se controle total e ausência de trava de versão.
- Perde-se atualização automática: correção de bug upstream não chega por `pnpm update`.
- O time precisa saber disso. Quem espera que atualizar pacote conserte um componente vai
  esperar sentado.

### O custo aceito

Contradiz parcialmente o "ninguém cria componente do zero" do ADR-0005. Na prática:

- Componentes base (botão, input, diálogo, select) vêm prontos pela CLI — o espírito da
  regra se mantém.
- **DataTable não vem pronto**: é uma receita sobre TanStack Table, construída uma vez e
  reutilizada. Sistema de gestão vive de tabela, então isto não é opcional — é trabalho
  agendado.
- **DatePicker** vem via `react-day-picker`, através do componente `calendar`.

A regra revisada: **ninguém cria componente base do zero; componentes compostos são
construídos uma vez, em `components/`, e reutilizados.** Um segundo desenvolvedor
reimplementando tabela é o sintoma de falha desta decisão.

### Nota de implementação

A versão atual do shadcn/ui é construída sobre **Base UI**, não Radix. Documentação e
tutoriais antigos referenciam Radix e vão divergir do que está no repositório.

## Formulários — resolvido

O estilo adotado é **`base-nova`** (Base UI), escolhido conscientemente mesmo após o custo
de cobertura de tutoriais ser apontado. Nele **não existe** o componente `form` clássico
do shadcn; o equivalente é **`field`** (`Field`, `FieldGroup`, `FieldLabel`,
`FieldDescription`, `FieldError`).

Atenção a uma armadilha da CLI: pedir um componente inexistente **não gera erro**. Ela
consulta o registro, não encontra e encerra em silêncio. Foi assim que `form` sumiu do
lote inicial sem ninguém notar. Ao instalar em lote, confira o que apareceu em
`components/ui/`.

**React Hook Form + Zod permanecem como o ADR-0005 prescreve.** O padrão oficial liga
`Controller` do RHF aos componentes `Field`. Como esse padrão repete `field.name` em três
lugares por campo — e esquecer o `htmlFor` ou o `aria-invalid` deixa o formulário
inacessível sem aviso — ele fica encapsulado em `components/campo-texto.tsx`.

Mensagens de validação em português vêm do locale `pt` embutido no Zod 4, configurado uma
única vez em `lib/validacao.ts`. Ninguém escreve mensagem de erro campo a campo.
