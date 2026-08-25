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

export function comAnoDe(iso: string, referencia: string): string {
  return `${referencia.slice(0, 4)}-${mesDia(iso)}`;
}

export function noIntervaloAnual(iso: string, de: string, ate: string): boolean {
  const alvo = mesDia(iso);
  const inicio = mesDia(de);
  const fim = mesDia(ate);
  return inicio <= fim
    ? alvo >= inicio && alvo <= fim
    : alvo >= inicio || alvo <= fim;
}
