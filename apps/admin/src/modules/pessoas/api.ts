import * as fake from "./fake/responses";
import type {
  AniversariantesParams,
  AniversariantesResponse,
  CadastrarPessoaRequest,
  CadastrarPessoaResponse,
  PautaParams,
  PautaResponse,
  PessoaFicha,
  PessoaFichaResponse,
  PessoasListParams,
  PessoasPagedResponse,
  PessoasSearchParams,
  PessoasSearchResponse,
} from "./types";

function toPessoaFicha(response: PessoaFichaResponse): PessoaFicha {
  if (response.fundidaEm !== null) {
    return {
      estado: "Fundida",
      id: response.id,
      nome: response.nome,
      fundidaEm: response.fundidaEm,
    };
  }

  if (response.anonimizada) {
    return {
      estado: "Anonimizada",
      id: response.id,
      nome: response.nome,
      situacao: response.situacao,
      vinculos: response.vinculos,
    };
  }

  return { estado: "Ativa", ...response };
}

export function buscarPessoas(
  params: PessoasSearchParams,
): Promise<PessoasSearchResponse> {
  return fake.buscarPessoas(params);
}

export function listarPessoas(
  params: PessoasListParams,
): Promise<PessoasPagedResponse> {
  return fake.listarPessoas(params);
}

export async function obterFicha(id: string): Promise<PessoaFicha> {
  return toPessoaFicha(await fake.obterFicha(id));
}

export function listarAniversariantes(
  params: AniversariantesParams,
): Promise<AniversariantesResponse> {
  return fake.listarAniversariantes(params);
}

export function obterPauta(params: PautaParams): Promise<PautaResponse> {
  return fake.obterPauta(params);
}

export function cadastrarPessoa(
  request: CadastrarPessoaRequest,
): Promise<CadastrarPessoaResponse> {
  return fake.cadastrarPessoa(request);
}
