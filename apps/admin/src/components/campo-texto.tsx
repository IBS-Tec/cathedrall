import type { ComponentProps } from "react";
import {
  Controller,
  type Control,
  type FieldPath,
  type FieldValues,
} from "react-hook-form";

import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";

/**
 * Campo de texto ligado ao React Hook Form.
 *
 * O padrão oficial do shadcn exige, para CADA campo, um Controller envolvendo
 * Field + FieldLabel + Input + FieldError, com `field.name` repetido em três lugares.
 * Funciona, mas é cerimônia repetida — e cerimônia repetida é onde iniciante erra
 * (esquece o htmlFor, esquece o aria-invalid, e o formulário fica inacessível sem
 * ninguém perceber).
 *
 * Este componente encapsula isso uma vez. Ver ADR-0011: componente composto se
 * constrói uma vez, em components/, e se reutiliza.
 */
type CampoTextoProps<T extends FieldValues> = {
  control: Control<T>;
  name: FieldPath<T>;
  rotulo: string;
  descricao?: string;
  placeholder?: string;
  type?: ComponentProps<typeof Input>["type"];
};

export function CampoTexto<T extends FieldValues>({
  control,
  name,
  rotulo,
  descricao,
  placeholder,
  type,
}: CampoTextoProps<T>) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <Field data-invalid={fieldState.invalid}>
          <FieldLabel htmlFor={field.name}>{rotulo}</FieldLabel>
          <Input
            {...field}
            id={field.name}
            type={type}
            placeholder={placeholder}
            aria-invalid={fieldState.invalid}
          />
          {descricao && <FieldDescription>{descricao}</FieldDescription>}
          {fieldState.invalid && <FieldError errors={[fieldState.error]} />}
        </Field>
      )}
    />
  );
}
