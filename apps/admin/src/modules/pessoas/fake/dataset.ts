import type {
  EstadoCivil,
  PessoaFichaResponse,
  Situacao,
  Vinculo,
} from "../types";
import { addDays, hoje, toIso } from "./dates";
import { createRandom, type Random } from "./random";

const SEMENTE = 20260822;
const QUANTIDADE = 90;

const NOMES = [
  "Ana",
  "Beatriz",
  "Carla",
  "Daniela",
  "Elisa",
  "Fernanda",
  "Gabriela",
  "Helena",
  "Isabel",
  "Juliana",
  "Larissa",
  "Marina",
  "Natália",
  "Patrícia",
  "Rafaela",
  "André",
  "Bruno",
  "Caio",
  "Daniel",
  "Eduardo",
  "Felipe",
  "Gustavo",
  "Henrique",
  "Igor",
  "João",
  "Lucas",
  "Marcelo",
  "Otávio",
  "Rodrigo",
  "Thiago",
];

const SOBRENOMES = [
  "Almeida",
  "Barbosa",
  "Cavalcanti",
  "Duarte",
  "Esteves",
  "Ferreira",
  "Guedes",
  "Henriques",
  "Ibrahim",
  "Jorge",
  "Lima",
  "Monteiro",
  "Nogueira",
  "Oliveira",
  "Pereira",
  "Queiroz",
  "Ramos",
  "Souza",
  "Teixeira",
  "Vasconcelos",
];

const BAIRROS = [
  "Centro",
  "Boa Vista",
  "Jardim América",
  "São José",
  "Vila Nova",
  "Alto da Colina",
  "Santo Antônio",
  "Bela Vista",
];

const PROFISSOES = [
  "Professora",
  "Eletricista",
  "Enfermeiro",
  "Autônomo",
  "Costureira",
  "Motorista",
  "Estudante",
  "Aposentada",
  "Comerciante",
  "Pedreiro",
];

const ESTADOS_CIVIS: EstadoCivil[] = [
  "Solteiro",
  "Casado",
  "UniaoEstavel",
  "Divorciado",
  "Viuvo",
];

const MOTIVOS_AFASTAMENTO = [
  "Sem contato desde o início do ano.",
  "Mudou de cidade a trabalho.",
  "Afastamento reconhecido pela liderança.",
  "Pediu afastamento por motivo de saúde.",
];

const IGREJAS_DESTINO = [
  "Transferido para a Igreja Batista de Vila Nova.",
  "Transferido para a Congregação do Alto da Colina.",
  "Transferido para igreja em outra cidade.",
];

interface Transicao {
  situacao: Situacao;
  motivo: string | null;
}

function proximaSituacao(random: Random, atual: Situacao): Transicao | null {
  const sorte = random.next();

  if (atual === "Visitante") {
    if (sorte < 0.62) return { situacao: "Membro", motivo: null };
    if (sorte < 0.64) return { situacao: "Falecido", motivo: null };
    return null;
  }

  if (atual === "Membro") {
    if (sorte < 0.12)
      return {
        situacao: "Afastado",
        motivo: random.pick(MOTIVOS_AFASTAMENTO),
      };
    if (sorte < 0.18)
      return { situacao: "Transferido", motivo: random.pick(IGREJAS_DESTINO) };
    if (sorte < 0.21) return { situacao: "Falecido", motivo: null };
    return null;
  }

  if (atual === "Afastado") {
    if (sorte < 0.35) return { situacao: "Membro", motivo: null };
    if (sorte < 0.45)
      return { situacao: "Transferido", motivo: random.pick(IGREJAS_DESTINO) };
    if (sorte < 0.5) return { situacao: "Falecido", motivo: null };
    return null;
  }

  if (atual === "Transferido") {
    if (sorte < 0.1) return { situacao: "Membro", motivo: null };
    if (sorte < 0.13) return { situacao: "Falecido", motivo: null };
    return null;
  }

  return null;
}

function gerarVinculos(random: Random, hojeIso: string): Vinculo[] {
  const vinculos: Vinculo[] = [];

  let situacao: Situacao = random.chance(0.25) ? "Visitante" : "Membro";
  let dataInicio = addDays(hojeIso, -random.int(20, 3600));
  let motivo: string | null = null;

  for (let passo = 0; passo < 4; passo += 1) {
    const proxima = proximaSituacao(random, situacao);
    if (proxima === null) break;

    const dataFim = addDays(dataInicio, random.int(30, 900));
    if (dataFim >= hojeIso) break;

    vinculos.push({ situacao, dataInicio, dataFim, motivo });
    situacao = proxima.situacao;
    motivo = proxima.motivo;
    dataInicio = dataFim;
  }

  vinculos.push({ situacao, dataInicio, dataFim: null, motivo });
  return vinculos;
}

function dataNascimentoQualquer(random: Random): string {
  const ano = random.int(1938, 2019);
  const mes = random.int(1, 12);
  const dia = random.int(1, 28);
  return toIso(new Date(Date.UTC(ano, mes - 1, dia)));
}

