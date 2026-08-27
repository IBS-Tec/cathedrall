import { ProblemDetailsError } from "@/lib/problem-details";

import type {
  Aniversariante,
  AniversariantesParams,
  AniversariantesResponse,
  CadastrarPessoaRequest,
  CadastrarPessoaResponse,
  PautaParams,
  PautaResponse,
  PessoaFichaResponse,
  PessoaErrorCode,
  PessoaListItem,
  PessoaSearchResult,
  PessoasListParams,
  PessoasPagedResponse,
  PessoasSearchParams,
  PessoasSearchResponse,
} from "../types";
import { adicionarPessoa, pessoas } from "./dataset";
import { comAnoDe, hoje, noIntervaloAnual } from "./dates";
import { createRandom } from "./random";

const ATRASO_MS = 220;
const MAXIMO_DA_BUSCA = 10;
const TAMANHO_PADRAO_DA_LISTA = 25;
const TAMANHO_MAXIMO_DA_LISTA = 50;

const random = createRandom(Date.now());

function responder<T>(valor: T): Promise<T> {
  return new Promise((resolve) => {
    setTimeout(() => resolve(valor), ATRASO_MS);
  });
}

function hex(digitos: number): string {
  let saida = "";
  for (let i = 0; i < digitos; i += 1) saida += random.int(0, 15).toString(16);
  return saida;
}

function recusar(
  status: number,
  code: PessoaErrorCode,
  detail: string,
): Promise<never> {
  const problem = new ProblemDetailsError({
    status,
    code,
    detail,
    traceId: `00-${hex(32)}-${hex(16)}-00`,
  });

  return new Promise((_, reject) => {
    setTimeout(() => reject(problem), ATRASO_MS);
  });
}

function normalizar(texto: string): string {
  return texto
    .normalize("NFD")
    .replace(/\p{Diacritic}/gu, "")
    .toLowerCase();
}

function tokens(texto: string): string[] {
  return normalizar(texto).split(/\s+/).filter(Boolean);
}

function casaPorToken(nome: string, termo: string): boolean {
  const doNome = tokens(nome);
  return tokens(termo).every((procurado) =>
    doNome.some((existente) => existente.startsWith(procurado)),
  );
}

function porId(id: string): PessoaFichaResponse | undefined {
  return pessoas.find((pessoa) => pessoa.id === id);
}

function resolverFusao(pessoa: PessoaFichaResponse): PessoaFichaResponse {
  const sobrevivente = pessoa.fundidaEm && porId(pessoa.fundidaEm.id);
  return sobrevivente ?? pessoa;
}

function vinculoVigente(pessoa: PessoaFichaResponse) {
  return (
    pessoa.vinculos.find((vinculo) => vinculo.dataFim === null) ??
    pessoa.vinculos[pessoa.vinculos.length - 1]
  );
}

function desde(pessoa: PessoaFichaResponse): string {
  return vinculoVigente(pessoa).dataInicio;
}

function visiveis(): PessoaFichaResponse[] {
  return pessoas.filter((pessoa) => pessoa.fundidaEm === null);
}

export function buscarPessoas(
  params: PessoasSearchParams,
): Promise<PessoasSearchResponse> {
  const termo = params.q.trim();
  if (termo === "") return responder({ results: [] });

  const encontradas = new Map<string, PessoaFichaResponse>();

  for (const pessoa of pessoas) {
    if (!casaPorToken(pessoa.nome, termo)) continue;

    const alvo = resolverFusao(pessoa);
    encontradas.set(alvo.id, alvo);
  }

  const results: PessoaSearchResult[] = [...encontradas.values()]
    .sort((a, b) => a.nome.localeCompare(b.nome, "pt-BR"))
    .slice(0, MAXIMO_DA_BUSCA)
    .map((pessoa) => ({
      id: pessoa.id,
      nome: pessoa.nome,
      situacao: pessoa.situacao,
      desde: desde(pessoa),
      convidadoPor: pessoa.convidadoPor,
    }));

  return responder({ results });
}

