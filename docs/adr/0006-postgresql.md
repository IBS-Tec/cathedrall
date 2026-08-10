# ADR-0006 — PostgreSQL como banco único

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O ecossistema .NET tende a SQL Server por inércia. O sistema roda inicialmente em um
servidor doméstico e depois deve migrar para VPS, com orçamento de igreja.

## Decisão

PostgreSQL, uma instância, com **databases separados**: `cathedrall` e `cms`.
Acesso via EF Core, com migrations versionadas no repositório.

## Motivos

- Licença livre e roda em qualquer lugar — do PC de casa à VPS mais barata.
- `jsonb` para campos customizados de cadastro, que a igreja inevitavelmente vai querer
  ("tamanho de camiseta", "instrumento que toca", "curso de batismo").
- É o banco de primeira classe do Directus.
- Migrar de SQL Server depois seria caro; começar em Postgres não custa nada.

## Consequências

- Databases separados na mesma instância: isolamento lógico com custo operacional de uma
  única instância. Backup precisa cobrir **os dois**.
- Sem recursos exclusivos de SQL Server. Nenhum é necessário.
- Backup é `pg_dump` para armazenamento externo, criptografado, com alerta de falha
  (dead-man switch). Backup nunca restaurado não é backup.
