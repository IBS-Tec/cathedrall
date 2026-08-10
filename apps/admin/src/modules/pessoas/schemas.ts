import * as z from "zod";

/**
 * ATENÇÃO: exemplo de referência, não o cadastro real.
 *
 * O modelo de Pessoa em docs/dominio.md ainda é rascunho e precisa ser validado com a
 * secretaria antes de virar tela de verdade. Estes três campos existem para demonstrar
 * o padrão de formulário — copie a estrutura, não os campos.
 *
 * Schema mora junto do módulo que o usa. Não existe pasta schemas/ global: mesma
 * lógica de fatia vertical do backend.
 */

export const pessoaSchema = z.object({
  nome: z.string().min(3, "Informe o nome completo."),

  telefone: z.string().min(10, "Informe o telefone com DDD."),

  // Muitos membros não têm e-mail. String vazia é resposta válida, não erro.
  email: z.union([z.email(), z.literal("")]),
});

export type PessoaFormValues = z.infer<typeof pessoaSchema>;
