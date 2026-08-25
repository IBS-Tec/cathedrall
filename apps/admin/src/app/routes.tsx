import type { RouteObject } from "react-router";
import { Layout } from "./Layout";
import { Home } from "./Home";
import { RecepcaoPage } from "@/modules/pessoas/RecepcaoPage";
import { PessoasPage } from "@/modules/pessoas/PessoasPage";
import { PessoaFichaPage } from "@/modules/pessoas/PessoaFichaPage";
import { PautaPage } from "@/modules/pessoas/PautaPage";
import { AniversariantesPage } from "@/modules/pessoas/AniversariantesPage";

export const routes: RouteObject[] = [
  {
    path: "/",
    element: <Layout />,
    children: [
      { index: true, element: <Home /> },
      { path: "recepcao", element: <RecepcaoPage /> },
      { path: "pessoas", element: <PessoasPage /> },
      { path: "pessoas/:id", element: <PessoaFichaPage /> },
      { path: "pauta", element: <PautaPage /> },
      { path: "aniversariantes", element: <AniversariantesPage /> },
    ],
  },
];
