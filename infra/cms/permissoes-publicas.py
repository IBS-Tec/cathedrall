#!/usr/bin/env python3
"""
Concede leitura pública às coleções que alimentam o site.

Por que leitura pública em vez de token de build:
o conteúdo do CMS é, por definição, o que já vai estar publicado num site aberto. Um
token traria um segredo a mais para guardar, rotacionar e configurar no CI — proteção
nenhuma sobre dado que é público de qualquer forma. O que precisa ficar protegido é o
PAINEL, e isso é papel do Cloudflare Access (ADR-0010).

Rascunho não vaza: coleções com `status` recebem filtro `status = published` na própria
permissão, não só na consulta do site. Filtro em consulta alguém esquece; filtro em
permissão vale para qualquer chamada.

Idempotente. Uso:
    cd infra/compose && set -a && . ./.env && set +a && python3 ../cms/permissoes-publicas.py
"""

import json
import os
import sys
import urllib.error
import urllib.request

BASE = os.environ.get("DIRECTUS_PUBLIC_URL", "http://localhost:8055")
EMAIL = os.environ["DIRECTUS_ADMIN_EMAIL"]
SENHA = os.environ["DIRECTUS_ADMIN_PASSWORD"]

PUBLICADO = {"status": {"_eq": "published"}}

# coleção -> filtro aplicado na permissão (None = sem filtro)
COLECOES = {
    "configuracao": None,
    "horarios_culto": None,
    "lideranca": None,
    "pequenos_grupos": None,
    "campos_missionarios": None,
    "videos": None,
    "paginas": PUBLICADO,
    "galerias": PUBLICADO,
    "galerias_files": None,
}

# Sem isto as imagens não resolvem. Campos limitados ao que o site usa —
# não há motivo para expor metadados de upload.
CAMPOS_ARQUIVO = [
    "id",
    "title",
    "description",
    "filename_download",
    "type",
    "width",
    "height",
]


def requisitar(metodo, caminho, token=None, corpo=None):
    req = urllib.request.Request(f"{BASE}{caminho}", method=metodo)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    dados = json.dumps(corpo).encode() if corpo is not None else None
    with urllib.request.urlopen(req, dados) as r:
        return json.loads(r.read() or "{}")


def main():
    token = requisitar("POST", "/auth/login", corpo={"email": EMAIL, "password": SENHA})[
        "data"
    ]["access_token"]

    politicas = requisitar("GET", "/policies", token)["data"]
    publica = next((p for p in politicas if not p.get("admin_access") and not p.get("app_access")), None)
    if not publica:
        print("não encontrei a política pública", file=sys.stderr)
        return 1
    print(f"política pública: {publica['id']}")

    existentes = {
        (p.get("collection"), p.get("action"))
        for p in requisitar("GET", "/permissions", token)["data"]
        if p.get("policy") == publica["id"]
    }

    alvos = [(c, f, ["*"]) for c, f in COLECOES.items()]
    alvos.append(("directus_files", None, CAMPOS_ARQUIVO))

    criados = falhos = 0
    for colecao, filtro, campos in alvos:
        if (colecao, "read") in existentes:
            print(f"  = {colecao} (já tem leitura pública)")
            continue
        try:
            requisitar(
                "POST",
                "/permissions",
                token,
                {
                    "policy": publica["id"],
                    "collection": colecao,
                    "action": "read",
                    "fields": campos,
                    "permissions": filtro or {},
                    "validation": {},
                },
            )
            marca = " (só publicados)" if filtro else ""
            print(f"  + {colecao}{marca}")
            criados += 1
        except urllib.error.HTTPError as e:
            print(f"  ! {colecao}: {e.code} {e.read().decode()[:200]}")
            falhos += 1

    print(f"\ncriados={criados} falhos={falhos}")
    return 1 if falhos else 0


if __name__ == "__main__":
    sys.exit(main())
