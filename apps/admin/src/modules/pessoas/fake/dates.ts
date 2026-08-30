export function toIso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

export function fromIso(iso: string): Date {
  return new Date(`${iso}T00:00:00Z`);
}

export function hoje(): string {
  return toIso(new Date());
}

export function addDays(iso: string, days: number): string {
  const date = fromIso(iso);
  date.setUTCDate(date.getUTCDate() + days);
  return toIso(date);
}

export function mesDia(iso: string): string {
  return iso.slice(5);
}

const MAXIMO_DE_DIAS = 31;

/**
 * Dia e mês de cada data do intervalo, apontando para a data em que o
 * aniversário cai. Espelha o SearchAniversariantesHandler da API: montar o
 * conjunto a partir de datas reais faz a virada do ano deixar de ser um caso
 * especial.
 */
export function diasDoIntervalo(de: string, ate: string): Map<string, string> {
  const teto = addDays(de, MAXIMO_DE_DIAS - 1);
  let fim = ate;

  if (fim < de) {
    fim = de;
  } else if (fim > teto) {
    fim = teto;
  }

  const dias = new Map<string, string>();

  for (let dia = de; dia <= fim; dia = addDays(dia, 1)) {
    dias.set(mesDia(dia), dia);
  }

  const vinteOito = dias.get("02-28");

  if (vinteOito !== undefined && !dias.has("02-29")) {
    dias.set("02-29", vinteOito);
  }

  return dias;
}