function gerarPessoa(
  random: Random,
  hojeIso: string,
  nome: string,
): PessoaFichaResponse {
  const vinculos = gerarVinculos(random, hojeIso);
  const situacao = vinculos[vinculos.length - 1].situacao;
  const fichaCompleta = situacao !== "Visitante";

  const estadoCivil = fichaCompleta ? random.pick(ESTADOS_CIVIS) : null;
  const casado = estadoCivil === "Casado" || estadoCivil === "UniaoEstavel";

  return {
    id: random.uuid(),
    nome,
    situacao,
    convidadoPor: null,
    celular:
      fichaCompleta && random.chance(0.85)
        ? `+55819${random.int(10000000, 99999999)}`
        : null,
    email:
      fichaCompleta && random.chance(0.55)
        ? `${nome.split(" ")[0].toLowerCase()}${random.int(1, 99)}@exemplo.com`
        : null,
    dataNascimento:
      fichaCompleta && random.chance(0.92) ? dataNascimentoQualquer(random) : null,
    estadoCivil,
    dataCasamento:
      casado && random.chance(0.8)
        ? addDays(hojeIso, -random.int(400, 12000))
        : null,
    endereco: fichaCompleta
      ? {
          cep: random.chance(0.6) ? `${random.int(50000000, 59999999)}` : null,
          logradouro: random.chance(0.7)
            ? `Rua ${random.pick(SOBRENOMES)}`
            : null,
          numero: random.chance(0.7)
            ? random.pick(["123", "45", "s/n", "1002-A", "78"])
            : null,
          complemento: random.chance(0.2) ? `Apto ${random.int(11, 402)}` : null,
          bairro: random.pick(BAIRROS),
          cidade: random.chance(0.8) ? "Recife" : null,
          uf: random.chance(0.8) ? "PE" : null,
        }
      : null,
    profissao: fichaCompleta && random.chance(0.6) ? random.pick(PROFISSOES) : null,
    dataBatismo:
      situacao === "Membro" && random.chance(0.7)
        ? addDays(hojeIso, -random.int(400, 9000))
        : null,
    vinculos,
    fundidaEm: null,
    anonimizada: false,
  };
}

function gerar(): PessoaFichaResponse[] {
  const random = createRandom(SEMENTE);
  const hojeIso = hoje();
  const pessoas: PessoaFichaResponse[] = [];

  for (let i = 0; i < QUANTIDADE; i += 1) {
    const nome = `${random.pick(NOMES)} ${random.pick(SOBRENOMES)}`;
    pessoas.push(gerarPessoa(random, hojeIso, nome));
  }

  const membros = pessoas.filter((pessoa) => pessoa.situacao === "Membro");
  for (const pessoa of pessoas) {
    const convida = pessoa.situacao === "Visitante" ? 0.8 : 0.35;
    if (!random.chance(convida)) continue;

    const anfitriao = random.pick(membros);
    if (anfitriao.id === pessoa.id) continue;
    pessoa.convidadoPor = { id: anfitriao.id, nome: anfitriao.nome };
  }

  const comData = pessoas.filter((pessoa) => pessoa.dataNascimento !== null);
  for (let i = 0; i < 9; i += 1) {
    const pessoa = comData[i];
    pessoa.dataNascimento = `${pessoa.dataNascimento!.slice(0, 4)}-${addDays(
      hojeIso,
      random.int(0, 6),
    ).slice(5)}`;
  }

  const casados = pessoas.filter((pessoa) => pessoa.dataCasamento !== null);
  for (let i = 0; i < 4; i += 1) {
    const pessoa = casados[i];
    pessoa.dataCasamento = `${pessoa.dataCasamento!.slice(0, 4)}-${addDays(
      hojeIso,
      random.int(0, 6),
    ).slice(5)}`;
  }

  const visitantesDeHoje = pessoas.filter(
    (pessoa) => pessoa.situacao === "Visitante",
  );
  for (let i = 0; i < 3; i += 1) {
    const pessoa = visitantesDeHoje[i];
    pessoa.vinculos = [
      { situacao: "Visitante", dataInicio: hojeIso, dataFim: null, motivo: null },
    ];
  }

  const homonimos = pessoas.slice(10, 12);
  homonimos[0].nome = "João Guedes";
  homonimos[1].nome = "João Guedes";
  pessoas[20].nome = "Marina Souza";
  pessoas[21].nome = "Marina Souza";

  const membroSemNascimento = pessoas.find(
    (pessoa) => pessoa.situacao === "Membro" && pessoa.dataNascimento !== null,
  )!;
  membroSemNascimento.dataNascimento = null;

  const sobrevivente = pessoas[30];
  const absorvida = pessoas[31];
  absorvida.nome = sobrevivente.nome;
  absorvida.fundidaEm = { id: sobrevivente.id, nome: sobrevivente.nome };

  const anonimizada = pessoas[40];
  anonimizada.nome = "Registro anonimizado";
  anonimizada.anonimizada = true;
  anonimizada.celular = null;
  anonimizada.email = null;
  anonimizada.dataNascimento = null;
  anonimizada.estadoCivil = null;
  anonimizada.dataCasamento = null;
  anonimizada.endereco = null;
  anonimizada.profissao = null;
  anonimizada.dataBatismo = null;
  anonimizada.convidadoPor = null;

  const semConvite = visitantesDeHoje[0];
  semConvite.convidadoPor = null;

  return pessoas;
}

export const pessoas: PessoaFichaResponse[] = gerar();

export function adicionarPessoa(pessoa: PessoaFichaResponse): void {
  pessoas.unshift(pessoa);
}
