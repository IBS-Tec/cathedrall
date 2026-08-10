#!/usr/bin/env python3
"""
Cria os campos das coleções do CMS via API do Directus.

Idempotente: campo que já existe é pulado. Pode rodar várias vezes.

Este script é andaime, não a fonte de verdade. Depois de rodar, exporte o schema
(`directus schema snapshot`) para infra/cms/schema.yaml — é o snapshot que versiona o
modelo de conteúdo, conforme ADR-0003.

Uso:
    cd infra/compose && set -a && . ./.env && set +a && python3 ../cms/criar-campos.py
"""

import json
import os
import sys
import urllib.error
import urllib.request

BASE = os.environ.get("DIRECTUS_PUBLIC_URL", "http://localhost:8055")
EMAIL = os.environ["DIRECTUS_ADMIN_EMAIL"]
SENHA = os.environ["DIRECTUS_ADMIN_PASSWORD"]

DIAS = ["domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado"]


# ─── helpers de definição de campo ────────────────────────────────────────────


def texto(nome, rotulo, obrigatorio=False, nota=None):
    return {
        "field": nome,
        "type": "string",
        "meta": {
            "interface": "input",
            "note": nota,
            "required": obrigatorio,
            "options": {"placeholder": rotulo},
        },
        "schema": {"is_nullable": not obrigatorio},
    }


def paragrafo(nome, nota=None):
    return {
        "field": nome,
        "type": "text",
        "meta": {"interface": "input-multiline", "note": nota},
        "schema": {"is_nullable": True},
    }


def rico(nome, nota=None):
    return {
        "field": nome,
        "type": "text",
        "meta": {"interface": "input-rich-text-html", "note": nota},
        "schema": {"is_nullable": True},
    }


def booleano(nome, padrao=True, nota=None):
    return {
        "field": nome,
        "type": "boolean",
        "meta": {"interface": "boolean", "note": nota},
        "schema": {"default_value": padrao, "is_nullable": False},
    }


def inteiro(nome, nota=None):
    return {
        "field": nome,
        "type": "integer",
        "meta": {"interface": "input", "note": nota, "hidden": nome == "ordem"},
        "schema": {"is_nullable": True},
    }


def decimal(nome, nota=None):
    return {
        "field": nome,
        "type": "float",
        "meta": {"interface": "input", "note": nota},
        "schema": {"is_nullable": True},
    }


def data(nome, nota=None):
    return {
        "field": nome,
        "type": "date",
        "meta": {"interface": "datetime", "note": nota},
        "schema": {"is_nullable": True},
    }


def hora(nome, nota=None):
    return {
        "field": nome,
        "type": "time",
        "meta": {"interface": "datetime", "note": nota},
        "schema": {"is_nullable": True},
    }


def escolha(nome, opcoes, obrigatorio=False, nota=None):
    return {
        "field": nome,
        "type": "string",
        "meta": {
            "interface": "select-dropdown",
            "note": nota,
            "required": obrigatorio,
            "options": {"choices": [{"text": o, "value": o} for o in opcoes]},
        },
        "schema": {"is_nullable": not obrigatorio},
    }


def imagem(nome, nota=None):
    return {
        "field": nome,
        "type": "uuid",
        "meta": {"interface": "file-image", "special": ["file"], "note": nota},
        "schema": {"is_nullable": True, "foreign_key_table": "directus_files"},
    }


def situacao():
    return {
        "field": "status",
        "type": "string",
        "meta": {
            "interface": "select-dropdown",
            "width": "half",
            "options": {
                "choices": [
                    {"text": "Publicado", "value": "published"},
                    {"text": "Rascunho", "value": "draft"},
                ]
            },
        },
        "schema": {"default_value": "draft", "is_nullable": False},
    }


# ─── o modelo ─────────────────────────────────────────────────────────────────

