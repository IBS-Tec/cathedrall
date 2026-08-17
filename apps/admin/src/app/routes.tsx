import type { RouteObject } from "react-router";
import { Layout } from "./Layout";
import { Inicio } from "./Inicio";
import { PessoaForm } from "@/modules/pessoas/PessoaForm";

export const rotas: RouteObject[] = [
  {
    path: "/",
    element: <Layout />,
    children: [
      { index: true, element: <Inicio /> },
      // Rota de referência do padrão de formulário. Sai quando o cadastro real existir.
      { path: "pessoas/nova", element: <PessoaForm /> },
    ],
  },
];
