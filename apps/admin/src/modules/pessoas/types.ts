export type Situacao =
  | "Visitante"
  | "Membro"
  | "Afastado"
  | "Transferido"
  | "Falecido";

export type EstadoCivil =
  | "Solteiro"
  | "Casado"
  | "UniaoEstavel"
  | "Divorciado"
  | "Viuvo";

export type TipoAniversario = "Nascimento" | "Casamento";

export interface PessoaRef {
  id: string;
  nome: string;
}

export interface Endereco {
  cep: string | null;
  logradouro: string | null;
  numero: string | null;
  complemento: string | null;
  bairro: string;
  cidade: string | null;
  uf: string | null;
}

export interface Vinculo {
  situacao: Situacao;
  dataInicio: string;
  dataFim: string | null;
  motivo: string | null;
}

export interface Aniversariante {
  id: string;
  nome: string;
  tipo: TipoAniversario;
  data: string;
}

export interface PessoaSearchResult {
  id: string;
  nome: string;
  situacao: Situacao;
  desde: string;
  convidadoPor: PessoaRef | null;
}

export interface PessoasSearchResponse {
  results: PessoaSearchResult[];
}

export interface PessoaListItem {
  id: string;
  nome: string;
  situacao: Situacao;
  desde: string;
  bairro: string | null;
}

export interface PessoasPagedResponse {
  items: PessoaListItem[];
  page: number;
  size: number;
  total: number;
}

export interface PessoaFichaResponse {
  id: string;
  nome: string;
  situacao: Situacao;
  convidadoPor: PessoaRef | null;
  celular: string | null;
  email: string | null;
  dataNascimento: string | null;
  estadoCivil: EstadoCivil | null;
  dataCasamento: string | null;
  endereco: Endereco | null;
  profissao: string | null;
  dataBatismo: string | null;
  vinculos: Vinculo[];
  fundidaEm: PessoaRef | null;
  anonimizada: boolean;
}

interface PessoaFichaBase {
  id: string;
  nome: string;
}

export interface PessoaFichaAtiva extends PessoaFichaBase {
  estado: "Ativa";
  situacao: Situacao;
  convidadoPor: PessoaRef | null;
  celular: string | null;
  email: string | null;
  dataNascimento: string | null;
  estadoCivil: EstadoCivil | null;
  dataCasamento: string | null;
  endereco: Endereco | null;
  profissao: string | null;
  dataBatismo: string | null;
  vinculos: Vinculo[];
}

export interface PessoaFichaFundida extends PessoaFichaBase {
  estado: "Fundida";
  fundidaEm: PessoaRef;
}

export interface PessoaFichaAnonimizada extends PessoaFichaBase {
  estado: "Anonimizada";
  situacao: Situacao;
  vinculos: Vinculo[];
}

export type PessoaFicha =
  | PessoaFichaAtiva
  | PessoaFichaFundida
  | PessoaFichaAnonimizada;

export interface VisitantePauta {
  id: string;
  nome: string;
  convidadoPor: PessoaRef | null;
}

export interface PautaResponse {
  visitantes: VisitantePauta[];
  aniversariantes: Aniversariante[];
}

export interface AniversariantesResponse {
  aniversariantes: Aniversariante[];
}

export interface CadastrarPessoaRequest {
  nome: string;
  convidadoPorId?: string;
}

export interface CadastrarPessoaResponse {
  id: string;
  nome: string;
  situacao: Situacao;
}

export interface PessoasSearchParams {
  q: string;
}

export interface PessoasListParams {
  situacao?: Situacao;
  bairro?: string;
  page: number;
  size: number;
}

export interface AniversariantesParams {
  from: string;
  to: string;
}

export interface PautaParams {
  date: string;
}

export type PessoaErrorCode =
  | "Pessoa.NotFound"
  | "Pessoa.TransicaoInvalida"
  | "Pessoa.Fundida"
  | "Pessoa.Anonimizada"
  | "Pessoa.NomeObrigatorio"
  | "Pessoa.MotivoObrigatorio"
  | "Pessoa.DataFutura"
  | "Pessoa.DataRetroativa"
  | "Pessoa.AutoConvite"
  | "Pessoa.FusaoConsigoMesma";