export function listarPessoas(
  params: PessoasListParams,
): Promise<PessoasPagedResponse> {
  const page = Math.max(params.page ?? 1, 1);
  const size = Math.min(
    Math.max(params.size ?? TAMANHO_PADRAO_DA_LISTA, 1),
    TAMANHO_MAXIMO_DA_LISTA,
  );

  const filtradas = visiveis()
    .filter(
      (pessoa) => !params.q?.trim() || casaPorToken(pessoa.nome, params.q),
    )
    .filter((pessoa) => !params.situacao || pessoa.situacao === params.situacao)
    .filter(
      (pessoa) =>
        !params.bairro ||
        normalizar(pessoa.endereco?.bairro ?? "") === normalizar(params.bairro),
    )
    .sort(
      (a, b) =>
        a.nome.localeCompare(b.nome, "pt-BR") || a.id.localeCompare(b.id),
    );

  const inicio = (page - 1) * size;
  const items: PessoaListItem[] = filtradas
    .slice(inicio, inicio + size)
    .map((pessoa) => ({
      id: pessoa.id,
      nome: pessoa.nome,
      situacao: pessoa.situacao,
      desde: desde(pessoa),
      bairro: pessoa.endereco?.bairro ?? null,
    }));

  return responder({ items, page, size, total: filtradas.length });
}

export function obterFicha(id: string): Promise<PessoaFichaResponse> {
  const pessoa = porId(id);
  if (!pessoa) {
    return recusar(404, "Pessoa.NotFound", "Pessoa não encontrada.");
  }
  return responder(pessoa);
}

function aniversariantesEntre(de: string, ate: string): Aniversariante[] {
  const lista: Aniversariante[] = [];

  for (const pessoa of visiveis()) {
    if (pessoa.situacao === "Falecido" || pessoa.situacao === "Transferido") {
      continue;
    }

    if (pessoa.dataNascimento && noIntervaloAnual(pessoa.dataNascimento, de, ate)) {
      lista.push({
        id: pessoa.id,
        nome: pessoa.nome,
        tipo: "Nascimento",
        data: comAnoDe(pessoa.dataNascimento, de),
      });
    }

    if (pessoa.dataCasamento && noIntervaloAnual(pessoa.dataCasamento, de, ate)) {
      lista.push({
        id: pessoa.id,
        nome: pessoa.nome,
        tipo: "Casamento",
        data: comAnoDe(pessoa.dataCasamento, de),
      });
    }
  }

  return lista.sort((a, b) => a.data.localeCompare(b.data));
}

export function listarAniversariantes(
  params: AniversariantesParams,
): Promise<AniversariantesResponse> {
  return responder({
    aniversariantes: aniversariantesEntre(params.from, params.to),
  });
}

export function obterPauta(params: PautaParams): Promise<PautaResponse> {
  const visitantes = visiveis()
    .filter((pessoa) => pessoa.situacao === "Visitante")
    .filter((pessoa) => vinculoVigente(pessoa).dataInicio === params.date)
    .map((pessoa) => ({
      id: pessoa.id,
      nome: pessoa.nome,
      convidadoPor: pessoa.convidadoPor,
    }));

  return responder({
    visitantes,
    aniversariantes: aniversariantesEntre(params.date, params.date),
  });
}

export function cadastrarPessoa(
  request: CadastrarPessoaRequest,
): Promise<CadastrarPessoaResponse> {
  const nome = request.nome.trim();
  if (nome === "") {
    return recusar(400, "Pessoa.NomeObrigatorio", "Informe o nome da pessoa.");
  }

  const anfitriao = request.convidadoPorId
    ? porId(request.convidadoPorId)
    : undefined;

  const nova: PessoaFichaResponse = {
    id: random.uuid(),
    nome,
    situacao: "Visitante",
    convidadoPor: anfitriao ? { id: anfitriao.id, nome: anfitriao.nome } : null,
    celular: null,
    email: null,
    dataNascimento: null,
    estadoCivil: null,
    dataCasamento: null,
    endereco: null,
    profissao: null,
    dataBatismo: null,
    vinculos: [
      { situacao: "Visitante", dataInicio: hoje(), dataFim: null, motivo: null },
    ],
    fundidaEm: null,
    anonimizada: false,
  };

  adicionarPessoa(nova);

  return responder({ id: nova.id, nome: nova.nome, situacao: nova.situacao });
}
