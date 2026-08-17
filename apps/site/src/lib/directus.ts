const CMS_URL = import.meta.env.DIRECTUS_URL ?? "http://localhost:8055";

/**
 * Busca itens do Directus em tempo de build.
 *
 * Falha ALTO de propósito: se o CMS estiver fora do ar ou sem permissão, o build quebra
 * em vez de publicar um site com as seções vazias. Site no ar sem endereço e sem
 * horário é pior que build vermelho — o build vermelho alguém conserta; o site vazio
 * fica semanas assim sem ninguém notar.
 *
 * Coleção vazia, por outro lado, é resultado legítimo e devolve lista vazia.
 */
async function fetchItems<T>(collection: string, params = ""): Promise<T> {
  const url = `${CMS_URL}/items/${collection}${params}`;
  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(
      `Directus respondeu ${response.status} em ${collection}. ` +
        `Confira se o CMS está no ar e se a coleção tem leitura pública.`,
    );
  }

  return (await response.json()).data as T;
}

export type Configuracao = {
  nome_igreja?: string;
  endereco_logradouro?: string;
  endereco_bairro?: string;
  endereco_cidade?: string;
  endereco_uf?: string;
  latitude?: number;
  longitude?: number;
  google_maps_url?: string;
  telefone?: string;
  whatsapp?: string;
  email?: string;
  instagram?: string;
  facebook?: string;
  youtube?: string;
};

export type HorarioCulto = {
  id: number;
  nome: string;
  dia_semana?: string;
  hora?: string;
  observacao?: string;
  descricao?: string;
  publico_alvo?: string;
};

export const cms = {
  configuracao: () => fetchItems<Configuracao>("configuracao"),

  horarios: () =>
    fetchItems<HorarioCulto[]>(
      "horarios_culto",
      "?filter[ativo][_eq]=true&sort=ordem",
    ),
};

/** "19:40:00" -> "19h40" · "09:00:00" -> "9h" */
export function formatTime(time?: string): string {
  if (!time) return "";
  const [h, m] = time.split(":");
  const hh = Number(h);
  return m === "00" ? `${hh}h` : `${hh}h${m}`;
}

export function fullAddress(c: Configuracao): string {
  const parts = [
    c.endereco_logradouro,
    c.endereco_bairro,
    [c.endereco_cidade, c.endereco_uf].filter(Boolean).join(" - "),
  ].filter(Boolean);
  return parts.join(", ");
}
