# Site institucional — mapa de páginas

Levantado a partir do site atual (`www.ibscristo.com.br`, construído no criador de sites
da Hostinger) em 10/08/2026. Este documento define o que o site novo terá e, a partir
disso, o que o Directus precisa modelar.

## O que existe hoje

```
/                  Home — banner, programação de cultos, Discipuluz
/igreja-local      Pastor local (Sidcley Andrade), lema
/denominacao       Quem somos, doutrinas, colegiado ministerial, 4 líderes
/blog-list         Blog — VAZIO
/missoes           IBS Parque do Sol, Pernambuquinho
/contato           Formulário (nome, WhatsApp, mensagem), e-mail, telefone
Eventos            Aponta para um único evento: "Dia das Crianças 2025"
```

Contato conhecido: `secretaria@ibscristo.com.br` · (83) 99141-9595 ·
Instagram, Facebook e YouTube em `@IBSCristoRedentor`.

## Problemas do site atual

Em ordem de gravidade:

1. **Não há endereço em lugar nenhum.** Nem rodapé, nem página de contato. Sem mapa. O
   visitante que decidiu vir não descobre onde é — o site falha no seu objetivo principal.
2. **"Eventos" aponta para um evento de 2025.** Estamos em agosto de 2026. Evento vencido
   em destaque comunica abandono com mais força do que não ter seção de eventos.
3. **Blog no menu, sem nenhum post.** Mesmo efeito. Seção vazia é pior que seção ausente.
4. **Programação de cultos ambígua.** O que está publicado hoje mistura horários e
   números soltos ("Cultos Jovens 17h30 (21+) e 19h15 (22+)") de leitura incerta. Precisa
   ser confirmado com a secretaria antes de republicar — horário errado é o pior erro
   possível aqui.
5. Erro de digitação visível no blog ("Fiche por dentro"). Sintoma de que ninguém revisa.

## Mapa proposto

| Rota | Origem | Fonte do conteúdo |
|---|---|---|
| `/` | existe | CMS + API (próximos eventos) |
| `/sobre` | `/igreja-local` | CMS |
| `/sobre/denominacao` | `/denominacao` | CMS |
| `/discipuluz` | seção da home | CMS (ver ressalva) |
| `/missoes` | existe | CMS |
| `/agenda` | substitui "Eventos" | **CathedrAll**, via `/public/eventos` |
| `/contato` | existe | CMS |

**Cortado do v1: o blog.** Não é preguiça — é que blog sem quem escreva vira exatamente o
que está lá hoje. Se alguém assumir o compromisso de publicar, ele volta; a estrutura no
Directus é barata de acrescentar depois.

**Prioridade de conteúdo na home**, nesta ordem: onde e quando (endereço + horários) →
próximos eventos → quem somos → Discipuluz. Hoje o endereço não existe e os horários vêm
depois do banner.

### Redirecionamentos

As URLs antigas estão indexadas. Ao publicar:

```
/igreja-local  → /sobre
/denominacao   → /sobre/denominacao
/blog-list     → /            (ou 410, se o blog não voltar)
```

## Coleções do Directus

Derivadas do mapa acima, não do que o Directus permite fazer.

| Coleção | Tipo | Campos principais |
|---|---|---|
| `configuracao` | singleton | endereço, coordenadas, telefone, WhatsApp, e-mail, redes sociais |
| `horarios_culto` | lista ordenável | dia da semana, hora, nome, descrição, ativo |
| `paginas` | lista | slug, título, resumo, corpo (rich text), imagem, SEO |
| `lideranca` | lista ordenável | nome, cargo, foto, bio, escopo (local \| colegiado), ordem |
| `grupos_discipuluz` | lista | nome, bairro, líderes, dia/hora, contato |
| `campos_missionarios` | lista | nome, responsáveis, descrição, foto |
| `galerias` | lista | título, data, evento relacionado, fotos (múltiplas) |
| `videos` | lista | título, ID do YouTube, data, destaque |

`configuracao` como singleton é o que conserta o problema nº 1 de forma definitiva:
endereço em **um** lugar, aparecendo no rodapé, na home e no contato.

## Mídia

**Fotos.** Galeria por evento, no Directus. O site **não** serve imagem direto do CMS: o
Astro baixa e otimiza em tempo de build, emitindo assets estáticos. O servidor doméstico
fica fora do caminho crítico do site público — do contrário, uma galeria de culto
compartilhada no WhatsApp num domingo à noite passa a consumir a banda de upload
residencial da casa.

**Vídeo.** Só embed do YouTube. Guarde o ID no CMS, não o arquivo. O limite de ~100 MB por
requisição do plano gratuito da Cloudflare já inviabilizava upload de vídeo
([ADR-0010](adr/0010-cloudflare-tunnel-como-ingress.md)).

**Instagram — fora do v1.** Não é conteúdo de CMS: é integração com API externa, e colide
com o site estático. O token não pode ir para o JavaScript público, então a busca teria de
acontecer em tempo de build, com rebuild agendado e feed algumas horas atrasado. Pior: a
API atual exige conta Business vinculada a página do Facebook e token renovado
periodicamente — quando expira, o feed some do site **em silêncio**. É a funcionalidade
que mais quebra sozinha em site de igreja.

No v1: link destacado para o perfil.

## Três decisões que o levantamento forçou

### 1. Liderança no site é conteúdo editorial, não dado de pessoa

Pastores e líderes aparecem com foto, biografia e família. Isso vai para o **Directus**,
como conteúdo público, e é **duplicado de propósito**. Não é, e nunca será, uma referência
ao cadastro de `Pessoa` do CathedrAll.

Parece desperdício ter o pastor cadastrado em dois lugares. Não é: a alternativa seria o
CMS enxergar o banco de pessoas, o que viola o invariante nº 1 e a LGPD. Bio pública e
cadastro de membro são dados diferentes com finalidades diferentes.

### 2. O Discipuluz é uma dívida conhecida

Os sete grupos (Jardim Planalto, JCU, Bancários, Manaíra, Mangabeira, Bessa, Cristo
Redentor) são células — entidade real de domínio, deliberadamente fora do MVP do
CathedrAll (`docs/dominio.md`).

Por ora vivem no CMS como conteúdo editorial. **Quando o CathedrAll ganhar células, isto
vira segunda fonte de verdade** e precisa migrar, como aconteceu com a agenda. Está
escrito aqui para ninguém se surpreender.

### 3. O formulário de contato conflita com o invariante nº 2

O site atual tem formulário (nome, WhatsApp, mensagem). O site novo é **estático**, sem
SSR — não tem para onde enviar. As opções:

- **Link de WhatsApp + e-mail, sem formulário.** Zero infraestrutura, e a igreja já
  atende por WhatsApp. **Recomendado para o v1.**
- **POST para a API.** Violaria o invariante nº 2 (`/public/*` é somente leitura).
  Exigiria ADR revendo essa fronteira.
- **Serviço de terceiro** (Formspree e similares). Faz um terceiro receber nome e telefone
  de visitantes — dado pessoal. Exigiria análise de LGPD.

Nenhuma escolha é neutra. A primeira é a única que não abre frente nova.

## Pendente

- Endereço completo e coordenadas — **bloqueia o v1**
- Programação de cultos confirmada pela secretaria — **bloqueia o v1**
- Identidade visual: logo em vetor, cores, tipografia
- Fotos em resolução decente (as atuais vêm do construtor da Hostinger)
