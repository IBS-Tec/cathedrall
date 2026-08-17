import type { RouteObject } from "react-router";
import { Layout } from "./Layout";
import { Home } from "./Home";
import { PessoaForm } from "@/modules/pessoas/PessoaForm";

export const routes: RouteObject[] = [
  {
    path: "/",
    element: <Layout />,
    children: [
      { index: true, element: <Home /> },
      // Rota de referência do padrão de formulário. Sai quando o cadastro real existir.
      { path: "pessoas/nova", element: <PessoaForm /> },
    ],
  },
];
