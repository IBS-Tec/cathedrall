# ADR-0005 — SPA React com trilhos rígidos

**Status:** Aceito · **Data:** 2026-08-10
**Revisto por:** [ADR-0011](0011-shadcn-tailwind-nos-dois-frontends.md) — a linha
"Componentes" da tabela abaixo foi substituída por shadcn/ui + Tailwind.

## Contexto

O front-end do CathedrAll é destinado a desenvolvedores iniciantes, como espaço de
aprendizado. React puro, sem convenções impostas, produz caos previsível: cada pessoa
inventa um padrão de requisição, de estado e de botão.

## Decisão

**Vite + React + TypeScript em modo strict**, como SPA autenticada. Não Next.js: é
ferramenta interna atrás de login, SSR não agrega e adicionaria um runtime Node para
manter.

Trilhos obrigatórios:

| Área | Escolha | Regra |
|---|---|---|
| Chamadas à API | Cliente TypeScript **gerado** do OpenAPI | **Ninguém escreve `fetch` à mão.** |
| Estado de servidor | TanStack Query | Sem Redux e sem Zustand no MVP |
| Componentes | Biblioteca pronta com DataTable, DatePicker, forms e modais | Ninguém cria botão do zero |
| Formulários | React Hook Form + Zod | |
| Qualidade | ESLint + Prettier + `tsc --noEmit` no CI | PR não passa sem verde |

Estrutura de `src/modules/` espelha os módulos do backend.

## Motivos

- Cliente gerado elimina uma categoria inteira de bug ("campo com nome errado") e torna o
  OpenAPI o contrato real entre back e front.
- Quase todo o estado de um sistema de gestão é estado de servidor. Sem uma regra
  explícita, iniciantes colocam tudo numa store global.
- O CI é o revisor mais paciente disponível. Portão automático ensina melhor e mais
  rápido do que revisão manual de vírgula.

## Consequências

- Investimento inicial de configuração antes de convidar qualquer iniciante. A Fase 0
  precisa estar pronta primeiro: gente iniciante precisa de um exemplo para copiar, não
  de uma folha em branco.
- Menos liberdade para experimentar. É o objetivo.
- **Mobile é PWA responsivo, não app nativo.** Líder lança escala pelo celular no
  domingo à noite; responsivo bem feito resolve, app nativo é um segundo projeto.
