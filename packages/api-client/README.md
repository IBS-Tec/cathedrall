# packages/api-client

Cliente TypeScript da API do CathedrAll, **gerado** a partir do documento OpenAPI.

> **Status:** vazio. Depende da API existir.

## Regra

Este pacote é **gerado, não escrito**. Nada em `src/generated/` deve ser editado à mão —
a próxima geração sobrescreve. Se algo está errado no cliente, o erro está na API.

Wrappers e helpers escritos à mão, se necessários, ficam fora de `generated/`.

## Por quê

O OpenAPI é o contrato real entre a API e a SPA. Gerar o cliente elimina a categoria de
bug mais comum e mais chata entre back e front: campo com nome errado, tipo divergente,
enum desatualizado. O compilador passa a pegar isso.

Ver [ADR-0005](../../docs/adr/0005-frontend-react-spa-com-trilhos.md).
