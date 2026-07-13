#!/bin/bash
# Runs once on first initialisation of the Postgres data volume.
# The POSTGRES_USER/POSTGRES_PASSWORD role is already created by the entrypoint;
# here we just create one database per microservice, all owned by that role.
set -euo pipefail

for db in kocluk_security kocluk_tenant kocluk_notification; do
  echo "Creating database ${db}..."
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    SELECT 'CREATE DATABASE ${db}'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${db}')\gexec
EOSQL
done

echo "Microservice databases are ready."