MODELO = {
    # Singleton. Existe para resolver o defeito nº 1 do site atual: o endereço da igreja
    # não aparece em lugar nenhum. Aqui ele vive num lugar só e é usado no rodapé, na
    # home e na página de contato.
    "configuracao": [
        texto("nome_igreja", "Igreja Bíblica Semear — Cristo Redentor", True),
        texto("endereco_logradouro", "Rua, número e complemento", True),
        texto("endereco_bairro", "Bairro", True),
        texto("endereco_cidade", "Cidade", True),
        texto("endereco_uf", "UF", True),
        texto("endereco_cep", "CEP"),
        decimal("latitude", "Para o mapa e para dados estruturados de SEO."),
        decimal("longitude", "Para o mapa e para dados estruturados de SEO."),
        texto("google_maps_url", "Link do Google Maps"),
        texto("telefone", "(83) 0000-0000"),
        texto("whatsapp", "Somente números com DDD, ex: 5583991419595"),
        texto("email", "secretaria@ibscristo.com.br"),
        texto("instagram", "URL completa do perfil"),
        texto("facebook", "URL completa da página"),
        texto("youtube", "URL completa do canal"),
    ],
    "horarios_culto": [
        texto("nome", "Culto de Celebração", True),
        escolha("dia_semana", DIAS, True),
        hora("hora"),
        # O site atual anuncia "1º e 3º sábados". Recorrência assim não cabe em
        # dia_semana + hora, e inventar um modelo de recorrência aqui seria caro para o
        # que a página precisa: exibir texto. A solução entediante resolve.
        texto("observacao", "Ex.: 1º e 3º sábados do mês"),
        paragrafo("descricao"),
        texto("publico_alvo", "Ex.: jovens, crianças, todos"),
        booleano("ativo", True),
        inteiro("ordem", "Ordem de exibição."),
    ],
    "paginas": [
        texto("slug", "sobre, sobre/denominacao, missoes", True, "Sem barra no início."),
        texto("titulo", "Título da página", True),
        paragrafo("resumo"),
        rico("corpo"),
        imagem("imagem_capa"),
        texto("seo_titulo", "Sobrescreve o título nos buscadores"),
        paragrafo("seo_descricao", "Até ~155 caracteres."),
        situacao(),
    ],
    "lideranca": [
        texto("nome", "Nome completo", True),
        texto("cargo", "Ex.: Pastor local"),
        imagem("foto"),
        rico("bio"),
        escolha(
            "escopo",
            ["local", "colegiado"],
            True,
            "local = liderança da Cristo Redentor; colegiado = denominação.",
        ),
        booleano("ativo", True),
        inteiro("ordem"),
    ],
    "pequenos_grupos": [
        texto("nome", "Ex.: Bancários", True),
        texto("bairro", "Bairro"),
        # Texto livre, NÃO referência ao cadastro de Pessoa do CathedrAll.
        # O CMS jamais enxerga dado de pessoa (invariante nº 1).
        texto("lideres", "Nomes dos líderes"),
        escolha("dia_semana", DIAS),
        hora("hora"),
        texto("contato_whatsapp", "Somente números com DDD"),
        # Ponto de referência, nunca endereço completo: pequenos grupos se reúnem em
        # casas de membros, e publicar o endereço residencial de alguém num site aberto
        # é expor dado pessoal sem necessidade.
        texto("referencia", "Ponto de referência — NUNCA o endereço completo"),
        booleano("ativo", True),
    ],
    "campos_missionarios": [
        texto("nome", "Ex.: IBS Parque do Sol", True),
        texto("local", "Cidade / região"),
        texto("responsaveis", "Ex.: Pr. Onésimo Fernandes e Raquel"),
        rico("descricao"),
        imagem("foto"),
        booleano("ativo", True),
        inteiro("ordem"),
    ],
    "galerias": [
        texto("titulo", "Ex.: Dia das Crianças 2026", True),
        data("data"),
        paragrafo("descricao"),
        imagem("capa"),
        situacao(),
    ],
    "videos": [
        texto("titulo", "Título do vídeo", True),
        # Só o ID. Upload de vídeo é inviável pelo limite de ~100 MB da Cloudflare
        # (ADR-0010), e vídeo hospedado em casa consumiria a banda residencial.
        texto("youtube_id", "Ex.: dQw4w9WgXcQ", True, "Apenas o ID, não a URL inteira."),
        data("data"),
        paragrafo("descricao"),
        booleano("destaque", False),
    ],
}

# Campo por onde cada coleção é ordenada no painel.
ORDENACAO = {
    "horarios_culto": "ordem",
    "lideranca": "ordem",
    "campos_missionarios": "ordem",
}

# Campos de arquivo único precisam da relação criada EXPLICITAMENTE.
#
# Verificado na prática: informar schema.foreign_key_table ao criar o campo NÃO cria a
# relação. O campo aparece no painel, a coluna existe no banco, e nada reclama — mas o
# arquivo não resolve e a expansão `?fields=foto.*` volta vazia. Falha silenciosa.
RELACOES_ARQUIVO = [
    ("paginas", "imagem_capa"),
    ("lideranca", "foto"),
    ("campos_missionarios", "foto"),
    ("galerias", "capa"),
]

