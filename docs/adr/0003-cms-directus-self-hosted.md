# ADR-0003 — Directus self-hosted como CMS

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O conteúdo do site deve ser gerido por uma pessoa não técnica, sem envolver
desenvolvedor. Há preferência explícita por solução self-hosted, com o MVP rodando em um
servidor doméstico. Candidatos avaliados: Directus, Payload 3, Strapi, Decap.

## Decisão

**Directus**, em container próprio, com **database Postgres separado** (`cms`) no mesmo
cluster do CathedrAll.

## Motivos

- É **uma imagem Docker oficial pronta**. Modelagem de conteúdo pela UI, atualização é
  trocar a tag da imagem. Não há build.
- Payload 3 exige um app Next.js no monorepo: mais um runtime Node, mais um build no CI,
  e mudança de modelo de conteúdo vira deploy. Com um único mantenedor e servidor
  doméstico, cada peça que exige build é uma peça que pode travar num domingo à noite.
- Decap (git-based) foi descartado: exige conta GitHub e a experiência de edição para
  leigo é fraca.

## Consequências

- **O modelo de conteúdo vive no banco, não no git.** Mitigação: `directus schema
  snapshot` versionado em `infra/cms/`. Requer disciplina — se ninguém rodar o snapshot,
  o modelo fica sem histórico.
- Database separado é **obrigatório**, não preferência: o painel do Directus expõe uma
  superfície de dados muito ampla e jamais pode alcançar a tabela de pessoas. Também é
  higiene de LGPD.
- Mais um serviço para manter atualizado e com backup.
- O painel do CMS não pode ficar exposto apenas com usuário e senha; precisa de camada
  de acesso na frente.

## Reavaliado em 10/08/2026 — mantido

A opção de CMS baseado em git (Keystatic, Sveltia) foi reaberta antes de qualquer
conteúdo existir, quando o custo de trocar ainda era quase zero. O argumento a favor era
forte e vale registrar: elimina um serviço com estado, coloca o modelo de conteúdo no git,
e faz desaparecer a sincronização de schema, o backup de um segundo banco e a cópia para
homologação — a categoria inteira de problema.

**Mantido o Directus por dois fatos novos:**

1. **Haverá galeria de fotos por evento.** É o pior caso para git: binário, volumoso, e
   cada upload vira commit e rebuild completo. O Directus traz gestão de arquivos e
   transformação de imagem sob demanda, que num site de fotos é a diferença entre 200 KB e
   4 MB por imagem no celular do visitante.
2. A objeção original — UX de edição para leigo — **envelheceu**. Keystatic e Sveltia são
   bem melhores que o Decap. Ela não teria sido suficiente sozinha para manter a decisão.

O acesso da pessoa de conteúdo ao GitHub, que seria o outro bloqueio, não é problema:
ela pode ter conta. Portanto, se a galeria sair de cena, esta decisão deve ser reaberta.

**Consequência de arquitetura que a galeria impõe:** o site **não** serve imagem direto do
Directus. O Astro baixa e otimiza em tempo de build, emitindo assets estáticos junto do
site. O Directus é origem de conteúdo, não CDN. Sem isso, cada visitante puxaria foto pela
banda de upload residencial do servidor doméstico — e uma galeria de culto compartilhada
no WhatsApp num domingo à noite derruba a máquina.
