import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";

import {
  buscarPessoas,
  cadastrarPessoa,
  listarAniversariantes,
  listarPessoas,
  obterFicha,
  obterPauta,
} from "./api";
import type {
  AniversariantesParams,
  PautaParams,
  PessoasListParams,
  PessoasSearchParams,
} from "./types";

export const pessoasKeys = {
  todas: ["pessoas"] as const,
  search: (params: PessoasSearchParams) =>
    [...pessoasKeys.todas, "search", params.search] as const,
  list: (params: PessoasListParams) =>
    [...pessoasKeys.todas, "list", params] as const,
  ficha: (id: string) => [...pessoasKeys.todas, "ficha", id] as const,
  pauta: (params: PautaParams) =>
    [...pessoasKeys.todas, "pauta", params.date] as const,
  aniversariantes: (params: AniversariantesParams) =>
    [...pessoasKeys.todas, "aniversariantes", params.from, params.to] as const,
};

export function usePessoasSearch(params: PessoasSearchParams) {
  return useQuery({
    queryKey: pessoasKeys.search(params),
    queryFn: () => buscarPessoas(params),
    enabled: params.search.trim() !== "",
    placeholderData: keepPreviousData,
  });
}

export function usePessoasList(params: PessoasListParams) {
  return useQuery({
    queryKey: pessoasKeys.list(params),
    queryFn: () => listarPessoas(params),
    placeholderData: keepPreviousData,
  });
}

export function usePessoaFicha(id: string) {
  return useQuery({
    queryKey: pessoasKeys.ficha(id),
    queryFn: () => obterFicha(id),
  });
}

export function usePauta(params: PautaParams) {
  return useQuery({
    queryKey: pessoasKeys.pauta(params),
    queryFn: () => obterPauta(params),
  });
}

export function useAniversariantes(params: AniversariantesParams) {
  return useQuery({
    queryKey: pessoasKeys.aniversariantes(params),
    queryFn: () => listarAniversariantes(params),
  });
}

export function useCadastrarPessoa() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: cadastrarPessoa,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: pessoasKeys.todas });
    },
  });
}
