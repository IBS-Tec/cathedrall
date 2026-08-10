import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";

import { CampoTexto } from "@/components/campo-texto";
import { Button } from "@/components/ui/button";
import { FieldGroup } from "@/components/ui/field";

import { pessoaSchema, type PessoaFormValues } from "./schemas";

/**
 * Formulário de referência. É este arquivo que os desenvolvedores iniciantes vão
 * copiar — mantenha-o pequeno e correto.
 */
export function PessoaForm() {
  const form = useForm<PessoaFormValues>({
    resolver: zodResolver(pessoaSchema),
    // defaultValues não é opcional: sem eles o React reclama de input não controlado
    // assim que o usuário digita, e o erro aparece só no console.
    defaultValues: { nome: "", telefone: "", email: "" },
  });

  function aoEnviar(valores: PessoaFormValues) {
    // A API ainda não existe. Quando existir, isto vira uma mutation do TanStack Query
    // chamando o cliente gerado em packages/api-client — nunca fetch à mão (ADR-0005).
    console.log(valores);
  }

  return (
    <form onSubmit={form.handleSubmit(aoEnviar)} className="max-w-md">
      <FieldGroup>
        <CampoTexto control={form.control} name="nome" rotulo="Nome completo" />
        <CampoTexto
          control={form.control}
          name="telefone"
          rotulo="Telefone"
          placeholder="(00) 00000-0000"
        />
        <CampoTexto
          control={form.control}
          name="email"
          rotulo="E-mail"
          descricao="Opcional."
          type="email"
        />
      </FieldGroup>

      <Button type="submit" className="mt-6">
        Salvar
      </Button>
    </form>
  );
}
