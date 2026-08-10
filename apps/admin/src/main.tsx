import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider, createBrowserRouter } from "react-router";

import "./index.css";
// Configura as mensagens do Zod em pt-BR. Precisa vir antes de qualquer schema.
import "./lib/validacao";
import { rotas } from "./app/rotas";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: 1 },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={createBrowserRouter(rotas)} />
    </QueryClientProvider>
  </StrictMode>,
);
