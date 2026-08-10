#!/bin/bash
# Cria os dois bancos com usuários separados.
#
# Esta separação é o invariante nº 1 da arquitetura (ADR-0003, ADR-0006), não uma
# preferência: o Directus expõe uma superfície de dados muito ampla pelo painel e jamais
# pode alcançar a tabela de pessoas. Mesma instância, bancos separados, usuários separados,
# sem permissão cruzada.
#
# Executa UMA vez, no primeiro start com o volume vazio.

set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres <<-EOSQL
    CREATE USER "${CMS_DB_USER}" WITH PASSWORD '${CMS_DB_PASSWORD}';
    CREATE DATABASE cms OWNER "${CMS_DB_USER}";

    CREATE USER "${APP_DB_USER}" WITH PASSWORD '${APP_DB_PASSWORD}';
    CREATE DATABASE cathedrall OWNER "${APP_DB_USER}";

    -- Nenhum dos dois enxerga o banco do outro.
    --
    -- ATENÇÃO à pegadinha: CREATE DATABASE concede CONNECT a PUBLIC automaticamente, e
    -- PUBLIC inclui todo usuário do cluster. Revogar do usuário específico NÃO adianta —
    -- a permissão continua chegando pelo grupo. É preciso revogar de PUBLIC e devolver
    -- só ao dono. Isto foi verificado empiricamente: a primeira versão deste script
    -- revogava do usuário e os dois bancos ficavam mutuamente acessíveis.
    REVOKE CONNECT ON DATABASE cms FROM PUBLIC;
    GRANT CONNECT ON DATABASE cms TO "${CMS_DB_USER}";

    REVOKE CONNECT ON DATABASE cathedrall FROM PUBLIC;
    GRANT CONNECT ON DATABASE cathedrall TO "${APP_DB_USER}";

    -- Sem isto, qualquer usuário do cluster cria tabela no schema public.
    REVOKE CREATE ON SCHEMA public FROM PUBLIC;
EOSQL

echo "Bancos 'cms' e 'cathedrall' criados com usuários separados."
