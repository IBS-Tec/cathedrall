import * as z from "zod";

/**
 * Mensagens de validação em português, uma vez só, para o app inteiro.
 *
 * Sem isto o Zod emite em inglês ("String must contain at least 3 character(s)"),
 * o que é inaceitável numa tela usada pela secretaria.
 *
 * Importe este módulo no bootstrap, ANTES de qualquer schema ser avaliado.
 *
 * Consequência prática: não escreva mensagem manual em cada campo. Só passe texto
 * customizado quando a mensagem padrão não explicar a regra de negócio — por exemplo
 * "Informe o nome completo" em vez de "muito pequeno: esperado que texto tenha >=3
 * caracteres". Mensagem manual em todo campo é como a consistência se perde.
 */
z.config(z.locales.pt());