# Múltiplos arquivos exigem coleção de junção — o Directus não cria sozinho via API.
# (coleção, campo, nome da junção)
GALERIA_FOTOS = ("galerias", "fotos", "galerias_files")


# ─── execução ─────────────────────────────────────────────────────────────────


def requisitar(metodo, caminho, token=None, corpo=None):
    req = urllib.request.Request(f"{BASE}{caminho}", method=metodo)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    dados = json.dumps(corpo).encode() if corpo is not None else None
    with urllib.request.urlopen(req, dados) as r:
        return json.loads(r.read() or "{}")


def main():
    token = requisitar(
        "POST", "/auth/login", corpo={"email": EMAIL, "password": SENHA}
    )["data"]["access_token"]

    existentes = {
        (f["collection"], f["field"])
        for f in requisitar("GET", "/fields", token)["data"]
    }

    criados = pulados = falhos = 0
    for colecao, campos in MODELO.items():
        for campo in campos:
            if (colecao, campo["field"]) in existentes:
                print(f"  = {colecao}.{campo['field']} (já existe)")
                pulados += 1
                continue
            try:
                requisitar("POST", f"/fields/{colecao}", token, campo)
                print(f"  + {colecao}.{campo['field']}")
                criados += 1
            except urllib.error.HTTPError as e:
                detalhe = e.read().decode()[:300]
                print(f"  ! {colecao}.{campo['field']}: {e.code} {detalhe}")
                falhos += 1

    # Relações de arquivo único
    relacoes = {
        (r["collection"], r["field"])
        for r in requisitar("GET", "/relations", token)["data"]
    }
    for colecao, campo in RELACOES_ARQUIVO:
        if (colecao, campo) in relacoes:
            print(f"  = relação {colecao}.{campo} (já existe)")
            continue
        try:
            requisitar(
                "POST",
                "/relations",
                token,
                {
                    "collection": colecao,
                    "field": campo,
                    "related_collection": "directus_files",
                },
            )
            print(f"  + relação {colecao}.{campo} -> directus_files")
        except urllib.error.HTTPError as e:
            print(f"  ! relação {colecao}.{campo}: {e.code} {e.read().decode()[:200]}")
            falhos += 1

    # Galeria com múltiplas fotos (many-to-many com directus_files)
    colecao, campo, juncao = GALERIA_FOTOS
    if (colecao, campo) not in existentes:
        try:
            requisitar(
                "POST",
                f"/fields/{colecao}",
                token,
                {
                    "field": campo,
                    "type": "alias",
                    "meta": {"interface": "files", "special": ["files"]},
                },
            )
            requisitar(
                "POST",
                "/collections",
                token,
                {
                    "collection": juncao,
                    "meta": {"hidden": True, "icon": "import_export"},
                    "schema": {"name": juncao},
                    "fields": [
                        {
                            "field": "id",
                            "type": "integer",
                            "meta": {"hidden": True},
                            "schema": {
                                "is_primary_key": True,
                                "has_auto_increment": True,
                            },
                        },
                        {"field": f"{colecao}_id", "type": "integer", "schema": {}},
                        {"field": "directus_files_id", "type": "uuid", "schema": {}},
                        {"field": "sort", "type": "integer", "schema": {}},
                    ],
                },
            )
            requisitar(
                "POST",
                "/relations",
                token,
                {
                    "collection": juncao,
                    "field": f"{colecao}_id",
                    "related_collection": colecao,
                    "meta": {"one_field": campo, "sort_field": "sort"},
                    "schema": {"on_delete": "CASCADE"},
                },
            )
            requisitar(
                "POST",
                "/relations",
                token,
                {
                    "collection": juncao,
                    "field": "directus_files_id",
                    "related_collection": "directus_files",
                    "schema": {"on_delete": "CASCADE"},
                },
            )
            print(f"  + {colecao}.{campo} (múltiplas fotos, junção {juncao})")
        except urllib.error.HTTPError as e:
            print(f"  ! {colecao}.{campo}: {e.code} {e.read().decode()[:300]}")
            falhos += 1
    else:
        print(f"  = {colecao}.{campo} (já existe)")

    for colecao, campo in ORDENACAO.items():
        try:
            requisitar("PATCH", f"/collections/{colecao}", token, {"meta": {"sort_field": campo}})
            print(f"  ~ {colecao}: ordenada por '{campo}'")
        except urllib.error.HTTPError as e:
            print(f"  ! ordenação de {colecao}: {e.code}")

    print(f"\ncriados={criados} pulados={pulados} falhos={falhos}")
    return 1 if falhos else 0


if __name__ == "__main__":
    sys.exit(main())
